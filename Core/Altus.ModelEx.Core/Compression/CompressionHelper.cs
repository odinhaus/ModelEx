using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;

namespace Altus.Core.Compression
{
    public enum CompressionType
    {
        GZip,
        BZip2,
        Zip
    }
    //========================================================================================================//
    /// <summary>
    /// Class name:  RxCompression
    /// Class description:
    /// Usage:
    /// <example></example>
    /// <remarks></remarks>
    /// </summary>
    //========================================================================================================//
    public static class CompressionHelper
    {
        #region Fields
        #region Static Fields
        #endregion Static Fields

        #region Instance Fields
        #endregion Instance Fields
        #endregion Fields

        #region Methods
        #region Public
        //========================================================================================================//
        /// <summary>
        /// Compresses the provided stream and returns a stream containing the compressed results
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type"></param>
        /// <param name="level">1-9 (1 least compression, 9 most compression only valid for Zip and BZip2 compression types)</param>
        /// <returns></returns>
        public static Stream Compress(Stream source, CompressionType type, int level, string name)
        {
            MemoryStream compressed = new MemoryStream();
            Compress(source, compressed, type, level, name);
            return compressed;
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Compresses the data in the source stream at the current position
        /// in destination stream.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="type"></param>
        /// <param name="level">1-9 (1 least compression, 9 most compression only valid for Zip and BZip2 compression types)</param>
        public static void Compress(Stream source, Stream destination, CompressionType type, int level, string name)
        {
            Stream s = OutputStream(destination, type, level, name);
            byte[] writeData = new byte[4096];
            while (true)
            {
                int size = source.Read(writeData, 0, writeData.Length);
                if (size > 0)
                {
                    s.Write(writeData, 0, size);

                }
                else
                {
                    break;
                }
            }
            s.Close();

        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Expands the provided compressed stream and returns the decompressed results in the return
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Stream Inflate(Stream source, CompressionType type)
        {
            MemoryStream inflated = new MemoryStream();
            Inflate(source, inflated, type);
            return inflated;
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Decompresses the data in source and writes to the current position of destination
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void Inflate(Stream source, Stream destination, CompressionType type)
        {
            byte[] writeData = new byte[4096];
            Stream s2 = InputStream(source, type);
            long totalRead = 0;
            while (true)
            {
                int size = s2.Read(writeData, 0, writeData.Length);
                if (size > 0)
                {
                    destination.Write(writeData, 0, size);
                    totalRead += size;
                }
                else
                {
                    break;
                }
            }
            s2.Close();
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Compresses the provided byte[]
        /// </summary>
        /// <param name="bytesToCompress"></param>
        /// <param name="type"></param>
        /// <param name="level">1-9 (1 least compression, 9 most compression only valid for Zip and BZip2 compression types)</param>
        /// <returns></returns>
        public static byte[] Compress(byte[] bytesToCompress, CompressionType type, int level, string name)
        {
            MemoryStream ms = new MemoryStream();
            Stream s = OutputStream(ms, type, level, name);
            s.Write(bytesToCompress, 0, bytesToCompress.Length);
            s.Close();
            return ms.ToArray();
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Compresses the provided string
        /// </summary>
        /// <param name="stringToCompress"></param>
        /// <param name="type"></param>
        /// <param name="level">1-9 (1 least compression, 9 most compression only valid for Zip and BZip2 compression types)</param>
        /// <returns></returns>
        public static string Compress(string stringToCompress, CompressionType type, int level, string name)
        {
            byte[] compressedData = CompressToByte(stringToCompress, type, level, name);
            string strOut = Convert.ToBase64String(compressedData);
            return strOut;
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Compresses the provided string into a byte[]
        /// </summary>
        /// <param name="stringToCompress"></param>
        /// <param name="type"></param>
        /// <param name="level">1-9 (1 least compression, 9 most compression only valid for Zip and BZip2 compression types)</param>
        /// <returns></returns>
        public static byte[] CompressToByte(string stringToCompress, CompressionType type, int level, string name)
        {
            byte[] bytData = Encoding.Unicode.GetBytes(stringToCompress);
            return Compress(bytData, type, level, name);
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Inflates the provided compressed string data
        /// </summary>
        /// <param name="stringToDecompress"></param>
        /// <returns></returns>
        public static string Inflate(string stringToDecompress, CompressionType type)
        {
            string outString = string.Empty;
            if (stringToDecompress == null)
            {
                throw new ArgumentNullException("stringToDecompress", "You tried to use an empty string");
            }
            try
            {
                byte[] inArr = Convert.FromBase64String(stringToDecompress.Trim());
                outString = System.Text.Encoding.Unicode.GetString(Inflate(inArr, type));
            }
            catch (NullReferenceException nEx)
            {
                return nEx.Message;
            }
            return outString;
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Inflates the provided compressed byte[]
        /// </summary>
        /// <param name="bytesToDecompress"></param>
        /// <returns></returns>
        public static byte[] Inflate(byte[] bytesToDecompress, CompressionType type)
        {
            byte[] writeData = new byte[4096];
            Stream s2 = InputStream(new MemoryStream(bytesToDecompress), type);
            MemoryStream outStream = new MemoryStream();
            while (true)
            {
                int size = s2.Read(writeData, 0, writeData.Length);
                if (size > 0)
                {
                    outStream.Write(writeData, 0, size);
                }
                else
                {
                    break;
                }
            }
            s2.Close();
            byte[] outArr = outStream.ToArray();
            outStream.Close();
            return outArr;

        }
        //========================================================================================================//

        #endregion Public

        #region Private
        private static Stream OutputStream(Stream inputStream, CompressionType type, int level, string name)
        {
            Stream retStream = null;

            switch (type)
            {
                case CompressionType.BZip2:
                    {
                        retStream = new ICSharpCode.SharpZipLib.BZip2.BZip2OutputStream(inputStream);
                        break;
                    }
                case CompressionType.GZip:
                    {
                        retStream = new ICSharpCode.SharpZipLib.GZip.GZipOutputStream(inputStream);
                        ((ICSharpCode.SharpZipLib.GZip.GZipOutputStream)retStream).SetLevel(level);
                        break;
                    }
                case CompressionType.Zip:
                    {
                        retStream = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream(inputStream);
                        ZipEntry entry = new ZipEntry(name);
                        entry.Comment = "Entry: " + name;
                        ((ICSharpCode.SharpZipLib.Zip.ZipOutputStream)retStream).PutNextEntry(entry);
                        ((ICSharpCode.SharpZipLib.Zip.ZipOutputStream)retStream).SetLevel(level);
                        break;
                    }
                default:
                    {
                        retStream = new ICSharpCode.SharpZipLib.GZip.GZipOutputStream(inputStream);
                        break;
                    }
            }

            return retStream;
        }

        private static Stream InputStream(Stream inputStream, CompressionType type)
        {
            Stream retStream = null;
            switch (type)
            {
                case CompressionType.BZip2:
                    {
                        retStream = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(inputStream);

                        break;
                    }
                case CompressionType.GZip:
                    {
                        retStream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(inputStream);
                        break;
                    }
                case CompressionType.Zip:
                    {
                        retStream = new ICSharpCode.SharpZipLib.Zip.ZipInputStream(inputStream);
                        ((ICSharpCode.SharpZipLib.Zip.ZipInputStream)retStream).GetNextEntry();
                        break;
                    }
                default:
                    {
                        retStream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(inputStream);
                        break;
                    }
            }
            return retStream;
        }
        #endregion Private

        #region Protected
        #endregion Protected
        #endregion Methods
    }
}
