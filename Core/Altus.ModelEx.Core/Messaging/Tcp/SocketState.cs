using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using certes.common.compression;
using certes.common.streams;

namespace certes.common.tcp
{
    public class SocketState
    {
        ManualResetEvent _wait = new ManualResetEvent(false);
        NetworkStream _networkStream;

        public SocketState(NetworkStream stream)
        {
            ReadStream = new MemoryStream();
            WaitHandle = _wait;
            MessageQueue = new Queue<Message>();
            RawQueue = new Queue<byte[]>();
            _networkStream = stream;
            NetworkStream = stream;
            ExtractPosition = 0;
        }

        public Stream ReadStream { get; private set; }
        public ManualResetEvent WaitHandle { get; private set; }
        private Queue<Message> MessageQueue { get; set; }
        private Queue<byte[]> RawQueue { get; set; }
        public NetworkStream NetworkStream { get; private set; }
        public int ExtractPosition { get; set; }
        public byte Version { get; set; }
        public byte Compressed { get; set; }

        public void EnqueueBuffer(byte[] buffer)
        {
            RawQueue.Enqueue(buffer);
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
                    msg.ReceivedBy = ((SocketStream)this.NetworkStream).EndPoint.ToString().PadRight(21);
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
    }
}
