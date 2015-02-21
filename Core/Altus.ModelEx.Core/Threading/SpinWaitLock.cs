using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace Altus.Core.Threading
{
    // NOTE: This is a value type so it works very efficiently when used as
    // a field in a class. Avoid boxing this or you will lose thread safety!
    public struct SpinWaitLock
    {
        private const Int32 c_lsFree = 0;
        private const Int32 c_lsOwned = 1;
        private Int32 m_LockState; // Defaults to 0=c_lsFree

        public void Enter()
        {
            Thread.BeginCriticalRegion();
            while (true)
            {
                // If resource available, set it to in-use and return
                if (Interlocked.Exchange(
                    ref m_LockState, c_lsOwned) == c_lsFree)
                {
                    return;
                }

                // Efficiently spin, until the resource looks like it might 
                // be free. NOTE: Just reading here (as compared to repeatedly 
                // calling Exchange) improves performance because writing 
                // forces all CPUs to update this value
                while (m_LockState == c_lsOwned)//Thread.VolatileRead(ref m_LockState) == c_lsOwned)
                {
                    StallThread();
                }
            }
        }

        public void Exit()
        {
            // Mark the resource as available
            Interlocked.Exchange(ref m_LockState, c_lsFree);
            Thread.EndCriticalRegion();
        }

        public bool IsFree { get { return m_LockState == c_lsFree; } }

        private static readonly Boolean IsSingleCpuMachine = (Environment.ProcessorCount == 1);

        private static void StallThread()
        {
            // On a single-CPU system, spinning does no good
            if (IsSingleCpuMachine) SwitchToThread();

            // Multi-CPU system might be hyper-threaded, let other thread run
            else Thread.SpinWait(1);
        }
        public static IDisposable CreateLockContext(ref SpinWaitLock theLock)
        {
            return new SpinWaitLockContext(ref theLock, true);
        }
        public static IDisposable CreateLockContext(ref SpinWaitLock theLock, bool lockOnEntry)
        {
            return new SpinWaitLockContext(ref theLock, lockOnEntry);
        }

        [DllImport("kernel32", ExactSpelling = true)]
        private static extern void SwitchToThread();
        private class SpinWaitLockContext : IDisposable
        {
            public SpinWaitLockContext(ref SpinWaitLock theLock, bool lockOnEntry)
            {
                SpinWaitLock = theLock;
                if (lockOnEntry)
                    SpinWaitLock.Enter();
            }

            public SpinWaitLock SpinWaitLock;

            public void Dispose()
            {
                SpinWaitLock.Exit();
            }
        }
    }

    
}
