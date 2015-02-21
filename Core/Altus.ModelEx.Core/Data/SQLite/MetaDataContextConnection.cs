using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using Altus.Core.Configuration;
using System.Data.Common;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using Altus.Core;
using Altus.Core.Component;
using Altus.Core.Threading;
using Altus.Core.Licensing;
using System.Diagnostics;
using Altus.Core.Reflection;

namespace Altus.Core.Data.SQlite
{
    public class MetaDataContextConnection : IDbConnection
    {
        private readonly string _CreateDb = "CreateDb";
        private readonly string _CreateSchema = "CreateSchema";
        private readonly string _SeedDb = "SeedDb";
        private readonly string _OpenDb = "OpenDb";
#if(DEBUG)
        private readonly string _SeedDbDev = "SeedDbDev";
#endif

        public DeclaredApp App { get; private set; }

        public MetaDataContextConnection(IMetaDataContext context) 
        {
            App = Altus.Core.Component.App.Instance[context.Name.Split(':')[1]];
            SetScriptPath(BuildScriptPath(App));
            DatabaseExists = CheckDbExists(new SQLiteConnection(BuildConnectionString(App)));
            this.DataContext = context;
            this.ConnectionManager = Activator.CreateInstance(Context.CurrentContext.CurrentApp.DeclaredDataConnectionManager, new object[]{ this }) as IDbConnectionManager;

            if (Altus.Core.Component.App.Instance != null
                && Altus.Core.Component.App.Instance.Shell != null)
            {
                Altus.Core.Component.App.Instance.Shell.Add(this.ConnectionManager, "ConnectionManager:" + this.DataContext.Name);
            }
        }

        private string BuildConnectionString(DeclaredApp app)
        {
            string con = string.Format(@"Data Source={1}\{0}.db3;Database=META;Version=3;Pooling=False;Max Pool Size=100;", 
                app.Name, 
                BuildScriptPath(app));
            return con;
        }

        private string BuildScriptPath(DeclaredApp app)
        {
            string path = string.Format(@"{0}\Data\Meta",  app.CodeBase);
            return path;
        }

        public DbLock CreateConnection()
        {
            return CreateConnection(LockShare.Shared, LockOperation.ReadWrite);
        }

        public  DbLock CreateConnection(LockShare share, LockOperation operation)
        {
            return ConnectionManager.CreateConnection(share, operation);
        }

        public bool CreateDatabase()
        {
            if (DatabaseExists)
            {
                this.DataContext.ExecuteScript(this._OpenDb);
                this.ConnectionManager.UpdateSchema();
                this.ConnectionManager.StartReplication();
            }
            else
            {
                this.DataContext.ExecuteScript(this._CreateDb);
                this.DataContext.ExecuteScript(this._CreateSchema);
                this.ConnectionManager.UpdateSchema();
                this.DataContext.ExecuteScript(this._SeedDb);
                this.DataContext.ExecuteScript(this._OpenDb);
                this.ConnectionManager.StartReplication();

#if(DEBUG)
                if (this.CommandScriptExists(_SeedDbDev))
                    this.DataContext.ExecuteScript(this._SeedDbDev);
#endif
            }
            
            return true;
        }

