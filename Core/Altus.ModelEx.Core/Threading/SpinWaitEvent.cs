using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using Altus.Core.Diagnostics;

namespace Altus.Core.Threading
{
    // NOTE: This is a value type so it works very efficiently when used as
    // a field in a class. Avoid boxing this or you will lose thread safety!
    public struct SpinWaitEvent
    {
        private const Int32 c_lsFree = 0;
        private const Int32 c_lsOwned = 1;
        private Int32 m_LockState; // Defaults to 0=c_lsFree
        private ManualResetEvent _evt;

        
        public void SetLock()
        {
            if (_evt == null)
            {
                _evt = new ManualResetEvent(true);
            }
            Thread.BeginCriticalRegion();
            while (true)
            {
                // If resource available, set it to in-use and return
                if (Interlocked.Exchange(
                    ref m_LockState, c_lsOwned) == c_lsFree)
                {
                    _evt.Reset();
                    return;
                }
            }
        }

        public void WaitOne()
        {
            // Efficiently spin, until the resource looks like it might 
            // be free. NOTE: Just reading here (as compared to repeatedly 
            // calling Exchange) improves performance because writing 
            // forces all CPUs to update this value
            int spinCount = -1;
            while (m_LockState == c_lsOwned)//Thread.VolatileRead(ref m_LockState) == c_lsOwned)
            {
                if (spinCount < 20)
                    StallThread(spinCount++);
                else
                    _evt.WaitOne();

                if (spinCount == int.MaxValue)
                    spinCount = 0;
            }
        }

        public void ReleaseLock()
        {
            // Mark the resource as available
            if (m_LockState != c_lsFree)
            {
                Interlocked.Exchange(ref m_LockState, c_lsFree);
                _evt.Set();
            }
            Thread.EndCriticalRegion();
        }

        public bool IsFree { get { return m_LockState == c_lsFree; } }

        private static readonly Boolean IsSingleCpuMachine = (Environment.ProcessorCount == 1);

        private static void StallThread(int spinCount)
        {
            if (spinCount < 10)
            {
                // On a single-CPU system, spinning does no good
                if (IsSingleCpuMachine) SwitchToThread();

                // Multi-CPU system might be hyper-threaded, let other thread run
                else Thread.SpinWait(1);
            }
            else //if (spinCount < 100000)
            {
                // On a single-CPU system, spinning does no good
                if (IsSingleCpuMachine) SwitchToThread();

                // Multi-CPU system might be hyper-threaded, let other thread run
                else Thread.SpinWait(5);
            }
            //else if (spinCount < 1000000)
            //{
            //    Thread.Sleep(0);
            //}
            //else
            //{
            //    Thread.Sleep(20);
            //}
        }

        [DllImport("kernel32", ExactSpelling = true)]
        private static extern void SwitchToThread();

        internal bool WaitOne(int millisecondsTimeout)
        {
            Int64 stime = 0;
            MetricsHelper.QueryPerformanceCounter(ref stime);
            stime = (long)MetricsHelper.TimerFrequency * stime;

            Int64 etime = 0;
            int spinCount = -1;
            while (Thread.VolatileRead(ref m_LockState) == c_lsOwned
                && etime < millisecondsTimeout)
            {
                if (spinCount < 100000)
                    StallThread(spinCount++);
                else
                    return _evt.WaitOne((int)(millisecondsTimeout - etime));

                StallThread(spinCount++);
                MetricsHelper.QueryPerformanceCounter(ref etime);
                etime = ((long)MetricsHelper.TimerFrequency * etime) - stime;
                if (spinCount == int.MaxValue)
                    spinCount = 0;
            }

            return etime < millisecondsTimeout;
        }

        internal bool WaitOne(TimeSpan timeout)
        {
            return WaitOne((int)timeout.TotalMilliseconds);
        }
    }
}
