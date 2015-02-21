using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Altus.Core.Diagnostics;
using Altus.Core.Pipeline;
using Altus.Core.Messaging;
using System.IO;
using Altus.Core.Messaging.Tcp;
using Altus.Core.Data;
using Altus.Core.Security;

[assembly: Component(ComponentType=typeof(TcpHost), Name="TcpHost")]

namespace Altus.Core.Messaging.Tcp
{
    public class TcpHost : InitializableComponent
    {
        Socket _listener;
        bool _running = false;
        Thread _listenThread;

        public static IPEndPoint ListenerEndPoint { get; private set; }

        protected override bool OnInitialize(params string[] args)
        {
            IPEndPoint endPoint;
            if (DataContext.Default.TryGetNodeEndPoint(NodeIdentity.NodeAddress, "tcp", out endPoint))
            {
                int attempt = 0;
                ListenerEndPoint = endPoint;
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
            return true;
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
            TcpConnection stream = TcpConnection.Create(clientSocket);
            stream.SocketException += new TcpSocketExceptionHandler(stream_SocketException);
        }

        void stream_SocketException(object sender, TcpSocketExceptionEventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
