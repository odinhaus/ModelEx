using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Threading;
using Altus.Core.Diagnostics;
using Altus.Core.Configuration;
using Altus.Core;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Reflection;
using Altus.Core.Collections;
using Altus.Core.Threading;
using Altus.Core.Component;
using Altus.Core.Licensing;
using Altus.Core.Dynamic;

namespace Altus.Core.Data
{
    public abstract class DataContext : InitializableComponent, IMetaDataContext
    {
        #region static
        private static IMetaDataContext _ctx;
        private static bool _log = false;
        private static string _logPath = null;

        static DataContext()
        {
            try
            {
                if (bool.TryParse(Context.GetEnvironmentVariable("MetaDataContextLogging").ToString(), out _log))
                {
                    _logPath = Path.Combine(Altus.Core.Component.App.Instance["Core"].CodeBase, "MetaDB.log");
                }
            }
            catch { }
        }

        public static IMetaDataContext Core
        {
            get
            {
                if (_ctx == null)
                {
                    _ctx = GetDataContext(Altus.Core.Component.App.Instance["Core"]);
                }
                return _ctx;
            }
        }

        public static IMetaDataContext Default
        {
            get
            {
                DeclaredApp app = Context.CurrentContext.CurrentApp;
                return GetDataContext(app);
            }
        }

        protected static IMetaDataContext GetDataContext(DeclaredApp app)
        {
            lock (typeof(DataContext))
            {
                if (app == null) return null;
                IMetaDataContext ctx = App.Instance.Shell.GetComponent<IMetaDataContext>("DataContext:" + app.Name);
                if (ctx == null)
                {
                    Assembly asm = app.PrimaryAssembly;
                    if (asm == null) return Core;
                    Type attrib = Context.CurrentContext.CurrentApp.DeclaredDataContext;
                    if (attrib == null) return Core;
                    ctx = Activator.CreateInstance(attrib, new object[] { app.Name }) as IMetaDataContext;
                    Altus.Core.Component.App.Instance.Shell.Add(ctx, "DataContext:" + ctx.Name);
                }
                return ctx;
            }
        }

        protected static IMetaDataContext CreateMetaContext(string appName)
        {
            lock (typeof(DataContext))
            {
                return (IMetaDataContext)Activator.CreateInstance(
                    TypeHelper.GetType(Context.GetEnvironmentVariable("MetaDataContextType").ToString()), 
                    new object[]{appName});
            }
        }
        #endregion

        #region private

        public DataContext(string name)
        {
            Name = name;
        }

        public void ExecuteScript(string scriptName, params DbParam[] dbParams)
        {
            ExecuteScript(scriptName, new DbState() { Transacted = true }, dbParams);
        }

        public void ExecuteScript(string scriptName, DbState state, params DbParam[] dbParams)
        {
            LockShare share = state.Transacted ? LockShare.Exclusive : LockShare.Shared;
            using (DbLock Lock = this.Connection.CreateConnection(share, LockOperation.ReadWrite))
            {
                Lock.Disposed += Lock_Disposed;
                try
                {
                    IEnumerable<string> commands = this.Connection.GetCommandScripts(scriptName, dbParams);
                    ExecuteInternal(commands, Lock, state);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    if (_log)
                    {
                        ObjectLogWriter.AppendObject(_logPath, ex.ToString());
                    }
                    if (state.Transacted 
                        && Connection.SupportsTransactions
                        && CurrentTransaction != null)
                    {
                        CurrentTransaction.RollbackTransaction();
                    }
                    throw;
                }
            }
        }

        public void ExecuteQuery(string query, params DbParam[] dbParams)
        {
            ExecuteQuery(query, new DbState() { Transacted = true }, dbParams);
        }

        public void ExecuteQuery(string query, DbState state, params DbParam[] dbParams)
        {
            LockShare share = state.Transacted ? LockShare.Exclusive : LockShare.Shared;
            using (DbLock Lock = this.Connection.CreateConnection(share, LockOperation.ReadWrite))
            {
                Lock.Disposed += Lock_Disposed;
                try
                {
                    string[] commands = query.Split('$');
                    this.Connection.ReplaceParams(commands, dbParams);
                    ExecuteInternal(commands, Lock, state);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    if (_log)
                    {
                        ObjectLogWriter.AppendObject(_logPath, ex.ToString());
                    }
                    if (state.Transacted
                        && Connection.SupportsTransactions
                        && CurrentTransaction != null)
                    {
                        CurrentTransaction.RollbackTransaction();
                    }
                    throw;
                }
            }
        }

        void Lock_Disposed(object sender, EventArgs e)
        {
            CommandStack.Clear();
        }

