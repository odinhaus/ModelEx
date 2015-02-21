using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using Altus.Core.Threading;

namespace Altus.Core.Data
{
    public interface IDbConnectionManager : IComponent
    {
        IDbConnection Connection { get;}
        DbLock CreateConnection();
        DbLock CreateConnection(LockShare share, LockOperation operation);
        void StartReplication();
        void StopReplication();
        void UpdateSchema();
    }
}
