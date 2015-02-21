using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Messaging.Udp;
using System.Net;
using Altus.Core.Messaging;
using Altus.Core.Processing;
using Altus.Core.Security;
using Altus.Core.Serialization;

namespace Altus.Core.PubSub
{
    public class MulticastSubscriberProxy : ISubscriberProxy
    {
        public MulticastSubscriberProxy(ReflectedPublicationDefinition pubDef)
        {
            this.Publication = pubDef;
            this.Subscription = new Subscription(new Subscriber(NodeIdentity.NodeAddress, this), pubDef.Topic, pubDef.Publisher);
        }

        private void CreateConnection()
        {
            MulticastConnection connection = new MulticastConnection(this.Publication.Topic.MulticastEndPoint, false);
            this.Connection = connection;
        }

        public ReflectedPublicationDefinition Publication
        {
            get;
            private set;
        }

        public Subscription Subscription { get; private set; }
        private IConnection Connection { get; set; }

        public void PublishData(params Processing.ServiceParameter[] parameters)
        {
            ServiceOperation.Publish(this.Publication, parameters);
        }

        public void PublishError(params Processing.ServiceParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public bool RequestDefinition()
        {
            return true;
        }
    }
}
