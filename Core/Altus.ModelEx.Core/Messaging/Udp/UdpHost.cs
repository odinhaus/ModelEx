using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using Altus.Core.Component;
using Altus.Core.Data;
using Altus.Core.Security;
using Altus.Core.PubSub;
using Altus.Core.Messaging.Udp;

[assembly: Component(ComponentType=typeof(UdpHost), Name="UdpHost")]

namespace Altus.Core.Messaging.Udp
{
    public class UdpHost : InitializableComponent
    {
        Dictionary<Topic, MulticastConnection> _mcastConnections = new Dictionary<Topic, MulticastConnection>();
        protected IPEndPoint _localUdpEP;
        Socket _mcSocket;
        //Dictionary<IPAddress, UdpSocketStream> _topicChannels = new Dictionary<IPAddress, UdpSocketStream>();

        protected override bool OnInitialize(params string[] args)
        {
            IPEndPoint ep;
            if (DataContext.Default.TryGetNodeEndPoint(NodeIdentity.NodeAddress, "udp", out ep))
            {
                this.EndPoint = ep;
                UdpConnection connection = new UdpConnection(ep);
                this.Connection = connection;
            }
            return true;
        }

        public EndPoint EndPoint { get; private set; }
        public IConnection Connection { get; private set; }

        //public void JoinGroup(string topicName)
        //{
        //    JoinGroup(topicName, true);
        //}

        //public void JoinGroup(string topicName, bool excludeMessagesFromSelf)
        //{
        //    Topic topic = topicName;
        //    JoinGroup(topic, excludeMessagesFromSelf);
        //}

        public void JoinGroup(Topic topic)
        {
            JoinGroup(topic, true);
        }

        public void JoinGroup(Topic topic, bool excludeMessagesFromSelf)
        {
            if (topic.IsMulticast)
            {
                lock (_mcastConnections)
                {
                    if (!_mcastConnections.ContainsKey(topic))
                    {
                        IPEndPoint ep = new IPEndPoint(IPAddress.Parse(topic.MulticastIP), topic.MulticastPort);
                        MulticastConnection mc = new MulticastConnection(ep, true);
                        _mcastConnections.Add(topic, mc);
                    }
                }
            }
            else
                throw (new InvalidOperationException("The topic does not support multi-cast"));
        }

        //public void LeaveGroup(string topicName)
        //{
        //    Topic topic = topicName;
        //    LeaveGroup(topic);
        //}

        public void LeaveGroup(Topic topic)
        {
            if (topic.IsMulticast)
            {
                lock (_mcastConnections)
                {
                    if (_mcastConnections.ContainsKey(topic))
                    {
                        IPEndPoint ep = new IPEndPoint(IPAddress.Parse(topic.MulticastIP), topic.MulticastPort);
                        MulticastConnection mc = _mcastConnections[topic];
                        mc.LeaveGroup();
                        _mcastConnections.Remove(topic);
                    }
                }
            }
            else
                throw (new InvalidOperationException("The topic does not support multi-cast"));
        }

        

        //private UdpSocketStream CreateMulticastChannel(IPAddress multicastIp)
        //{
        //    Socket socket = new Socket(AddressFamily.InterNetwork,
        //        SocketType.Dgram, ProtocolType.Udp);
        //    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        //    socket.ExclusiveAddressUse = false;

        //    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(Arguments.GetArgValue("ListenUdpIP")), 0);
        //    socket.Bind(ep);


        //    EndPoint multicastEndPoint = new IPEndPoint(multicastIp, int.Parse(Arguments.GetArgValue("ListenUdpPort")));
        //    socket.SetSocketOption(SocketOptionLevel.IP,
        //        SocketOptionName.AddMembership,
        //        new MulticastOption(multicastIp, ep.Address));
        //    socket.SetSocketOption(SocketOptionLevel.IP,
        //        SocketOptionName.MulticastTimeToLive, 50);

        //    UdpSocketStream stream = new UdpSocketStream(socket, ref multicastEndPoint);
        //    //stream.MessageReceived += new UdpMessageReceivedHandler(stream_UdpMessageReceived);
        //    //stream.MessageSent += new UdpMessageSentHandler(stream_UdpMessageSent);
        //    stream.SocketException += new UdpSocketExceptionHandler(stream_UdpSocketException);
        //    return stream;
        //}

        //protected IPAddress GetMulticastIpForTopic(string topicName)
        //{
        //    IUdpTopicAllocationStrategy allocator = Application.Instance.Shell.GetComponent<IUdpTopicAllocationStrategy>();
        //    lock (allocator)
        //    {
        //        if (allocator.TopicExists(topicName))
        //            return allocator.GetAddress(topicName);
        //        else
        //            return allocator.AllocateAddress(topicName);
        //    }
        //}

        

        //Dictionary<string, IPEndPoint> _endPoints = new Dictionary<string, IPEndPoint>();
        //private EndPoint GetRemoteEndPoint(MessagingContext ctx)
        //{
        //    try
        //    {
        //        lock (_endPoints)
        //        {
        //            return _endPoints[ctx.ServiceUri];
        //        }
        //    }
        //    catch
        //    {
        //        IPEndPoint ep = new IPEndPoint(Dns.GetHostAddresses(ctx.ServiceHost.Split(':')[0])[0], int.Parse((string)Context.GetEnvironmentVariable("ListenUdpPort")));
        //        lock (_endPoints)
        //        {
        //            _endPoints.Add(ctx.ServiceUri, ep);
        //        }
        //        return ep;
        //    }
        //}

        

        //void stream_UdpSocketException(object sender, UdpSocketExceptionEventArgs e)
        //{
        //    //throw new NotImplementedException();
        //}

        //public UdpSocketStream SocketStreamMulti { get; set; }
    }
}
