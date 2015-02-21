using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.component;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using Altus.diagnostics;
using Altus.messaging;
using System.IO;
using Altus.messaging.tcp;
using Altus.processing;

namespace Altus.messaging.udp
{
    public class UdpTcpBridgeClient : InitializableComponent
    {
        protected override void OnInitialize(params string[] args)
        {
            CreateUdpSocket(args);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(Context.GetEnvironmentVariable("ListenTcpIP").ToString()),
                int.Parse(Context.GetEnvironmentVariable("ListenTcpPort").ToString()));

            int attempt = 0;
        retry:
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                socket.NoDelay = true;
                socket.Connect(Context.GetEnvironmentVariable("ListenUdpBridgeIP").ToString(),
                    int.Parse(Context.GetEnvironmentVariable("ListenUdpBridgePort").ToString()));
                HandleSocket(socket);
            }
            catch (SocketException)
            {
                attempt++;
                Logger.LogError("SocketException, retrying ... ");
                if (attempt < 25)
                    goto retry;
                else
                    Logger.LogError("SocketException giving up ... ");
            }
        }

        private void CreateUdpSocket(string[] args)
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

            UdpTcpBridgeStream.SetUdpSocket(sock, ref multicastEndPoint); // wire up the bridge to the Udp listener
        }

        private void HandleSocket(Socket clientSocket)
        {
            UdpTcpBridgeStream bridge = new UdpTcpBridgeStream(clientSocket, System.IO.FileAccess.ReadWrite, true);
            SocketStream = bridge;
        }

        public UdpTcpBridgeStream SocketStream { get; private set; }

        public void Send(string serviceUri, object requestData, params Header[] headers)
        {
            //ServiceContext ctx = new ServiceContext(SocketStream, new MemoryStream(), new MemoryStream());
            //ctx.Id = Guid.NewGuid().ToString();
            //ctx.Action = Altus.processing.Action.POST;
            //List<string> recipients = new List<string>();
            //foreach (Header header in headers)
            //{
            //    switch (header.Name)
            //    {
            //        case "Altus-MessageType":
            //            {
            //                ctx.MessageType = (MessageType)Enum.Parse(typeof(MessageType), header.Value);
            //                break;
            //            }
            //        case "Altus-Sender":
            //            {
            //                ctx.Sender = header.Value;
            //                break;
            //            }
            //        case "Altus-GuaranteedDelivery":
            //            {
            //                ctx.DeliveryGuaranteed = bool.Parse(header.Value);
            //                break;
            //            }
            //        case "Altus-Recipient":
            //            {
            //                recipients.Add(header.Value);
            //                break;
            //            }
            //        case "Altus-ResponseUri":
            //            {
            //                ctx.ResponseUri = header.Value;
            //                break;
            //            }
            //    }
            //}
            //ctx.Recipients = recipients.ToArray();
            //ctx.SetServiceUri(serviceUri);
            ////ctx.Request.InputArguments.Add("arg1", requestData);
            
            //SocketStream.WriteMessage(ctx);

        }
    }
}
