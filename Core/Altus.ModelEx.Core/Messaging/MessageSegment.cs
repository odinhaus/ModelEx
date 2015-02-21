using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Altus.Core.Messaging.Udp;
using Altus.Core.Messaging.Tcp;

namespace Altus.Core.Messaging
{
    public enum Protocol
    {
        Tcp = 1,
        Http = 2,
        Udp = 3
    }

    public enum SegmentType
    {
        Unknown,
        Header,
        Segment
    }


    public abstract class MessageSegment
    {
        public MessageSegment(IConnection connection, Protocol protocol, EndPoint ep, byte[] data)
        {
            Protocol = protocol;
            EndPoint = ep;
            Data = data;
            Connection = connection;
        }

        public abstract byte[] Payload { get; }
        public abstract ushort PayloadLength { get; }
        public abstract uint SegmentNumber { get; }
        public byte[] Data { get; set; }
        public Protocol Protocol { get; set; }
        public EndPoint EndPoint { get; set; }
        public IConnection Connection { get; private set; }
        public bool IsValid { get { return OnIsValid(); } }

        protected abstract bool OnIsValid();

        private ulong _sender;
        public unsafe ulong Sender
        {
            get
            {
                if (_sender == 0 && Data != null)
                {
                    fixed (byte* Pointer = Data)
                    {
                        _sender = *(((ulong*)(Pointer + 1)));
                    }
                }
                return _sender;
            }
        }
        private SegmentType _type = SegmentType.Unknown;
        public SegmentType SegmentType
        {
            get
            {
                if (_type == Messaging.SegmentType.Unknown
                    && Data != null)
                {
                    if (((int)Data[0] & (1 << 1)) == (1 << 1))
                    {
                        // segment
                        _type = Messaging.SegmentType.Segment;
                    }
                    else
                    {
                        // header
                        _type = Messaging.SegmentType.Header;
                    }
                }
                return _type;
            }
        }

        private uint _id = 0;
        public uint MessageId
        {
            get
            {
                if (_id == 0
                    && Data != null)
                {
                    byte[] msgId = new byte[4];
                    for (int i = 0; i < 4; i++)
                        msgId[i] = Data[i + 9];
                    _id = BitConverter.ToUInt32(msgId, 0);
                }
                return _id;
            }
        }

        internal static bool TryCreate(IConnection connection, Protocol protocol, EndPoint ep, byte[] buffer, out MessageSegment segment)
        {
            segment = null;
            switch (protocol)
            {
                case Protocol.Udp:
                    {
                        if (((int)buffer[0] & (1 << 1)) == (1 << 1))
                        {
                            // segment
                            segment = new UdpSegmentSegment(connection, ep, buffer);
                        }
                        else
                        {
                            // header
                            segment = new UdpHeaderSegment(connection, ep, buffer);
                        }
                        break;
                    }
                case Protocol.Tcp:
                    {
                        if (((int)buffer[0] & (1 << 1)) == (1 << 1))
                        {
                            // segment
                            segment = new TcpSegmentSegment(connection, ep, buffer);
                        }
                        else
                        {
                            // header
                            segment = new TcpHeaderSegment(connection, ep, buffer);
                        }
                        break;
                    }
                default:
                    {
                        throw (new NotImplementedException());
                    }
            }
            if (!segment.IsValid) segment = null;
            return segment != null;
        }
    }
}
