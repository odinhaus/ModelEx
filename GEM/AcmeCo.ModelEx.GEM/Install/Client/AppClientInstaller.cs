using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Altus.Core;
using Altus.Core.Compilation;
using Altus.Core.Data;
using Altus.Core.Licensing;

namespace Altus.GEM.Install
{
    public partial class Installer
    {
        protected virtual bool OnAppClientInstall(ILicense license, DeclaredApp app, Altus.GEM.Schema.ModelEx config)
        {
            if (Context.CurrentContext.CurrentApp.Manifest.LastUpdated == DateTime.MinValue
                || Context.CurrentContext.CurrentApp.WasUpdated
                || Context.CurrentContext.CurrentApp.PrimaryAssembly == null
                || !LicensedAppInstaller.Instance.GetAppConfig(app.Name).IsValid)
            {
                EmbeddedCodeReader rdr = new EmbeddedCodeReader();

                OnCompileSources(license, app, config, rdr);
                OnBuildDbSchema(license, app, config, rdr);

                Context.CurrentContext.CurrentApp.Manifest.LastUpdated = DateTime.Now;
                Context.CurrentContext.CurrentApp.Manifest.Save();
            }
            return true;
        }

        private void OnCompileSources(ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            MSBuildCompiler.MSBuildPackage project = new MSBuildCompiler.MSBuildPackage(GetRunningVersion(), app.Name, "Altus.GEM." + app.Name);
           
            project.Sources.AddRange(OnCreatePlatformSource(license, app, config, rdr));
            project.Sources.AddRange(OnCreateReferenceSources(license, app, config, rdr));
            project.Sources.AddRange(OnCreateCompileSources(license, app, config, rdr));
            project.Sources.AddRange(OnCreatePageSources(license, app, config, rdr));
            project.Sources.AddRange(OnCreateResourceSources(license, app, config, rdr));

            Assembly asm = MSBuildCompiler.Compile(project);
            if (app.PrimaryFile == null)
            {
                app.Manifest.Targets[0].Files.Add(
                    new Core.Component.DiscoveryFileElement()
                    {
                        LoadedAssembly = asm,
                        CodeBase = new Uri(asm.CodeBase).LocalPath,
                        IsPrimary = true,
                        Reflect = true,
                        Name = project.ProjectName + ".dll"
                    });
            }
            else
            {
                app.PrimaryFile.LoadedAssembly = asm;
                app.PrimaryFile.CodeBase = new Uri(asm.CodeBase).LocalPath;
            }
   
            Altus.Core.Component.App.Instance.Shell.ComponentLoader.Add(asm);
        }

        private string GetRunningVersion()
        {
            if (Environment.Version.Build == 0)
                return string.Format("{0}.{1}", Environment.Version.Major, Environment.Version.Minor);
            else
            {
                if (Environment.Version.Major == 4)
                {
                    if (Environment.Version.Build < 17626)
                        return "4.0";
                    else return (Environment.OSVersion.Platform == PlatformID.Win32NT
                        && Environment.OSVersion.Version.Major >= 6 ? "4.5" : "4.0");
                }
                else return string.Format("{0}.{1}", Environment.Version.Major, Environment.Version.Minor);
            }
        }

        private MSBuildCompiler.MSBuildSource[] OnCreatePlatformSource(ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            MSBuildCompiler.MSBuildSource assemblyInfo = new MSBuildCompiler.MSBuildSource()
            {
                SourceType = MSBuildCompiler.MSBuildSourceType.Compile,
                Include = "AssemblyInfo.cs",
                Data = OnGetResource(rdr, @"Install\Client\AssemblyInfo.cs",
                    new KeyValuePair<string, string>("Guid", Guid.NewGuid().ToString()),
                    new KeyValuePair<string, string>("App", app.Name))
            };
            return new MSBuildCompiler.MSBuildSource[]{ assemblyInfo };
        }

        private MSBuildCompiler.MSBuildSource[] OnCreateReferenceSources(ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            MSBuildCompiler.MSBuildSource core = new MSBuildCompiler.MSBuildSource()
                {
                    SourceType = MSBuildCompiler.MSBuildSourceType.Reference,
                    Include = new Uri(typeof(Altus.Core.Context).Assembly.CodeBase).LocalPath,
                    SpecificVersion = false
                };
            MSBuildCompiler.MSBuildSource gem = new MSBuildCompiler.MSBuildSource()
            {
                SourceType = MSBuildCompiler.MSBuildSourceType.Reference,
                Include = new Uri(typeof(Altus.GEM.Install.Installer).Assembly.CodeBase).LocalPath,
                SpecificVersion = false
            };
            return new MSBuildCompiler.MSBuildSource[] { core, gem };
        }