        private void ExecuteInternal(IEnumerable<string> commands, DbLock Lock, DbState state)
        {
            DbCommand cmd = Lock.Connection.CreateCommand();
            
            cmd.CommandType = System.Data.CommandType.Text;

            DataContext.GuidsInserted = new Dictionary<string, string>();

            if (state.Transacted && Connection.SupportsTransactions)
            {
                if (CurrentTransaction == null 
                    || CurrentTransaction.Transaction == null 
                    || CurrentTransaction.IsFinalized)
                {
                    CurrentTransaction = new TransactionContext(cmd.Connection.BeginTransaction());
                }
                cmd.Transaction = CurrentTransaction.Transaction;
                CurrentTransaction.BeginTransaction();
            }
                
            foreach (string cmdText in commands)
            {
                if (!string.IsNullOrEmpty(cmdText))
                {
                    string cmdRaw = this.Connection.PreProcessScript(cmdText);
                    cmd.CommandText = cmdRaw;
                    if (_log)
                    {
                        ObjectLogWriter.AppendObject(_logPath, cmdRaw);
                    }
                    if (!ExecuteCommand(cmd, state, false))
                        break;
                    string cmdPost = this.Connection.PostProcessScript(cmdRaw);
                    if (!string.IsNullOrEmpty(cmdPost))
                    {
                        cmd.CommandText = cmdPost;
                        if (_log)
                        {
                            ObjectLogWriter.AppendObject(_logPath, cmdPost);
                        }
                        if (!ExecuteCommand(cmd, state, true))
                            break;
                    }
                }
            }
            
            if (state.Transacted && Connection.SupportsTransactions)
            {
                CurrentTransaction.CommitTransaction();
            }
        }

        private bool ExecuteCommand(DbCommand command, DbState state, bool isInternal)
        {
            CommandStack.Push(command.CommandText);

            if (CurrentReader != null
                && !CurrentReader.IsClosed)
                CurrentReader.Close();

            using (DbDataReader reader = command.ExecuteReader())
            {
                CurrentReader = reader;
                if (state != null && !isInternal)
                {
                    state.Reader = reader;
                    state.ScriptStep++;
                    if (state.Callback != null)
                    {
                        state.Command = command;
                        state.Callback(state);
                    }
                    if (state.Cancel)
                        return false;
                }
                if (!reader.IsClosed)
                    reader.Close();
            }

            CommandStack.Pop();
            return true;
        }

        [ThreadStatic]
        static DbDataReader _rdr;
        public static DbDataReader CurrentReader
        {
            get
            {
                return _rdr;
            }
            private set
            {
                _rdr = value;
            }
        }

        public static string CurrentCommand
        {
            get
            {
                return _cmdStack.Peek();
            }
        }

        [ThreadStatic]
        static Stack<string> _cmdStack;
        public static Stack<string> CommandStack
        {
            get
            {
                if (_cmdStack == null) _cmdStack = new Stack<string>();
                return _cmdStack;
            }
        }

        [ThreadStatic]
        static Dictionary<string, string> _lastGUID;
        public static Dictionary<string, string> GuidsInserted
        {
            get
            {
                return _lastGUID;
            }
            private set
            {
                _lastGUID = value;
            }
        }

        #endregion

        #region props
        [ThreadStatic]
        private TransactionContext _current;
        public TransactionContext CurrentTransaction
        {
            get
            {
                return _current;
            }
            set
            {
                _current = value;
            }
        }
        public IDbConnection Connection { get; protected set; }
        #endregion

        public void Insert<T>(ref T entity)
        {
            if (!this.TryGetOverloadedInsertOperation<T>(ref entity))
            {
                this.Insert<T>(ref entity, null, null);
            }
        }

        public void Insert<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler)
        {
            Insert<T>(ref entity, parameterBuilderHandler, null);
        }

