using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core;
using System.Net;

namespace Altus.Core.Messaging.Udp
{
    public class UdpSegmentSegment : MessageSegment
    {
        public UdpSegmentSegment(IConnection connection, EndPoint ep, byte[] data) : base(connection, Protocol.Udp, ep, data) { }
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
        protected override bool OnIsValid()
        {
            try
            {
                return this.SegmentLength <= this.Data.Length
                    && base.SegmentType == SegmentType.Segment
                    && this.Sender > 0
                    && this.MessageId > 0
                    && this.SegmentNumber > 0;
            }
            catch { return false; }
        }

        private uint _segNo;
        public override uint SegmentNumber
        {
            get
            {
                if (_segNo == 0 && Data != null)
                {
                    _segNo = Data[13];
                }
                return _segNo;
            }
        }
        private DateTime _ttl = DateTime.MinValue;
        public DateTime TimeToLive
        {
            get
            {
                if (_ttl == DateTime.MinValue && Data != null)
                {
                    byte[] ttl = new byte[8];
                    for (int i = 0; i < 8; i++)
                        ttl[i] = Data[i + 14];
                    _ttl = DateTime.FromBinary(BitConverter.ToInt64(ttl, 0)).ToLocalTime();
                }
                return _ttl;
            }
        }
        private ushort _pl;
        public override ushort PayloadLength
        {
            get
            {
                if (_pl == 0 && Data != null)
                {
                    byte[] pl = new byte[2];
                    pl[0] = Data[22];
                    pl[1] = Data[23];
                    _pl = BitConverter.ToUInt16(pl, 0);
                }
                return _pl;
            }
        }
        byte[] _plData;
        public override byte[] Payload
        {
            get
            {
                if (_plData == null && Data != null)
                {
                    _plData = new byte[PayloadLength];
                    Data.Copy(24, _plData, 0, PayloadLength);
                }
                return _plData;
            }
        }
        public int HeaderLength
        {
            get { return 24; }
        }

        public int SegmentLength
        {
            get
            {
                return HeaderLength + PayloadLength;
            }
        }
    }
}