        /*
        <Compile Include="ViewModels\ExplorerFolder.cs" />
        <Compile Include="Views\Explorer_body.xaml.cs">
            <DependentUpon>Explorer_body.xaml</DependentUpon>
        </Compile>
        <Compile Include="Views\Explorer_main.xaml.cs">
            <DependentUpon>Explorer_main.xaml</DependentUpon>
        </Compile>
        <Compile Include="Views\Explorer_tool.xaml.cs">
            <DependentUpon>Explorer_tool.xaml</DependentUpon>
        </Compile>
        </ItemGroup>
        <ItemGroup>
        <None Include="GEM.manifest" />
        <None Include="License\GEM.lic" />
        <None Include="Schema\GEM_Config.xsd">
            <SubType>Designer</SubType>
        </None>
        </ItemGroup>
        <ItemGroup>
        <Resource Include="Styles\Default_Styles.xaml">
            <Generator>MSBuild:Compile</Generator>
            <SubType>Designer</SubType>
        </Resource>
        <Page Include="Views\Explorer_main.xaml">
            <SubType>Designer</SubType>
            <Generator>MSBuild:Compile</Generator>
        </Page>
         */

        private MSBuildCompiler.MSBuildSource[] OnCreateCompileSources(ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            List<MSBuildCompiler.MSBuildSource> sources = new List<MSBuildCompiler.MSBuildSource>();
            foreach(Schema.Entity e in config.EntityModels )
            {
                sources.Add(OnCreateEntitySource(e, license, app, config, rdr));
                sources.AddRange(OnCreateEntityViews(e, license, app, config, rdr));
            }
            return sources.ToArray();
        }



        private IEnumerable<MSBuildCompiler.MSBuildSource> OnCreateEntityViews(Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            List<MSBuildCompiler.MSBuildSource> sources = new List<MSBuildCompiler.MSBuildSource>();
            if (e.CustomViews != null)
            {
                foreach (var view in e.CustomViews)
                {
                    if (view is Schema.WpfView)
                    {
                        AddIf(sources, OnCreateWpfViewCode((Schema.WpfView)view, e, license, app, config, rdr));
                        AddIf(sources, OnCreateWpfViewXaml((Schema.WpfView)view, e, license, app, config, rdr));
                        AddIf(sources, OnCreateWpfViewStyleXaml((Schema.WpfView)view, e, license, app, config, rdr));
                        AddIf(sources, OnCreateWpfViewResources((Schema.WpfView)view, e, license, app, config, rdr));
                        AddIf(sources, OnCreateWpfViewReferences((Schema.WpfView)view, e, license, app, config, rdr));
                    }
                }
            }
            return sources;
        }

        private IEnumerable<MSBuildCompiler.MSBuildSource> OnCreateWpfViewReferences(Schema.WpfView view, Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            List<MSBuildCompiler.MSBuildSource> sources = new List<MSBuildCompiler.MSBuildSource>();
            if (view.References != null)
            {
                foreach (Schema.WpfViewReference reference in view.References)
                {
                    AddIf(sources, OnCreateWpfViewReference(reference, view, e, license, app, config, rdr));
                }
            }
            return sources;
        }
        private MSBuildCompiler.MSBuildSource OnCreateWpfViewReference(Schema.WpfViewReference reference, Schema.WpfView view, Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            MSBuildCompiler.MSBuildSource source = null;
            if (reference != null)
            {
                source = new MSBuildCompiler.MSBuildSource()
                    {
                        SourceType = MSBuildCompiler.MSBuildSourceType.Reference,
                        Include = new Uri(reference.codeBase).LocalPath
                    };
            }
            return source;
        }

        private IEnumerable<MSBuildCompiler.MSBuildSource> OnCreateWpfViewResources(Schema.WpfView view, Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            List<MSBuildCompiler.MSBuildSource> sources = new List<MSBuildCompiler.MSBuildSource>();
            if (view.Resources != null)
            {
                foreach (Schema.Resource resource in view.Resources)
                {
                    AddIf(sources, OnCreateWpfViewResource(resource, view, e, license, app, config, rdr));
                }
            }
            return sources;
        }

