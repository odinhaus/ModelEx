﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Altus.Core.Serialization;
using Altus.Core.Component;
using System.Threading;
using Altus.Core.Diagnostics;
using System.IO;
using Altus.Core.Processing;
using Altus.Core.Security;
using Altus.Core.Net;
using Altus.Core;
using Altus.Core.Streams;


namespace Altus.Core.Messaging.Udp
{
    public delegate void DataReceivedHandler(object sender, DataReceivedArgs e);

    public class DataReceivedArgs : EventArgs
    {
        public DataReceivedArgs(byte[] buffer, int length, EndPoint source, EndPoint destination) 
        { 
            this.Buffer = buffer; 
            this.Length = length;
            this.SourceEndPoint = source;
            this.DestinationEndPoint = destination;
        }
        public byte[] Buffer { get; private set; }
        public int Length { get; private set; }
        public EndPoint SourceEndPoint { get; private set; }
        public EndPoint DestinationEndPoint { get; set; }
    }

    public class MulticastConnection : IConnection, IDisposable
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
        private static PerformanceCounter MsgSentRate;
        private static PerformanceCounter MsgReceivedRate;
        private static PerformanceCounter BytesSentTotal;

        static MulticastConnection()
        {
            string name = NodeIdentity.NodeAddress;
            BytesSentRate = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.UDP_RateOfBytesSent_NAME);
            BytesSentTotal = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.UDP_NUMBEROFBYTESSENT_NAME);
            BytesReceivedRate = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.UDP_RateOfBytesReceived_NAME);

            MsgReceivedRate = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.UDP_RateOfMessagesReceived_NAME);
            MsgSentRate = PerformanceCounter.GetPerfCounterInstance(PerformanceCounterNames.UDP_RateOfMessagesSent_NAME);
        }

        static Dictionary<string, object> _locks = new Dictionary<string, object>();
        object _lock;

        public MulticastConnection(IPEndPoint mcastGroup, bool listen) : this(mcastGroup, listen, true)
        {

        }

        public MulticastConnection(IPEndPoint mcastGroup, bool listen, bool excludeMessagesFromSelf) 
            : this(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp), mcastGroup, listen, excludeMessagesFromSelf) 
        { 
        }

        public MulticastConnection(Socket udpSocket, IPEndPoint mcastGroup, bool listen, bool excludeMessagesFromSelf) 
        {
            this.DataReceivedHandler = new DataReceivedHandler(this.DefaultDataReceivedHandler);
            this.ExcludeMessagesFromSelf = excludeMessagesFromSelf;
            this.Socket = udpSocket;
            this.Socket.SendBufferSize = 8192 * 2;
            this.EndPoint = IPEndPointEx.LocalEndPoint(mcastGroup.Port, true);
            this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.Socket.ExclusiveAddressUse = false;
            this.Socket.Bind(this.EndPoint);
            this.McastEndPoint = mcastGroup;
            this.JoinGroup(listen);
            lock (_locks)
            {
                if (!_locks.ContainsKey(mcastGroup.ToString()))
                {
                    _locks.Add(mcastGroup.ToString(), new object());
                }
            }
            _lock = _locks[mcastGroup.ToString()];
            this.ContentType = this.DefaultFormat;
            this.TextEncoding = Encoding.Unicode;
            this.Cleaner = new Timer(new TimerCallback(CleanInboundOrphans), null, 1000, 1000);
        }

        public MulticastConnection(IPEndPoint mcastGroup, bool listen, DataReceivedHandler handler)
            : this(mcastGroup, listen, true, handler)
        {

        }
        public MulticastConnection(IPEndPoint mcastGroup, bool listen, bool excludeMessagesFromSelf, DataReceivedHandler handler)
            : this(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp), mcastGroup, listen, excludeMessagesFromSelf, handler)
        {
        }


        public MulticastConnection(Socket udpSocket, IPEndPoint mcastGroup, bool listen, bool excludeMessagesFromSelf, DataReceivedHandler handler)
        {
            if (handler == null) throw new ArgumentException("DataReceivedHandler cannot be null.");

            this.DataReceivedHandler = handler;
            this.ExcludeMessagesFromSelf = excludeMessagesFromSelf;
            this.Socket = udpSocket;
            this.Socket.SendBufferSize = 8192 * 2;
            this.EndPoint = IPEndPointEx.LocalEndPoint(mcastGroup.Port, true); //new IPEndPoint(IPAddress.Any, mcastGroup.Port);
            this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.Socket.ExclusiveAddressUse = false;
            this.Socket.Bind(this.EndPoint);
            this.McastEndPoint = mcastGroup;
            this.JoinGroup(listen);
            lock (_locks)
            {
                if (!_locks.ContainsKey(mcastGroup.ToString()))
                {
                    _locks.Add(mcastGroup.ToString(), new object());
                }
            }
            _lock = _locks[mcastGroup.ToString()];
            this.ContentType = this.DefaultFormat;
            this.TextEncoding = Encoding.Unicode;
            this.Cleaner = new Timer(new TimerCallback(CleanInboundOrphans), null, 1000, 1000);
        }

        private DataReceivedHandler DataReceivedHandler;

        private System.Threading.Timer Cleaner;
        private void CleanInboundOrphans(object state)
        {
            uint[] orphans = new uint[0];
            System.DateTime now = CurrentTime.Now;
            lock (this._udpInboundMessages)
            {
                try
                {
                    orphans = this._udpInboundMessages
                        .Where(kvp => kvp.Value.UdpSegments.Length > 0 && kvp.Value.UdpSegments.Count(s => s.TimeToLive >= now) > 0)
                        .Select(kvp => kvp.Key).ToArray();
                }
                catch { }
            }

            for (int i = 0; i < orphans.Length; i++)
            {
                this._udpInboundMessages.Remove(orphans[i]);
            }
        }

        public void Send(byte[] data)
        {
            lock (_lock)
            {
                Socket.SendTo(data, this.McastEndPoint);
                BytesSentRate.IncrementBy(data.Length);
                BytesSentTotal.IncrementBy(data.Length);
            }
        }

        public void Send(Message message)
        {
            if (this.TextEncoding == null)
                if (message.Encoding != null)
                    this.TextEncoding = Encoding.GetEncoding(message.Encoding);
                else
                    this.TextEncoding = Encoding.Unicode;

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
        public ServiceOperation Call(string application, string objectPath, string operation, string format, TimeSpan timespan, params ServiceParameter[] parms)
        {
            string uri = string.Format(
                "udp://{0}/{1}/{2}({3})[{4}]",
                this.McastEndPoint.ToString(),
                application,
                objectPath,
                operation,
                format);
            ServiceOperation op = new ServiceOperation(OperationType.Request, ServiceType.RequestResponse, uri, parms);

            IConnection connection = this;
            Message msg = new Message(op);

            Message resp = connection.Call(msg, timespan);

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

        public Socket Socket { get; private set; }
        public EndPoint EndPoint { get; set; }
        public EndPoint McastEndPoint { get; set; }
        public Protocol Protocol { get { return Messaging.Protocol.Udp; } }
        public Altus.Core.Processing.Action Action { get { return Processing.Action.POST; } }
        public bool ExcludeMessagesFromSelf { get; private set; }

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
            _aspects = new Dictionary<string, object>();
            _encoding = null;
            _length = 0;
            _type = null;
        }

        public string DefaultFormat { get { return StandardFormats.BINARY; } }

        public void JoinGroup(bool listen)
        {
            this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            Logger.LogInfo("Joining Multicast Group: " + this.McastEndPoint.ToString() + ", on Local Address: " + this.EndPoint.ToString());
            this.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(((IPEndPoint)this.McastEndPoint).Address, ((IPEndPoint)this.EndPoint).Address));
            this.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
            this.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
            if (listen)
            {
                ReadMessages();
            }
        }

        public void LeaveGroup()
        {
            try
            {
                Logger.LogInfo("Leaving Multicast Group: " + this.McastEndPoint.ToString() + ", from Local Address: " + this.EndPoint.ToString());
                this.Socket.SetSocketOption(SocketOptionLevel.IP,
                               SocketOptionName.DropMembership,
                               new MulticastOption(((IPEndPoint)this.McastEndPoint).Address, IPEndPointEx.LocalAddress(true)));
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, "An unexpected error occurred leaving Multicast Group: " + this.McastEndPoint.ToString() + ", from Local Address: " + this.EndPoint.ToString());
            }
        }

        Thread _receiveThread;
        protected void ReadMessages()
        {
            _receiveThread = new Thread(new ThreadStart(ReadMessagesLoop));
            _receiveThread.Name = "UDP Multicast Listener [" + this.McastEndPoint.ToString() + "]";
            _receiveThread.Priority = ThreadPriority.Highest;
            _receiveThread.IsBackground = true;
            _receiveThread.Start();

        }

        private void DefaultDataReceivedHandler(object sender, DataReceivedArgs e)
        {
            MessageSegment segment;
            if (MessageSegment.TryCreate(this, Protocol.Udp, EndPoint, e.Buffer, out segment))
                ProcessInboundUdpSegment(segment);
        }

        private void ReadMessagesLoop()
        {
            Context.CurrentContext = Context.GlobalContext;
            while (!disposed)
            {
                //DateTime start = CurrentTime.Now;
                try
                {
                    
                    //System.Diagnostics.Debug.WriteLine("Multicast Loop Start:\t\t" + CurrentTime.Now.ToString("yyyy-MM-dd H:mm:ss.ffffff") + ";\tAvailable: " + this.Socket.Available + ";\tdT = " + CurrentTime.Now.Subtract(start).TotalMilliseconds);

                    byte[] buffer = new byte[SocketOptions.BUFFER_SIZE];
                    EndPoint ep = this.McastEndPoint;
                    //System.Diagnostics.Debug.WriteLine("Multicast Rcv Start:\t\t" + CurrentTime.Now.ToString("yyyy-MM-dd H:mm:ss.ffffff") + ";\tAvailable: " + this.Socket.Available + ";\tdT = " + CurrentTime.Now.Subtract(start).TotalMilliseconds);
                    int read = this.Socket.ReceiveFrom(buffer, ref ep);
                    //System.Diagnostics.Debug.WriteLine("Multicast Rcv End:\t\t" + CurrentTime.Now.ToString("yyyy-MM-dd H:mm:ss.ffffff") + ";\tAvailable: " + this.Socket.Available + ";\tdT = " + CurrentTime.Now.Subtract(start).TotalMilliseconds);

                    BytesReceivedRate.IncrementBy(read);
                    if (read > 0)
                    {
                        this.DataReceivedHandler(this, new DataReceivedArgs(buffer, read, ep, this.McastEndPoint));
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

                //System.Diagnostics.Debug.WriteLine("Multicast Loop End:\t\t" + CurrentTime.Now.ToString("yyyy-MM-dd H:mm:ss.ffffff") + ";\tAvailable: " + this.Socket.Available + ";\tdT = " + CurrentTime.Now.Subtract(start).TotalMilliseconds);
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
                if (ExcludeMessagesFromSelf
                    && message.Sender.Equals(NodeIdentity.NodeAddress, StringComparison.InvariantCultureIgnoreCase)) return; // don't process out own mutlicast publications

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

        protected virtual void OnSocketException(Exception e)
        {
            if (this.SocketException != null)
            {
                this.SocketException(this, new SocketExceptionEventArgs(this, e));
            }
            OnDisconnected();
        }

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
            lock (this)
            {
                try
                {
                    if (_receiveThread != null)
                    {
                        _receiveThread.Abort();
                        _receiveThread = null;
                    }
                    this.LeaveGroup();
                    this.Socket.Close();
                    this.Socket.Dispose();
                    if (this.Cleaner != null)
                    {
                        this.Cleaner.Dispose();
                        this.Cleaner = null;
                    }
                    OnDisconnected();
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

        #endregion
    }
}
