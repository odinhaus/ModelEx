using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Altus.Core.Threading
{
    /// <summary>
    /// A WaitHandle class that can be used for multi-threaded resource control
    /// based on count.
    /// 
    /// In this implementation, WaitOne methods block all calling threads until
    /// the Count property exceeds zero.
    /// 
    /// The Count property is altered by calling Increment and Decrement to allow/deny
    /// access to the controlled resource or section of code.
    /// 
    /// Typical usage might include a thread that needs to process items in a queue,
    /// while other items add new entried to the queue.  During the Enqueue operation,
    /// the Increment method would be called, and during Dequeue the decrement method
    /// would be called.  The Dequeue loop would be controlled by a WaitOne on this object
    /// such that only if there was one or more items in the queue, would the WaitOne
    /// release.  In this case, it would assist in a form a Multiple Writer / Single Reader
    /// implementation.
    /// </summary>
    public class CounterEvent : WaitHandle
    {
        protected SpinWaitEvent _evt = new SpinWaitEvent();
        protected int _i = 0;

        /// <summary>
        /// Creates a new counter wait handle, with an initial count of zero.
        /// Waiting on this handle after construction will block until Increment()
        /// has been called.
        /// </summary>
        public CounterEvent() 
        {
            IsValid = true;
            _evt.SetLock();
        }

        /// <summary>
        /// Creates a new counter wait handle, with an initial count given by
        /// initialCount.  If initialCount > 0, all calls to WaitOne will
        /// return true without blocking initially, until Decerement has been called
        /// enough times to bring the Count down to zero.  Similarly, if an 
        /// initialCount less than or equal to zero is specified, the WaitOne
        /// calls will block until Increment has been called enough times to bring
        /// the Count to a value greater than zero.
        /// </summary>
        /// <param name="initialCount"></param>
        public CounterEvent(int initialCount) : this()
        {
            IsValid = true;
            _i = initialCount;

            if (_i > 0)
            {
                _evt.ReleaseLock();
            }
        }

        /// <summary>
        /// Blocks the caller indefinitely until the Count goes above zero (see Increment).
        /// Returns true.
        /// </summary>
        /// <returns></returns>
        public override bool WaitOne()
        {
            _evt.WaitOne();
            return IsValid;
        }

        public bool IsValid { get; private set; }

        /// <summary>
        /// Blocks the caller until the count goes above zero, or the timeout period has elapsed,
        /// whichever comes first.  If the timeout occurs, this method will return false.
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        /// <param name="exitContext"></param>
        /// <returns></returns>
        public override bool WaitOne(int millisecondsTimeout)
        {
            return _evt.WaitOne(millisecondsTimeout) && IsValid;
        }

        /// <summary>
        /// Blocks the caller until the count goes above zero, or the timeout period has elapsed,
        /// whichever comes first.  If the timeout occurs, this method will return false.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="exitContext"></param>
        /// <returns></returns>
        public override bool WaitOne(TimeSpan timeout)
        {
            return _evt.WaitOne(timeout) && IsValid;
        }

        /// <summary>
        /// Causes the WaitHandle to release all WaitOne blocks without incrementing
        /// or decrementing the count.
        /// </summary>
        public override void Close()
        {
            IsValid = false;
            //lock (_evt)
            //{
                _evt.ReleaseLock();
            //}
            base.Close();
        }

        /// <summary>
        /// Increments the wait counter by 1.  If the current count goes above 0, then
        /// WaitOne will return true without blocking.
        /// </summary>
        /// <returns></returns>
        public int Increment()
        {
            //int lastI = _i;
            int newI = Interlocked.Increment(ref _i);
            //lock (_evt)
            //{
                if (//lastI <= 0 && 
                    newI > 0)
                {
                    _evt.ReleaseLock();
                }
            //}
            return newI;
        }

        /// <summary>
        /// Increments the current Count by the count
        /// specified.  If the current count goes above 0, then
        /// WaitOne will return true without blocking.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public int Increment(int count)
        {
            //int lastI = _i;
            int newI = Interlocked.Add(ref _i, count);
            //lock (_evt)
            //{
            if (//lastI <= 0 && 
                newI > 0)
            {
                    _evt.ReleaseLock();
                }
            //}
            return newI;
        }

        /// <summary>
        /// Decrements the wait counter by 1.  If the current count goes to zero or below,
        /// then WaitOne will block until the count goes above zero, ir until the timeout
        /// condition has been reached.
        /// </summary>
        /// <returns></returns>
        public int Decrement()
        {
            int newI = Interlocked.Decrement(ref _i); // decrement the count

            //lock (_evt)
            //{
                if (newI <= 0)
                {
                    _evt.SetLock(); // cause the WaitOne methods to block
                }
            //}

            return newI;
        }

        /// <summary>
        /// Decrements the wait counter by the count specified.  If the current count goes to zero or below,
        /// then WaitOne will block until the count goes above zero, ir until the timeout
        /// condition has been reached.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public int Decrement(int count)
        {
            int newI =Interlocked.Add(ref _i, -count); // decrement the count

            //lock (_evt)
            //{
                if (newI <= 0)
                {
                    _evt.SetLock(); // cause the WaitOne methods to block
                }
            //}

            return newI;
        }

        /// <summary>
        /// Returns the current count of the wait handle.
        /// </summary>
        public int Count
        {
            get { return _i; }
        }

        /// <summary>
        /// Sets the count to exactly zero (which signals the event if it is in a wait state)
        /// </summary>
        public void Reset()
        {
            //lock (_evt)
            //{
                _i = 0;
                _evt.SetLock();
            //}
        }
    }
}