        private MSBuildCompiler.MSBuildSource OnCreateWpfViewResource(Schema.Resource resource, Schema.WpfView view, Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            MSBuildCompiler.MSBuildSource source = null;
            if (resource != null)
            {
                source = new MSBuildCompiler.MSBuildSource()
                {
                    Data = OnGetWpfViewResourceData(resource, view, e, license, app, rdr),
                    Include = resource.destinationPath,
                    SourceType = MSBuildCompiler.MSBuildSourceType.Resource,
                    Generator = "MSBuild:Compile",
                    SubType = "Designer"
                };
            }
            return source;
        }

        private byte[] OnGetWpfViewResourceData(Schema.Resource resource, Schema.WpfView view, Schema.Entity e, ILicense license, DeclaredApp app, EmbeddedCodeReader rdr)
        {
            if (resource is Schema.BinaryResource)
            {
                return ((Schema.BinaryResource)resource).Data;
            }
            else
            {
                return File.ReadAllBytes(((Schema.FileResource)resource).sourcePath);
            }
        }

        private MSBuildCompiler.MSBuildSource OnCreateWpfViewStyleXaml(Schema.WpfView view, Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            MSBuildCompiler.MSBuildSource source = null;
            if (!string.IsNullOrEmpty(view.StyleXAML))
            {
                source = new MSBuildCompiler.MSBuildSource()
                {
                    Data = UTF8Encoding.UTF8.GetBytes(view.StyleXAML),
                    Include = OnGetWpfViewStyleFileName(view, e),
                    SourceType = MSBuildCompiler.MSBuildSourceType.Resource,
                    Generator = "MSBuild:Compile",
                    SubType = "Designer"
                };
            }
            return source;
        }

        private MSBuildCompiler.MSBuildSource OnCreateWpfViewXaml(Schema.WpfView view, Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            MSBuildCompiler.MSBuildSource source = null;
            if (!string.IsNullOrEmpty(view.ViewXAML))
            {
                source = new MSBuildCompiler.MSBuildSource()
                {
                    Data = UTF8Encoding.UTF8.GetBytes(view.ViewXAML),
                    Include = OnGetWpfViewFileName(view, e),
                    SourceType = view.wpfViewType == Schema.WpfViewWpfViewType.UserControl ?
                        MSBuildCompiler.MSBuildSourceType.Page
                        : MSBuildCompiler.MSBuildSourceType.Resource,
                    Generator = "MSBuild:Compile",
                    SubType = "Designer"
                };
            }
            return source;
        }

        private MSBuildCompiler.MSBuildSource OnCreateWpfViewCode(Schema.WpfView view, Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            MSBuildCompiler.MSBuildSource source = null;
            if (view.Code != null)
            {
                string xamlFile = OnGetWpfViewFileName(view, e);
                source = OnCreateCodeSource(view.Code, xamlFile + ".cs", license, app, config, rdr);
                source.DependentUpon = xamlFile;
            }
            return source;
        }

        private string OnGetWpfViewFileName(Schema.WpfView view, Schema.Entity e)
        {
            if (view.wpfViewType == Schema.WpfViewWpfViewType.DataTemplate)
                return string.Format(@"Views\DataTemplates\{2}\{0}_{1}.xaml", e.name, view.type, view.windowName);
            else
                return string.Format(@"Views\Controls\{0}_{1}.xaml", e.name, view.type);
        }

        private string OnGetWpfViewStyleFileName(Schema.WpfView view, Schema.Entity e)
        {
            return string.Format(@"Styles\{0}_{1}.xaml", e.name, view.type);
        }

        private MSBuildCompiler.MSBuildSource OnCreateCodeSource(string code, string path, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            MSBuildCompiler.MSBuildSource source = null;
            if (!string.IsNullOrEmpty(code))
            {
                source = new MSBuildCompiler.MSBuildSource()
                {
                    Data = UTF8Encoding.UTF8.GetBytes(code),
                    Include = path,
                    SourceType = MSBuildCompiler.MSBuildSourceType.Compile
                };
            }
            return source;
        }

        private void AddIf(List<MSBuildCompiler.MSBuildSource> list, MSBuildCompiler.MSBuildSource source)
        {
            if (source != null) list.Add(source);
        }

        private void AddIf(List<MSBuildCompiler.MSBuildSource> list, IEnumerable<MSBuildCompiler.MSBuildSource> sources)
        {
            if (sources != null && sources.Count() > 0) list.AddRange(sources);
        }

