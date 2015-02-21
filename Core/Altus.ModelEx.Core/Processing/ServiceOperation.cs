using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Security;
using System.Net;
using Altus.Core.Entities;
using Altus.Core.Serialization;
using System.IO;
using Altus.Core.Messaging;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;
using Altus.Core.Data;
using Altus.Core.PubSub;
using Altus.Core.Topology;
using Altus.Core.Messaging.Udp;
using Altus.Core.Messaging.Tcp;
using Altus.Core.Messaging.Http;
using Altus.Core;
using Altus.Core.Streams;
using Altus.Core.Serialization.Binary;
using Altus.Core.Net;

namespace Altus.Core.Processing
{
    public class Protocols
    {
        public const string TCP = "tcp";
        public const string UDP = "udp";
        public const string HTTP = "http";
    }

    public enum OperationType
    {
        Request,
        Response
    }
    [DataContract()]
    [System.Serializable]
    public class ServiceOperation : AbstractEntity
    {
        protected ServiceOperation() 
        {
            this.Parameters = new ServiceParameterCollection();
        }

        public ServiceOperation(OperationType type, ServiceType serviceType, string serviceUri, params ServiceParameter[] args)
        {
            this.ServiceType = serviceType;
            this.ParseUri(serviceUri);
            this.Parameters = new ServiceParameterCollection(args);
            this.Type = type;
            try
            {
                this.EndPoint = DataContext.Default.GetNodeEndPoint(this.NodeAddress, this.Protocol);
            }
            catch
            {
                this.EndPoint = new IPEndPoint(string.IsNullOrEmpty(this.NodeAddress) ?  IPAddress.Any : IPAddress.Parse(this.NodeAddress), this.Port);
            }
        }

        public ServiceOperation(OperationType type,
            ServiceType serviceType,
            string protocol, 
            string nodeAddress, 
            string format, 
            string application, 
            string objectPath, 
            string operation, 
            params ServiceParameter[] args)
        {
            this.ServiceType = serviceType;
            this.Type = type;
            Parameters = new ServiceParameterCollection(args);
            IPEndPoint ep;
            if (nodeAddress.TryParseEndPoint(out ep))
            {
                this.NodeAddress = ep.Address.ToString();
                this.EndPoint = ep;
            }
            else
            {
                this.NodeAddress = nodeAddress;
                this.EndPoint = DataContext.Default.GetNodeEndPoint(this.NodeAddress, this.Protocol);
            }
            this.Operation = operation;
            this.Format = format;
            this.ObjectPath = objectPath;
            this.Application = application;
            this.Protocol = protocol;
            
            this.Port = ((IPEndPoint)this.EndPoint).Port;
        }

