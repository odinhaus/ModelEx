using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Altus.Core.Component;
using Altus.Core.Data.SQlite;
using Altus.Core.Processing;
using Altus.Core.Processing.Rpc;
using Altus.Core.Security;
using Altus.Core.Threading;

namespace Altus.Core.Data.SQlite
{
    [RpcEndPoint("*://*:*/*/Meta")]
    public class MetaDataContextConnectionManager : InitializableComponent, IDbConnectionManager
    {
        Thread _replThread;
        SharedLock.SharedLockGlobals _lockDB ;

        public MetaDataContextConnectionManager(IDbConnection dbConnection) 
        {
            this.Connection = dbConnection;
            _lockDB = SharedLock.CreateGlobals(dbConnection.DataContext.Name);
        }

        public IDbConnection Connection { get; private set; }

        public DbLock CreateConnection()
        {
            return CreateConnection(LockShare.Shared, LockOperation.ReadWrite);
        }
        [ThreadStatic]
        static DbLock _lock;
        public DbLock CreateConnection(LockShare share, LockOperation operation)
        {
            if (_lock == null || _lock.IsDisposed)
            {
                _lock = new SQLiteDbLock(this.Connection.ConnectionString, _lockDB, share, operation);
                _lock.Disposed += _lock_Disposed;
                return _lock;
            }
            else
            {
                return new SQLiteDbLock(this.Connection.ConnectionString, _lockDB, share, operation); // increments the lock stack
            }
        }

        void _lock_Disposed(object sender, EventArgs e)
        {
            _lock = null;
        }

        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        //ReplicationManager _mgr;
        public void UpdateSchema()
        {
            //_mgr = new ReplicationManager(this);
            //_mgr.UpdateSchema();
        }

        public void StartReplication()
        {
            //if (Altus.Instance != null 
            //    && Altus.Instance.Shell != null
            //    && Altus.Instance.Shell.GetComponent(_mgr.GetType()) == null)
            //    Altus.Instance.Shell.Add(_mgr);
        }

        public void StopReplication()
        {
            //ReplicationManager mgr = Altus.Instance.Shell.GetComponent<ReplicationManager>();
            //mgr.Kill();
            //if (Altus.Instance != null && Altus.Instance.Shell != null)
            //    Altus.Instance.Shell.Remove(mgr);
        }
    }
}
