using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Messaging;
using Altus.Core.Component;
using Altus.Core.Exceptions;

namespace Altus.Core.Processing.Rpc
{
    public class RpcContext : ServiceContext<RpcRequest, RpcResponse>
    {
        public RpcContext(Message message, ServiceOperation operation, ServiceOperationAttribute attrib, RpcPipeline pipeline, IConnection connection, RpcRequest request, RpcResponse response)
            : base()
        {
            this.Message = message;
            this.ProcessingContext = pipeline;
            this.Request = request;
            this.Response = response;
            //this.Action = operation.Action;
            this.Connection = connection;
            this.ResponseFormat = operation.Format;
            //this.ResponseUri = operation.ResponseUri;
            this.SetServiceUri(operation.ServiceUri);
            this.Operation = operation;
            this.StatusCode = 200;
        }

        protected override void OnDisposeManagedResources()
        {
            this.Message = null;
            this.ProcessingContext = null;
            this.Request = null;
            this.Response = null;
            this.Connection = null;
            this.Operation = null;
            base.OnDisposeManagedResources();
        }

        protected ServiceOperation Operation { get; set; }

        protected override object OnSetRequestMessagePayload()
        {
            return this.Request.ServiceOperation;
        }

        protected override object OnSetResponseMessagePayload()
        {
            return this.Response.OperationResponse;
        }

        protected override void OnUnhandledException(object sender, Exception exception)
        {
            this.StatusCode = 500;
            if (this.Response.OperationResponse == null)
            {
                this.Response.OperationResponse = new ServiceOperation(OperationType.Response, this.Request.ServiceOperation.ServiceType, this.Operation.ServiceUri);
            }
            this.Response.OperationResponse.Parameters.Add(new ServiceParameter("Error", exception.GetType().FullName, ParameterDirection.Error) { Value = new RuntimeException(exception) });
        }

        protected override List<KeyValuePair<string, string>> OnSetResponseHeaders()
        {
            return new List<KeyValuePair<string, string>>();
        }
    }
}