        public ServiceOperation(OperationType type,
            ServiceType serviceType,
            DeliveryOption delivery,
            string nodeAddress,
            string format,
            string application,
            string objectPath,
            string operation,
            params ServiceParameter[] args)
        {
            this.ServiceType = serviceType;
            this.Type = type;
            Parameters = new ServiceParameterCollection(args);

            this.NodeAddress = nodeAddress;
            this.Operation = operation;
            this.Format = format;
            this.ObjectPath = objectPath;
            this.Application = application;
            Dictionary<string, IPEndPoint> endPoints = new Dictionary<string, IPEndPoint>();

            if (nodeAddress != "*"
                && delivery != DeliveryOption.MulticastBestEffort)
            {
                try
                {
                    endPoints.Add(Protocols.TCP, DataContext.Default.GetNodeEndPoint(nodeAddress, Protocols.TCP));
                }
                catch { }
                try
                {
                    endPoints.Add(Protocols.UDP, DataContext.Default.GetNodeEndPoint(nodeAddress, Protocols.UDP));
                }
                catch { }
                try
                {
                    endPoints.Add(Protocols.HTTP, DataContext.Default.GetNodeEndPoint(nodeAddress, Protocols.HTTP));
                }
                catch { }
            }

            if (delivery == DeliveryOption.Guaranteed)
            {
                if (endPoints.ContainsKey(Protocols.TCP))
                {
                    this.Protocol = Protocols.TCP;
                    this.NodeAddress = endPoints[Protocols.TCP].Address.ToString();
                    this.Port = endPoints[Protocols.TCP].Port;
                }
                else if (endPoints.ContainsKey(Protocols.HTTP))
                {
                    this.Protocol = Protocols.HTTP;
                    this.NodeAddress = endPoints[Protocols.HTTP].Address.ToString();
                    this.Port = endPoints[Protocols.HTTP].Port;
                }
                else
                {
                    throw (new ProtocolViolationException("The specified node does not support a protocol with guaranteed delivery"));
                }
                try
                {
                    this.EndPoint = DataContext.Default.GetNodeEndPoint(this.NodeAddress, this.Protocol);
                }
                catch
                {
                    this.EndPoint = new IPEndPoint(Dns.GetHostAddresses(this.NodeAddress).Where(ipa => !IPAddress.IsLoopback(ipa)).First(), this.Port);
                }
            }
            else if (delivery == DeliveryOption.BestEffort)
            {
                if (endPoints.ContainsKey(Protocols.UDP))
                {
                    this.Protocol = Protocols.UDP;
                    this.NodeAddress = endPoints[Protocols.UDP].Address.ToString();
                    this.Port = endPoints[Protocols.UDP].Port;
                }
                else if (endPoints.ContainsKey(Protocols.TCP))
                {
                    this.Protocol = Protocols.TCP;
                    this.NodeAddress = endPoints[Protocols.TCP].Address.ToString();
                    this.Port = endPoints[Protocols.TCP].Port;
                }
                else if (endPoints.ContainsKey(Protocols.HTTP))
                {
                    this.Protocol = Protocols.HTTP;
                    this.NodeAddress = endPoints[Protocols.HTTP].Address.ToString();
                    this.Port = endPoints[Protocols.HTTP].Port;
                }
                else
                {
                    throw (new ProtocolViolationException("The specified node does not support an active listener on any protocol"));
                }
                try
                {
                    this.EndPoint = DataContext.Default.GetNodeEndPoint(this.NodeAddress, this.Protocol);
                }
                catch
                {
                    this.EndPoint = new IPEndPoint(Dns.GetHostAddresses(this.NodeAddress).Where(ipa => !IPAddress.IsLoopback(ipa)).First(), this.Port);
                }
            }
            else
            {
                //Topic topic = operation;
                //this.Protocol = Protocols.UDP;
                //this.Port = topic.MulticastPort;
                //this.EndPoint = new IPEndPoint(IPAddress.Parse(topic.MulticastIP), topic.MulticastPort);
                throw new NotSupportedException();
            }
        }

        public ServiceOperation(OperationType type,
            ServiceType serviceType,
            DeliveryOption delivery,
            string protocol,
            string nodeAddress,
            int port,
            string format,
            string application,
            string objectPath,
            string operation,
            params ServiceParameter[] args)
        {

        }