        Regex _insertValues = new Regex(@"INSERT\s+INTO\s+(?<Table>\w+)\s*\((?<Fields>.*?)\)\s+Values\s*\((?<Values>.*?)\);$*",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        string _insertValuesReplace = @"INSERT INTO ${Table}(repl_rowid, ${Fields}) VALUES(GUID(), ${Values});";

        Regex _insertSelect = new Regex(@"INSERT\s+INTO\s+(?<Table>\w+)\s*\((?<Fields>.*?)\)\s+SELECT\s+(?<Distinct>(DISTINCT\s+)?)(?<Select>.*?);$*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);
        string _insertSelectReplace = @"INSERT INTO ${Table}(repl_rowid, ${Fields}) SELECT ${Distinct}GUID(), ${Select};";

        public string PreProcessScript(string script)
        {
            return script;

            StringBuilder newScript = new StringBuilder();
            //foreach (string part in script.Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries))
            //{
            string newPart = script; // part + ";" + Environment.NewLine;
                Dictionary<string, string>.Enumerator gen = Altus.Core.Data.DataContext.GuidsInserted.GetEnumerator(); ;
                // swap out repl GUIDs
                while (gen.MoveNext())
                {
                    newPart = newPart.Replace(gen.Current.Key, "'" + gen.Current.Value + "'");
                }

                //add repl_rowid to INSERTS
                newPart = _insertValues.Replace(newPart, _insertValuesReplace);
                newPart = _insertSelect.Replace(newPart, _insertSelectReplace);
                newScript.AppendLine(newPart);
            //}
            return newScript.ToString();
        }

        public string PostProcessScript(string script)
        {
            string sql = string.Empty;
            //AddInsertTrigger(script, out sql);
            return sql;
        }

        Regex _insert = new Regex(@"INSERT\s+INTO\s+(?<Table>\w+)\s*\(",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private bool AddInsertTrigger(string newPart, out string sql)
        {
            Match m = _insert.Match(newPart);
            bool doInsert = m.Success && m.Groups["Table"].Value.ToLower() != "repl_evt";
            sql = string.Empty;
            if (doInsert)
            {
                sql = @"INSERT INTO repl_evt ([Timestamp], TargetTable, EventType, RowToken, SyncToken)
                            SELECT
                                CURRENT_TIMESTAMP,
                                '{0}',
                                'INSERT',
                                repl_rowid,
                                hex(randomblob(18))
                            FROM [{0}]
                            WHERE ROWID = last_insert_rowid();";
                sql = string.Format(sql, m.Groups["Table"].Value);
            }
            return doInsert;
        }

        private void SetScriptPath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Context.CurrentContext.CodeBase, path);
            }
            ScriptPath = path;
        }

        public bool SupportsTransactions { get { return true; } }
        public IMetaDataContext DataContext { get; private set; }

        private bool CheckDbExists(DbConnection connection)
        {
            bool isNew = false;
            try
            {
                string dataPath = BuildScriptPath(App);
                Directory.CreateDirectory(dataPath);

                Regex r = new Regex(@"Data Source=(?<file>.+?)(;|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);
                string file = connection.DataSource;

                Match m = r.Match(connection.ConnectionString);
                if (m.Success)
                {
                    file = m.Groups["file"].Value;
                }

                string path = Path.Combine(dataPath, file);

                isNew = System.IO.File.Exists(path);

                this.ConnectionString = connection.ConnectionString.Replace(file, path);
                
            }
            catch { }
            finally { connection.Dispose(); }
            return isNew;
        }

        public IDbConnectionManager ConnectionManager { get; private set; }

        public bool DatabaseExists
        {
            get;
            private set;
        }

        public string ConnectionString { get; set; }
        public string ScriptPath { get; private set; }

        public bool CommandScriptExists(string scriptName)
        {
            string path = Path.Combine(ScriptPath, string.Format("{0}.txt", scriptName));
            if (!System.IO.File.Exists(path))
            {
                string source;
                return TryGetEmbeddedScript(scriptName, out source);
            }
            else return true;
        }

        public bool CommandScriptExists<T>(object entity, DbOperation operation, out string scriptName)
        {
            scriptName = GetScriptName<T>(entity, operation);
            return CommandScriptExists(scriptName);
        }

        private void SaveCommandScript(string scriptName, string script)
        {
            string path = Path.Combine(ScriptPath, string.Format("{0}.txt", scriptName));

            using (FileStream fs = System.IO.File.Create(path))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(script);
                }
            }
        }

        public IEnumerable<string> GetCommandScripts(string scriptName, params DbParam[] dbParams)
        {
            string script = scriptName;
            if (!Context.Cache.ContainsKey(scriptName))
            {
               
                try
                {
                    string path = Path.Combine(ScriptPath, string.Format("{0}.txt", scriptName));

                    if (System.IO.File.Exists(path))
                    {
                        StreamReader rdr = new StreamReader(path);
                        script = rdr.ReadToEnd();
                        rdr.Close();
                    }
                }
                catch { };

                if (scriptName == script) TryGetEmbeddedScript(scriptName, out script);

                Regex r = new Regex(@"(?<Comment>/\*[^\*/]*\*/)", RegexOptions.Compiled); // strips comments defined by /* ... */ delimiters
                script = r.Replace(script, ""); // strip out comments
                Context.Cache.Add(scriptName, script, DateTime.MaxValue, TimeSpan.FromHours(1), new string[0]);
            }
            else
            {
                script = (string)Context.Cache[scriptName];
            }

            string[] scripts = script.Split(new char[] { '$' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s)).ToArray();

            if (dbParams != null)
            {
                ReplaceParams(scripts, dbParams);
            }
            return scripts;
        }

