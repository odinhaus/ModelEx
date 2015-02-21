using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Messaging;
using Altus.Core.Pipeline;
using System.Reflection;
using Altus.Core;
using Altus.Core.Serialization;
using Altus.Core.Component;
using Altus.Core.Diagnostics;
using Altus.Core.Streams;
using Altus.Core.Messaging.Http;

namespace Altus.Core.Processing.Rpc
{
    public class RpcOperationProxy : ServiceOperationProxy
    {
        public RpcOperationProxy(Message message, ServiceOperationAttribute attrib, IConnection connection) : base(message, attrib, connection) { }

        protected override object OnProcess(ServiceContext request, object target, object[] args)
        {
            object ret = base.OnProcess(request, target, args);
            ((RpcContext)this.ServiceContext).Response.OperationResponse = 
                OnBuildResponse(ret, args, Attribute.Method.ReturnType.Equals(typeof(void)));
            return ret;
        }

        protected override Pipeline.IPipeline<ServiceContext> OnCreatePipeline()
        {
            return new RpcPipeline(this);
        }

        protected override ServiceContext OnCreateServiceContext(Message message, ServiceOperation operation, ServiceOperationAttribute attrib, IPipeline<ServiceContext> pipeline, IConnection connection)
        {
            return new RpcContext(message, operation, attrib, (RpcPipeline)pipeline, connection, new RpcRequest(), new RpcResponse());
        }
    }
}