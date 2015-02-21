using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.component;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using Altus.diagnostics;

namespace Altus.messaging.udp
{
    public class UdpTcpBridgeHost : InitializableComponent
    {
        Socket _listener;
        bool _running = false;
        Thread _listenThread;
        List<UdpTcpBridgeStream> _bridgedConnections = new List<UdpTcpBridgeStream>();

        protected override void OnInitialize(params string[] args)
        {
            CreateUdpSocket(args);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(Context.GetEnvironmentVariable("ListenUdpBridgeIP").ToString()),
                int.Parse(Context.GetEnvironmentVariable("ListenUdpBridgePort").ToString()));

            int attempt = 0;
        retry:
            try
            {
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Bind(endPoint);
                listener.Listen(1000);
                _listener = listener;
                _running = true;
                _listenThread = new Thread(new ThreadStart(AcceptLoop));
                _listenThread.IsBackground = true;
                _listenThread.Priority = ThreadPriority.AboveNormal;
                _listenThread.Name = "Socket Acceptor [" + endPoint.ToString() + "]";
                _listenThread.Start();
                Logger.LogInfo("Listening for connections at " + endPoint.Address.ToString() + ":" + endPoint.Port.ToString());
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

        private void AcceptLoop()
        {
        retry:
            try
            {
                while (_running)
                {
                    IAsyncResult ar = _listener.BeginAccept(new AsyncCallback(Accept_Handler), null);
                    ar.AsyncWaitHandle.WaitOne();
                }
            }
            catch (ThreadAbortException) { throw; }
            catch (Exception)
            {
                goto retry;
            }
        }

        private void Accept_Handler(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = _listener.EndAccept(ar);
                Context.CurrentContext = Context.GlobalContext;

                HandleSocket(clientSocket);

                Logger.LogInfo("Connection accepted from " + clientSocket.RemoteEndPoint.ToString());
            }
            catch (Exception)
            {
            }
            finally
            {
            }
        }

        private void HandleSocket(Socket clientSocket)
        {
            UdpTcpBridgeStream bridge = new UdpTcpBridgeStream(clientSocket, System.IO.FileAccess.ReadWrite, true);
            _bridgedConnections.Add(bridge);
        }
    }
}
