using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core
{
    public static class ByteEx
    {
        public static unsafe void Copy(this byte[] src, int srcIndex, byte[] dst, int dstIndex, int count)
        {
            if (src == null || srcIndex < 0 ||
                dst == null || dstIndex < 0 || count < 0)
            {
                throw new System.ArgumentException();
            }

            int srcLen = src.Length;
            int dstLen = dst.Length;
            if (srcLen - srcIndex < count || dstLen - dstIndex < count)
            {
                throw new System.ArgumentException();
            }

            // The following fixed statement pins the location of the src and dst objects
            // in memory so that they will not be moved by garbage collection.
            fixed (byte* pSrc = src, pDst = dst)
            {
                byte* ps = pSrc;
                byte* pd = pDst;
                ps += srcIndex;
                pd += dstIndex;

                // Loop over the count in blocks of 4 bytes, copying an integer (4 bytes) at a time:
                int qtrCount = count / 4;
                for (int i = 0; i < count; i += 4)
                {
                    *((int*)pd) = *((int*)ps);
                    pd += 4;
                    ps += 4;
                }

                // Complete the copy by moving any bytes that weren't moved in blocks of 4:
                for (int i = 0; i < count % 4; i++)
                {
                    *pd = *ps;
                    pd++;
                    ps++;
                }
            }
        }

        public static string ToBase16(this byte[] bytes)
        {
            return ToBase16(bytes, -1, -1);
        }

        public static string ToBase16(this byte[] bytes, int maxLength)
        {
            return ToBase16(bytes, maxLength, -1);
        }

        /// <summary>
        /// Coverts the provided byte array to a base16 string of the given max length.  If the converted string exceeds maxLength in length,
        /// the remain characters are truncated from the returned string.  Specify -1 to avoid truncation.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string ToBase16(this byte[] bytes, int maxLength, int dashPosition)
        {
            string hash = Convert.ToBase64String(bytes);
            return hash.ToBase16(maxLength, dashPosition);
        }
    }
}