        public void Insert<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, ScalarWriteStorageCallback<T> executeCallback)
        {
            ExecuteWriteOperation<T>(ref entity, DbOperation.Insert, parameterBuilderHandler, executeCallback);
        }

        public void Update<T>(ref T entity)
        {
            if (!this.TryGetOverloadedUpdateOperation<T>(ref entity))
            {
                Update<T>(ref entity, null, null);
            }
        }

        public void Update<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler)
        {
            Update<T>(ref entity, parameterBuilderHandler, null);
        }

        public void Update<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, ScalarWriteStorageCallback<T> executeCallback)
        {
            ExecuteWriteOperation<T>(ref entity, DbOperation.Update, parameterBuilderHandler, executeCallback);
        }

        public T Get<T>(object filter)
        {
            T entity;
            if (!TryGetOverloadedGetOperation<T>((DynamicWrapper)(!(filter is DynamicWrapper) ? new DynamicWrapper(filter) : filter), out entity))
                return Get<T>(filter, null, null);
            else
                return entity;
        }

        public T Get<T>(object filter, Func<object, DbParam[], DbParam[]> parameterBuilderHandler)
        {
            return Get<T>(filter, parameterBuilderHandler, null);
        }

        public T Get<T>(object filter, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, ScalarReadStorageCallback<T> executeCallback)
        {
            return ExecuteReadOperation<T>(filter, DbOperation.Get, parameterBuilderHandler, executeCallback);
        }

        public void Delete<T>(ref T entity)
        {
            Delete<T>(ref entity, null, null);
        }

        public void Delete<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler)
        {
            Delete<T>(ref entity, parameterBuilderHandler, null);
        }

        public void Delete<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, ScalarWriteStorageCallback<T> executeCallback)
        {
            ExecuteWriteOperation<T>(ref entity, DbOperation.Delete, parameterBuilderHandler, executeCallback);
        }

        public IEnumerable<T> Select<T>(object filter = null)
        {
            filter = filter == null ? new object() : filter;
            IEnumerable<T> list;
            if (!this.TryGetOverloadedSelectOperation<T>((DynamicWrapper)(!(filter is DynamicWrapper) ? new DynamicWrapper(filter) : filter), out list))
                return Select<T>(filter, null, null);
            else return list;
        }

        public IEnumerable<T> Select<T>(object filter, Func<object, DbParam[], DbParam[]> parameterBuilderHandler)
        {
            return Select<T>(filter, parameterBuilderHandler, null);
        }

        public IEnumerable<T> Select<T>(object filter, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, EnumerableReadStorageCallback<T> executeCallback)
        {
            return ExecuteSelectOperation<T>(filter, parameterBuilderHandler, executeCallback);
        }


        protected delegate void ScalarEntityOperation<T>(ref T entity);
        private void ExecuteOperation<T>(ref T entity, DbOperation op, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, ScalarReadStorageCallback<T> callback, string scriptName = null)
        {
            if (scriptName == null)
            {
                if (!this.Connection.CommandScriptExists<T>(entity, op, out scriptName))
                {
                    if (!this.Connection.CreateCommandScript<T>(entity, op))
                        throw (new InvalidOperationException("The current database connection does not support automated script creation."));
                }
            }

            DbParam[] parms;
            if (!this.Connection.CreateCommandParameters<T>(entity, op, out parms))
                throw (new InvalidOperationException(string.Format("The current database connection could create parameter mappings for the {0} command script.", scriptName)));

            if (parameterBuilderHandler != null)
                parms = parameterBuilderHandler(entity, parms);

            DbState state = new DbState()
            {
                StateObject = new { Entity = entity, EntityType = typeof(T), Callback = callback },
                Callback = GetOperationCallback(op)
            };

            this.ExecuteScript(scriptName, state, parms);
            entity = (T)state.StateObject;
        }

        private T ExecuteReadOperation<T>(object entity, DbOperation op, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, ScalarReadStorageCallback<T> callback, string scriptName = null)
        {
            if (scriptName == null)
            {
                if (!this.Connection.CommandScriptExists<T>(entity, op, out scriptName))
                {
                    if (!this.Connection.CreateCommandScript<T>(entity, op))
                        throw (new InvalidOperationException("The current database connection does not support automated script creation."));
                }
            }

            DbParam[] parms;
            if (!this.Connection.CreateCommandParameters<T>(entity, op, out parms))
                throw (new InvalidOperationException(string.Format("The current database connection could create parameter mappings for the {0} command script.", scriptName)));

            if (parameterBuilderHandler != null)
                parms = parameterBuilderHandler(entity, parms);

            DbState state = new DbState()
            {
                StateObject =  new { Filter = entity, EntityType = typeof(T), Callback = callback},
                Callback = GetOperationCallback(op)
            };

            this.ExecuteScript(scriptName, state, parms);
            return (T)state.StateObject;
        }

        private T ExecuteWriteOperation<T>(ref T entity, DbOperation op, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, ScalarWriteStorageCallback<T> callback, string scriptName = null, Func<DbState, bool> onPopulateCallback = null)
        {
            if (onPopulateCallback == null)
                onPopulateCallback = new Func<DbState, bool>(delegate(DbState s) { return s.ScriptStep == 2; });

            if (scriptName == null)
            {
                if (!this.Connection.CommandScriptExists<T>(entity, op, out scriptName))
                {
                    if (!this.Connection.CreateCommandScript<T>(entity, op))
                        throw (new InvalidOperationException("The current database connection does not support automated script creation."));
                }
            }

            DbParam[] parms;
            if (!this.Connection.CreateCommandParameters<T>(entity, op, out parms))
                throw (new InvalidOperationException(string.Format("The current database connection could create parameter mappings for the {0} command script.", scriptName)));

            if (parameterBuilderHandler != null)
                parms = parameterBuilderHandler(entity, parms);

            DbState state = new DbState()
            {
                StateObject = new { Entity = entity, EntityType = typeof(T), Callback = callback, PopulateCallback = onPopulateCallback },
                Callback = GetOperationCallback(op)
            };

            this.ExecuteScript(scriptName, state, parms);
            return (T)((dynamic)state.StateObject).Entity;
        }

        
        private IEnumerable<T> ExecuteSelectOperation<T>(dynamic filter, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, EnumerableReadStorageCallback<T> callback, string scriptName = null)
        {
            if (scriptName == null)
            {
                if (!this.Connection.CommandScriptExists<T>(filter, DbOperation.Select, out scriptName))
                {
                    if (!this.Connection.CreateCommandScript<T>(filter, DbOperation.Select))
                        throw (new InvalidOperationException("The current database connection does not support automated script creation."));
                }
            }

            DbParam[] parms;
            if (!this.Connection.CreateCommandParameters<T>(filter, DbOperation.Select, out parms))
                throw (new InvalidOperationException(string.Format("The current database connection could create parameter mappings for the {0} command script.", scriptName)));

            if (parameterBuilderHandler != null)
                parms = parameterBuilderHandler(filter, parms);

            DbState state = new DbState()
            {
                StateObject = new { Filter = filter, EntityType = typeof(T), Callback = callback},
                Callback = GetOperationCallback(DbOperation.Select) 
            };

            this.ExecuteScript(scriptName, state, parms);
            return (state.StateObject as IEnumerable<T>).ToObservable();
        }

        protected delegate T ScalarReadOperation<T>(object entity);
        protected delegate IEnumerable<T> EnumerableReadOperation<T>(object filter);
        protected delegate void ScalarWriteOperation<T>(ref T entity);

        protected delegate bool CustomScalarReadExecutionOverloadHandler<T>(object filter, out Func<object, DbParam[], DbParam[]> parameterBuilderHandler, out ScalarReadStorageCallback<T> entityBuilderCallback, out string scriptName);
        protected delegate bool CustomEnumerableReadExecutionOverloadHandler<T>(object filter, out Func<object, DbParam[], DbParam[]> parameterBuilderHandler, out EnumerableReadStorageCallback<T> entityBuilderCallback, out string scriptName);
        protected delegate bool CustomScalarWriteExecutionOverloadHandler<T>(ref T entity, out Func<object, DbParam[], DbParam[]> parameterBuilderHandler, out ScalarWriteStorageCallback<T> entityBuilderCallback, out string scriptName, out Func<DbState, bool> populateOnStepCallback);


        private bool TryGetOverloadedInsertOperation<T>(ref T entity)
        {
            ScalarWriteOperation<T> del = null;
            if (TryGetOverloadedInsertHandler<T>(ref entity, out del))
            {
                del(ref entity);
                return true;
            }
            return false;
        }

        private bool TryGetOverloadedInsertHandler<T>(ref T entity, out ScalarWriteOperation<T> del)
        {
            CustomScalarWriteExecutionOverloadHandler<T> builder = null;
            del = null;
            if (TryGetCustomInsertBuilderHandler<T>(ref entity, out builder))
            {
                del = new ScalarWriteOperation<T>(delegate(ref T f)
                {
                    Func<object, DbParam[], DbParam[]> pb;
                    ScalarWriteStorageCallback<T> cb;
                    Func<DbState, bool> onPopulateCallback;
                    string scriptName;
                    builder(ref f, out pb, out cb, out scriptName, out onPopulateCallback);
                    this.ExecuteWriteOperation<T>(ref f, DbOperation.Insert, pb, cb, scriptName, onPopulateCallback);
                });
                return true;
            }
            return false;
        }

        private bool TryGetCustomInsertBuilderHandler<T>(ref T entity, out CustomScalarWriteExecutionOverloadHandler<T> builder)
        {
            builder = null;
            MethodInfo match = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals("OnInsert" + typeof(T).Name)).FirstOrDefault();

            if (match != null)
            {
                builder = CreateScalarWriteExecutionOverloadHandler<T>(match);
            }

            return match != null;
        }

        private bool TryGetOverloadedUpdateOperation<T>(ref T entity)
        {
            ScalarWriteOperation<T> del = null;
            if (TryGetOverloadedUpdateHandler<T>(ref entity, out del))
            {
                del(ref entity);
                return true;
            }
            return false;
        }

        private bool TryGetOverloadedUpdateHandler<T>(ref T entity, out ScalarWriteOperation<T> del)
        {
            CustomScalarWriteExecutionOverloadHandler<T> builder = null;
            del = null;
            if (TryGetCustomUpdateBuilderHandler<T>(ref entity, out builder))
            {
                del = new ScalarWriteOperation<T>(delegate(ref T f)
                {
                    Func<object, DbParam[], DbParam[]> pb;
                    ScalarWriteStorageCallback<T> cb;
                    Func<DbState, bool> onPopulateCallback;
                    string scriptName;
                    builder(ref f, out pb, out cb, out scriptName, out onPopulateCallback);
                    this.ExecuteWriteOperation<T>(ref f, DbOperation.Insert, pb, cb, scriptName);
                });
                return true;
            }
            return false;
        }

        private bool TryGetCustomUpdateBuilderHandler<T>(ref T entity, out CustomScalarWriteExecutionOverloadHandler<T> builder)
        {
            builder = null;
            MethodInfo match = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals("OnIUpdate" + typeof(T).Name)).FirstOrDefault();

            if (match != null)
            {
                builder = CreateScalarWriteExecutionOverloadHandler<T>(match);
            }

            return match != null;
        }

        private bool TryGetOverloadedDeleteOperation<T>(ref T entity)
        {
            ScalarWriteOperation<T> del = null;
            if (TryGetOverloadedDeleteHandler<T>(ref entity, out del))
            {
                del(ref entity);
                return true;
            }
            return false;
        }

        private bool TryGetOverloadedDeleteHandler<T>(ref T entity, out ScalarWriteOperation<T> del)
        {
            CustomScalarWriteExecutionOverloadHandler<T> builder = null;
            del = null;
            if (TryGetCustomDeleteBuilderHandler<T>(ref entity, out builder))
            {
                del = new ScalarWriteOperation<T>(delegate(ref T f)
                {
                    Func<object, DbParam[], DbParam[]> pb;
                    ScalarWriteStorageCallback<T> cb;
                    Func<DbState, bool> onPopulateCallback;
                    string scriptName;
                    builder(ref f, out pb, out cb, out scriptName, out onPopulateCallback);
                    this.ExecuteWriteOperation<T>(ref f, DbOperation.Insert, pb, cb, scriptName);
                });
                return true;
            }
            return false;
        }

        private bool TryGetCustomDeleteBuilderHandler<T>(ref T entity, out CustomScalarWriteExecutionOverloadHandler<T> builder)
        {
            builder = null;
            MethodInfo match = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals("OnDelete" + typeof(T).Name)).FirstOrDefault();

            if (match != null)
            {
                builder = CreateScalarWriteExecutionOverloadHandler<T>(match);
            }

            return match != null;
        }

        private bool TryGetOverloadedGetOperation<T>(DynamicWrapper filter, out T entity)
        {
            ScalarReadOperation<T> del = null;
            entity = default(T);
            if (TryGetOverloadedGetHandler<T>(filter, out del))
            {
                entity = del(filter);
                return true;
            }
            return false;
        }

        private bool TryGetOverloadedSelectOperation<T>(DynamicWrapper filter, out IEnumerable<T> list)
        {
            EnumerableReadOperation<T> del = null;
            list = new List<T>();
            if (TryGetOverloadedSelectHandler<T>(filter, out del))
            {
                list = del(filter);
                return true;
            }
            return false;
        }

        private bool TryGetOverloadedSelectHandler<T>(DynamicWrapper filter, out EnumerableReadOperation<T> del)
        {
            CustomEnumerableReadExecutionOverloadHandler<T> builder = null;
            del = null;
            if (TryGetCustomSelectBuilderHandler<T>(filter, out builder))
            {
                del = new EnumerableReadOperation<T>(delegate(object f)
                    {
                        Func<object, DbParam[], DbParam[]> pb;
                        EnumerableReadStorageCallback<T> cb;
                        string scriptName;
                        builder(f, out pb, out cb, out scriptName);
                        return this.ExecuteSelectOperation<T>(f, pb, cb, scriptName);
                    });
                return true;
            }
            return false;
        }

        private bool TryGetOverloadedGetHandler<T>(DynamicWrapper filter, out ScalarReadOperation<T> del)
        {
            CustomScalarReadExecutionOverloadHandler<T> builder = null;
            del = null;
            if (TryGetCustomGetBuilderHandler<T>(filter, out builder))
            {
                del = new ScalarReadOperation<T>(delegate(object f)
                {
                    Func<object, DbParam[], DbParam[]> pb;
                    ScalarReadStorageCallback<T> cb;
                    string scriptName;
                    builder(f, out pb, out cb, out scriptName);
                    return this.ExecuteReadOperation<T>(f, DbOperation.Get, pb, cb, scriptName);
                });
                return true;
            }
            return false;
        }

        private bool TryGetCustomSelectBuilderHandler<T>(DynamicWrapper filter, out CustomEnumerableReadExecutionOverloadHandler<T> builder)
        {
            builder = null;
            List<object> matches = new List<object>();
            foreach (MethodInfo method in this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals("OnSelect" + typeof(T).Name)).DefaultIfEmpty())
            {
                if (method == null) break;
                int delta;
                bool found = MatchMethodToFilter(method, filter, out delta);
                if (found)
                {
                    matches.Add(new { Method = method, Delta = delta });
                }
            }

            if (matches.Count > 0)
            {
                matches.Sort(delegate(object o1, object o2)
                {
                    return ((dynamic)o1).Delta.CompareTo(((dynamic)o2).Delta);
                });
                builder = CreateEnumerableExecutionOverloadHandler<T>(((dynamic)matches[0]).Method);
            }

            return matches.Count > 0;
        }

        private bool TryGetCustomGetBuilderHandler<T>(DynamicWrapper filter, out CustomScalarReadExecutionOverloadHandler<T> builder)
        {
            builder = null;
            List<object> matches = new List<object>();
            foreach (MethodInfo method in this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals("OnGet" + typeof(T).Name)).DefaultIfEmpty())
            {
                if (method == null) break;
                int delta;
                bool found = MatchMethodToFilter(method, filter, out delta);
                if (found)
                {
                    matches.Add(new { Method = method, Delta = delta });
                }
            }

            if (matches.Count > 0)
            {
                matches.Sort(delegate(object o1, object o2)
                {
                    return ((dynamic)o1).Delta.CompareTo(((dynamic)o2).Delta);
                });
                builder = CreateScalarReadExecutionOverloadHandler<T>(((dynamic)matches[0]).Method);
            }

            return matches.Count > 0;
        }

        private CustomEnumerableReadExecutionOverloadHandler<T> CreateEnumerableExecutionOverloadHandler<T>(MethodInfo method)
        {
            return new CustomEnumerableReadExecutionOverloadHandler<T>(delegate(object filter, out Func<object, DbParam[], DbParam[]> parmBuilder, out EnumerableReadStorageCallback<T> listBuilder, out string scriptName)
                {
                    object[] args = CreateArgArray(method, filter);
                    method.Invoke(this, args);
                    parmBuilder = (Func<object, DbParam[], DbParam[]>)args[args.Length - 3];
                    listBuilder = (EnumerableReadStorageCallback<T>)args[args.Length - 2];
                    scriptName = (string)args[args.Length - 1];
                    return true;
                });
        }

        private object[] CreateArgArray(MethodInfo method, object filter)
        {
            ParameterInfo[] parms = method.GetParameters();
            if (filter is DynamicWrapper) filter = ((DynamicWrapper)filter).BackingInstance;
            object[] args = new object[parms.Length];
            for (int i = 0; i < parms.Length; i++)
            {
                if (!parms[i].IsOut)
                {
                    object value = null;
                    if (!TryGetMemberValue(parms[i].Name, filter, out value))
                    {
                        if (parms[i].ParameterType == typeof(string))
                            value = string.Empty;
                        else if (parms[i].ParameterType == typeof(byte))
                            value = (byte)0;
                        else if (parms[i].ParameterType == typeof(char))
                            value = (char)0;
                        else if (parms[i].ParameterType == typeof(ushort))
                            value = (ushort)0;
                        else if (parms[i].ParameterType == typeof(short))
                            value = (short)0;
                        else if (parms[i].ParameterType == typeof(uint))
                            value = (uint)0;
                        else if (parms[i].ParameterType == typeof(int))
                            value = (int)0;
                        else if (parms[i].ParameterType == typeof(ulong))
                            value = (long)0;
                        else if (parms[i].ParameterType == typeof(float))
                            value = (float)0;
                        else if (parms[i].ParameterType == typeof(double))
                            value = (double)0;
                        else if (parms[i].ParameterType == typeof(decimal))
                            value = (decimal)0;
                        else if (parms[i].ParameterType == typeof(DateTime))
                            value = DateTime.MinValue;
                        else
                            value = Activator.CreateInstance(parms[i].ParameterType);
                    }
                    args[i] = value;
                }
            }
            return args;
        }

        private bool TryGetMemberValue(string memberName, object target, out object value)
        {
            MemberInfo member = target.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals(memberName, StringComparison.InvariantCultureIgnoreCase)
                && (mi.MemberType == MemberTypes.Property || mi.MemberType == MemberTypes.Field)).FirstOrDefault();
            value = null;
            if (member == null) return false;
            if (member.MemberType == MemberTypes.Property)
            {
                value = ((PropertyInfo)member).GetValue(target, null);
            }
            else
            {
                value = ((FieldInfo)member).GetValue(target);
            }
            return true;
        }

        private CustomScalarReadExecutionOverloadHandler<T> CreateScalarReadExecutionOverloadHandler<T>(MethodInfo method)
        {
            return new CustomScalarReadExecutionOverloadHandler<T>(delegate(object filter, out Func<object, DbParam[], DbParam[]> parmBuilder, out ScalarReadStorageCallback<T> listBuilder, out string scriptName)
            {
                object[] args = CreateArgArray(method, filter);
                method.Invoke(this, args);
                parmBuilder = (Func<object, DbParam[], DbParam[]>)args[args.Length - 3];
                listBuilder = (ScalarReadStorageCallback<T>)args[args.Length - 2];
                scriptName = (string)args[args.Length - 1];
                return true;
            });
        }

        private CustomScalarWriteExecutionOverloadHandler<T> CreateScalarWriteExecutionOverloadHandler<T>(MethodInfo method)
        {
            return new CustomScalarWriteExecutionOverloadHandler<T>(
                delegate(ref T entity, out Func<object, DbParam[], DbParam[]> parmBuilder, out ScalarWriteStorageCallback<T> listBuilder, out string scriptName, out Func<DbState, bool> onPopulateCallback)
            {
                object[] args = CreateArgArray(method, entity);
                method.Invoke(this, args);
                parmBuilder = (Func<object, DbParam[], DbParam[]>)args[args.Length - 4];
                listBuilder = (ScalarWriteStorageCallback<T>)args[args.Length - 3];
                scriptName = (string)args[args.Length - 2];
                onPopulateCallback = (Func<DbState, bool>)args[args.Length - 1];
                return true;
            });
        }


        private bool MatchMethodToFilter(MethodInfo method, DynamicWrapper filter, out int delta)
        {
            ParameterInfo[] parms = method.GetParameters();
            List<MemberInfo> members = new List<MemberInfo>();
            delta = 0;

            foreach (MemberInfo member in filter.BackingInstance.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (member is PropertyInfo || member is FieldInfo)
                {
                    members.Add(member);
                }
            }

            int count = 0;
            foreach (ParameterInfo parm in parms)
            {
                if (parm.IsOut) continue;
                if (members.Count(m => (m.Name.Equals(parm.Name, StringComparison.InvariantCultureIgnoreCase)
                    && (MemberType(m) == parm.ParameterType || MemberType(m).IsSubclassOf(parm.ParameterType)))) == 0
                    && !parm.IsOptional)
                    return false;
                count++;
            }
            delta = members.Count - count;
            return delta <= 0;
        }

        private Type MemberType(MemberInfo mi)
        {
            if (mi.MemberType == MemberTypes.Property)
                return ((PropertyInfo)mi).PropertyType;
            else
                return ((FieldInfo)mi).FieldType;
        }

        private DbCallback GetOperationCallback(DbOperation op)
        {
            switch (op)
            {
                case DbOperation.Delete:
                    return new DbCallback(DeleteExecuteHandler);
                case DbOperation.Insert:
                    return new DbCallback(InsertExecuteHandler);
                case DbOperation.Update:
                    return new DbCallback(UpdateExecuteHandler);
                case DbOperation.Select:
                    return new DbCallback(SelectExecuteHandler);
                default:
                    return new DbCallback(GetExecuteHandler);
            }
        }

        private void DeleteExecuteHandler(DbState state)
        {
            if (state.Reader.RecordsAffected == 0)
            {
                throw (new KeyNotFoundException("The underlying storage provider could not find a corresponding item to delete."));
            }
            //state.StateObject = ((dynamic)state.StateObject).Entity;
        }

        private void UpdateExecuteHandler(DbState state)
        {
            if (state.Reader.RecordsAffected == 0)
            {
                throw (new KeyNotFoundException("The underlying storage provider could not find a corresponding item to update."));
            }
            //state.StateObject = ((dynamic)state.StateObject).Entity;
        }

        private void InsertExecuteHandler(DbState state)
        {
            if (((dynamic)state.StateObject).PopulateCallback(state))
            {
                Type entityType = ((dynamic)state.StateObject).EntityType;
                StorageMapping sm = StorageMapping.CreateFromType(entityType);
                Delegate cb = ((dynamic)state.StateObject).Callback;

                object entity = ((dynamic)state.StateObject).Entity;
                if (cb == null)
                {
                    if (state.Reader.HasRows)
                    {
                        OnPopulateEntity(entity, state.Reader, sm);
                    }
                }
                else
                {
                    cb.DynamicInvoke(entity, state);
                }
                state.StateObject = new { Entity = entity, EntityType = entityType, Callback = cb };
            }
        }

        private void GetExecuteHandler(DbState state)
        {
            Type entityType = ((dynamic)state.StateObject).EntityType;
            StorageMapping sm = StorageMapping.CreateFromType(entityType);
            Delegate cb = ((dynamic)state.StateObject).Callback;

            if (state.Reader.HasRows)
            {
                object entity = CreateEntityInstance(entityType);
                if (cb == null)
                {
                    state.StateObject = OnPopulateEntity(entity, state.Reader, sm); ;
                }
                else
                {
                    state.StateObject = cb.DynamicInvoke(entity, state.Reader); ;
                }
            }
            else
            {
                state.StateObject = null;
            }
        }

        protected virtual object OnPopulateEntity(object entity, DbDataReader reader, StorageMapping mapping)
        {
            if (reader.Read())
            {
                foreach (StorageMapping.StorageFieldMapping sfm in mapping.MappedMembers)
                {
                    if (sfm.MemberInfo.DeclaringType != entity.GetType())
                        sfm.MemberInfo = entity.GetType().GetMember(sfm.MemberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
                    sfm.SetValue(entity, reader[sfm.StorageMemberName]);
                }
            }
            else
            {
                return null;
            }
            return entity;
        }

        protected virtual void OnPopulateEntityList(ref IList list, Type entityType, DbDataReader reader, StorageMapping mapping)
        {
            while(true)
            {
                object entity = CreateEntityInstance(entityType);
                entity = OnPopulateEntity(entity, reader, mapping);
                if (entity == null) break;
                list.Add(entity);
            }
        }

        protected virtual void OnPopulateEntityList<T>(ref IList list, DbDataReader reader, StorageMapping mapping)
        {
            this.OnPopulateEntityList(ref list, typeof(T), reader, mapping);
        }

        private void SelectExecuteHandler(DbState state)
        {
            Type entityType = ((dynamic)state.StateObject).EntityType;
            StorageMapping sm = StorageMapping.CreateFromType(entityType);

            Type genList = typeof(List<>).GetGenericTypeDefinition();
            Type entList = genList.MakeGenericType(entityType);

            IList list = Activator.CreateInstance(entList) as IList;
            Delegate cb = ((dynamic)state.StateObject).Callback;
            if (cb == null)
            {
                this.OnPopulateEntityList(ref list, entityType, state.Reader, sm);
            }
            else
            {
                cb.DynamicInvoke(list, state.Reader);
            }

            state.StateObject = list;
        }

        private object CreateEntityInstance(Type entType)
        {
            ConstructorInfo ci = entType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[0], null);
            if (ci == null)
                throw (new InvalidOperationException(string.Format("Entity type {0} does not declare a parameterless constructor.", entType.FullName)));

            return ci.Invoke(null);
        }


        protected override bool OnInitialize(params string[] args)
        {
            List<string> commands = new List<string>();
            Logger.LogInfo("Creating Meta DB Connection");
            Connection = (IDbConnection)Activator.CreateInstance(Context.CurrentContext.CurrentApp.DeclaredDataConnection, new object[] { this });
            Connection.CreateDatabase();
            return true;
        }
    }

    public delegate void DbCallback(DbState state);
    public class DbState
    {
        public DbState() { Transacted = true; }
        public DbCommand Command { get; set; }
        public DbCallback Callback { get; set; }
        public DbDataReader Reader { get; set; }
        public object StateObject { get; set; }
        public bool Cancel { get; set; }
        public int ScriptStep { get; set; }
        public bool Transacted { get; set; }
    }

    public enum DbParamType
    {
        Equals,
        InSetOf
    }

    public class DbParam
    {
        public DbParam() { Type = DbParamType.Equals; }
        public DbParam(string name, object value) { Name = name; Value = value; Type = DbParamType.Equals; }
        public string Name { get; set; }
        public object Value { get; set; }

        public DbParamType Type { get; set; }

        public string ReplaceInCommand(string commandText)
        {
            if (Type == DbParamType.Equals)
                return commandText.Replace(String.Format("@{0}", Name), GetEncodedValue());
            else
                return commandText.Replace(String.Format("@{0}", Name), GetEncodedValueIn());
        }

        private string GetEncodedValue()
        {
            if (Value == null)
            {
                return "null";
            }
            else if (Value is bool)
            {
                return ((bool)Value ? "1" : "0");
            }
            else if (Value is string)
            {
                return String.Format("'{0}'", Value.ToString());
            }
            else if (Value is DateTime)
            {
                return String.Format("'{0}'", ((DateTime)Value).ToString("s")); // ISO8601 format
            }
            else if (Value is string[])
            {
                string list = null;
                foreach (string s in (string[])Value)
                {
                    if (list != null)
                        list += ", ";
                    list += String.Format("'{0}'", s);
                }
                return list;
            }
            else if (Value is Array)
            {
                string list = null;
                foreach (object s in (Array)Value)
                {
                    if (list != null)
                        list += ", ";
                    if (s is bool)
                    {
                        list += ((bool)Value ? "1" : "0");
                    }
                    else if (s is DateTime)
                    {
                        list += String.Format("'{0}'", ((DateTime)Value).ToString("s")); // ISO8601 format
                    }
                    else
                        list += s;
                }
                return list;
            }
            else
            {
                return Value.ToString();
            }
        }

        private string GetEncodedValueIn()
        {
            if (Value == null)
            {
                return "null";
            }
            else if (Value is bool)
            {
                return ((bool)Value ? "1" : "0");
            }
            else if (Value is string)
            {
                return String.Format("'{0}'", Value.ToString());
            }
            else if (Value is DateTime)
            {
                return String.Format("'{0}'", ((DateTime)Value).ToString("s")); // ISO8601 format
            }
            else if (Value is string[])
            {
                string list = null;
                foreach (string s in (string[])Value)
                {
                    if (list != null)
                        list += ", ";
                    list += String.Format("{0}", s);
                }
                return "'" + list + "'";
            }
            else if (Value is Array)
            {
                string list = null;
                foreach (object s in (Array)Value)
                {
                    if (list != null)
                        list += ", ";
                    if (s is bool)
                    {
                        list += ((bool)Value ? "1" : "0");
                    }
                    else if (s is DateTime)
                    {
                        list += String.Format("{0}", ((DateTime)Value).ToString("s")); // ISO8601 format
                    }
                    else
                        list += s;
                }
                return "'" + list + "'";
            }
            else
            {
                return Value.ToString();
            }
        }
    }
}
