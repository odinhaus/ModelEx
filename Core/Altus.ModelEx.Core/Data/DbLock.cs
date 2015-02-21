using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Altus.Core.Threading;

namespace Altus.Core.Data
{
    public abstract class DbLock : IDisposable
    {
        [ThreadStatic]
        static Stack<DbLock> Locks;
        [ThreadStatic]
        static DbConnection _cn;
        public DbConnection Connection { get { return _cn; } private set { _cn = value; } }
        [ThreadStatic]
        static SharedLock.SharedLockContext _lc;
        public SharedLock.SharedLockContext LockContext { get { return _lc; } private set { _lc = value; } }

        public DbLock(string connectionString, SharedLock.SharedLockGlobals lockGlobals) 
            : this(connectionString, lockGlobals, LockShare.Shared, LockOperation.ReadWrite)
        {
            
        }

        public DbLock(string connectionString, SharedLock.SharedLockGlobals lockGlobals, LockShare share, LockOperation operation)
        {
            if (Locks == null)
                Locks = new Stack<DbLock>();
            if (Connection == null)
            {
                DbConnection dbc = OnCreateConnection(connectionString);
                if (dbc.State == System.Data.ConnectionState.Broken
                || dbc.State == System.Data.ConnectionState.Closed)
                {
                    OnOpenConnection(dbc);
                }
                Connection = dbc;
                LockContext = SharedLock.Lock(lockGlobals, share, operation);
            }
            Locks.Push(this);
        }

        protected abstract DbConnection OnCreateConnection(string connectionString);
        protected virtual void OnOpenConnection(DbConnection connection)
        {
            connection.Open();
        }

        public bool IsDisposed { get; private set; }
        public event EventHandler Disposed;
        public void Dispose()
        {
            DbLock aLock = Locks.Pop();
            if (Locks.Count == 0)
            {
                try
                {
                    Connection.Dispose();
                }
                catch { }
                try
                {
                    LockContext.Dispose();
                }
                catch { }
                Connection = null;
                try
                {
                    if (Disposed != null)
                        Disposed(this, new EventArgs());
                }
                catch { }
            }
            aLock.IsDisposed = true;
        }

        public static DbLock Current { get { return Locks == null || Locks.Count == 0 ? null : Locks.Peek(); } }

    }
}
