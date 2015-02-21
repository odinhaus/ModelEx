using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using WoodMac.ModelEx.Core;
using WoodMac.ModelEx.Core.Compilation;
using WoodMac.ModelEx.Core.Data;
using WoodMac.ModelEx.Core.Dynamic;

namespace WoodMac.ModelEx.GEM.ViewModels
{
    public interface IListBuilder
    {
        IEnumerable<Entity> BuildList(ExplorerFolder parentFolder, IDataReader reader);
    }
    public interface IListReader
    {
        IDataReader GetList(ExplorerFolder parentFolder);
    }
    public class Entity : Extendable<Entity>
    {
        public class ExplorerFolderReader
        {
            public ExplorerFolderReader(ExplorerFolder folder, IListReader reader, IListBuilder builder)
            {
                this.Reader = reader;
                this.Builder = builder;
                this.Folder = folder;
            }

            private IListReader Reader;
            private IListBuilder Builder;
            private ExplorerFolder Folder;

            public IEnumerable<Entity> CreateChildren()
            {
                return Builder.BuildList(Folder, Reader.GetList(Folder));
            }
        }

        static Dictionary<string, Type> _builtTypes = new Dictionary<string, Type>();
        public static bool CreateChildReader(ExplorerFolder parent, XmlElement xFoldersNode, out ExplorerFolderReader sfr)
        {
            IListReader rdr = GetListReader(parent, xFoldersNode);
            IListBuilder bldr = GetListBuilder(parent, xFoldersNode);
            sfr = new ExplorerFolderReader(parent, rdr, bldr);
            return true;
        }


        static Dictionary<string, Type> _builtListBuilders = new Dictionary<string, Type>();
        static HashSet<string> _builtTypeNames = new HashSet<string>();
        private static IListBuilder GetListBuilder(ExplorerFolder parentFolder, XmlElement xFoldersNode)
        {
            Type entityType;
            string entityTypeName = xFoldersNode.GetAttribute("name");
            if (!_builtTypes.TryGetValue(entityTypeName, out entityType))
            {
                entityType = BuildEntityType(parentFolder, xFoldersNode);
                _builtTypes.Add(entityTypeName, entityType);
                _builtTypeNames.Add(entityType.Name);
            }

            Type builderType;
            if (!_builtListBuilders.TryGetValue(entityType.FullName, out builderType))
            {
                builderType = BuildBuilderType(parentFolder, entityType, xFoldersNode);
                _builtListBuilders.Add(entityType.FullName, builderType);
            }

            return Activator.CreateInstance(builderType) as IListBuilder;
        }

        private static Type BuildBuilderType(ExplorerFolder parentFolder, Type entityType, XmlElement xFoldersNode)
        {
            XmlElement xEntity = xFoldersNode.SelectSingleNode("m:Entity", _xnsm) as XmlElement;
            string template = _listBuilderTemplate.Replace("@Class", xFoldersNode.GetAttribute("name"));
            string name = "unknown";
            string props = "";
            foreach (XmlElement xProp in xEntity.SelectNodes("m:Properties/m:Property", _xnsm))
            {
                if (xProp.GetAttribute("name") == "Name")
                    name = xProp.GetAttribute("source");
                else
                    switch (xProp.GetAttribute("xsi:type"))
                    {
                        default:
                            {
                                props += string.Format("e.{0} = {1}{2};\r\n",
                                    xProp.GetAttribute("name"),
                                    GetCastType(xProp.GetAttribute("name"), entityType),
                                    "reader[\"" + xProp.GetAttribute("source") + "\"]");
                                break;
                            }
                        case "ScalarProperty":
                            {
                                props += string.Format("e.{0} = {1}{2};\r\n",
                                     xProp.GetAttribute("name"),
                                     GetCastType(xProp.GetAttribute("name"), entityType),
                                     xProp.GetAttribute("value"));
                                break;
                            }
                        case "EvaluatedProperty":
                            {
                                props += string.Format("e.{0} = {1}{2};\r\n",
                                     xProp.GetAttribute("name"),
                                     GetCastType(xProp.GetAttribute("name"), entityType),
                                     xProp.GetAttribute("expression"));
                                break;
                            }
                    }
            }
            template = template.Replace("@Name", name).Replace("@Props", props);
            template = template.Replace("@Entity", entityType.FullName);
            bool err;
            CompilerErrorCollection errs;
            return CSharpCompiler.Compile(
                template,
                xFoldersNode.GetAttribute("name") + "_Builder",
                Context.GetEnvironmentVariable<string>("TempDir", "Temp"),
                new string[]
                {
                    typeof(Entity).Assembly.CodeBase,
                    entityType.Assembly.CodeBase,
                    typeof(System.Data.IDbConnection).Assembly.CodeBase, 
                    typeof(XmlDocument).Assembly.CodeBase
                },
                out err,
                out errs);
        }

