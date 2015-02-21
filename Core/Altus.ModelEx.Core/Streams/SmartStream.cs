using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Altus.Core.Configuration;

namespace Altus.Core.Streams
{
    public class SmartStream : Stream
    {
        #region static
        #endregion

        #region fields
        protected Stream _streamBase;
        protected string _file = "";
        protected static int _threshold = 85000;
        protected static bool _deleteOnClose = true;
        #endregion


        #region ctor

        static SmartStream()
        {
            try
            {
                int.TryParse(ConfigurationManager.GetAppSetting("SmartStreamThreshold"),
                    out _threshold);
            }
            catch { }
            try
            {
                bool.TryParse(ConfigurationManager.GetAppSetting("SmartStreamDeleteOnClose"),
                    out _deleteOnClose);
            }
            catch { }
        }

        //===============================================================================//
        /// <summary>
        /// This shouldnever be called by the framework
        /// </summary>
        ~SmartStream()
        {
            Dispose(false);
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Default ctor used when no idea about the eventual 
        /// number of bytes will be written to the stream.
        /// </summary>
        public SmartStream()
        {
            _streamBase = new MemoryStream();
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Use this ctor when you have a belief about the expected
        /// number of bites that will be written into the SmartStream.
        /// 
        /// Expected byte allocations in excess of 85,000 will be streamed
        /// from disk from the start.  Allocations under 85,000 will be streamed
        /// from memory initially, but if the write allocation exceeds 85,000, the 
        /// internal stream type will switch to disk-based to avoid large object
        /// heap allocation (and thereby additional virtual memory pressure which may
        /// or may not be reclaimed during GC).
        /// </summary>
        /// <param name="anticipatedCapacity"></param>
        public SmartStream(long anticipatedCapacity)
        {
            _streamBase = CreateStream(anticipatedCapacity);
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Create a SmartStream from an initial data buffer.
        /// 
        /// If the buffer's length exceeds 85,000 bytes, the stream will
        /// be source from disk, otherwise it will be sourced from a MemoryStream.
        /// </summary>
        /// <param name="buffer"></param>
        public SmartStream(byte[] buffer)
        {
            _streamBase = CreateStream(buffer.Length);
            StreamHelper.Copy(buffer, _streamBase);
            _streamBase.Position = 0;
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Create a SmartStream from an existing stream.
        /// </summary>
        /// <param name="source"></param>
        public SmartStream(Stream source)
        {
            _streamBase = CreateStream(source.Length);
            StreamHelper.Copy(source, _streamBase);
            _streamBase.Position = 0;
        }
        //===============================================================================//

        #endregion

        #region methods
        //===============================================================================//
        /// <summary>
        /// Returns the entire contents of the stream in a byte[]
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            if (_streamBase is MemoryStream)
            {
                return ((MemoryStream)_streamBase).ToArray();
            }
            else
            {
                byte[] bytes = new byte[_streamBase.Length];
                long curPos = _streamBase.Position;
                long lastPos = 0;
                _streamBase.Position = 0;

                byte[] buffer = new byte[2048];

                int read = _streamBase.Read(buffer, 0, buffer.Length);
                while (read > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        bytes[lastPos + i] = buffer[i];
                    }

                    lastPos = _streamBase.Position;
                    read = _streamBase.Read(buffer, 0, buffer.Length);
                }

                _streamBase.Position = curPos;
                return bytes;
            }
        }
        //===============================================================================//

        //===============================================================================//
        /// <summary>
        /// Returns the entire contents of the stream as an ASCII Encoded string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ASCIIEncoding.ASCII.GetString(ToArray());
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Closes the stream and releases any resources associated with the stream
        /// </summary>
        public override void Close()
        {
            _streamBase.Close();
            base.Close();
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Disposes of the stream resources
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {

                if (_streamBase is FileStream)
                {
                    if (_deleteOnClose)
                    {
                        try
                        {
                            System.IO.File.Delete(_file);
                        }
                        catch { }
                    }
                }
                _streamBase.Dispose();
            }
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Creates a stream of the appropriate type based on the capacity provided
        /// </summary>
        /// <param name="capacity"></param>
        /// <returns></returns>
        private Stream CreateStream(long capacity)
        {
            Stream s = null;
            if (capacity >= _threshold)
            {
                _file = System.IO.Path.GetTempFileName();
                s = new FileStream(_file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            else
            {
                s = new MemoryStream();
            }
            return s;
        }
        //===============================================================================//

        #endregion

        #region Abstract Stream
        //===============================================================================//
        /// <summary>
        /// Indicated whether stream can be read from
        /// </summary>
        public override bool CanRead
        {
            get { return _streamBase.CanRead; }
        }
        //===============================================================================//

        //===============================================================================//
        /// <summary>
        /// Indicated whether stream supports seeking
        /// </summary>
        public override bool CanSeek
        {
            get { return _streamBase.CanSeek; }
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Indicates whether stream support writing
        /// </summary>
        public override bool CanWrite
        {
            get { return _streamBase.CanWrite; }
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Flushes all buffered content to stream and clears all buffers
        /// </summary>
        public override void Flush()
        {
            _streamBase.Flush();
        }
        //===============================================================================//

        //===============================================================================//
        /// <summary>
        /// Gets the total length of the stream in bytes
        /// </summary>
        public override long Length
        {
            get { return _streamBase.Length; }
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Gets/sets the current read/write pointer position in the stream
        /// </summary>
        public override long Position
        {
            get
            {
                return _streamBase.Position;
            }
            set
            {
                _streamBase.Position = value;
            }
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Reads data from the stream into the provided byte buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _streamBase.Read(buffer, offset, count);
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Seeks the specified position in the stream
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _streamBase.Seek(offset, origin);
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Sets the new desired length of the stream.  If this length exceeds _threshold bytes,
        /// this stream will be converted to a file-based stream - otherwise it will be converted
        /// to an in-memory based stream.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            // check stream conversion && convert stream if required
            if (value >= _threshold && _streamBase is MemoryStream)
            {
                // expand the stream
                Stream temp = CreateStream(value);
                StreamHelper.Copy(_streamBase, temp);
                _streamBase = temp;
                // fill the rest of the unallocated space
                _streamBase.SetLength(value);
            }
            else if (value < _threshold && _streamBase is FileStream)
            {
                // truncate the stream
                Stream temp = CreateStream(value);
                StreamHelper.Copy(_streamBase, 0, value, temp);
                _streamBase = temp;
                // no need to set length here, because we just did
            }
        }
        //===============================================================================//


        //===============================================================================//
        /// <summary>
        /// Writes the contents of the byte buffer to the underlying stream.
        /// 
        /// If the write operation will cause the length of the stream to exceed
        /// _threshold bytes, the underlying stream type will be automatically converted 
        /// to a file-based stream prior to writing.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // check write count + stream length
            if (_streamBase is MemoryStream
                && _streamBase.Length + count > _threshold
                && _streamBase.Length < _threshold)
            {
                // we need to switch to a file-stream
                Stream temp = CreateStream(_streamBase.Length + count);
                long curPos = _streamBase.Position;
                StreamHelper.Copy(_streamBase, temp);
                _streamBase = temp;
                _streamBase.Position = curPos;
            }

            _streamBase.Write(buffer, offset, count);
        }
        //===============================================================================//

        #endregion
    }
}
