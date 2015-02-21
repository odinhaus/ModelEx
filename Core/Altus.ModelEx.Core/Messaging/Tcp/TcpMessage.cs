using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Altus.Core.Security;

namespace Altus.Core.Messaging.Tcp
{
    public class TcpMessage : IComparer<MessageSegment>
    {
        static uint MESSAGE_ID = 1;
        static object _lock = new object();
        MD5 _hasher = MD5.Create();

        public TcpMessage(IConnection connection) 
        { 
            TcpSegmentsPrivate = new List<MessageSegment>(); 
            Connection = connection; 
            lock (_lock) 
            { 
                this.MessageId = MESSAGE_ID; MESSAGE_ID++; 
            } 
        }
        public TcpMessage(IConnection connection, Message source)
            : this(connection)
        {
            FromMessage(source);
        }

         public TcpMessage(IConnection connection, MessageSegment segment)
            : this(connection)
        {
            this.MessageId = segment.MessageId;
            this.Sender = segment.Sender;
            if (segment is TcpHeaderSegment)
            {
                this.TcpHeaderSegment = (TcpHeaderSegment)segment;
            }
            
            this.AddSegment(segment);
        }

        private void FromMessage(Message source)
        {
            MemoryStream ms = new MemoryStream(source.ToByteArray());
            this.Sender = NodeIdentity.NodeId;
            /** ======================================================================================================================================
             * TCP HEADER DESCRIPTOR
             * FIELD            LENGTH (bytes)      POS         SUBFIELDS/Description
             * TAG              1                   0           VVVVVVSC - Version (6 bits), Segment Type (0 = Header, 1 = Segment), Compressed (0 = false, 1 = true)
             * SENDERID         8                   1           Alpha-Numeric Unique Sender ID
             * MESSAGEID        4                   9           Sequential UINT per SENDER
             * MESSAGEHASH      16                  13          byte[] MD5 hash using secret hashkey + message body
             * SEGEMENTCOUNT    4                   29          total count of message segments, including header segment for complete message
             * TIMETOLIVE       8                   33          absolute message expiration date/time in UTC for message reassembly to occur, before message is discarded
             * DATALENGTH       2                   41          length in bytes of any included transfer data
             * DATA             N (up to 1024 - 40) 43            included message data
             * =======================================================================================================================================
             * Total            43 bytes            
         */
            ushort headerLength = (ushort)Math.Min(ms.Length, SocketOptions.MTU_SIZE - 43);
            byte[] hdr = new byte[43];
            hdr[0] = (byte)0;
            byte[] nodeIdNum = BitConverter.GetBytes(this.Sender);
            byte[] msgIdNum = BitConverter.GetBytes(this.MessageId);
            nodeIdNum.CopyTo(hdr, 1);
            msgIdNum.CopyTo(hdr, 9);
            

            byte[] data = new byte[headerLength];
            ms.Read(data, 0, headerLength);

            byte[] hdrData = new byte[hdr.Length + data.Length];
            byte[] secretData = NodeIdentity.SecretKey(source.Sender);
            byte[] cryptoData = new byte[secretData.Length + data.Length];
            secretData.CopyTo(cryptoData, 0);
            data.CopyTo(cryptoData, secretData.Length);

            _hasher.ComputeHash(cryptoData).CopyTo(hdr, 13);
            uint segmentCount = 1;
            if (ms.Length > SocketOptions.MTU_SIZE - 43)
            {
                int len = (int)ms.Length - (SocketOptions.MTU_SIZE - 43);
                segmentCount += (uint)Math.Ceiling((float)len / (float)(SocketOptions.MTU_SIZE - 19));
            }
            segmentCount.GetBytes().CopyTo(hdr, 29);
            BitConverter.GetBytes(source.Timestamp.Add(source.TTL).ToBinary()).CopyTo(hdr, 33);
            BitConverter.GetBytes((ushort)data.Length).CopyTo(hdr, 41);
            hdr.CopyTo(hdrData, 0);
            data.CopyTo(hdrData, 43);

            TcpHeaderSegment ths = new TcpHeaderSegment(Connection, Connection.EndPoint, hdrData);
            this.TcpHeaderSegment = ths;
            //this.TcpSegmentsPrivate.Add(ths);
            this.AddSegment(ths);
            uint segNo = 2;
            while (ms.Position < ms.Length)
            {
                /* =======================================================================================================================================
                 * TCP SEGMENT DESCRIPTOR
                 * FIELD            LENGTH              POS     SUBFIELDS/Description
                 * TAG              1                   0       NNNNNNSN - Not Used (6 bits), Segment Type (0 = Header, 1 = Segment), Not Used (1 bit)
                 * SENDERID         8                   1       Alpha-Numeric Unique Sender ID
                 * MESSAGEID        4                   9       Sequential UINT per SENDER
                 * SEGMENT NUMBER   4                   13
                 * DATALENGTH       2                   17      length in bytes of any included transfer data
                 * DATA             N (up to 1024 - 14) 19      included message data
                 * =======================================================================================================================================
                 * Total            19 bytes     
             */
                ushort segLength = (ushort)Math.Min(ms.Length - ms.Position, SocketOptions.MTU_SIZE - 19);
                byte[] seg = new byte[19];
                
                byte[] sdata = new byte[segLength];
                ms.Read(sdata, 0, segLength);

                seg[0] = (byte)(1 << 1);
                nodeIdNum.CopyTo(seg, 1);
                msgIdNum.CopyTo(seg, 9);
                segNo.GetBytes().CopyTo(seg, 13);
                segNo++;
                BitConverter.GetBytes(segLength).CopyTo(seg, 17);

                byte[] segData = new byte[seg.Length + sdata.Length];
                seg.CopyTo(segData, 0);
                sdata.CopyTo(segData, 19);

                TcpSegmentSegment tss = new TcpSegmentSegment(Connection, Connection.EndPoint, segData);
                //this.TcpSegmentsPrivate.Add(tss);
                this.AddSegment(tss);
            }
        }


        public IConnection Connection { get; private set; }
        public TcpHeaderSegment TcpHeaderSegment { get; set; }
        private List<MessageSegment> TcpSegmentsPrivate { get; set; }
        bool _sorted = false;
        MessageSegment[] _segments;
        public MessageSegment[] TcpSegments
        {
            get
            {
                if (!_sorted)
                {
                    TcpSegmentsPrivate.Sort(this);
                    _sorted = true;
                    _segments = null;
                }
                if (_segments == null)
                {
                    _segments = TcpSegmentsPrivate.ToArray();
                }
                return _segments;
            }
        }
        public ulong Sender { get; private set; }
        public uint MessageId { get; private set; }

        public bool IsComplete
        {
            get
            {
                return this.TcpHeaderSegment != null
                    && this.TcpHeaderSegment.SegmentCount == TcpSegmentsPrivate.Count;
            }
        }


        Stream _payload = null;
        public Stream Payload
        {
            get
            {
                if (_payload == null && IsComplete)
                {
                    MessageSegment[] segments = TcpSegments;
                    MemoryStream ms = new MemoryStream();
                    for (int i = 0; i < segments.Length; i++)
                    {
                        ms.Write(segments[i].Payload, 0, segments[i].PayloadLength);
                    }
                    ms.Position = 0;
                    _payload = ms;
                }
                return _payload;
            }
        }

        public void AddSegment(MessageSegment segment)
        {
            if (segment.MessageId == 0) return;
            TcpSegmentsPrivate.Add(segment);
            _sorted = false;
        }

        public int Compare(MessageSegment x, MessageSegment y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return x.SegmentNumber.CompareTo(y.SegmentNumber);
        }
    }
}
