using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core.Compilation
{
    public static class MSBuildCompiler
    {
        public static Assembly Compile(MSBuildPackage package)
        {
            string tempDir = Context.GetEnvironmentVariable<string>("TempDir", "");
            if (!Path.IsPathRooted(tempDir))
                tempDir = Path.Combine(Context.CurrentContext.CodeBase, tempDir, package.ProjectName);
            string outDir = package.OutputPath ?? "";
            if (!Path.IsPathRooted(outDir))
                outDir = Path.Combine(Context.CurrentContext.CodeBase, outDir);
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir);
                }
                catch { }
            }
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(outDir);

            string projFile = string.Format(@"{0}\{1}.csproj", tempDir, package.ProjectName);
            

            string template = _csProjTemplate;
            template = template.Replace("@Platform", package.Platform)
                .Replace("@Configuration", package.Configuration)
                .Replace("@Namespace", package.RootNamespace)
                .Replace("@Assembly", package.ProjectName)
                .Replace("@Guid", "{" + Guid.NewGuid().ToString() + "}")
                .Replace("@References", GetReferences(package, tempDir))
                .Replace("@Sources", GetItems(package, tempDir))
                .Replace("@TargetVersion", package.TargetVersion);

            using (StreamWriter sw = File.CreateText(projFile))
                sw.Write(template);

            var globalProperties = new Dictionary<string, string>();
            var buildRequest = new BuildRequestData(projFile, 
                globalProperties, null, new string[] { "Build" }, null);
            var pc = new ProjectCollection();

            var result = BuildManager.DefaultBuildManager.Build(new BuildParameters(pc), buildRequest);
            if (result.OverallResult == BuildResultCode.Success)
            {
                string asmFile = Path.ChangeExtension(projFile, ".dll");
                asmFile = Path.Combine(outDir, Path.GetFileName(asmFile));

                if (File.Exists(asmFile))
                    File.Delete(asmFile);
                if (File.Exists(Path.ChangeExtension(asmFile,".pdb")))
                    File.Delete(Path.ChangeExtension(asmFile, ".pdb"));

                File.Move(
                    result.ResultsByTarget["Build"].Items[0].ItemSpec,
                    asmFile);

                if (File.Exists(Path.ChangeExtension(result.ResultsByTarget["Build"].Items[0].ItemSpec, ".pdb")))
                    File.Move(
                    Path.ChangeExtension(result.ResultsByTarget["Build"].Items[0].ItemSpec, ".pdb"),
                    Path.ChangeExtension(asmFile, ".pdb"));

                Assembly assembly = Assembly.LoadFrom(asmFile);

                return assembly;
            }
            else throw new InvalidProgramException("Runtime Build System Compilation Failed.");
        }

        private static string GetItems(MSBuildPackage package, string tempDir)
        {
            StringBuilder sb = new StringBuilder();
            foreach (MSBuildSource source in package.Sources
                .Where(s => s.SourceType != MSBuildSourceType.Reference))
            {
                string sourcePath = Path.Combine(tempDir, source.Include);
                Directory.CreateDirectory(Path.GetDirectoryName(sourcePath));
                if (source.Data != null) File.WriteAllBytes(sourcePath, source.Data);

                sb.Append(string.Format("\t<{1} Include=\"{0}\"", source.Include, source.SourceType.ToString()));
                if (string.IsNullOrEmpty(source.DependentUpon)
                    && string.IsNullOrEmpty(source.Generator)
                    && string.IsNullOrEmpty(source.SubType))
                {
                    sb.Append(" />\r\n");
                }
                else
                {
                    sb.Append(">\r\n");
                    if (!string.IsNullOrEmpty(source.DependentUpon))
                    {
                        sb.Append(string.Format("\t\t<DependentUpon>{0}</DependentUpon>\r\n", 
                            source.DependentUpon));
                    }
                    if (!string.IsNullOrEmpty(source.Generator))
                    {
                        sb.Append(string.Format("\t\t<Generator>{0}</Generator>\r\n",
                            source.Generator));
                    }
                    if (!string.IsNullOrEmpty(source.SubType))
                    {
                        sb.Append(string.Format("\t\t<SubType>{0}</SubType>\r\n",
                            source.SubType));
                    }

                    sb.Append(string.Format("</{0}>\r\n", source.SourceType.ToString()));
                }
            }
            return sb.ToString();
        }

        private static string GetReferences(MSBuildPackage package, string tempDir)
        {
            StringBuilder sb = new StringBuilder();
            foreach(MSBuildSource source in package.Sources
                .Where(s => s.SourceType == MSBuildSourceType.Reference))
            {
                
                try
                {
                    string reference = source.Include;
                    var asm = Assembly.ReflectionOnlyLoadFrom(source.Include);
                    reference = asm.ToString();
                    source.HintPath = source.Include;
                    source.Include = reference;
                }
                catch { }

                sb.Append(string.Format("\t<Reference Include=\"{0}\"", source.Include));
                if (string.IsNullOrEmpty(source.HintPath))
                    sb.Append(" />\r\n");
                else
                {
                    sb.Append(">\r\n");
                    sb.Append(string.Format("\t\t<SpecificVersion>{0}</SpecificVersion>", source.SpecificVersion));
                    sb.Append(string.Format("\t\t<HintPath>{0}</HintPath>",source.HintPath));
                    
                    sb.Append("</Reference>\r\n");
                }
            }
            return sb.ToString();
        }
        public class MSBuildPackage
        {
            public MSBuildPackage(string targetVersion, string projectName, params MSBuildSource[] sources)
                : this(targetVersion, projectName, "Altus.Core", "Debug", "AnyCPU", sources) { }
            public MSBuildPackage(string targetVersion, string projectName,
                string rootNamespace, params MSBuildSource[] sources)
                : this(targetVersion, projectName, rootNamespace, "Debug", "AnyCPU", sources) { }
            public MSBuildPackage(string targetVersion, string projectName,
                string rootNamespace,
                string buildConfiguration, params MSBuildSource[] sources)
                : this(targetVersion, projectName, rootNamespace, buildConfiguration, "AnyCPU", sources) { }
            public MSBuildPackage(string targetVersion, string projectName, 
                string rootNamespace, 
                string buildConfiguration,
                string buildTargetPlatform, params MSBuildSource[] sources)
            {
                Sources = new List<MSBuildSource>(sources);
                OutputPath = "";
                ProjectName = projectName;
                RootNamespace = rootNamespace;
                Configuration = buildConfiguration;
                Platform = buildTargetPlatform;
                TargetVersion = targetVersion;
            }

            public List<MSBuildSource> Sources { get; private set; }
            public string ProjectName { get; set; }
            public string RootNamespace { get; set; }
            public string Configuration { get; set; }
            public string Platform { get; set; }
            public string OutputPath { get; set; }
            public string TargetVersion { get; set; }

        }

        public class MSBuildSource
        {
            public MSBuildSourceType SourceType { get; set; }
            /// <summary>
            /// The project relative source path for the item to include in the build
            /// </summary>
            public string Include { get; set; }
            /// <summary>
            /// Optional.  Any file that the Include file depends on 
            /// (e.g. View.xaml.cs depends on View.xaml)
            /// </summary>
            public string DependentUpon { get; set; }
            /// <summary>
            /// Optional. The subtype for the included file (e.g. Designer)
            /// </summary>
            public string SubType { get; set; }
            /// <summary>
            /// Optional.  The special tool used to build the file 
            /// (e.g. MSBuild:Compile for xaml style sheets)
            /// </summary>
            public string Generator { get; set; }
            /// <summary>
            /// Optional.  For binary, non-GAC Reference MSBuildSourceType, 
            /// specifies the relative or absolute path to the included reference.
            /// </summary>
            internal string HintPath { get; set; }
            /// <summary>
            /// Required for non-GAC Assemblies, and source files.  When the file does 
            /// not exist on disk, set the Data property to 
            /// binary data for the file to compile.
            /// </summary>
            public byte[] Data { get; set; }
            /// <summary>
            /// Required for on-disk assembly references.  
            /// </summary>
            public bool SpecificVersion { get; set; }

        }

        public enum MSBuildSourceType
        {
            None,
            Compile,
            Page,
            Resource,
            Reference,
            EmbeddedResource
        }

        static string _csProjTemplate = 
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""12.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">@Configuration</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">@Platform</Platform>
    <ProjectGuid>@Guid</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>@Namespace</RootNamespace>
    <AssemblyName>@Assembly</AssemblyName>
    <TargetFrameworkVersion>v@TargetVersion</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <SccProjectName>SAK</SccProjectName>
    <SccLocalPath>SAK</SccLocalPath>
    <SccAuxPath>SAK</SccAuxPath>
    <SccProvider>SAK</SccProvider>
    <TargetFrameworkProfile />
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""PresentationCore"" />
    <Reference Include=""PresentationFramework"" />
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""System.Drawing"" />
    <Reference Include=""System.Windows"" />
    <Reference Include=""System.Windows.Forms"" />
    <Reference Include=""System.Xaml"" />
    <Reference Include=""System.Xml.Linq"" />
    <Reference Include=""System.Data.DataSetExtensions"" />
    <Reference Include=""Microsoft.CSharp"" />
    <Reference Include=""System.Data"" />
    <Reference Include=""System.Xml"" />
    <Reference Include=""WindowsBase"" />
    <!--<Reference Include=""Irony"">
      <HintPath>..\..\Binaries\Irony\Irony.dll</HintPath>
    </Reference>-->
@References
  </ItemGroup>
  <ItemGroup>
@Sources
    <!--<Compile Include=""Schema\GEM_Config.cs"">
      <DependentUpon>GEM_Config.xsd</DependentUpon>
    </Compile>-->
    <!--<Compile Include=""Selectors\BodyContainerStyleSelector.cs"" />-->
    <!--<Compile Include=""Views\Explorer_tool.xaml.cs"">
      <DependentUpon>Explorer_tool.xaml</DependentUpon>
    </Compile>-->
    <!--<None Include=""Schema\GEM_Config.xsd"">
      <SubType>Designer</SubType>
    </None>-->
    <!--<Resource Include=""Styles\Default_Styles.xaml"">
      <Generator>MSBuild:Compile</Generator>
      <SubType>Designer</SubType>
    </Resource>-->
    <!--<Page Include=""Views\Explorer_tool.xaml"">
      <SubType>Designer</SubType>
      <Generator>MSBuild:Compile</Generator>
    </Page>-->
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild"">
  </Target>
  -->
</Project>";
    }
}