        private MSBuildCompiler.MSBuildSource OnCreateEntitySource(Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            MSBuildCompiler.MSBuildSource entity = new MSBuildCompiler.MSBuildSource()
            {
                SourceType = MSBuildCompiler.MSBuildSourceType.Compile,
                Include = string.Format("Entities\\{0}.cs", e.name),
                Data = OnGetResource(rdr, OnGetEntityTemplate(e),
                    new KeyValuePair<string, string>("Name", e.name),
                    new KeyValuePair<string, string>("Reader", OnCreateEntityReader(e, license, app, config, rdr)),
                    new KeyValuePair<string, string>("Props", OnCreateEntityProperties(e, license, app, config, rdr)),
                    new KeyValuePair<string, string>("Code", OnCreateEntityCode(e, license, app, config, rdr)),
                    new KeyValuePair<string, string>("XamlArgs", OnCreateEntityXamlArgs(e, license, app, config, rdr)),
                    new KeyValuePair<string, string>("Xaml", OnCreateEntityXamlString(e, license, app, config, rdr)))
            };
            return entity;
        }

        private string OnCreateEntityXamlArgs(Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            if (e is Schema.ReportFileEntity
                && config.ReportModels != null)
            {
                string id = ((Schema.ReportFileEntity)e).id;
                Schema.ReportModel report = config.ReportModels
                    .Where(rm => rm.Entities != null
                        && rm.Entities.Where(ee => ee.id.Equals(id)).Count() > 0).FirstOrDefault();
                if (report != null
                    && report.ParameterMappings != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach(Schema.ParameterMapping pm in report.ParameterMappings)
                    {
                        sb.AppendLine(string.Format("\t\t\targs.Add(\"{0}\", this.{1});",
                            pm.parameter,
                            pm.entityProperty));
                    }
                    return sb.ToString();
                }
            }

            return string.Empty;
        }

        private string OnCreateEntityXamlString(Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            if (e is Schema.ReportFileEntity
                && config.ReportModels != null)
            {
                string id = ((Schema.ReportFileEntity)e).id;
                Schema.ReportModel report = config.ReportModels
                    .Where(rm =>rm.Entities != null 
                        &&  rm.Entities.Where(ee => ee.id.Equals(id)).Count() > 0).FirstOrDefault();
                if (report != null
                    && report.CalculationModels != null)
                {
                    Schema.CalculationModel calc = config.CalculationModels
                        .Where(cm=>cm.id.Equals(report.CalculationModels.First().modelId)).FirstOrDefault();
                    if (calc is Schema.WorkflowCalculationModel)
                    {
                        return ((Schema.WorkflowCalculationModel)calc).Xaml.Replace("\"","\"\"");
                    }
                }
            }

            return string.Empty;
        }

        private string OnGetEntityTemplate(Schema.Entity e)
        {
            if (e is Schema.DataFileEntity)
                return @"Install\Client\DataFileEntity.cs";
            else if (e is Schema.CalculationFileEntity)
                return @"Install\Client\CalculationFileEntity.cs";
            else if (e is Schema.ReportFileEntity)
                return @"Install\Client\ReportFileEntity.cs";
            else
                return @"Install\Client\Entity.cs";
        }

        private string OnCreateEntityCode(Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            return e.Code ?? "";
        }

        private string OnCreateEntityProperties(Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            StringBuilder sb = new StringBuilder();
            string propTemplate = "\t\tprivate {0} _{1};\r\n\t\tpublic {0} {1} {{ get {{ return _{1}; }} set {{ _{1} = value; OnPropertyChanged(\"{1}\"); }}}}\r\n";
            string propTemplateEval = "\t\tpublic {0} {1} {{ get {{ return {2}; }}}}\r\n";
            if (e.Properties != null)
            {
                foreach (var prop in e.Properties)
                {
                    if (prop is Schema.DataPropertyScalar)
                    {
                        sb.Append(string.Format(propTemplate, OnGetCSType(((Schema.DataPropertyScalar)prop).type), prop.name));
                    }
                    else if (prop is Schema.DataPropertyArray)
                    {
                        sb.Append(string.Format(propTemplate, OnGetCSType(((Schema.DataPropertyArray)prop).type), prop.name));
                    }
                    else if (prop is Schema.EvaluatedProperty)
                    {
                        sb.Append(string.Format(propTemplateEval,
                            OnGetCSType(((Schema.EvaluatedProperty)prop).type), prop.name, ((Schema.EvaluatedProperty)prop).expression));
                    }
                    else if (prop is Schema.ScalarProperty)
                    {
                        sb.Append(string.Format(propTemplateEval,
                            OnGetCSType(((Schema.ScalarProperty)prop).type), prop.name, ((Schema.ScalarProperty)prop).value));
                    }
                }
            }
            return sb.ToString();
        }

