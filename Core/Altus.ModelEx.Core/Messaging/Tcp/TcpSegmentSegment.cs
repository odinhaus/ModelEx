using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Altus.Core;

namespace Altus.Core.Messaging.Tcp
{
    public class TcpSegmentSegment : MessageSegment
    {
        public TcpSegmentSegment(IConnection connection, EndPoint ep, byte[] data) : base(connection, Protocol.Tcp, ep, data) { }
        /* =======================================================================================================================================
             * TCP SEGMENT DESCRIPTOR
             * FIELD            LENGTH              POS     SUBFIELDS/Description
             * TAG              1                   0       NNNNNNSN - Not Used (6 bits), Segment Type (0 = Header, 1 = Segment), Not Used (1 bit)
             * SENDERID         8                   1       Alpha-Numeric Unique Sender ID
             * MESSAGEID        4                   9       Sequential UINT per SENDER
             * SEGMENT NUMBER   4                   13 
             * DATALENGTH       2                   17      length in bytes of any included transfer data
             * DATA             N (up to 1024 - 23) 19      included message data
             * =======================================================================================================================================
             * Total            19 bytes     
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
                    _segNo = Data.ToUInt32(13);
                }
                return _segNo;
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
                    pl[0] = Data[17];
                    pl[1] = Data[18];
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
                    Data.Copy(19, _plData, 0, PayloadLength);
                }
                return _plData;
            }
        }

        public int HeaderLength
        {
            get { return 19; }
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