        private static string GetCastType(string property, Type entityType)
        {
            PropertyInfo pi = entityType.GetProperty(property);
            return string.Format("({0})", pi.PropertyType.FullName);
        }

        private static string _listBuilderTemplate = @"
        using System;
        using System.CodeDom.Compiler;
        using System.Collections.Generic;
        using System.Data;
        using System.Linq;
        using System.Text;
        using System.Text.RegularExpressions;
        using System.Xml;
        using WoodMac.ModelEx.GEM.ViewModels;

        namespace WoodMac.Modelex.GEM.ViewModels
        {
            public class @Class_Builder : IListBuilder
            {
                public IEnumerable<Entity> BuildList(ExplorerFolder parentFolder, IDataReader reader)
                {
                    List<Entity> list = new List<Entity>();
                    while (reader.Read())
                        list.Add(BuildEntity(parentFolder, reader));
                    reader.Close();
                    return list;
                }

                private Entity BuildEntity(ExplorerFolder parentFolder, IDataReader reader)
                {
                    @Entity e = new @Entity(reader[""@Name""].ToString(), parentFolder);
                    @Props
                    return e;
                }
            }
        }";


        private static Type BuildEntityType(ExplorerFolder parentFolder, XmlElement xFoldersNode)
        {
            XmlElement xEntity = xFoldersNode.SelectSingleNode("m:Entity", _xnsm) as XmlElement;
            string name = xFoldersNode.GetAttribute("name");
            Dictionary<string, Type> props = new Dictionary<string, Type>();
            string deserialize = "protected override void FromReader(System.IO.BinaryReader reader) {\r\n\t\t\t";
            string serialize = "public override void Serialize(System.IO.BinaryWriter writer) { \r\n\t\t\tbase.Serialize(writer);\r\n\t\t\t";
            foreach (XmlElement xProp in xEntity.SelectNodes("m:Properties/m:Property", _xnsm))
            {
                if (xProp.GetAttribute("name") != "Name")
                {
                    string prop = xProp.GetAttribute("name");
                    Type type = TypeHelper.GetType(xProp.GetAttribute("type"));
                    props.Add(prop, type);

                    if (type.IsArray)
                    {
                        serialize += "writer.Write(this." + prop + ".Length);\r\n\t\t\t";
                    }
                    else if (type.Equals(typeof(DateTime)))
                        serialize += "writer.Write(this." + prop + ".ToBinary());\r\n\t\t\t";
                    else
                        serialize += "writer.Write(this." + prop + ");\r\n\t\t\t";
                    bool isCustom = false;
                    string readStr = GetReader(type, out isCustom);
                    if (isCustom)
                        deserialize += "this." + prop + " = " + readStr + ";\r\n\t\t\t";
                    else
                        deserialize += "this." + prop + " = reader." + readStr + ";\r\n\t\t\t";
                }
            }
            deserialize += "\t}";
            serialize += "\t}\r\n";

            return RuntimeTypeBuilder.GetDynamicType(
                props, 
                typeof(Entity), 
                name,
                "public " + name + "(string name, WoodMac.ModelEx.GEM.ViewModels.ExplorerFolder parentFolder) : base (name, parentFolder){}\r\n"
                + "protected " + name + "(System.IO.BinaryReader reader, WoodMac.ModelEx.GEM.ViewModels.ExplorerFolder parentFolder) : base(reader, parentFolder){}\r\n"
                + serialize
                + deserialize,
                typeof(Entity).Assembly.CodeBase);
        }

