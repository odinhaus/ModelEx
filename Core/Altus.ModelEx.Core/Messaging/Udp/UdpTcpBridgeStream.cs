using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;
using Altus.diagnostics;
using System.Threading;
using Altus.compression;
using Altus.streams;
using Altus.messaging;
using Altus.messaging.tcp;
using Altus.processing;
using Altus.serialization;

namespace Altus.messaging.udp
{
    public class UdpTcpBridgeStream : NetworkStream, IConnection
    {
        private UdpTcpBridgeState _readState;
        private static UdpTcpBridgeState _udpReadState;
        

        public static void SetUdpSocket(Socket udpSocket, ref EndPoint multicastEndPoint)
        {
            if (udpSocket.ProtocolType != ProtocolType.Udp)
                throw (new InvalidOperationException("udpSocket only supports UDP sockets."));
            NetworkStreams = new List<UdpTcpBridgeStream>();
            UdpEndPoint = multicastEndPoint; 
            UdpSocket = udpSocket;
            ReadUdpMessages();
        }

        private static void ReadUdpMessages()
        {
            _udpReadState = new UdpTcpBridgeState(ProtocolType.Udp);

            EndPoint remoteEP = UdpEndPoint;
            _udpReadState.AsyncResult = UdpSocket.BeginReceiveFrom(_udpReadState.Buffer, 0, _udpReadState.Buffer.Length,
                SocketFlags.None,
                ref remoteEP,
                new AsyncCallback(ReadMessageUdpCB),
                _udpReadState);
        }

        public static Socket UdpSocket { get; private set; }
        internal static List<UdpTcpBridgeStream> NetworkStreams { get; private set; }
        public Protocol Protocol { get { return messaging.Protocol.Tcp; } }

        public UdpTcpBridgeStream(Socket socket) : base(socket) 
        {
            if (socket.ProtocolType != ProtocolType.Tcp)
                throw (new InvalidOperationException("socket only supports TCP sockets."));
            EndPoint = socket.RemoteEndPoint; 
            ReceivedBy = ASCIIEncoding.ASCII.GetBytes(EndPoint.ToString());
            NetworkStreams.Add(this);
            ReadMessages();
        }
        public UdpTcpBridgeStream(Socket socket, bool ownsSocket) : base(socket, ownsSocket) 
        {
            if (socket.ProtocolType != ProtocolType.Tcp)
                throw (new InvalidOperationException("socket only supports TCP sockets."));
            EndPoint = socket.RemoteEndPoint;
            ReceivedBy = ASCIIEncoding.ASCII.GetBytes(EndPoint.ToString());
            NetworkStreams.Add(this);
            ReadMessages();
        }
        public UdpTcpBridgeStream(Socket socket, FileAccess fa) : base(socket, fa) 
        {
            if (socket.ProtocolType != ProtocolType.Tcp)
                throw (new InvalidOperationException("socket only supports TCP sockets."));
            EndPoint = socket.RemoteEndPoint;
            ReceivedBy = ASCIIEncoding.ASCII.GetBytes(EndPoint.ToString());
            NetworkStreams.Add(this);
            ReadMessages();
        }
        public UdpTcpBridgeStream(Socket socket, FileAccess fa, bool ownsSocket) : base(socket, fa, ownsSocket) 
        {
            if (socket.ProtocolType != ProtocolType.Tcp)
                throw (new InvalidOperationException("socket only supports TCP sockets."));
            EndPoint = socket.RemoteEndPoint;
            ReceivedBy = ASCIIEncoding.ASCII.GetBytes(EndPoint.ToString());
            NetworkStreams.Add(this);
            ReadMessages();
        }

        public new Socket Socket { get { return base.Socket; } }
        public EndPoint EndPoint { get; private set; }
        public byte[] ReceivedBy { get; private set; }
        public static EndPoint UdpEndPoint { get; private set; }

        protected void ReadMessages()
        {
            _readState = new UdpTcpBridgeState(this, ProtocolType.Tcp);
            _udpReadState = new UdpTcpBridgeState(this, ProtocolType.Udp);
          
            _readState.AsyncResult = this.BeginRead(_readState.Buffer, 0, _readState.Buffer.Length,
                new AsyncCallback(ReadMessageCB),
                _readState);
        }

