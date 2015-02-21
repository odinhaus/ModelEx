using Altus.Core;
using Altus.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Altus.Core.Threading
{
    public class TwoPhaseWait : WaitHandle
    {
        bool _state = false;
        bool _closed = false;
        ManualResetEvent _mre;
        SpinWait _spinner = new SpinWait();

        public static readonly int SPIN_SLEEP_THRESHOLD = 2;

        static TwoPhaseWait()
        {
            SPIN_SLEEP_THRESHOLD = int.Parse(Context.GetEnvironmentVariable("SPIN_SLEEP_THRESHOLD", "2"));
        }

        public TimeSpan TargetWaitTime { get; set; }

        public TwoPhaseWait(TimeSpan targetWaitTime, bool signaled)
        {
            _state = signaled;
            _mre = new ManualResetEvent(signaled);
            TargetWaitTime = targetWaitTime;
        }

        public override bool WaitOne()
        {
            return WaitOne(DateTime.MaxValue);
        }

        public override bool WaitOne(int millisecondsTimeout)
        {
            return WaitOne(DateTime.MaxValue);
        }

        public override bool WaitOne(int millisecondsTimeout, bool exitContext)
        {
            return WaitOne(DateTime.MaxValue);
        }

        public override bool WaitOne(TimeSpan timeout)
        {
            DateTime expired;
            try
            {
                expired = CurrentTime.Now.Add(timeout);
            }
            catch(ArgumentOutOfRangeException)
            {
                expired = DateTime.MaxValue;
            }
            return WaitOne(expired);
        }

        public override bool WaitOne(TimeSpan timeout, bool exitContext)
        {
            DateTime expired;
            try
            {
                expired = CurrentTime.Now.Add(timeout);
            }
            catch(ArgumentOutOfRangeException)
            {
                expired = DateTime.MaxValue;
            }
            return WaitOne(expired);
        }

        public bool WaitOne(DateTime expired)
        {
            DateTime now = CurrentTime.Now; 

            if (expired > now)
            {
                DateTime targetExp = now.Add(TargetWaitTime);
                
                int sleepThresh = (int)(SPIN_SLEEP_THRESHOLD * TimeSpan.TicksPerMillisecond);
                int ticksToWait = TargetWaitTime.Ticks > sleepThresh ? (int)(TargetWaitTime.Ticks - sleepThresh) : (int)(TargetWaitTime.Ticks / 2D);
                if (ticksToWait < 0) ticksToWait = 0;

                while (!_closed
                    && !_state
                    && CurrentTime.Now < expired)
                {
                    if (ticksToWait >= sleepThresh)
                    {
                        Thread.Sleep((int)(ticksToWait / TimeSpan.TicksPerMillisecond));
                        ticksToWait = System.Math.Max(0, (int)targetExp.Subtract(CurrentTime.Now).Ticks / 2);
                    }
                    else if (ticksToWait > 0)
                    {
                        if (_spinner.NextSpinWillYield)
                            _spinner.Reset();
                        _spinner.SpinOnce();
                    }
                }
            }
            _state = true;
            return _state;
        }

        public void Set()
        {
            _state = true;
            _mre.Set();
        }

        public void Reset()
        {
            _state = false;
            _spinner.Reset();
            _mre.Reset();
        }

        public override void Close()
        {
            _closed = true;
            _mre.Close();
            base.Close();
        }
    }
}
