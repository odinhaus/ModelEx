using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core.Data
{
    public interface IReplicationManager : IComponent
    {
        string DbSyncToken { get; }
    }
}