        private static string GetReader(Type type, out bool isCustom)
        {
            isCustom = false;
            switch (type.FullName)
            {
                case "System.Boolean":
                    return "ReadBoolean();";
                case "System.Byte":
                    return "ReadByte();";
                case "System.Byte[]":
                    return "ReadBytes(reader.ReadInt32());";
                case "System.Char":
                    return "ReadChar();";
                case "System.Char[]":
                    return "ReadChars(reader.ReadInt32());";
                case "System.Decimal":
                    return "ReadDecimal();";
                case "System.Double":
                    return "ReadDouble();";
                case "System.Int16":
                    return "ReadInt16();";
                case "System.Int32":
                    return "ReadInt32();";
                case "System.Int64":
                    return "ReadInt64();";
                case "System.SByte":
                    return "ReadSByte();";
                case "System.Single":
                    return "ReadSingle();";
                case "System.String":
                    return "ReadString();";
                case "System.UInt16":
                    return "ReadUInt16();";
                case "System.UInt32":
                    return "ReadUInt32();";
                case "System.UInt64":
                    return "ReadUInt64();";
                case "System.DateTime":
                    {
                        isCustom = true;
                        return "System.DateTime.FromBinary(reader.ReadInt64())";
                    }
                default: throw new NotSupportedException("Property type is not supported for serialization.");

            }
        }


        static Dictionary<string, Type> _builtListReaders = new Dictionary<string, Type>();
        private static IListReader GetListReader(ExplorerFolder parentFolder, XmlElement xFoldersNode)
        {
            XmlElement xEntity = (XmlElement)xFoldersNode.SelectSingleNode("m:Entity", _xnsm);
            Type rdrType;
            if (!_builtListReaders.TryGetValue(xFoldersNode.GetAttribute("name"), out rdrType))
            {
                rdrType = CreateListReader(parentFolder, xEntity);
                _builtListReaders.Add(xFoldersNode.GetAttribute("name"), rdrType);
            }
            return Activator.CreateInstance(rdrType) as IListReader;
        }