        private string OnCreateEntityReader(Schema.Entity e, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            StringBuilder sb = new StringBuilder();
            string propTemplate = "\t\t\tthis._{0} = {1}rdr[\"{2}\"];\r\n";
            if (e.Properties != null)
            {
                foreach (var prop in e.Properties)
                {
                    if (prop is Schema.DataPropertyScalar)
                    {
                        sb.Append(string.Format(propTemplate,
                            prop.name,
                            OnGetDbCastType(((Schema.DataPropertyScalar)prop).type),
                            ((Schema.DataPropertyScalar)prop).source));
                    }
                    else if (prop is Schema.DataPropertyArray)
                    {
                        sb.Append(string.Format(propTemplate,
                            prop.name,
                            OnGetDbCastType(((Schema.DataPropertyArray)prop).type),
                            ((Schema.DataPropertyArray)prop).source));
                    }
                }
            }
            return sb.ToString();
        }

        private string OnGetDbCastType(Schema.CS_ArrayTypes type)
        {
            switch (type)
            {
                default:
                case Schema.CS_ArrayTypes.SystemBoolean:
                case Schema.CS_ArrayTypes.SystemByte:
                case Schema.CS_ArrayTypes.SystemChar:
                case Schema.CS_ArrayTypes.SystemDateTime:
                case Schema.CS_ArrayTypes.SystemDecimal:
                case Schema.CS_ArrayTypes.SystemDouble:
                case Schema.CS_ArrayTypes.SystemInt16:
                case Schema.CS_ArrayTypes.SystemInt32:
                case Schema.CS_ArrayTypes.SystemInt64:
                case Schema.CS_ArrayTypes.SystemSByte:
                case Schema.CS_ArrayTypes.SystemSingle:
                case Schema.CS_ArrayTypes.SystemString:
                case Schema.CS_ArrayTypes.SystemUInt16:
                case Schema.CS_ArrayTypes.SystemUInt32:
                case Schema.CS_ArrayTypes.SystemUInt64:
                    return "(" + OnGetCSType(type) + ")";
            }
        }

        private string OnGetCSType(Schema.CS_ArrayTypes type)
        {
            return type.ToString().Replace("System", "System.");
        }

        private string OnGetDbCastType(Schema.CS_Types type)
        {
            switch (type)
            {
                default:
                case Schema.CS_Types.SystemBoolean:
                case Schema.CS_Types.SystemByte:
                case Schema.CS_Types.SystemChar:
                case Schema.CS_Types.SystemDateTime:
                case Schema.CS_Types.SystemDecimal:
                case Schema.CS_Types.SystemDouble:
                case Schema.CS_Types.SystemInt16:
                case Schema.CS_Types.SystemInt32:
                case Schema.CS_Types.SystemInt64:
                case Schema.CS_Types.SystemSByte:
                case Schema.CS_Types.SystemSingle:
                case Schema.CS_Types.SystemString:
                case Schema.CS_Types.SystemUInt16:
                case Schema.CS_Types.SystemUInt32:
                case Schema.CS_Types.SystemUInt64:
                case Schema.CS_Types.SystemByte1:
                    return "(" + type.ToString().Replace("System", "System.") + ")";
            }
        }

        private string OnGetCSType(Schema.CS_Types type)
        {
            return type.ToString().Replace("System", "System.");
        }

        private MSBuildCompiler.MSBuildSource[] OnCreatePageSources(ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            return new MSBuildCompiler.MSBuildSource[0];
        }

        private MSBuildCompiler.MSBuildSource[] OnCreateResourceSources(ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            List<MSBuildCompiler.MSBuildSource> sources = new List<MSBuildCompiler.MSBuildSource>();
            sources.Add(OnCreateDBScriptResourceSource(@"Install\Client\Scripts\SelectEntities.txt", license, app, config, rdr));
            sources.Add(OnCreateDBScriptResourceSource(@"Install\Client\Scripts\SelectEntitiesExtended.txt", license, app, config, rdr));
            return sources.ToArray();
        }

        private MSBuildCompiler.MSBuildSource OnCreateDBScriptResourceSource(string resource, ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            MSBuildCompiler.MSBuildSource source = new MSBuildCompiler.MSBuildSource()
            {
                SourceType = MSBuildCompiler.MSBuildSourceType.EmbeddedResource,
                Include = @"Data\SQLite\Scripts\" + Path.GetFileName(resource),
                Data = OnGetResource(rdr, resource)
            };
            return source;
        }


