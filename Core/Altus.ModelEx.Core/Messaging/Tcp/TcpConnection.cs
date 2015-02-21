using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;
using Altus.Core.Diagnostics;
using System.Threading;
using Altus.Core.Compression;
using Altus.Core.Streams;
using Altus.Core.Messaging;
using Altus.Core.Processing;
using Altus.Core.Serialization;
using Altus.Core.Component;
using Altus.Core;
using Altus.Core.Security;

namespace Altus.Core.Messaging
{
    public delegate void MessageReceivedHandler(object sender, Message message);
}

namespace Altus.Core.Messaging.Tcp
{
    public delegate void TcpSocketExceptionHandler(object sender, TcpSocketExceptionEventArgs e);
    public class TcpSocketExceptionEventArgs
    {
        public TcpSocketExceptionEventArgs(TcpConnection sender, Exception e)
        {
            SocketException = e;
            TcpConnection = sender;
        }

        public Exception SocketException { get; private set; }
        public TcpConnection TcpConnection { get; set; }
    }

    public class TcpConnection : NetworkStream, IConnection
    {
        public event TcpSocketExceptionHandler SocketException;
        public bool IsDisconnected { get; protected set; }
        public event EventHandler Disconnected;
        protected void OnDisconnected()
        {
            IsDisconnected = true;
            if (Disconnected != null)
                Disconnected(this, new EventArgs());
        }

        private static PerformanceCounter BytesSentRate;
        private static PerformanceCounter BytesReceivedRate;
        private static PerformanceCounter BytesSentRateTotal;
        private static PerformanceCounter BytesReceivedRateTotal;

        private static PerformanceCounter BytesSent;
        private static PerformanceCounter BytesSentTotal;

