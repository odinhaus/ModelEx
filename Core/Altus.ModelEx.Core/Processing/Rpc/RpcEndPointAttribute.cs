using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Processing.Rpc
{
    public class RpcEndPointAttribute : ServiceEndPointAttribute
    {
        public RpcEndPointAttribute(params string[] routes) : base(typeof(RpcOperationProxy), ServiceTypes.RPC, routes) { }
    }
}