        private byte[] OnGetResource(EmbeddedCodeReader rdr, string resourcePath, params KeyValuePair<string, string>[] args)
        {
            string resource = rdr.LoadResource(resourcePath);
            foreach(KeyValuePair<string, string> kvp in args)
            {
                resource = resource.Replace("@" + kvp.Key, kvp.Value);
            }
            return UTF8Encoding.UTF8.GetBytes(resource);
        }

        private void OnBuildDbSchema(ILicense license, DeclaredApp app, Schema.ModelEx config, EmbeddedCodeReader rdr)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(rdr.LoadResource(@"Install\Client\DBSchema.sql"));

            foreach(Schema.Entity e in config.EntityModels)
            {
                sb.Append(OnCreateEntitySchema(e));
            }

            if (sb.Length > 0)
                DataContext.Default.ExecuteQuery(sb.ToString());
        }

        private string OnCreateEntitySchema(Schema.Entity e)
        {
            if (e.DataSource is Schema.DBEntityDataSource)
            {
                StringBuilder sb = new StringBuilder();
                /*  CREATE TABLE IF NOT EXISTS [Entity] (
                    [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, 
                    [Name] NVARCHAR(256), 
                    [ParentId] INT CONSTRAINT [FK_ENTITY_PARENT] REFERENCES [Entity]([Id]) ON DELETE SET NULL ON UPDATE CASCADE);
                 */
                sb.Append("CREATE TABLE IF NOT EXISTS [");
                sb.Append(e.name);
                sb.Append("] (");
                sb.Append("[Id] INTEGER NOT NULL PRIMARY KEY CONSTRAINT [FK_ENTITY_PARENT] REFERENCES [Entity]([Id]) ON DELETE CASCADE ON UPDATE CASCADE");
                sb.Append(OnCreateEntityFields(e));
                sb.Append(");$\r\n\r\n");

                return sb.ToString();
            }
            return string.Empty;
        }

        private string OnCreateEntityFields(Schema.Entity e)
        {
            StringBuilder sb = new StringBuilder();
            if (e is Schema.FileEntity)
            {
                sb.Append(",\r\n");
                sb.Append(OnCreateEntityField(
                    new Schema.DataPropertyScalar()
                    {
                        name = "File",
                        recordsetIndex = 0,
                        source = "File",
                        type = Schema.CS_Types.SystemString
                    }));
            }
            if (e.Properties == null) e.Properties = new Schema.Property[0];
            foreach (Schema.Property prop in e.Properties)
            {
                sb.Append(",\r\n");
                sb.Append(OnCreateEntityField(prop));
            }
            return sb.ToString();
        }

        private string OnCreateEntityField(Schema.Property prop)
        {
            if (prop is Schema.DataPropertyScalar)
            {
                return string.Format("[{0}] {1}",
                    ((Schema.DataPropertyScalar)prop).source,
                    OnGetDbType(((Schema.DataPropertyScalar)prop).type));
            }
            else if (prop is Schema.DataPropertyArray)
            {
                
            }
            throw new NotSupportedException();
        }

        private string OnGetDbType(Schema.CS_Types p)
        {
            switch (p)
            {
                case Schema.CS_Types.SystemString: return "TEXT";
                case Schema.CS_Types.SystemUInt16: 
                case Schema.CS_Types.SystemInt16: return "SMALLINT";
                case Schema.CS_Types.SystemUInt32:
                case Schema.CS_Types.SystemInt32: return "INTEGER";
                case Schema.CS_Types.SystemUInt64:
                case Schema.CS_Types.SystemInt64: return "BIGINT";
                case Schema.CS_Types.SystemSingle: return "FLOAT";
                case Schema.CS_Types.SystemDouble: return "DOUBLE";
                case Schema.CS_Types.SystemDateTime: return "DATETIME";
                case Schema.CS_Types.SystemBoolean: return "BOOLEAN";
                case Schema.CS_Types.SystemByte: return "BINARY(1)";
                case Schema.CS_Types.SystemSByte: return "BINARY(1)";
                case Schema.CS_Types.SystemChar: return "CHAR";
                case Schema.CS_Types.SystemDecimal: return "CURRENCY";
                default:
                case Schema.CS_Types.SystemByte1: return "BLOB";
            }
        }


    }
}
