using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Serialization;

namespace Altus.Core.Processing.Rpc
{
    public class RpcOperationAttribute : ServiceOperationAttribute
    {
        public RpcOperationAttribute(string operationName) : base(StandardFormats.PROTOCOL_DEFAULT, operationName) { }
        public RpcOperationAttribute(string operationName, Delegate handler) : base(StandardFormats.PROTOCOL_DEFAULT, operationName, handler) { }
    }
}
