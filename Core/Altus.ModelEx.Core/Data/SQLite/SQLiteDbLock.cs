using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Altus.Core.Threading;

namespace Altus.Core.Data.SQlite
{
    public class SQLiteDbLock : DbLock
    {
        //[DllImport("System.Data.SQLite.dll", CallingConvention = CallingConvention.Cdecl)]
        //public static extern int sqlite3_enable_shared_cache(int enable);
        static SQLiteDbLock()
        {
            //sqlite3_enable_shared_cache(1);
        }

        public SQLiteDbLock(string connectionString, SharedLock.SharedLockGlobals globalLock)
            : base(connectionString, globalLock)
        {}

        public SQLiteDbLock(string connectionString, SharedLock.SharedLockGlobals globalLock, LockShare share, LockOperation operation)
            : base(connectionString, globalLock, share, operation)
        { }

        protected override System.Data.Common.DbConnection OnCreateConnection(string connectionString)
        {
            SQLiteConnection conn = new SQLiteConnection(connectionString);
            return conn;
        }

        protected override void OnOpenConnection(System.Data.Common.DbConnection connection)
        {
            base.OnOpenConnection(connection);
            SQLiteCommand cmd = new SQLiteCommand("PRAGMA read_uncommitted = 1;", (SQLiteConnection)connection);
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.ExecuteNonQuery();
        }
    }
}
