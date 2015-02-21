using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Altus.Core.Threading;

namespace Altus.Core.Data
{
    public enum DbOperation
    {
        Insert,
        Update,
        Get,
        Delete,
        Select
    }

    public interface IDbConnection 
    {
        string ConnectionString { get; }
        IDbConnectionManager ConnectionManager { get; }
        bool DatabaseExists { get; }
        bool SupportsTransactions { get; }
        IEnumerable<string> GetCommandScripts(string name, params DbParam[] parms);
        bool CommandScriptExists(string name);
        bool CommandScriptExists<T>(object entity, DbOperation operation, out string scriptName);
        bool CreateCommandScript<T>(object entity, DbOperation operation);
        bool CreateCommandParameters<T>(object entity, DbOperation operation, out DbParam[] parms);
        bool CreateDatabase();
        DbLock CreateConnection();
        DbLock CreateConnection(LockShare share, LockOperation operation);
        IMetaDataContext DataContext { get; }
        string GetLastInsertIdCommandScript();
        string PreProcessScript(string script);
        string PostProcessScript(string script);
        void ReplaceParams(string[] scripts, IEnumerable<DbParam> dbParams);
    }
}