        private static void ReadMessageUdpCB(IAsyncResult ar)
        {
            UdpTcpBridgeState state = (UdpTcpBridgeState)ar.AsyncState;
            try
            {
                EndPoint remoteEP = UdpEndPoint;
                int read = UdpSocket.EndReceiveFrom(ar, ref remoteEP);
                if (read > 0)
                {
                    // we got some data
                    state.AppendData(read);
                    
                    state.AsyncResult = UdpSocket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length,
                        SocketFlags.None,
                        ref remoteEP,
                        new AsyncCallback(ReadMessageUdpCB),
                        state);
                }
                else
                {
                    // socket died - bail out

                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void ReadMessageCB(IAsyncResult ar)
        {
            UdpTcpBridgeState state = (UdpTcpBridgeState)ar.AsyncState;
            try
            {
                int read = state.NetworkStream.EndRead(ar);
                if (read > 0)
                {
                    // we got some data
                    state.AppendData(read);
                    state.AsyncResult = this.BeginRead(state.Buffer, 0, state.Buffer.Length,
                        new AsyncCallback(ReadMessageCB),
                        state);
                }
                else
                {
                    // socket died - bail out
                    
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
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
            }
        }

        //protected Message MessageFromContext(ServiceContext ctx)
        //{
        //    Message message = new Message(
        //        ctx.ResponseFormat.Equals(StandardFormats.PROTOCOL_DEFAULT) ? ctx.Connection.DefaultFormat : ctx.ResponseFormat,
        //        ctx.Id,
        //        ctx.ServiceUri.ToString(),
        //        ctx.ServiceType,
        //        ctx.Sender,
        //        ctx.Action);
        //    message.CorrelationId = ctx.CorrelationId;
        //    message.DeliveryGuaranteed = ctx.DeliveryGuaranteed;
        //    message.Recipients = ctx.Recipients;
        //    //message.Parameters.Add("Context", ctx.GetType().FullName, ParameterDirection.Aspect, ctx);
        //    message.Timestamp = ctx.Timestamp;
        //    return message;
        //}

        //public void WriteMessage(ServiceContext ctx)
        //{
        //    try
        //    {
        //        Message message = MessageFromContext(ctx);
        //        byte[] data = message.ToByteArray();
        //        foreach (byte[] chunk in Altus.messaging.tcp.SocketOptions.Chunk(data))
        //        {
        //            this.Write(chunk, 0, data.Length);
        //            this.Flush();
        //        }
                
        //        //Logger.Log("Sent " + message.Size + " byte message to " + this.EndPoint.ToString());
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.Log(e);
        //    }
        //}

        public override void Close()
        {
            if (_readState != null
                && this.CanRead)
            {
                Logger.Log("Closed connection to " + this.EndPoint.ToString());
            }
            base.Close();
        }

        internal void Broadcast(byte[] messageBlock)
        {
            foreach(byte[] chunk in SocketOptions.Chunk(messageBlock))
                UdpSocket.SendTo(chunk, UdpEndPoint);
        }

        public void Send(byte[] data)
        {
            this.Write(data, 0, data.Length);
        }

        public void Send(Message message)
        {
            throw (new NotImplementedException());
        }

        public EndPoint RemoteEndPoint
        {
            get { return EndPoint; }
        }


        public Encoding TextEncoding
        {
            get;
            set;
        }

        public long ContentLength
        {
            get;
            set;
        }

        public string ContentType
        {
            get;
            set;
        }

        public string DefaultFormat { get { return StandardFormats.BINARY; } }


        public Message Request(Message msg)
        {
            throw new NotImplementedException();
        }
    }

    public class UdpTcpBridgeState
    {
        static byte[] _netId;
        byte[] _rcvdBy;

        static UdpTcpBridgeState()
        {
            _netId = ASCIIEncoding.ASCII.GetBytes(Context.GetEnvironmentVariable("NetworkId").ToString());
        }

        public UdpTcpBridgeState(ProtocolType protocol)
        {
            ReadStream = new MemoryStream();
            ClearBuffer();
            ExtractPosition = 0;
            ProtocolType = protocol;
        }

        public UdpTcpBridgeState(UdpTcpBridgeStream stream, ProtocolType protocol)
        {
            ReadStream = new MemoryStream();
            ClearBuffer();
            NetworkStream = stream;
            ExtractPosition = 0;
            ProtocolType = protocol;
            _rcvdBy = this.NetworkStream.ReceivedBy;
        }

        public Stream ReadStream { get; private set; }
        public IAsyncResult AsyncResult { get; internal set; }
        public byte[] Buffer { get; private set; }
        public UdpTcpBridgeStream NetworkStream { get; private set; }
        public int ExtractPosition { get; set; }
        public byte Version { get; set; }
        public byte Compressed { get; set; }
        public ProtocolType ProtocolType { get; private set; }

        private void ClearBuffer()
        {
            Buffer = new byte[SocketOptions.BUFFER_SIZE];
        }

        private void ExtractMessages(int read)
        {
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
                    byte[] messageBlock = new byte[5 + ExtractPosition];
                    ReadStream.Position = 0;
                    ReadStream.Read(messageBlock, 0, messageBlock.Length);

                    if (ProtocolType == System.Net.Sockets.ProtocolType.Tcp)
                    {
                        // record that our TCP endpoint received this message already so that we don't get into a broadcast loop
                        // when it gets bounced back to us on our local UDP endpoint
                        byte len = messageBlock[5];
                        for (int i = 1; i < this.NetworkStream.ReceivedBy.Length + 1; i++)
                        {
                            messageBlock[i + 5] = _rcvdBy[i - 1];
                        }
                        this.NetworkStream.Broadcast(messageBlock);
                    }
                    else
                    {
                        // check each TCP client one at a time
                        foreach (UdpTcpBridgeStream ns in UdpTcpBridgeStream.NetworkStreams)
                        {
                            // check this multicast message didn't come from our TCP end point
                            byte len = messageBlock[5];
                            byte[] rcvdBy = new byte[len];
                            ns.ReceivedBy.CopyTo(rcvdBy, 0);
                            bool isFromMe = true;

                            for (int i = 0; i < len; i++)
                            {
                                if (messageBlock[i + 6] != rcvdBy[i])
                                {
                                    isFromMe = false;
                                    break;
                                }
                            }
                            
                            if (!isFromMe)
                            {
                                // unicast send UDP message to TCP bridge client
                                ns.Write(messageBlock, 0, messageBlock.Length);
                            }
                        }
                    }
                    
                    
                    MemoryStream ms = new MemoryStream();
                    StreamHelper.Copy(ReadStream,
                        ReadStream.Position,
                        ReadStream.Length - (messageBlock.Length),
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
        }

        internal void AppendData(int read)
        {
            ReadStream.Write(this.Buffer, 0, read);
            this.ExtractMessages(read);
            this.ClearBuffer();
        }
    }
}
