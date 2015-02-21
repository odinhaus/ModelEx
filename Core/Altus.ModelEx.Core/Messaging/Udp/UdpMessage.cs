using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Altus.Core.Security;

namespace Altus.Core.Messaging.Udp
{
    public class UdpMessage : IComparer<UdpSegmentSegment>
    {
        static uint MESSAGE_ID = 1;
        MD5 _hasher = MD5.Create();
        static object _lock = new object();

        public UdpMessage(IConnection connection) 
        { 
            UdpSegmentsPrivate = new List<UdpSegmentSegment>(); 
            Connection = connection;
            lock (_lock)
            {
                this.MessageId = MESSAGE_ID;
                MESSAGE_ID++;
            }
        }

        public UdpMessage(IConnection connection, Message source) : this(connection)
        {
            FromMessage(source);
        }

        public UdpMessage(IConnection connection, MessageSegment segment)
            : this(connection)
        {
            this.MessageId = segment.MessageId;
            this.Sender = segment.Sender;
            if (segment is UdpHeaderSegment)
            {
                this.UdpHeaderSegment = (UdpHeaderSegment)segment;
            }
            else if (segment is UdpSegmentSegment)
            {
                this.AddSegment((UdpSegmentSegment)segment);
            }
        }


        private void FromMessage(Message source)
        {
            MemoryStream ms = new MemoryStream(source.ToByteArray());
            this.Sender = NodeIdentity.NodeId;
            /** ======================================================================================================================================
             * UDP HEADER DESCRIPTOR
             * FIELD            LENGTH (bytes)      POS         SUBFIELDS/Description
             * TAG              1                   0           VVVVVVSC - Version (6 bits), Segment Type (0 = Header, 1 = Segment), Compressed (0 = false, 1 = true)
             * SENDERID         8                   1           Alpha-Numeric Unique Sender ID
             * MESSAGEID        4                   9           Sequential UINT per SENDER
             * MESSAGEHASH      16                  13          byte[] MD5 hash using secret hashkey + message body
             * SEGEMENTCOUNT    1                   29          total count of message segments, including header segment for complete message
             * TIMETOLIVE       8                   30          absolute message expiration date/time in UTC for message reassembly to occur, before message is discarded
             * DATALENGTH       2                   38          length in bytes of any included transfer data
             * DATA             N (up to 1024 - 48) 40            included message data
             * =======================================================================================================================================
             * Total            40 bytes            
             */

            ushort headerLength = (ushort)Math.Min(ms.Length, SocketOptions.MTU_SIZE - 40);
            byte[] hdr = new byte[40];
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
            byte segmentCount = 1;
            if (ms.Length > SocketOptions.MTU_SIZE - 40)
            {
                int len = (int)ms.Length - (SocketOptions.MTU_SIZE - 40);
                segmentCount += (byte)Math.Ceiling((float)len / (float)(SocketOptions.MTU_SIZE - 15));
            }
            hdr[29] = segmentCount;
            BitConverter.GetBytes(source.Timestamp.Add(source.TTL).ToBinary()).CopyTo(hdr, 30);
            BitConverter.GetBytes((ushort)data.Length).CopyTo(hdr, 38);
            hdr.CopyTo(hdrData, 0);
            data.CopyTo(hdrData, 40);

            UdpHeaderSegment ths = new UdpHeaderSegment(Connection, Connection.EndPoint, hdrData);
            byte segNo = 2;
            while (ms.Position < ms.Length)
            {
                /* =======================================================================================================================================
                 * UDP SEGMENT DESCRIPTOR
                 * FIELD            LENGTH              POS     SUBFIELDS/Description
                 * TAG              1                   0       NNNNNNSN - Not Used (6 bits), Segment Type (0 = Header, 1 = Segment), Not Used (1 bit)
                 * SENDERID         8                   1       Alpha-Numeric Unique Sender ID
                 * MESSAGEID        4                   9       Sequential UINT per SENDER
                 * SEGMENTNUMBER    1                   13      Segement sequence number 
                 * TIMETOLIVE       8                   14      Message segment expiration datetime
                 * DATALENGTH       2                   22      length in bytes of any included transfer data
                 * DATA             N (up to 1024 - 23) 24      included message data
                 * =======================================================================================================================================
                 * Total            24 bytes     
                 */

                ushort segLength = (ushort)Math.Min(ms.Length - ms.Position, SocketOptions.MTU_SIZE - 24);
                byte[] seg = new byte[24];

                byte[] sdata = new byte[segLength];
                ms.Read(sdata, 0, segLength);

                seg[0] = (byte)(1 << 1);
                nodeIdNum.CopyTo(seg, 1);
                msgIdNum.CopyTo(seg, 9);
                seg[13] = segNo;
                segNo++;
                BitConverter.GetBytes(source.Timestamp.Add(source.TTL).ToBinary()).CopyTo(seg, 14);
                BitConverter.GetBytes(segLength).CopyTo(seg, 22);

                byte[] segData = new byte[seg.Length + sdata.Length];
                seg.CopyTo(segData, 0);
                sdata.CopyTo(segData, 24);

                UdpSegmentSegment tss = new UdpSegmentSegment(Connection, Connection.EndPoint, segData);
                this.UdpSegmentsPrivate.Add(tss);
            }
            this.UdpHeaderSegment = ths;
        }


        public IConnection Connection { get; private set; }
        public UdpHeaderSegment UdpHeaderSegment { get; set; }
        private List<UdpSegmentSegment> UdpSegmentsPrivate { get; set; }
        bool _sorted = false;
        public UdpSegmentSegment[] UdpSegments
        {
            get
            {
                if (!_sorted)
                {
                    UdpSegmentsPrivate.Sort(this);
                    _sorted = true;
                }
                return UdpSegmentsPrivate.ToArray();
            }
        }
        public ulong Sender { get; private set; }
        public uint MessageId { get; private set; }

        public bool IsComplete
        {
            get
            {
                return this.UdpHeaderSegment != null
                    && (this.UdpHeaderSegment.SegmentCount == UdpSegmentsPrivate.Count + 1);
            }
        }

        Stream _payload = null;
        public Stream Payload
        {
            get
            {
                if (_payload == null && IsComplete)
                {
                    UdpSegmentSegment[] segments = UdpSegments;
                    MemoryStream ms = new MemoryStream();
                    ms.Write(UdpHeaderSegment.Payload, 0, UdpHeaderSegment.PayloadLength);
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

        public void AddSegment(UdpSegmentSegment segment)
        {
            if (segment.MessageId == 0) return;
            UdpSegmentsPrivate.Add(segment);
            _sorted = false;
        }

        public int Compare(UdpSegmentSegment x, UdpSegmentSegment y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return x.SegmentNumber.CompareTo(y.SegmentNumber);
        }
    }
}
