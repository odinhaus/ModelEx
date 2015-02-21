using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Altus.Core;

namespace Altus.Core.Messaging.Tcp
{
    public class TcpHeaderSegment : MessageSegment
    {
        public TcpHeaderSegment(IConnection connection, EndPoint ep, byte[] data) : base(connection, Protocol.Tcp, ep, data) { }
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

        protected override bool OnIsValid()
        {
            try
            {
                return this.SegmentLength <= this.Data.Length
                    && base.SegmentType == SegmentType.Header
                    && this.MessageId > 0;
            }
            catch { return false; }
        }

        private byte[] _hash;
        public byte[] MessageHash
        {
            get
            {
                if (_hash == null && Data != null)
                {
                    _hash = new byte[16];
                    for (int i = 0; i < 16; i++)
                        _hash[i] = Data[i + 13];
                }
                return _hash;
            }
        }
        private uint _count = 0;
        public uint SegmentCount
        {
            get
            {
                if (_count == 0 && Data != null)
                {
                    _count = Data.ToUInt32(29);
                }
                return _count;
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
                        ttl[i] = Data[i + 33];
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
                    pl[0] = Data[41];
                    pl[1] = Data[42];
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
                    Data.Copy(43, _plData, 0, PayloadLength);
                }
                return _plData;
            }
        }

        public int HeaderLength
        {
            get { return 43; }
        }

        public int SegmentLength
        {
            get
            {
                return HeaderLength + PayloadLength;
            }
        }

        public override uint SegmentNumber
        {
            get { return 1; }
        }
    }
}
