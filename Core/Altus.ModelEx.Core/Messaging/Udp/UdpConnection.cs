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
using Altus.Core.Security;


namespace Altus.Core.Messaging.Udp
{
    public delegate void TcpSocketExceptionHandler(object sender, SocketExceptionEventArgs e);
    public class SocketExceptionEventArgs
    {
        public SocketExceptionEventArgs(IConnection sender, Exception e)
        {
            SocketException = e;
            Connection = sender;
        }

        public Exception SocketException { get; private set; }
        public IConnection Connection { get; set; }
    }

    public class UdpConnection :  IConnection
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
        //private static PerformanceCounter BytesSentRateTotal;
        //private static PerformanceCounter BytesReceivedRateTotal;

        //private static PerformanceCounter BytesSent;
        private static PerformanceCounter BytesSentTotal;

        private static PerformanceCounter MsgReceivedRate;
        private static PerformanceCounter MsgSentRate;

        static UdpConnection()
        {
            string name = NodeIdentity.NodeAddress;
            BytesSentRate = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.UDP_RateOfBytesSent_NAME);
            BytesSentTotal = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.UDP_NUMBEROFBYTESSENT_NAME);
            BytesReceivedRate = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.UDP_RateOfBytesReceived_NAME);

            MsgReceivedRate = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.UDP_RateOfMessagesReceived_NAME);
            MsgSentRate = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.UDP_RateOfMessagesSent_NAME);
        }

        public UdpConnection(IPEndPoint localEndPoint) 
        {
            this.EndPoint = localEndPoint;
            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.Socket.ExclusiveAddressUse = false;
            this.Socket.Bind(this.EndPoint);
            this.ContentType = this.DefaultFormat;
            this.TextEncoding = Encoding.Unicode;
            ReadMessages(); 
        }

        public Socket Socket { get; private set; }
        public EndPoint EndPoint { get; private set; }
        public Protocol Protocol { get { return Messaging.Protocol.Udp; } }
        public Altus.Core.Processing.Action Action { get { return Processing.Action.POST; } }

        public void Send(byte[] data)
        {
            this.Socket.SendTo(data, EndPoint);
            BytesSentRate.IncrementBy(data.Length);
            //BytesSentRateTotal.IncrementBy(data.Length);
            //BytesSent.IncrementBy(data.Length);
            BytesSentTotal.IncrementBy(data.Length);
        }

        public void Send(Message message)
        {
            if (message.Encoding == null)
                message.Encoding = this.TextEncoding.EncodingName;

            this.ContentType = message.PayloadFormat;


            SerializationContext.TextEncoding = Encoding.GetEncoding(message.Encoding);
            UdpMessage tcpMsg = new UdpMessage(this, message);
            this.Send(tcpMsg.UdpHeaderSegment.Data);
            for (int i = 0; i < tcpMsg.UdpSegments.Length; i++)
            {
                this.Send(tcpMsg.UdpSegments[i].Data);
            }

            MsgSentRate.IncrementByFast(1);
            this.ConnectionAspects = new Dictionary<string, object>();
        }

        public void SendError(Message message, Exception ex)
        {
            throw (new NotImplementedException());
        }

        static Dictionary<string, MessageReceivedHandler> _receivers = new Dictionary<string, MessageReceivedHandler>();
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
            AsyncRequest async = new AsyncRequest(message, timespan);

            if (async.CanHaveResponse)
            {
                lock (_receivers)
                {
                    _receivers.Add(message.Id, async.ResponseCallback);
                }
            }

            this.Send(message);

            return async.GetResponse();
        }

        Thread _receiveThread;
        protected void ReadMessages()
        {
            _receiveThread = new Thread(new ThreadStart(ReadMessagesLoop));
            _receiveThread.Priority = ThreadPriority.Highest;
            _receiveThread.IsBackground = true;
            _receiveThread.Start();

        }

        private void ReadMessagesLoop()
        {
            Context.CurrentContext = Context.GlobalContext;
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[SocketOptions.BUFFER_SIZE];
                    EndPoint ep = this.EndPoint;
                    int read = this.Socket.Receive(buffer);
                    BytesReceivedRate.IncrementBy(read);
                    //BytesReceivedRateTotal.IncrementBy(read);
                    if (read > 0)
                    {
                        MessageSegment segment;
                        if (MessageSegment.TryCreate(this, Protocol.Udp, EndPoint, buffer, out segment))
                            ProcessInboundUdpSegment(segment);
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

        Dictionary<uint, UdpMessage> _udpInboundMessages = new Dictionary<uint, UdpMessage>();
        private void ProcessInboundUdpSegment(MessageSegment segment)
        {
            if (segment.SegmentType == SegmentType.Segment)
            {
                // segment
                ProcessInboundUdpSegmentSegment((UdpSegmentSegment)segment);
            }
            else
            {
                // header
                ProcessInboundUdpHeaderSegment((UdpHeaderSegment)segment);
            }
        }

        private void ProcessInboundUdpSegmentSegment(UdpSegmentSegment segment)
        {
            UdpMessage msg = null;
            if (segment.MessageId == 0) return; // discard bad message
            lock (_udpInboundMessages)
            {
                try
                {
                    if (_udpInboundMessages.ContainsKey(segment.MessageId))
                    {
                        msg = _udpInboundMessages[segment.MessageId];
                        msg.AddSegment(segment);
                    }
                    else
                    {
                        //key not found
                        msg = new UdpMessage(segment.Connection, segment);
                        msg.AddSegment(segment);

                        _udpInboundMessages.Add(msg.MessageId, msg);
                    }
                }
                catch
                {

                }

                if (msg != null && msg.IsComplete)
                {
                    try
                    {
                        ProcessCompletedInboundUdpMessage(msg);
                    }
                    finally
                    {
                        lock (_udpInboundMessages)
                        {
                            _udpInboundMessages.Remove(segment.MessageId);
                        }
                    }
                }
            }
        }

        private void ProcessInboundUdpHeaderSegment(UdpHeaderSegment segment)
        {
            UdpMessage msg = null;
            if (segment.MessageId == 0) return; // discard bad message
            lock (_udpInboundMessages)
            {
                try
                {
                    // udp can duplicate messages, or send payload datagrams ahead of the header
                    if (_udpInboundMessages.ContainsKey(segment.MessageId))
                    {
                        msg = _udpInboundMessages[segment.MessageId];
                        msg.UdpHeaderSegment = segment;
                    }
                    else
                    {
                        msg = new UdpMessage(segment.Connection, segment);
                        lock (_udpInboundMessages)
                        {
                            _udpInboundMessages.Add(msg.MessageId, msg);
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
                        ProcessCompletedInboundUdpMessage(msg);
                    }
                    finally
                    {
                        lock (_udpInboundMessages)
                        {
                            _udpInboundMessages.Remove(segment.MessageId);
                        }
                    }
                }
            }
        }

        private void ProcessCompletedInboundUdpMessage(UdpMessage udpMessage)
        {
            this.ConnectionAspects = new Dictionary<string, object>();
            Message message = (Message)Message.FromStream(udpMessage.Payload);
            SerializationContext.TextEncoding = System.Text.Encoding.Unicode;
            ProcessInboundMessage(message);
            MsgReceivedRate.IncrementByFast(1);
        }

        private void ProcessInboundMessage(Message message)
        {
            if (_receivers.ContainsKey(message.CorrelationId))
            {
                MessageReceivedHandler callback = _receivers[message.CorrelationId];
                try
                {
                    callback(this, message);
                }
                catch { }
                finally
                {
                    _receivers.Remove(message.CorrelationId);
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
                    if (ctx.ServiceType == ServiceType.RequestResponse)
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
        
        protected virtual void OnSocketException(Exception e)
        {
            if (this.SocketException != null)
            {
                this.SocketException(this, new SocketExceptionEventArgs(this, e));
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


        #region IDisposable Members
        bool disposed = false;
        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue 
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

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
        private void Dispose(bool disposing)
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
            try
            {
                if (_receiveThread != null)
                {
                    _receiveThread.Abort();
                }
            }
            catch { }

            if (this.Socket != null)
            {
                try
                {
                    this.Socket.Close();
                    this.Socket.Dispose();
                }
                catch { }
            }
            OnDisconnected();
        }

        /// <summary>
        /// Dispose unmanaged (native resources)
        /// </summary>
        protected virtual void OnDisposeUnmanagedResources()
        {
        }

        #endregion
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