        private static Type CreateListReader(ExplorerFolder parentFolder, XmlElement xEntity)
        {
            XmlElement xDb = (XmlElement)parentFolder.XRoot.SelectSingleNode("m:Configuration/m:DataSource", _xnsm);
            string provider = xDb.GetAttribute("provider");
            string conStr = xDb.GetAttribute("connectionString");
            XmlElement xDataSource = (XmlElement)xEntity.SelectSingleNode("m:DataSource/m:Read", _xnsm);
            string command = xDataSource.InnerText;
            string regex = @"(?<Prop>@[\w\.]+)"; // capture @Name1.Nam2.Value type expressions

            string template = _listReaderTemplate.Replace("@Class", ((XmlElement)xEntity.ParentNode).GetAttribute("name"));
            template = template.Replace("@ConnectionString", conStr);
            template = template.Replace("@Provider", provider);

            Regex r = new Regex(regex, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match m = r.Match(command);
            int count = 0;
            string argList = "";
            while (m.Success)
            {
                string expression = m.Groups["Prop"].Value;
                command = command.Replace(expression, "{" + count + "}");

                if (count > 0)
                {
                    argList += ", ";
                }

                argList += "((dynamic)parentFolder.Entity)." + expression.Replace("@", "");
                m = m.NextMatch();
                count++;
            }
            if (string.IsNullOrEmpty(argList))
            {
                template = template.Replace("@Query", "@\"" + command.Replace("\"", "\"\"") + "\"");
            }
            else
            {
                template = template.Replace("@Query",
                    string.Format(@"string.Format(@""{0}"",{1})", command.Replace("\"", "\"\""), argList));
            }


            bool err;
            CompilerErrorCollection errs;
            Type rdrType = WoodMac.ModelEx.Core.Compilation.CSharpCompiler.Compile(
                template,
                 ((XmlElement)xEntity.ParentNode).GetAttribute("name") + "_ListReader",
                Context.GetEnvironmentVariable("TempDir").ToString(),
                new string[]
                {
                    typeof(System.Data.IDbConnection).Assembly.CodeBase, 
                    typeof(XmlDocument).Assembly.CodeBase,
                    typeof(IListReader).Assembly.CodeBase
                },
                out err,
                out errs);
            if (err)
            {
                // log it
                return null;
            }
            else
            {
                return rdrType;
            }
        }

        #region List Reader Template
        static string _listReaderTemplate = @"
        using System;
        using System.Collections.Generic;
        using System.Data;
        using System.Linq;
        using System.Text;
        using System.Xml;

        namespace WoodMac.ModelEx.GEM.ViewModels
        {
            public class @Class_ListReader : IListReader
            {
                public IDataReader GetList(ExplorerFolder parentFolder)
                {
                    string commandText = @Query;
                    IDbConnection connection = CreateConnection();
                    IDbCommand command = connection.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = commandText;
                    connection.Open();
                    return command.ExecuteReader(CommandBehavior.CloseConnection);
                }

                private IDbConnection CreateConnection()
                {
                    string conStr = @""@ConnectionString"";
                    return (IDbConnection)WoodMac.ModelEx.Core.TypeHelper.CreateType(""@Provider"",
                        new object[] { conStr });
                }
            }
        }";
        #endregion



        public ExplorerFolder ParentFolder { get; private set; }

        public static Entity CreateRoot(string name)
        {
            return new Entity(name, null);
        }


        public Entity(string name, ExplorerFolder parentFolder)
            : base(name, null, true, new MemberResolutionHandler(ResolveMember))
        {
            this.ParentFolder = parentFolder;
        }

        private static bool ResolveMember(string memberName, out object resolvedResult)
        {
            resolvedResult = null;
            return false;
        }

        protected override IEnumerable<DynamicProperty<Entity>> OnGetProperties()
        {
            return new DynamicProperty<Entity>[0];
        }

        protected override IEnumerable<DynamicFunction<Entity>> OnGetFunctions()
        {
            return new DynamicFunction<Entity>[0];
        }

        protected override string OnGetInstanceType()
        {
            return this.GetType().FullName;
        }

        protected override IEnumerable<string> OnGetAliases()
        {
            return new string[0];
        }

        public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
        {
            Entity found;
            if (_builtTypeNames.Contains(binder.Name)
                && TryWalkEntityTree(this, binder.Name, out found))
            {
                result = found;
                return true;
            }

            return base.TryGetMember(binder, out result);
        }

        private bool TryWalkEntityTree(Entity startEntity, string entityTypeName, out Entity result)
        {
            result = startEntity;
            if (startEntity == null) return false;

            bool ret = entityTypeName.Equals(startEntity.GetType().Name);
            if (!ret && startEntity.ParentFolder != null)
                ret = TryWalkEntityTree(startEntity.ParentFolder.Entity, entityTypeName, out result);
            return ret;
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write(this.GetType().AssemblyQualifiedName);
            writer.Write(this.Name);
        }

        protected virtual void FromReader(BinaryReader reader) { }

        public static Entity Deserialize(BinaryReader reader, ExplorerFolder parentFolder)
        {
            return TypeHelper.CreateType(reader.ReadString(), new object[] { reader, parentFolder }) as Entity;
        }

        public Entity(BinaryReader reader, ExplorerFolder parentFolder)
            : base(reader.ReadString(), null, true, new MemberResolutionHandler(ResolveMember))
        {
            ParentFolder = parentFolder;
            FromReader(reader);
        }

        static XmlNamespaceManager _xnsm;
        public static void SetNamespaceManager(XmlNamespaceManager xnsm)
        {
            _xnsm = xnsm;
        }

    }
}
