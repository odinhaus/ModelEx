using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using certes.common.messaging.tcp;
using certes.common.messaging;
using System.IO;
using System.Threading;
using certes.common.streams;
using System.Net;

namespace certes.common.messaging.udp
{
    public class UdpClient
    {
        public UdpClient(string hostNode)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            sock.ExclusiveAddressUse = false;
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse((string)Context.GetEnvironmentVariable("ListenUdpIP")),
                    int.Parse((string)Context.GetEnvironmentVariable("ListenUdpPort")));
            sock.Bind(iep);

            EndPoint multicastEndPoint = new IPEndPoint(IPAddress.Parse(Context.GetEnvironmentVariable("UdpMulticastIP").ToString()), 
                int.Parse((string)Context.GetEnvironmentVariable("ListenUdpPort")));
            sock.SetSocketOption(SocketOptionLevel.IP,
                SocketOptionName.AddMembership,
                new MulticastOption(IPAddress.Parse(Context.GetEnvironmentVariable("UdpMulticastIP").ToString()), 
                    IPAddress.Parse((string)Context.GetEnvironmentVariable("ListenUdpIP"))));
            sock.SetSocketOption(SocketOptionLevel.IP,
                SocketOptionName.MulticastTimeToLive, 50);

            Method = "POST";
            HandleSocket(sock, ref multicastEndPoint);
        }

        private void HandleSocket(Socket clientSocket, ref EndPoint multicastEndPoint)
        {
            UdpSocketStream stream = new UdpSocketStream(clientSocket, ref multicastEndPoint);
            stream.SocketException += new UdpSocketExceptionHandler(OnSocketException);
            SocketStream = stream;
        }

        public string Method { get; set; }

        public MessagingContext Send(string serviceUri, object requestData, params Header[] headers)
        {
            MessagingContext ctx = MessagingContext.Create(serviceUri, requestData, headers);

            EndPoint ep = GetRemoteEndPoint(ctx);
            if (ctx.ResponseUri != null)
            {
                PendingResponse pending = new PendingResponse()
                {
                    MessagingContext = ctx,
                    WaitHandle = new ManualResetEvent(false)
                };
                lock (_pending)
                    _pending.Add(ctx.Id, pending);
                SocketStream.WriteMessage(ctx, ep);
                pending.WaitHandle.WaitOne();
                pending.WaitHandle.Reset();
                return pending.MessagingContext;
            }
            else
            {
                SocketStream.WriteMessage(ctx, ep);
                return null;
            }
        }

        public void SendRaw(byte[] data, EndPoint ep)
        {
            SocketStream.Write(data, ep);
        }

        Dictionary<string, IPEndPoint> _endPoints = new Dictionary<string, IPEndPoint>();
        private EndPoint GetRemoteEndPoint(MessagingContext ctx)
        {
            try
            {
                lock (_endPoints)
                {
                    return _endPoints[ctx.ServiceUri];
                }
            }
            catch
            {
                IPEndPoint ep = new IPEndPoint(Dns.GetHostAddresses(ctx.ServiceHost.Split(':')[0])[0], int.Parse((string)Context.GetEnvironmentVariable("ListenUdpPort")));
                lock (_endPoints)
                {
                    _endPoints.Add(ctx.ServiceUri, ep);
                }
                return ep;
            }
        }

        private UdpSocketStream SocketStream { get; set; }
        private Dictionary<string, PendingResponse> _pending = new Dictionary<string, PendingResponse>();

        //private void OnMessageReceived(object sender, UdpMessageReceivedEventArgs e)
        //{
        //    bool hasPending = false;
        //    PendingResponse response = null;
        //    lock (_pending)
        //    {
        //        hasPending = _pending.ContainsKey(e.MessagingContext.CorrelationId);
        //        if (hasPending)
        //        {
        //            response = _pending[e.MessagingContext.CorrelationId];
        //            _pending.Remove(e.MessagingContext.CorrelationId);
        //        }
        //    }
        //    if (hasPending)
        //    {
        //        StreamHelper.Copy(e.MessagingContext.RequestStream, response.MessagingContext.ResponseStream);
        //        response.MessagingContext.ResponseObject = e.MessagingContext.RequestObject;
        //        response.WaitHandle.Set();
        //    }
        //}
        //private void OnMessageSent(object sender, UdpMessageSentEventArgs e)
        //{

        //}
        private void OnSocketException(object sender, UdpSocketExceptionEventArgs e)
        {

        }

    }

    public class PendingResponse
    {
        public MessagingContext MessagingContext { get; set; }
        public ManualResetEvent WaitHandle { get; set; }
    }
}