        static TcpConnection()
        {
            string name = NodeIdentity.NodeAddress;
            BytesSentRateTotal = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.TCP_RateOfBytesSent_NAME);
            BytesSentRate = PerformanceCounter.CreateCounterInstance(BytesSentRateTotal.Category,
                BytesSentRateTotal.Name,
                name,
                BytesSentRateTotal.Type,
                false);
            BytesSentTotal = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.TCP_NUMBEROFBYTESSENT_NAME);
            BytesSent = PerformanceCounter.CreateCounterInstance(BytesSentTotal.Category,
                BytesSentTotal.Name,
                name,
                BytesSentTotal.Type,
                false);
            BytesReceivedRateTotal = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.TCP_RateOfBytesReceived_NAME);
            BytesReceivedRate = PerformanceCounter.CreateCounterInstance(BytesReceivedRateTotal.Category,
                BytesReceivedRateTotal.Name,
                name,
                BytesReceivedRateTotal.Type,
                false);
        }

        //public SocketState _readState;
        //protected TcpConnection() : base(null) {  }
        //protected TcpConnection(EndPoint remoteEndPoint) : base(new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)) 
        //{
        //    this.ContentType = this.DefaultFormat;
        //    this.TextEncoding = Encoding.Unicode;
        //    ReadMessages(); 
        //}
        protected TcpConnection(Socket socket) : base(socket) 
        {
            this.ContentType = this.DefaultFormat;
            if (SerializationContext.TextEncoding == null) SerializationContext.TextEncoding = Encoding.Unicode;
            this.TextEncoding = SerializationContext.TextEncoding;
            ReadMessages(); 
        }
        public static TcpConnection Create(EndPoint remoteEndPoint)
        {
            Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(remoteEndPoint);
            return Create(socket);
        }
        public static TcpConnection Create(Socket socket)
        {
            return new TcpConnection(socket);
        }
        //public TcpConnection(Socket socket, bool ownsSocket) : base(socket, ownsSocket) 
        //{
        //    this.ContentType = this.DefaultFormat;
        //    this.TextEncoding = Encoding.Unicode;
        //    ReadMessages(); 
        //}
        //public TcpConnection(Socket socket, FileAccess fa) : base(socket, fa) 
        //{
        //    this.ContentType = this.DefaultFormat;
        //    this.TextEncoding = Encoding.Unicode;
        //    ReadMessages(); 
        //}
        //public TcpConnection(Socket socket, FileAccess fa, bool ownsSocket) : base(socket, fa, ownsSocket) 
        //{
        //    this.ContentType = this.DefaultFormat;
        //    this.TextEncoding = Encoding.Unicode;
        //    ReadMessages(); 
        //}

        public new Socket Socket { get { return base.Socket; } }
        public EndPoint EndPoint { get; set; }
        public Protocol Protocol { get { return Messaging.Protocol.Tcp; } }
        public Altus.Core.Processing.Action Action { get { return Processing.Action.POST; } }

        public void Send(byte[] data)
        {
            this.Write(data, 0, data.Length);
            BytesSentRate.IncrementBy(data.Length);
            BytesSentRateTotal.IncrementBy(data.Length);
            BytesSent.IncrementBy(data.Length);
            BytesSentTotal.IncrementBy(data.Length);
        }

        public void Send(Message message)
        {
            if (message.Encoding == null)
                message.Encoding = this.TextEncoding.EncodingName;

            this.ContentType = message.PayloadFormat;

            SerializationContext.TextEncoding = Encoding.GetEncoding(message.Encoding);
            
            TcpMessage tcpMsg = new TcpMessage(this, message);
            //this.Send(tcpMsg.TcpHeaderSegment.Data);
            for (int i = 0; i < tcpMsg.TcpSegments.Length; i++)
            {
                this.Send(tcpMsg.TcpSegments[i].Data);
            }
            this.ConnectionAspects = new Dictionary<string, object>();
        }

        public void SendError(Message message, Exception ex)
        {
            throw (new NotImplementedException());
        }

        static Dictionary<string, MessageReceivedHandler> _receivers = new Dictionary<string, MessageReceivedHandler>();
        bool _closeOnResponseComplete = false;
        public Message Call(Message message)
        {
            return this.Call(message, 30000);
        }

        public Message Call(Message message, int timespan)
        {
            return this.Call(message, TimeSpan.FromMilliseconds(timespan));
        }

        public Message Call(Message message, TimeSpan timespan)
        {
            this._closeOnResponseComplete = true;
            AsyncRequest async = new AsyncRequest(message, timespan);

            if (async.CanHaveResponse)
            {
                lock (_receivers)
                {
                    _receivers.Add(message.Id, async.ResponseCallback);
                }
            }

            this.Send(message);

            Message response = null;

            try
            {
                response = async.GetResponse();
            }
            catch(Exception ex)
            {
                this.Close();
                this.OnSocketException(ex);
            }

            return response;
        }

        Thread _receiveThread;
        protected void ReadMessages()
        {
            EndPoint = this.Socket.RemoteEndPoint;
            _receiveThread = new Thread(new ThreadStart(ReadMessagesLoop));
            _receiveThread.Priority = ThreadPriority.Highest;
            _receiveThread.IsBackground = true;
            _receiveThread.Name = "TCP Message Processor";
            _receiveThread.Start();
        }

        private void ReadMessagesLoop()
        {
            Context.CurrentContext = Context.GlobalContext;
            byte[] residual = new byte[0];
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[SocketOptions.BUFFER_SIZE];
                    int read = this.Socket.Receive(buffer);
                    BytesReceivedRate.IncrementBy(read);
                    BytesReceivedRateTotal.IncrementBy(read);
                    byte[] raw = new byte[residual.Length + read];
                    residual.Copy(0, raw, 0, residual.Length);
                    buffer.Copy(0, raw, residual.Length, read);
                    if (read > 0)
                    {
                        MessageSegment segment = null;
                        do
                        {
                            if (MessageSegment.TryCreate(this, Protocol.Tcp, EndPoint, raw, out segment))
                            {
                                bool messageComplete;
                                int used = ProcessInboundTcpSegment(segment, out messageComplete);
                                if (_closeOnResponseComplete && messageComplete)
                                {
                                    HandleCompletedMessage();
                                    return;
                                }
                                residual = new byte[raw.Length - used];
                                raw.Copy(used, residual, 0, residual.Length);
                                raw = residual;
                            }
                            else
                            {
                                residual = raw;
                            }
                        } while (residual.Length > 0 && segment != null);
                    }
                    else
                    {
                        //_readState.WaitHandle.Set();
                        this.OnSocketException(new IOException("An existing connection was closed by the remote host."));
                        break;
                    }

                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if ((this.Socket != null) && (this.Socket.Connected == true))
                    {
                        Logger.LogInfo("Trying to release the Socket since it's been severed");
                        // Release the socket.
                        this.Socket.Shutdown(SocketShutdown.Both);
                        this.Socket.Disconnect(true);
                        if (this.Socket.Connected)
                            Logger.LogError("We're still connnected!!!!");
                        else
                            Logger.LogInfo("We're disconnected");
                    }
                    this.OnSocketException(ex);
                    break;
                }
            }
        }

        private void HandleCompletedMessage()
        {
            
        }

        private int ProcessInboundTcpSegment(MessageSegment segment, out bool messageComplete)
        {
            if (segment.SegmentType == SegmentType.Segment)
            {
                // segment
                ProcessInboundTcpSegmentSegment((TcpSegmentSegment)segment, out messageComplete);
                return ((TcpSegmentSegment)segment).SegmentLength;
            }
            else
            {
                // header
                ProcessInboundTcpHeaderSegment((TcpHeaderSegment)segment, out messageComplete);
                return ((TcpHeaderSegment)segment).SegmentLength;
            }
        }

        private void ProcessInboundTcpSegmentSegment(TcpSegmentSegment segment, out bool messageComplete)
        {
            TcpMessage msg = null;
            messageComplete = false;
            if (segment.MessageId == 0) return; // discard bad message
            lock (_tcpInboundMessages)
            {
                try
                {
                    if (_tcpInboundMessages.ContainsKey(segment.MessageId))
                    {
                        msg = _tcpInboundMessages[segment.MessageId];
                        msg.AddSegment(segment);
                    }
                    else
                    {
                        //key not found
                        msg = new TcpMessage(segment.Connection, segment);
                        msg.AddSegment(segment);

                        _tcpInboundMessages.Add(msg.MessageId, msg);
                    }
                }
                catch
                {

                }

                if (msg != null && msg.IsComplete)
                {
                    try
                    {
                        messageComplete = true;
                        ProcessCompletedInboundTcpMessage(msg);
                    }
                    finally
                    {
                        lock (_tcpInboundMessages)
                        {
                            _tcpInboundMessages.Remove(segment.MessageId);
                        }
                    }
                }
            }
        }

        Dictionary<uint, TcpMessage> _tcpInboundMessages = new Dictionary<uint, TcpMessage>();
        private void ProcessInboundTcpHeaderSegment(TcpHeaderSegment segment, out bool messageComplete)
        {
            messageComplete = false;
            TcpMessage msg = null;
            if (segment.MessageId == 0) return; // discard bad message
            lock (_tcpInboundMessages)
            {
                try
                {
                    // udp can duplicate messages, or send payload datagrams ahead of the header
                    if (_tcpInboundMessages.ContainsKey(segment.MessageId))
                    {
                        msg = _tcpInboundMessages[segment.MessageId];
                        msg.TcpHeaderSegment = segment;
                        msg.AddSegment(segment);
                    }
                    else
                    {
                        msg = new TcpMessage(segment.Connection, segment);
                        lock (_tcpInboundMessages)
                        {
                            _tcpInboundMessages.Add(msg.MessageId, msg);
                        }
                    }
                }
                catch
                {

                }

                if (msg != null && msg.IsComplete)
                {
                    try
                    {
                        messageComplete = true;
                        ProcessCompletedInboundTcpMessage(msg);
                    }
                    finally
                    {
                        lock (_tcpInboundMessages)
                        {
                            _tcpInboundMessages.Remove(segment.MessageId);
                        }
                    }
                }
            }
        }

        private void ProcessCompletedInboundTcpMessage(TcpMessage tcpMessage)
        {
            this.ConnectionAspects = new Dictionary<string, object>();
            Message message = (Message)Message.FromStream(tcpMessage.Payload);
            SerializationContext.TextEncoding = Encoding.GetEncoding(message.Encoding);
            this.TextEncoding = SerializationContext.TextEncoding;
            ProcessInboundMessage(message);
        }

        private void ProcessInboundMessage(Message message)
        {
            bool call = false;
            lock (_receivers)
            {
                call = _receivers.ContainsKey(message.CorrelationId);
            }
            if (call)
            {
                MessageReceivedHandler callback;
                lock (_receivers)
                {
                    callback = _receivers[message.CorrelationId];
                }
                try
                {
                    callback(this, message);
                }
                catch { }
                finally
                {
                    lock (_receivers)
                    {
                        _receivers.Remove(message.CorrelationId);
                    }
                }
            }
            else
            {
                ServiceContext ctx;
                ServiceContextFactory factory = App.Instance.Shell.GetComponents<ServiceContextFactory>().Where(scf => scf.CanProcess(message)).FirstOrDefault();
                if (factory != null
                    && factory.CreateContext(message, this, out ctx))
                {
                    ServiceContext.Current = ctx;
                    ctx.ProcessingContext.Process(ctx);
                    if (ctx.ServiceType == ServiceType.RequestResponse
                        && !ctx.IsCanceled)
                        ProcessResponse(ctx);
                    ctx.Dispose();
                }
                else
                {
                    throw (new InvalidOperationException("Context Factory either could not be found, or could not process the request: " + message.ServiceUri));
                }
            }
        }

        private void ProcessResponse(ServiceContext ctx)
        {
            Message message = ctx.ToResponseMessage();
            this.Send(message);
        }

        public bool IsClosing { get; private set; }
        public bool IsClosed { get; private set; }

        public override void Close()
        {
            if (!IsClosed && !IsClosing)
            {
                IsClosing = true;
                if (_receiveThread != null)
                {
                    try
                    {
                        _receiveThread.Abort();
                        _receiveThread = null;
                    }
                    catch { }
                }
                try
                {
                    if (this.Socket != null)
                    {
                        this.Socket.Shutdown(SocketShutdown.Both);
                        this.Socket.Disconnect(false);
                        OnDisconnected();
                    }
                }
                catch { }
                Logger.LogInfo("Closed connection to " + this.EndPoint.ToString());
                base.Close();
                IsClosing = false;
            }
            IsClosed = true;
        }

        protected virtual void OnSocketException(Exception e)
        {
            if (this.SocketException != null)
            {
                this.SocketException(this, new TcpSocketExceptionEventArgs(this, e));
            }
        }

        [ThreadStatic()]
        static Dictionary<string, object> _aspects;
        public Dictionary<string, object> ConnectionAspects { get { return _aspects; } set { _aspects = value; } }

        [ThreadStatic()]
        static Encoding _encoding;
        public Encoding TextEncoding
        {
            get { return _encoding; }
            set { _encoding = value; }
        }

        [ThreadStatic()]
        static long _length;
        public long ContentLength
        {
            get { return _length; }
            set { _length = value; }
        }

        [ThreadStatic()]
        static string _type;
        public string ContentType
        {
            get { return _type; }
            set { _type = value; }
        }

        public void ResetProperties()
        {
            _aspects.Clear();
            _encoding = null;
            _length = 0;
            _type = null;
        }

        public string DefaultFormat { get { return StandardFormats.BINARY; } }

        bool disposed = false;
        public event EventHandler Disposing;
        public event EventHandler Disposed;
        //========================================================================================================//
        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the 
        // runtime from inside the finalizer and you should not reference 
        // other objects. Only unmanaged resources can be disposed.
        protected override void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                if (this.Disposing != null)
                    this.Disposing(this, new EventArgs());
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    this.OnDisposeManagedResources();
                }

                // Call the appropriate methods to clean up 
                // unmanaged resources here.
                // If disposing is false, 
                // only the following code is executed.
                this.OnDisposeUnmanagedResources();
                if (this.Disposed != null)
                    this.Disposed(this, new EventArgs());
            }
            disposed = true;
        }

        /// <summary>
        /// Dispose managed resources
        /// </summary>
        protected virtual void OnDisposeManagedResources()
        {
            if (this.Socket != null)
            {
                this.Close();

                try
                {
                    if (this.Socket != null)
                    {
                        this.Socket.Dispose();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Dispose unmanaged (native resources)
        /// </summary>
        protected virtual void OnDisposeUnmanagedResources()
        {
        }
    }

    public class SocketOptions
    {
        public const int BUFFER_SIZE = 4096;
        public const int MTU_SIZE = 1300;
        public static IEnumerable<byte[]> Chunk(byte[] data)
        {
            bool read = true;
            MemoryStream ms = new MemoryStream(data);
            while (read)
            {
                byte[] chunk;
                if (ms.Length - ms.Position >= BUFFER_SIZE)
                {
                    chunk = new byte[BUFFER_SIZE];
                }
                else
                {
                    chunk = new byte[ms.Length - ms.Position];
                    read = false;
                }
                ms.Read(chunk, 0, chunk.Length);
                yield return chunk;
            }
        }
    }
}
