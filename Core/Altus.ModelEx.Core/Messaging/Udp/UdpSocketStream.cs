using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using certes.common.messaging.tcp;
using System.Net.Sockets;
using certes.common.compression;
using System.IO;
using certes.common.streams;
using System.Threading;
using certes.common.messaging;
using certes.common.diagnostics;
using System.Net;
using System.Diagnostics;

namespace certes.common.messaging.udp
{
   

    public delegate void UdpSocketExceptionHandler(object sender, UdpSocketExceptionEventArgs e);
    public class UdpSocketExceptionEventArgs
    {
        public UdpSocketExceptionEventArgs(UdpSocketStream sender, Exception e)
        {
            SocketException = e;
            SocketStream = sender;
        }

        public Exception SocketException { get; private set; }
        public UdpSocketStream SocketStream { get; set; }
    }

    public class UdpSocketStream
    {
        public event UdpSocketExceptionHandler SocketException;


        public SocketState _readState;
        public UdpSocketStream(Socket udpSocket, ref EndPoint multicastEndPoint) 
        {
            if (udpSocket.ProtocolType != ProtocolType.Udp)
                throw (new InvalidOperationException("UdpSocketStream only supports UDP sockets."));
            this.Socket = udpSocket;
            this.EndPoint = multicastEndPoint;
            ReadMessages(); 
        }

        public Socket Socket { get; private set; }
        public EndPoint EndPoint { get; set; }

        public void BroadcastMessage(MessagingContext ctx)
        {
            try
            {
                Message message = MessageFromContext(ctx);
                byte[] data = message.ToByteArray();

                this.Socket.SendTo(data, EndPoint);

                //if (this.MessageSent != null)
                //{
                //    try
                //    {
                //        this.OnTcpMessageSent(ContextFromMessage(message));
                //    }
                //    catch { }
                //}
                //Logger.Log("Sent " + message.Size + " byte message to " + this.EndPoint.ToString());
            }
            catch (Exception e)
            {
                if (this.SocketException != null)
                {
                    try
                    {
                        this.SocketException(this, new UdpSocketExceptionEventArgs(this, e));
                    }
                    catch { }
                }
            }
        }

        public void Write(byte[] data, System.Net.EndPoint ep)
        {
            try
            {
                foreach (byte[] chunk in SocketOptions.Chunk(data))
                    this.Socket.SendTo(chunk, ep);


                //if (this.MessageSent != null)
                //{
                //    try
                //    {
                //        this.OnTcpMessageSent(ContextFromMessage(message));
                //    }
                //    catch { }
                //}
                Logger.Log("Sent " + data.Length + " byte message to " + this.EndPoint.ToString());
            }
            catch (Exception e)
            {
                if (this.SocketException != null)
                {
                    try
                    {
                        this.SocketException(this, new UdpSocketExceptionEventArgs(this, e));
                    }
                    catch { }
                }
            }
        }

        public void WriteMessage(MessagingContext ctx, EndPoint ep)
        {
            try
            {
                Message message = MessageFromContext(ctx);
                byte[] data = message.ToByteArray();

                int sent = 0;

                foreach (byte[] chunk in SocketOptions.Chunk(data))
                {
                    while(sent != chunk.Length)
                    {
                        sent = this.Socket.SendTo(chunk, ep);
#if(DEBUG)
                            Debug.Assert(sent == chunk.Length);
#endif
                    }
                }
                

                //if (this.MessageSent != null)
                //{
                //    try
                //    {
                //        this.OnTcpMessageSent(ctx);
                //    }
                //    catch { }
                //}
                //Logger.Log("Sent " + message.Size + " byte message to " + this.EndPoint.ToString());
            }
            catch (Exception e)
            {
                if (this.SocketException != null)
                {
                    try
                    {
                        this.SocketException(this, new UdpSocketExceptionEventArgs(this, e));
                    }
                    catch { }
                }
            }
        }

        protected MessagingContext ContextFromMessage(Message message)
        {
            MessagingContext ctx = new MessagingContext(message.PayloadStream, new MemoryStream());
            ctx.Id = message.Id;
            ctx.CorrelationId = message.CorrelationId;
            ctx.DeliveryGuaranteed = message.DeliveryGuaranteed;
            ctx.MessageType = message.MessageType;
            ctx.Recipients = message.Recipients;
            ctx.Sender = message.Sender;
            ctx.SetServiceUri(message.ServiceUri);
            ctx.ResponseUri = message.ResponseUri;
            ctx.Timestamp = message.Timestamp;
            return ctx;
        }