        private bool TryGetEmbeddedScript(string scriptName, out string scriptSource)
        {
            scriptSource = scriptName;
            try
            {
                HashSet<Assembly> checkedAssemblies = new HashSet<Assembly>();
                Assembly assembly = Context.CurrentContext.CurrentApp.PrimaryAssembly ?? this.DataContext.GetType().Assembly;
                string resourceName = GetResourceKey(assembly, scriptName);
                if (!TryGetManifestResource(resourceName, assembly, out scriptSource))
                {
                    checkedAssemblies.Add(assembly);
                    assembly = this.GetType().Assembly;
                    resourceName = GetResourceKey(assembly, scriptName);
                    if (!TryGetManifestResource(resourceName, assembly, out scriptSource))
                    {
                        checkedAssemblies.Add(assembly);
                        StackTrace trace = new StackTrace(0);
                        
                        for (int i = 0; i < trace.FrameCount; i++)
                        {
                            StackFrame frame = trace.GetFrame(i);
                            if (frame.GetMethod().DeclaringType != null)
                            {
                                assembly = frame.GetMethod().DeclaringType.Assembly;
                                if (!checkedAssemblies.Contains(assembly))
                                {
                                    resourceName = GetResourceKey(assembly, scriptName);
                                    if (TryGetManifestResource(resourceName, assembly, out scriptSource))
                                        return true;
                                    checkedAssemblies.Add(assembly);
                                }
                            }
                        }
                        scriptSource = scriptName;
                        return false;
                    }
                }
            }
            catch { return false; }
            return true;
        }

        private string GetResourceKey(Assembly assembly, string scriptName)
        {
            DataContextScriptsAttribute attrib = assembly.GetCustomAttribute<DataContextScriptsAttribute>();
            if (attrib == null)
            {
                return assembly.GetName().Name + ".Data.MetaDataContextScripts." + scriptName + ".txt";
            }
            else
            {
                return attrib.ResourceKey + "." + scriptName + ".txt";
            }
        }

