using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Messaging;

namespace Altus.Core.Processing.Rpc
{
    public class RpcResponse : ServiceResponse
    {
        public ServiceOperation OperationResponse { get; set; }
    }
}
