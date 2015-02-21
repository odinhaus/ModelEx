using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.topology;
using Altus.messaging.udp;
using Altus.data;
using System.Net;
using Altus.serialization;
using Altus.processing;
using Altus.security;
using Altus.messaging.tcp;
using Altus.messaging.http;

namespace Altus.messaging
{
    public class MessagingClient
    {
        public MessagingClient()
        {

        }

        /// <summary>
        /// Send the provided data values to the specified NodeAddress using BestEffort delivery
        /// </summary>
        /// <param name="values"></param>
        public void Send(string nodeAddress, ServiceOperation operation)
        {
            string serviceUri;

            

            Message msg = new Message(StandardFormats.BINARY, operation.ServiceUri, operation.ServiceType, NodeIdentity.NodeAddress, processing.Action.POST);
            msg.Parameters.Add(new ServiceParameter("operation", operation.GetType().FullName, ParameterDirection.In) { Value = operation });
            

            if (msg.ServiceType == ServiceType.RequestResponse)
            {
                Message response = connection.Request(msg);
            }
            else
            {
                connection.Send(msg);
            }
        }
    }
}
