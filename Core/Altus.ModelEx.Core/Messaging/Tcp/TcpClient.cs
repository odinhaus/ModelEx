using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Altus.messaging.tcp;
using Altus.messaging;
using System.IO;
using System.Threading;
using Altus.streams;
using Altus.security;
using Altus.diagnostics;
using Altus.processing;
using Altus.serialization;

namespace Altus.messaging.tcp
{
    public class TcpClient
    {
        private Socket Socket;

        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public TcpClient(string hostNode, int hostPort)
        {
            this.HostNode = hostNode;
            this.HostPort = hostPort;
        }

        public bool Connect()
        {
            try
            {
                this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                this.Socket.NoDelay = true;
                this.Socket.Connect(this.HostNode, this.HostPort);
                this.SocketStream = new TcpConnection(
                    this.Socket,
                     System.IO.FileAccess.ReadWrite,
                     true);
                this.SocketStream.SocketException += new TcpSocketExceptionHandler(SocketStream_SocketException);
                if (Connected != null)
                    Connected(this, new EventArgs());
                return true;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
                try
                {
                    this.SocketStream.Dispose();
                }
                catch { }
                try
                {
                    this.Socket.Dispose();
                }
                catch { }
                this.SocketStream = null;
                this.Socket = null;
                return false;
            }
        }

        void SocketStream_SocketException(object sender, TcpSocketExceptionEventArgs e)
        {
            OnSocketException(this, e);
        }

        public void Send(ServiceOperation sep)
        {
            Send(sep, null);
        }

        public void Send(ServiceOperation sep, MessageReceivedHandler responseCallback)
        {
            Message message = new Message(
                StandardFormats.BINARY, 
                sep.ServiceUri, 
                ServiceType.Directed, 
                NodeIdentity.NodeAddress, 
                Altus.processing.Action.POST);
            //message.SetPayload(sep.Arguments);
            message.DeliveryGuaranteed = true;

            SocketStream.Send(message);
        }

        public ServiceContext SendWithResponse(ServiceOperation sep)
        {
            Message message = new Message(
                StandardFormats.BINARY,
                sep.ServiceUri, 
                ServiceType.Directed,
                NodeIdentity.NodeAddress, 
                Altus.processing.Action.POST);
            //message.SetPayload(sep.Arguments);
            message.DeliveryGuaranteed = true;

            PendingResponse pending = new PendingResponse()
            {
                Message = message,
                WaitHandle = new ManualResetEvent(false)
            };
            lock (_pending)
                _pending.Add(message.Id, pending);
            SocketStream.WriteMessage(null, message, new MessageReceivedHandler(OnMessageReceived));
            if (pending.WaitHandle.WaitOne(message.TTL))
            {
                pending.WaitHandle.Reset();
                return pending.ResponseContext;
            }
            else
            {
                throw (new TimeoutException("A response was not received within the TTL limit set for the request.  The request may still have been executed on the remote device."));
            }
        }

        public void SendRaw(byte[] data)
        {
            foreach (byte[] chunk in SocketOptions.Chunk(data))
                SocketStream.Write(chunk, 0, chunk.Length);
        }

        public int HostPort { get; private set; }
        public string HostNode { get; private set; }
        private TcpConnection SocketStream { get; set; }
        private Dictionary<string, PendingResponse> _pending = new Dictionary<string, PendingResponse>();
        public bool IsConnected
        {
            get
            {
                return this.SocketStream != null
                    && this.Socket.Connected;
            }
        }

        private void OnMessageReceived(object sender, ServiceContext responseContext)
        {
            bool hasPending = false;
            PendingResponse response = null;
            lock (_pending)
            {
                hasPending = _pending.ContainsKey(responseContext.CorrelationId);
                if (hasPending)
                {
                    response = _pending[responseContext.CorrelationId];
                    response.ResponseContext = responseContext;
                    _pending.Remove(responseContext.CorrelationId);
                }
            }
            if (hasPending)
            {
                response.WaitHandle.Set();
            }
        }
        

        private void OnSocketException(object sender, TcpSocketExceptionEventArgs e)
        {
            Logger.Log(e.SocketException);

            try
            {
                this.SocketStream.Dispose();
            }
            catch { }
            try
            {
                this.Socket.Dispose();
            }
            catch { }
            this.Socket = null;
            this.SocketStream = null;

            if (Disconnected != null)
                Disconnected(this, new EventArgs());
        }
    }

    public class PendingResponse
    {
        public Message Message { get; set; }
        public ServiceContext ResponseContext { get; set; }
        public ManualResetEvent WaitHandle { get; set; }
    }
}