        public ServiceOperation(Message request, OperationType type)
        {
            this.ServiceType = request.ServiceType;
            this.ParseUri(request.ServiceUri);
            this.Parameters = new ServiceParameterCollection();
            this.Type = type;
            try
            {
                this.EndPoint = new IPEndPoint(Dns.GetHostAddresses(this.NodeAddress)[0], this.Port);
            }
            catch
            {
                this.EndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), this.Port);
            }
        }

        private void ParseUri(string serviceUri)
        {
            string protocol, node, application, objectPath, operation, format;
            int port;

            if (ServiceEndPointManager.TryParseServiceUri(serviceUri, out protocol, out node, out port, out application, out objectPath, out operation, out format))
            {
                this.Protocol = protocol;
                this.NodeAddress = node;
                this.ObjectPath = objectPath;
                this.Operation = operation;
                this.Port = port;
                this.Application = application;
                this.Format = format;
            }
        }

        [DataMember()]
        [BinarySerializable(1)]
        public string ObjectPath { get; protected set; }

        [DataMember()]
        [BinarySerializable(2)]
        public string NodeAddress { get; protected set; }

        [DataMember()]
        [BinarySerializable(3)]
        public string Operation { get; protected set; }

        [DataMember()]
        [BinarySerializable(4)]
        public string Protocol { get; protected set; }

        [DataMember()]
        [BinarySerializable(5)]
        public string Format { get; protected set; }

        [DataMember()]
        [BinarySerializable(10, SerializationType=typeof(IList<ServiceParameter>))]
        public ServiceParameterCollection Parameters { get; protected set; }

        [DataMember()]
        [BinarySerializable(6)]
        public int Port { get; protected set; }

        [DataMember()]
        [BinarySerializable(8)]
        public OperationType Type { get; protected set; }

        [DataMember()]
        [BinarySerializable(9)]
        public string Application { get; protected set; }
        [ScriptIgnore()]
        public EndPoint EndPoint { get; protected set; }

        [BinarySerializable(10)]
        public ServiceType ServiceType { get; protected set; }

        public string ServiceUri
        {
            get
            {
                string rootUri = string.Format("{0}://{1}:{5}/{2}/{3}({4})[{6}]",
                    this.Protocol,
                    this.NodeAddress,
                    this.Application,
                    this.ObjectPath,
                    this.Operation,
                    this.Port,
                    this.Format);
                string args = "";
                if (this.Parameters.Count > 0)
                {
                    foreach (ServiceParameter sp in Parameters)
                    {
                        if (!string.IsNullOrEmpty(args))
                            args += "&";
                        if (sp.Value == null)
                            args += sp.Name + "=<null>";
                        else
                            args += sp.Name + "=" + sp.Value.ToString();
                    }
                    args = "?" + args;
                }
                return rootUri + args;
            }
        }


        private static IConnection CreateConnection(ServiceOperation operation)
        {
            IConnection connection;
            if (operation.Protocol == "udp")
            {
                if (DataContext.Default.CheckIsMulticast((IPEndPoint)operation.EndPoint))
                {
                    connection = ConnectionManager.CreateMulticastConnection((IPEndPoint)operation.EndPoint, false);
                }
                else
                {
                    connection = new UdpConnection((IPEndPoint)operation.EndPoint);
                }
            }
            else if (operation.Protocol == "tcp")
            {
                connection = TcpConnection.Create((IPEndPoint)operation.EndPoint);
            }
            else
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(operation.ServiceUri);
                req.Method = "POST";
                connection = new HttpConnection(req, (IPEndPoint)operation.EndPoint);
            }
            return connection;
        }

        /// <summary>
        /// Calls the specified serviceUri using the supplied parameters and timeout, and return the results of the call
        /// </summary>
        /// <param name="serviceUri"></param>
        /// <param name="timeout"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public static ServiceOperation Call(string serviceUri, TimeSpan timeout, params ServiceParameter[] parms)
        {
            ServiceOperation op = new ServiceOperation();
            op.ParseUri(serviceUri);
            op.EndPoint = new IPEndPoint(Dns.GetHostAddresses(op.NodeAddress).Where(ipa => !IPAddress.IsLoopback(ipa)).First(), op.Port);
            op.Parameters.AddRange(parms);
            op.ServiceType = ServiceType.RequestResponse;
            return Call(op, timeout);
        }

        /// <summary>
        /// Calls the specified method on the specified nodeAddress/application/object, using the supplied parameters, and returns the results
        /// serialized in the supplied format in a ServiceOperation instance.
        /// </summary>
        /// <param name="nodeAddress"></param>
        /// <param name="application"></param>
        /// <param name="objectPath"></param>
        /// <param name="operation"></param>
        /// <param name="format"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public static ServiceOperation Call(string nodeAddress, string application, string objectPath, string operation, string format, params ServiceParameter[] parms)
        {
            return Call(nodeAddress, application, objectPath, operation, format, TimeSpan.FromSeconds(30), parms);
        }

        /// <summary>
        /// Calls the specified method on the specified nodeAddress/application/object, using the supplied parameters, and returns the results
        /// serialized in the supplied format in a ServiceOperation instance.
        /// </summary>
        /// <param name="nodeAddress"></param>
        /// <param name="application"></param>
        /// <param name="objectPath"></param>
        /// <param name="operation"></param>
        /// <param name="format"></param>
        /// <param name="timespan">the time to wait for a response</param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public static ServiceOperation Call(string nodeAddress, string application, string objectPath, string operation, string format, TimeSpan timespan, params ServiceParameter[] parms)
        {
            ServiceOperation op = new ServiceOperation(
                OperationType.Request,
                ServiceType.RequestResponse,
                DeliveryOption.Guaranteed,
                nodeAddress,
                format,
                application,
                objectPath,
                operation,
                parms);

            return Call(op, timespan);

        }

        public static ServiceOperation Call(ServiceOperation operation, TimeSpan timespan)
        {
            IConnection connection = CreateConnection(operation);
            Message msg = new Message(operation);

            Message resp = connection.Call(msg, timespan);
            connection.Dispose();

            ISerializer serializer = Altus.Core.Component.App.Instance.Shell.GetComponents<ISerializer>().Where(
                s => s.SupportsFormat(resp.PayloadFormat) && s.SupportsType(TypeHelper.GetType(resp.PayloadType))).FirstOrDefault();
            if (serializer == null) throw (new Altus.Core.Serialization.SerializationException("Deserializer for " + resp.PayloadType + " in " + resp.PayloadFormat + " format could not be found."));
            object value = serializer.Deserialize(StreamHelper.GetBytes(resp.PayloadStream), TypeHelper.GetType(resp.PayloadType));
            if (value is ServiceOperation)
            {
                return value as ServiceOperation;
            }
            else if (value is ServiceParameterCollection)
            {
                ServiceOperation so = new ServiceOperation(resp, OperationType.Response);
                so.Parameters.AddRange(value as ServiceParameterCollection);
                return so;
            }
            else
                throw (new InvalidOperationException("Return type not supported"));
        }

        /// <summary>
        /// Calls the specified method on the specified nodeAddress/application/object using the designated protocol, using the supplied parameters, and returns the results
        /// serialized in the supplied format in a ServiceOperation instance.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="nodeAddress"></param>
        /// <param name="application"></param>
        /// <param name="objectPath"></param>
        /// <param name="operation"></param>
        /// <param name="format"></param>
        /// <param name="timespan"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public static ServiceOperation Call(string protocol, string nodeAddress, string application, string objectPath, string operation, string format, TimeSpan timespan, params ServiceParameter[] parms)
        {
            ServiceOperation op = new ServiceOperation(
                OperationType.Request,
                ServiceType.RequestResponse,
                protocol,
                nodeAddress,
                format,
                application,
                objectPath,
                operation,
                parms);

            IConnection connection = CreateConnection(op);
            Message msg = new Message(op);

            Message resp = connection.Call(msg, timespan);
            connection.Dispose();

            ISerializer serializer = Altus.Core.Component.App.Instance.Shell.GetComponents<ISerializer>().Where(
                s => s.SupportsFormat(resp.PayloadFormat) && s.SupportsType(TypeHelper.GetType(resp.PayloadType))).FirstOrDefault();
            if (serializer == null) throw (new Altus.Core.Serialization.SerializationException("Deserializer for " + resp.PayloadType + " in " + resp.PayloadFormat + " format could not be found."));
            object value = serializer.Deserialize(StreamHelper.GetBytes(resp.PayloadStream), TypeHelper.GetType(resp.PayloadType));
            if (value is ServiceOperation)
            {
                return value as ServiceOperation;
            }
            else if (value is ServiceParameterCollection)
            {
                ServiceOperation so = new ServiceOperation(resp, OperationType.Response);
                so.Parameters.AddRange(value as ServiceParameterCollection);
                return so;
            }
            else
                throw (new InvalidOperationException("Return type not supported"));
        }

        /// <summary>
        /// Calls the specified method on the specified nodeAddress/application/object, using the supplied parameters, and returns the results in a ServiceOperation instance.
        /// </summary>
        /// <param name="nodeAddress"></param>
        /// <param name="application"></param>
        /// <param name="objectPath"></param>
        /// <param name="operation"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public static ServiceOperation Call(string nodeAddress, string application, string objectPath, string operation, params ServiceParameter[] parms)
        {
            return Call(nodeAddress, application, objectPath, operation, StandardFormats.BINARY, parms);
        }

        /// <summary>
        /// Calls the default method on the specified nodeAddress/application/object, using the supplied parameters, and returns the results in a ServiceOperation instance.
        /// </summary>
        /// <param name="nodeAddress">the node to process the request</param>
        /// <param name="application">the application</param>
        /// <param name="objectPath">the object to call</param>
        /// <param name="parms">the input and aspect parameters to supply to the call</param>
        /// <returns>ServiceOperation containing the results of the call</returns>
        public static ServiceOperation Call(string nodeAddress, string application, string objectPath, params ServiceParameter[] parms)
        {
            return Call(nodeAddress, application, objectPath, "", StandardFormats.BINARY, parms);
        }

        //public static void Publish(string topic, params ServiceParameter[] parms)
        //{

        //}

        //public static void Publish(string topic, string nodeAddress, params ServiceParameter[] parms)
        //{

        //}

        public static void Publish(PublicationDefinition publication, params ServiceParameter[] parameters)
        {
            ServiceOperation op = new ServiceOperation(OperationType.Request,
                ServiceType.Broadcast,
                DeliveryOption.MulticastBestEffort,
                "*",
                publication.Format,
                NodeIdentity.Application,
                "*",
                publication.Id,
                parameters);

            IConnection connection = CreateConnection(op);
            Message msg = new Message(op);
            connection.Send(msg);
        }
    }
}
