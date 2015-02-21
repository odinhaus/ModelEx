using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Altus.Core.Memory
{
    public static class MemoryManagement
    {
        public enum LimitFlags : int
        {
            /// <summary>
            /// The working set may fall below the minimum working set limit if memory demands are high. This flag cannot be used with QUOTA_LIMITS_HARDWS_MIN_ENABLE.
            /// </summary>
            QUOTA_LIMITS_HARDWS_MIN_DISABLE = 2,
            /// <summary>
            /// The working set will not fall below the minimum working set limit.  This flag cannot be used with QUOTA_LIMITS_HARDWS_MIN_DISABLE.
            /// </summary>
            QUOTA_LIMITS_HARDWS_MIN_ENABLE = 1,
            /// <summary>
            /// The working set may exceed the maximum working set limit if there is abundant memory.  This flag cannot be used with QUOTA_LIMITS_HARDWS_MAX_ENABLE.
            /// </summary>
            QUOTA_LIMITS_HARDWS_MAX_DISABLE = 8,
            /// <summary>
            /// The working set will not exceed the maximum working set limit.  This flag cannot be used with QUOTA_LIMITS_HARDWS_MAX_DISABLE.
            /// </summary>
            QUOTA_LIMITS_HARDWS_MAX_ENABLE = 4
        }

        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSizeEx(IntPtr proc, int min, int max, int flags);

        public static void FlushMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }
        }

        public static void SetLimits(int min, int max, LimitFlags flags)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT
                && Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessWorkingSetSizeEx(System.Diagnostics.Process.GetCurrentProcess().Handle, min, max, (int)flags);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, min, max);
            }
        }
    }

    /// <summary>
    /// Contains methods for allocating and freeing memory.
    /// </summary>
    public class Memory
    {
        /// <summary>
        /// Allocates fixed memory. The return value is a pointer to the memory object.
        /// </summary>
        public const uint LMEM_FIXED = 0x0000;
        /// <summary>
        /// Initializes memory contents to zero.
        /// </summary>
        public const uint LMEM_ZEROINIT = 0x0040;
        /// <summary>
        /// Enables the memory allocation to move if it cannot be allocated in place.
        /// </summary>
        public const uint LMEM_MOVEABLE = 0x0002;
        /// <summary>
        /// Allows modification of the attributes of the memory object.
        /// </summary>
        public const uint LMEM_MODIFY = 0x0080;

        /// <summary>
        /// This function allocates the specified number of bytes from the heap. In
        /// the linear Microsoft® Windows® CE application programming interface (API)
        /// environment, there is no difference between the local heap and the global
        /// heap.
        /// </summary>
        /// <param name="uFlags">[in] Specifies how to allocate memory. If zero is
        /// specified, the default is the LMEM_FIXED flag. This parameter is a combination
        /// of LMEM_FIXED and LMEM_ZEROINIT. </param>
        /// <param name="uBytes">[in] Specifies the number of bytes to allocate.</param>
        /// <returns>A handle to the newly allocated memory object indicates success.
        /// NULL indicates failure. To get extended error information,
        /// call GetLastError.</returns>
        [DllImport("kernel32.dll")]
        extern public static IntPtr LocalAlloc(uint uFlags, uint uBytes);

        /// <summary>
        /// This function frees the specified local memory object and invalidates its handle.
        /// </summary>
        /// <param name="hMem">Handle to the local memory object. This handle is returned
        /// by either the LocalAlloc or LocalReAlloc function.</param>
        /// <returns>NULL indicates success. A handle to the local memory object indicates
        /// failure. To get extended error information, call GetLastError.</returns>
        [DllImport("kernel32.dll")]
        extern public static IntPtr LocalFree(IntPtr hMem);

        /// <summary>
        /// This function changes the size or the attributes of a specified local
        /// memory object. The size can increase or decrease.
        /// </summary>
        /// <param name="hMem">[in] Handle to the local memory object to be reallocated.
        /// This handle is returned by either the LocalAlloc or LocalReAlloc function.</param>
        /// <param name="uBytes">[in] New size, in bytes, of the memory block. If fuFlags
        /// specifies the LMEM_MODIFY flag, this parameter is ignored.</param>
        /// <param name="fuFlags">[in] Flag that specifies how to reallocate the local
        /// memory object. If the LMEM_MODIFY flag is specified, this parameter modifies
        /// the attributes of the memory object, and the uBytes parameter is ignored.
        /// Otherwise, this parameter controls the reallocation of the memory object.
        /// The LMEM_MODIFY can be combined with LMEM_MOVEABLE.
        /// If this parameter does not specify LMEM_MODIFY, this parameter can be any
        /// combination of LMEM_MOVEABLE and LMEM_ZEROINIT.</param>
        /// <returns>A handle to the reallocated memory object indicates success.
        /// NULL indicates failure. To get extended error information, call
        /// GetLastError.</returns>
        [DllImport("kernel32.dll")]
        extern public static IntPtr LocalReAlloc(IntPtr hMem, uint uBytes, uint fuFlags);

    }
}