        protected Message MessageFromContext(MessagingContext ctx)
        {
            Message message = new Message(
                ctx.Id,
                ctx.ServiceUri,
                ctx.MessageType,
                ctx.Sender,
                ctx.Action);
            message.CorrelationId = ctx.CorrelationId;
            message.DeliveryGuaranteed = ctx.DeliveryGuaranteed;
            message.Recipients = ctx.Recipients;
            message.SetPayload(ctx);
            message.Timestamp = ctx.Timestamp;
            message.ResponseUri = ctx.ResponseUri == null ? "" : ctx.ResponseUri.ToString();
            return message;
        }



        /// <summary>
        /// Reads from the network stream until we have at least one complete message
        /// </summary>
        /// <returns></returns>
        public Message ReadMessage()
        {
            Message msg = _readState.NextMessage(true);
            if (msg != null)
            {
                return msg;
            }
            else
            {
                this.Close();
                throw (new InvalidOperationException("The underlying socket has closed, and can no longer be read from."));
            }
        }

        protected void ReadMessages()
        {
            _readState = new SocketState(this);

            EndPoint remoteEP = EndPoint;
            _readState.AsyncResult = Socket.BeginReceiveFrom(_readState.Buffer, 0, _readState.Buffer.Length,
                SocketFlags.None,
                ref remoteEP,
                new AsyncCallback(ReadMessageCB),
                _readState);
        }

        private void ReadMessageCB(IAsyncResult ar)
        {
            SocketState state = (SocketState)ar.AsyncState;
            try
            {
                EndPoint ep = EndPoint;
                int read = Socket.EndReceiveFrom(ar, ref ep);
                if (read > 0)
                {
                    // we got some data
                    if (state.AppendData(read)
                        && this.MessageReceived != null)
                    {
                        Message msg = state.NextMessage(false);
                        while (msg != null)
                        {
                            Logger.Log("Received " + msg.Size + " byte message  from " + this.EndPoint.ToString());
                            MessagingContext ctx = ContextFromMessage(msg);
                            msg.GetPayload(ctx);
                            ctx.RequestObject = msg.Payload;
                            //this.OnTcpMessageReceived(ctx, ep);  // fire off the event on a threadpool thread so we don't block here
                            msg = state.NextMessage(false);
                        }
                    }

                    EndPoint remoteEP = EndPoint;
                    state.AsyncResult = Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length,
                        SocketFlags.None,
                        ref remoteEP,
                        new AsyncCallback(ReadMessageCB),
                        state);
                    
                }
                else
                {
                    // socket died - bail out
                    state.WaitHandle.Set();
                    this.OnSocketException(new IOException("An existing connection was closed by the remote host."));
                }
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
            }
        }

        public void Close()
        {
            _readState.WaitHandle.Set();
            Logger.Log("Closed connection to " + this.EndPoint.ToString());
            Socket.Close();
        }

        //protected void OnTcpMessageReceived(MessagingContext ctx, EndPoint senderEndPoint)
        //{
        //    try
        //    {
        //        if (this.MessageReceived != null)
        //        {
        //            ThreadPool.QueueUserWorkItem(new WaitCallback(DoRaiseEvent),
        //                new WorkItem()
        //                {
        //                    TheDelegate = this.MessageReceived,
        //                    Args = new object[] { this, new UdpMessageReceivedEventArgs(ctx, this) },
        //                    Received = true,
        //                    SenderEndPoint = senderEndPoint
        //                });
        //        }
        //    }
        //    catch (Exception ex) { Logger.LogError("Error OnTcpMessageReceived: " + ex.Message); }
        //}

        //protected void OnTcpMessageSent(MessagingContext ctx)
        //{
        //    try
        //    {
        //        if (this.MessageSent != null)
        //        {
        //            ThreadPool.QueueUserWorkItem(new WaitCallback(DoRaiseEvent),
        //                new WorkItem()
        //                {
        //                    TheDelegate = this.MessageSent,
        //                    Args = new object[] { this, new UdpMessageSentEventArgs(ctx, this) },
        //                    Received = false
        //                });
        //        }
        //    }
        //    catch (Exception ex) { Logger.LogError("Error OnTcpMessageSent: " + ex.Message); }
        //}

        //private void DoRaiseEvent(object state)
        //{
        //    WorkItem wi = (WorkItem)state;
        //    try
        //    {
        //        wi.TheDelegate.DynamicInvoke(wi.Args);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogError("Error DoRaiseEvent: " + ex.GetBaseException().ToString());
        //        if (wi.Received)
        //        {
        //            TcpFault fault = new TcpFault()
        //            {
        //                Code = -500,
        //                Message = ex.GetBaseException().Message,
        //                StackTrace = ex.GetBaseException().StackTrace
        //            };

