using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Pipeline;
using Altus.Core.Messaging;
using Altus.Core.Processing.Msp;
using Altus.Core.Processing.Rpc;
using System.IO;


[assembly: Component(
    ComponentType = typeof(Altus.Core.Processing.ServiceContextFactory),
    Name = "ServiceContextFactory")]

namespace Altus.Core.Processing
{
    public class ServiceContextFactory : InitializableComponent
    {
        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        public bool CanProcess(Message request)
        {
            return request.MessageType == 0 | request.MessageType == 1;
        }

        public bool CreateContext(Message request, IConnection connection, out ServiceContext context)
        {
            ServiceEndPointManager sem = App.Instance.Shell.GetComponent<ServiceEndPointManager>();
            MemoryStream ms = new MemoryStream();
            connection.ConnectionAspects.Add("InputStream", ms);
            ServiceOperationProxy proxy = sem.GetProxy(request, connection);
            context = proxy.ServiceContext;
            if (context != null)
            {
                //context.Action = request.Action;
                context.CorrelationId = request.CorrelationId;
                context.DeliveryGuaranteed = request.DeliveryGuaranteed;
                context.Id = request.Id;
                context.Recipients = request.Recipients;
                context.Sender = request.Sender;
                context.Timestamp = request.Timestamp;
                context.ServiceType = request.ServiceType;
            }
            return context != null;
        }

        public int Priority
        {
            get { return 1; }
        }
    }
}