        private bool TryGetManifestResource(string resourceName, Assembly assembly, out string resource)
        {
            resource = resourceName;
            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return false;
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        resource = reader.ReadToEnd();
                        return true;
                    }
                }
            }
            catch { return false; }
        }

        public void ReplaceParams(string[] scripts, IEnumerable<DbParam> dbParams)
        {
            for (int i = 0; i < scripts.Length; i++)
            {
                foreach (DbParam p in dbParams)
                {
                    scripts[i] = p.ReplaceInCommand(scripts[i]);
                }
            }
        }

        public string GetLastInsertIdCommandScript()
        {
            return "SELECT last_insert_row()";
        }

        private string BuildDeleteScript<T>(object entity, DbOperation op)
        {
            string script = "DELETE FROM {0} WHERE {1};";
            StorageMapping sm;
            if (GetMapping<T>(entity, out sm))
            {
                string where = BuildReadWhere(sm);
                script = string.Format(script, sm.StorageEntity, where);
            }
            else
            {
                throw (new InvalidOperationException(string.Format("The provided entity of type {0} does not expose a {1} attribute.", typeof(T).FullName, typeof(StorageMappingAttribute).FullName)));
            }
            return script;
        }

        private string BuildInsertScript<T>(object entity, DbOperation op)
        {
            string script = "INSERT INTO {0} ({1}) VALUES({2});$";
            StorageMapping sm;
            if (GetMapping<T>(entity, out sm))
            {
                string fields = BuildInsertFields(sm);
                string values = BuildInsertValues(sm);
                script = string.Format(script, sm.StorageEntity, fields, values);
            }
            else
            {
                throw (new InvalidOperationException(string.Format("The provided entity of type {0} does not expose a {1} attribute.", typeof(T).FullName, typeof(StorageMappingAttribute).FullName)));
            }
            script += string.Format("SELECT * FROM {0} ORDER BY RowID desc LIMIT 1;", sm.StorageEntity);
            return script;
        }

       
        private string BuildUpdateScript<T>(object entity, DbOperation op)
        {
            string script = "UPDATE {0} SET {1} WHERE {2};";
            StorageMapping sm;
            if (GetMapping<T>(entity, out sm))
            {
                string where = BuildReadWhere(sm);
                string values = BuildSetValues(sm);
                script = string.Format(script, sm.StorageEntity, values, where);
            }
            else
            {
                throw (new InvalidOperationException(string.Format("The provided entity of type {0} does not expose a {1} attribute.", typeof(T).FullName, typeof(StorageMappingAttribute).FullName)));
            }
            return script;
        }

        private string BuildSetValues(StorageMapping sm)
        {
            StringBuilder sb = new StringBuilder();
            bool isFirstField = true;
            foreach (StorageMapping.StorageFieldMapping sfm in sm.MappedMembers.Where(m => !m.StorageFieldModifiers.HasFlagFast(StorageFieldModifiers.Key)
                && !m.StorageFieldModifiers.HasFlagFast(StorageFieldModifiers.AutoGenerated)))
            {
                if (!isFirstField)
                {
                    sb.Append(",");
                    sb.Append(Environment.NewLine);
                }
                isFirstField = false;
                sb.Append(sfm.StorageMemberName);
                sb.Append("=");
                sb.Append("@" + sfm.MemberName);
            }
            return sb.ToString();
        }

        private string BuildInsertFields(StorageMapping sm)
        {
            StringBuilder sb = new StringBuilder();
            bool isFirstField = true;
            foreach (StorageMapping.StorageFieldMapping sfm in sm.MappedMembers.Where(m => !m.StorageFieldModifiers.HasFlagFast(StorageFieldModifiers.AutoGenerated)))
            {
                if (!isFirstField)
                {
                    sb.Append(",");
                    sb.Append(Environment.NewLine);
                }
                isFirstField = false;
                sb.Append(sfm.StorageMemberName);
            }
            return sb.ToString();
        }

        private string BuildInsertValues(StorageMapping sm)
        {
            StringBuilder sb = new StringBuilder();
            bool isFirstField = true;
            foreach (StorageMapping.StorageFieldMapping sfm in sm.MappedMembers.Where(m => !m.StorageFieldModifiers.HasFlagFast(StorageFieldModifiers.AutoGenerated)))
            {
                if (!isFirstField)
                {
                    sb.Append(",");
                    sb.Append(Environment.NewLine);
                }
                isFirstField = false;
                sb.Append("@" + sfm.MemberName);
            }
            return sb.ToString();
        }

        private bool CheckWriteConstraints<T>(object entity, StorageMapping mapping, out string errorMessage)
        {
            errorMessage = string.Empty;
            bool ret = true;

            foreach (StorageMapping.StorageFieldMapping sfm in mapping.MappedMembers.Where(mm => mm.ConstraintHandlers.Length > 0))
            {
                foreach (Func<object, bool> handler in sfm.ConstraintHandlers)
                {
                    ret = handler(entity);
                    if (!ret) return false;
                }
            }

            return ret;
        }

        private string BuildReadScript<T>(object entity, DbOperation op)
        {
            string script = "SELECT * FROM {0} WHERE {1};";
            StorageMapping sm;
            if (GetMapping<T>(entity, out sm))
            {
                string where = BuildReadWhere(sm);
                script = string.Format(script, sm.StorageEntity, where);
            }
            else
            {
                throw (new InvalidOperationException(string.Format("The provided entity of type {0} does not expose a {1} attribute.", typeof(T).FullName, typeof(StorageMappingAttribute).FullName)));
            }
            return script;
        }

        private string BuildSelectScript<T>(object entity, DbOperation op)
        {
            string script = "SELECT * FROM {0} WHERE {1};";
            StorageMapping sm;
            if (GetMapping<T>(entity, out sm))
            {
                string where = BuildSelectWhere(sm);
                script = string.Format(script, sm.StorageEntity, where);
            }
            else
            {
                throw (new InvalidOperationException(string.Format("The provided entity of type {0} does not expose a {1} attribute.", typeof(T).FullName, typeof(StorageMappingAttribute).FullName)));
            }
            return script;
        }

        private string BuildReadWhere(StorageMapping sm)
        {
            StringBuilder where = new StringBuilder();
            StringBuilder keyWhere = new StringBuilder("(\n\t");
            StringBuilder uqWhere = new StringBuilder();
            bool isFirstKey = true;
            bool isFirstUQ = true;
            if (sm != null && sm.IsMapped)
            {
                foreach (StorageMapping.StorageFieldMapping sfm in sm.MappedMembers.Where(m => m.StorageFieldModifiers.HasFlagFast(StorageFieldModifiers.Key)))
                {
                    if (!isFirstKey)
                    {
                        keyWhere.Append("\n\tand\n\t");
                    }
                    keyWhere.Append(sfm.StorageMemberName);
                    if (sfm.MemberType == typeof(string))
                    {
                        keyWhere.Append(" like ");
                    }
                    else
                    {
                        keyWhere.Append(" = ");
                    }
                    keyWhere.Append("@" + sfm.MemberName);
                    isFirstKey = false;
                }

                if (keyWhere.Length > 0)
                {
                    keyWhere.Append("\n)");
                }

                foreach (StorageMapping.StorageFieldMapping sfm in sm.MappedMembers.Where(m =>
                    m.StorageFieldModifiers.HasFlagFast(StorageFieldModifiers.Unique)
                    && !m.StorageFieldModifiers.HasFlagFast(StorageFieldModifiers.Key)))
                {
                    if (keyWhere.Length > 0)
                    {
                        keyWhere.Append("\nor\n");
                    }
                    if (uqWhere.Length == 0)
                    {
                        uqWhere.Append("(\n\t");
                    }
                    if (!isFirstUQ)
                    {
                        uqWhere.Append("\n\tand\n\t");
                    }
                    uqWhere.Append(sfm.StorageMemberName);
                    if (sfm.MemberType == typeof(string))
                    {
                        uqWhere.Append(" like ");
                    }
                    else
                    {
                        uqWhere.Append(" = ");
                    }
                    uqWhere.Append("@" + sfm.MemberName);
                    isFirstUQ = false;
                }
            }
            else
            {
                keyWhere = new StringBuilder();
                uqWhere = new StringBuilder();
            }
            if (uqWhere.Length > 0)
            {
                uqWhere.Append("\n)");
            }

            if (keyWhere.Length == 0 && uqWhere.Length == 0)
            {
                where.Append("1 = 1");
            }
            if (keyWhere.Length > 0)
            {
                where.Append(keyWhere.ToString());
            }
            if (uqWhere.Length > 0)
            {
                where.Append(uqWhere.ToString());
            }
            return where.ToString();
        }

        private string BuildSelectWhere(StorageMapping sm)
        {
            StringBuilder where = new StringBuilder();
            if (sm != null && sm.IsMapped)
            {
                foreach (StorageMapping.StorageFieldMapping sfm in sm.MappedMembers)
                {
                    if (where.Length > 0)
                    {
                        where.Append("\n\tand\n\t");
                    }
                    where.Append("(");
                    where.Append("@" + sfm.MemberName);
                    where.Append(" is null or ");
                    where.Append(sfm.StorageMemberName);
                    if (sfm.MemberType == typeof(string))
                    {
                        where.Append(" like ");
                    }
                    else
                    {
                        where.Append(" = ");
                    }
                    where.Append("@" + sfm.MemberName);
                    where.Append(")");
                }
            }
            if (where.Length == 0)
            {
                where.Append("1 = 1");
            }
            return where.ToString();
        }

        private bool GetMapping<T>(object entity, out StorageMapping mapping)
        {
            mapping = StorageMapping.CreateFromType(typeof(T));
            return mapping != null;
        }

        private string GetScriptName<T>(object entity, DbOperation operation)
        {
            StorageMapping sm = StorageMapping.CreateFromType(typeof(T));
            return string.Format("_{0}{1}", operation.ToString(), sm.StorageEntity);
        }

        public bool CreateCommandScript<T>(object entity, DbOperation operation)
        {
            string scriptName = GetScriptName<T>(entity, operation);
            string script = "";
            switch (operation)
            {
                case DbOperation.Delete:
                    {
                        script = BuildDeleteScript<T>(entity, operation);
                        break;
                    }
                case DbOperation.Insert:
                    {
                        script = BuildInsertScript<T>(entity, operation);
                        break;
                    }
                case DbOperation.Update:
                    {
                        script = BuildUpdateScript<T>(entity, operation);
                        break;
                    }
                case DbOperation.Select:
                    {
                        script = BuildSelectScript<T>(entity, operation);
                        break;
                    }
                default:
                    {
                        script = BuildReadScript<T>(entity, operation);
                        break;
                    }
            }
            this.SaveCommandScript(scriptName, script);
            return true;
        }

        public bool CreateCommandParameters<T>(object entity, DbOperation operation, out DbParam[] parms)
        {
            parms = GetParameters<T>(entity, operation);
            return parms != null;
        }

        private DbParam[] GetParameters<T>(object entity, DbOperation op)
        {
            switch (op)
            {
                case DbOperation.Delete:
                    return DeleteParameters<T>(entity);
                case DbOperation.Insert:
                    return InsertParameters<T>(entity);
                case DbOperation.Update:
                    return UpdateParameters<T>(entity);
                case DbOperation.Select:
                    return SelectParameters<T>(entity);
                default:
                    return ReadParameters<T>(entity);
            }
        }

        private DbParam[] DeleteParameters<T>(object entity)
        {
            return ReadParameters<T>(entity);
        }        

        private DbParam[] InsertParameters<T>(object entity)
        {
            StorageMapping sm;
            if (GetMapping<T>(entity, out sm))
            {
                List<DbParam> parms = new List<DbParam>();
                foreach (StorageMapping.StorageFieldMapping sfm in sm.MappedMembers.Where(m => !m.StorageFieldModifiers.HasFlagFast(StorageFieldModifiers.AutoGenerated)))
                {
                    parms.Add(new DbParam(sfm.MemberName, sfm.GetValue(entity)));
                }
                return parms.ToArray();
            }
            else
                throw (new InvalidOperationException(String.Format("The provided entity of type {0} is not mapped to a storage provider.", typeof(T).FullName)));
        }

        private DbParam[] UpdateParameters<T>(object entity)
        {
            StorageMapping sm;
            if (GetMapping<T>(entity, out sm))
            {
                List<DbParam> parms = new List<DbParam>();
                foreach (StorageMapping.StorageFieldMapping sfm in sm.MappedMembers)
                {
                    parms.Add(new DbParam(sfm.MemberName, sfm.GetValue(entity)));
                }
                return parms.ToArray();
            }
            else
                throw (new InvalidOperationException(String.Format("The provided entity of type {0} is not mapped to a storage provider.", typeof(T).FullName)));
        }

        private DbParam[] ReadParameters<T>(object entity)
        {
            StorageMapping sm;
            if (GetMapping<T>(entity, out sm))
            {
                List<DbParam> parms = new List<DbParam>();
                foreach (StorageMapping.StorageFieldMapping sfm in sm.MappedMembers.Where(m =>
                    m.StorageFieldModifiers.HasFlagFast(StorageFieldModifiers.Key)
                    || m.StorageFieldModifiers.HasFlagFast(StorageFieldModifiers.Unique)))
                {
                    MemberInfo member = entity.GetType().GetMember(sfm.MemberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
                    if (member == null)
                    {
                        parms.Add(new DbParam(sfm.MemberName, null));
                    }
                    else
                    {
                        sfm.MemberInfo = member;
                        parms.Add(new DbParam(sfm.MemberName, sfm.GetValue(entity)));
                    }
                }
                return parms.ToArray();
            }
            else
                throw (new InvalidOperationException(String.Format("The provided entity of type {0} is not mapped to a storage provider.", typeof(T).FullName)));
        }

        private DbParam[] SelectParameters<T>(object entity)
        {
            StorageMapping sm;
            if (GetMapping<T>(entity, out sm))
            {
                List<DbParam> parms = new List<DbParam>();
                if (sm != null && sm.IsMapped)
                {
                    foreach (StorageMapping.StorageFieldMapping sfm in sm.MappedMembers)
                    {
                        MemberInfo member = entity.GetType().GetMember(sfm.MemberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
                        if (member == null)
                        {
                            parms.Add(new DbParam(sfm.MemberName, null));
                        }
                        else
                        {
                            sfm.MemberInfo = member;
                            object value = sfm.GetValue(entity);
                            parms.Add(new DbParam(sfm.MemberName, value));
                        }
                    }
                }
                return parms.ToArray();
            }
            else
                throw (new InvalidOperationException(String.Format("The provided entity of type {0} is not mapped to a storage provider.", typeof(T).FullName)));
        }
    }
}