        //            MessagingContext sourceCtx = GetContextFromArgs(wi.Args);
        //            if (sourceCtx != null)
        //            {
        //                sourceCtx.ResponseObject = fault;
        //                sourceCtx.Action = certes.common.messaging.Action.ERROR;
        //                this.WriteMessage(sourceCtx, wi.SenderEndPoint);
        //            }
        //        }
        //    }
        //}

        //private MessagingContext GetContextFromArgs(object[] p)
        //{
        //    TcpMessageReceivedEventArgs e = p.OfType<TcpMessageReceivedEventArgs>().FirstOrDefault();
        //    if (e != null)
        //    {
        //        return e.MessagingContext;
        //    }
        //    return null;
        //}

        protected virtual void OnSocketException(Exception e)
        {
            if (this.SocketException != null)
            {
                this.SocketException(this, new UdpSocketExceptionEventArgs(this, e));
            }
        }
    }

    public class SocketOptions
    {
        public static int BUFFER_SIZE = 1024;
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

    public class SocketState
    {
        ManualResetEvent _wait = new ManualResetEvent(false);
        Queue<Message> _messages = new Queue<Message>();

        public SocketState(UdpSocketStream stream)
        {
            ReadStream = new MemoryStream();
            WaitHandle = _wait;
            ClearBuffer();
            MessageQueue = _messages;
            NetworkStream = stream;
            ExtractPosition = 0;
        }

        public Stream ReadStream { get; private set; }
        public ManualResetEvent WaitHandle { get; private set; }
        public IAsyncResult AsyncResult { get; internal set; }
        public byte[] Buffer { get; private set; }
        internal Queue<Message> MessageQueue { get; set; }
        public UdpSocketStream NetworkStream { get; private set; }
        public int ExtractPosition { get; set; }
        public byte Version { get; set; }
        public byte Compressed { get; set; }

        public Message NextMessage(bool block)
        {
            Message msg = null;
            if (block)
                WaitHandle.WaitOne();
            lock (MessageQueue)
            {
                if (MessageQueue.Count > 0)
                {
                    msg = MessageQueue.Dequeue();
                    if (MessageQueue.Count == 0)
                        WaitHandle.Reset();
                }
            }

            return msg;
        }

        private void ClearBuffer()
        {
            Buffer = new byte[SocketOptions.BUFFER_SIZE];
        }

        private bool ExtractMessages(int read)
        {
            bool releaseWait = false;
            bool doneReading = false;
            // find the delimiter in the memory stream
            // move the stream position to current position - buffer length - delimiter length

            while (!doneReading)
            {
                if (ExtractPosition == 0
                    && ReadStream.Length >= 5)
                {
                    // first read, get the length from the buffer
                    ReadStream.Position = 0;
                    byte prefix = (byte)ReadStream.ReadByte();
                    Compressed = (byte)(prefix & (1 << 7));
                    Version = (byte)((prefix << 1) >> 1);
                    byte[] length = new byte[4];
                    ReadStream.Read(length, 0, 4);
                    ExtractPosition = BitConverter.ToInt32(length, 0); // length of payload, not including 5 byte header
                }

                if (ReadStream.Length >= ExtractPosition + 5)
                {
                    byte[] messageBlock = new byte[ExtractPosition];
                    ReadStream.Position = 5;
                    ReadStream.Read(messageBlock, 0, messageBlock.Length);
                    if (Compressed == (byte)(1 << 7))
                    {
                        messageBlock = CompressionHelper.Inflate(messageBlock, CompressionType.Zip);// inflated.ToArray();
                    }
                    Message msg = Message.FromByteArray(messageBlock);
                    MessageQueue.Enqueue(msg);
                    releaseWait = true;

                    MemoryStream ms = new MemoryStream();
                    StreamHelper.Copy(ReadStream,
                        ReadStream.Position,
                        ReadStream.Length - (messageBlock.Length + 5),
                        ms); // cut out the footer during the copy
                    ReadStream = ms; // reset the read stream
                    ReadStream.Position = 0; // start looking from the beginning
                    ExtractPosition = 0;
                }
                else
                {
                    doneReading = true;
                    ReadStream.Position = ReadStream.Length;
                }
            }

            if (releaseWait) this.WaitHandle.Set(); //we found 1 (+) message(s) let the people see it
            //ReadStream.Position = ReadStream.Length;
            return releaseWait;
        }

        internal bool AppendData(int read)
        {
            ReadStream.Write(this.Buffer, 0, read);
            bool hasMessages = this.ExtractMessages(read);
            this.ClearBuffer();
            return hasMessages;
        }
    }

    public class WorkItem
    {
        public Delegate TheDelegate { get; set; }
        public object[] Args { get; set; }
        public bool Received { get; set; }
        public EndPoint SenderEndPoint { get; set; }
    }
}
