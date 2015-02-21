using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Altus.Core.Diagnostics;

namespace Altus.Core.Threading
{
    public enum LockShare : int
    {
        Shared = 1,
        Exclusive = 2,
        EscalatedExclusive = Shared | Exclusive
    }

    public enum LockOperation : int
    {
        None = 0,
        Read = 1,
        Write = 2,
        Schema = 4,
        ReadWrite = Read | Write,
        ReadSchema = Read | Schema,
        WriteSchema = Write | Schema,
        ReadWriteSchema = ReadWrite | Schema
    }

    public struct SharedLock
    {
        public class SharedLockGlobals
        {
            public object Lock;
            public string Name;
            public long SharedCount;
            public long ExclusiveCount;
            public long IntentSharedCount;
            public long IntentExclusiveCount;
        }

        private static readonly Boolean IsSingleCpuMachine = (Environment.ProcessorCount == 1);
        public string Name;

        private SharedLockGlobals Globals;

        [ThreadStatic()]
        private static LockOperation _currentOperation;
        [ThreadStatic()]
        private static LockShare _currentShare;
        [ThreadStatic()]
        private static long _currentSharedCount = 0;
        [ThreadStatic()]
        private static long _currentExclusiveCount = 0;
        [ThreadStatic()]
        private static long _currentIntentSharedCount = 0;
        [ThreadStatic()]
        private static long _currentIntentExclusiveCount = 0;

        [ThreadStatic()]
        private static long _currentReadCount = 0;
        [ThreadStatic()]
        private static long _currentWriteCount = 0;
        [ThreadStatic()]
        private static long _currentSchemaCount = 0;

        private LockOperation _thisOperation;
        private LockShare _thisShare;

        private SharedLock(SharedLockGlobals globals, LockShare share, LockOperation operation)
        {
            Thread.BeginCriticalRegion();
            Globals = globals;
            Name = globals.Name;
            _thisOperation = operation;
            _thisShare = share;

            if (share == LockShare.Shared)
            {
                Interlocked.Increment(ref Globals.IntentSharedCount);
                _currentIntentSharedCount++;
            }
            else
            {
                Interlocked.Increment(ref Globals.IntentExclusiveCount);
                _currentIntentExclusiveCount++;
            }

            if (((int)operation).HasFlagFast((int)LockOperation.Read))
            {
                _currentReadCount++;
            }
            else if (((int)operation).HasFlagFast((int)LockOperation.Write))
            {
                _currentWriteCount++;
            }
            if (((int)operation).HasFlagFast((int)LockOperation.Schema))
            {
                _currentSchemaCount++;
            }

            if (((int)_currentShare).HasFlagFast((int)share))
            {
                // we already have this lock type, so allow re-entry and increment counters
                _currentOperation |= operation;
                if (share == LockShare.Shared)
                {
                    Interlocked.Increment(ref Globals.SharedCount);
                    Interlocked.Decrement(ref Globals.IntentSharedCount);
                    _currentSharedCount++;
                    _currentIntentSharedCount--;
                }
                else
                {
                    Interlocked.Increment(ref Globals.ExclusiveCount);
                    Interlocked.Decrement(ref Globals.IntentExclusiveCount);
                    _currentExclusiveCount++;
                    _currentIntentExclusiveCount--;
                }
            }
            else
            {
                if (share == LockShare.Shared
                    && ((int)_currentShare).HasFlagFast((int)LockShare.Exclusive))
                {
                    // looking to take a shared lock while holding an exclusive lock, simply allow it
                    Interlocked.Increment(ref Globals.SharedCount);
                    Interlocked.Decrement(ref Globals.IntentSharedCount);
                    _currentSharedCount++;
                    _currentIntentSharedCount--;
                }
                else if (share == LockShare.Shared)
                {
                    bool waited = false;
                    while (Interlocked.Read(ref Globals.IntentExclusiveCount) - _currentIntentExclusiveCount > 0
                        || Interlocked.Read(ref Globals.ExclusiveCount) - _currentExclusiveCount > 0)
                    {
                        StallThread();
                        if (!waited)
                        {
                            waited = true;
                            Logger.LogInfo("Shared " + this._thisOperation + " Lock " + this.Name + " waiting on operations to complete: " + Thread.CurrentThread.Name); 
                        }
                    }
                    // nobody is waiting to take an exclusive lock, so allow the lock
                    Interlocked.Increment(ref Globals.SharedCount);
                    Interlocked.Decrement(ref Globals.IntentSharedCount);
                    _currentIntentSharedCount--;
                    _currentSharedCount++;
                }
                else if (share == LockShare.Exclusive)
                {
                    bool waited = false;
                    while ((Interlocked.Read(ref Globals.ExclusiveCount) - _currentExclusiveCount) > 0
                        || (Interlocked.Read(ref Globals.SharedCount) - _currentSharedCount) > 0)
                    {
                        StallThread();
                        if (!waited)
                        {
                            waited = true;
                            Logger.LogInfo("Exclusive " + this._thisOperation + " Lock " + this.Name + " waiting on operations to complete: " + Thread.CurrentThread.Name);
                        }
                    }
                    lock (globals.Lock)
                    {
                        // nobody is waiting to take an exclusive lock, so allow the lock
                        Interlocked.Increment(ref Globals.ExclusiveCount);
                        Interlocked.Decrement(ref Globals.IntentExclusiveCount);
                        _currentExclusiveCount++;
                        _currentIntentExclusiveCount--;
                    }
                }
                _currentOperation |= operation;
                _currentShare |= share;
            }
        }

        private void ReleaseLock()
        {
            if (_thisShare == LockShare.Shared)
            {
                if (Interlocked.Read(ref Globals.SharedCount) > 0)
                    Interlocked.Decrement(ref Globals.SharedCount);
                if (Interlocked.Read(ref _currentSharedCount) > 0)
                    Interlocked.Decrement(ref _currentSharedCount);
            }
            else
            {
                if (Interlocked.Read(ref  Globals.ExclusiveCount) > 0)
                    Interlocked.Decrement(ref Globals.ExclusiveCount);
                if (Interlocked.Read(ref _currentExclusiveCount) > 0)
                    Interlocked.Decrement(ref _currentExclusiveCount);
                Logger.LogInfo("Exclusive lock released on " + Globals.Name);
            }

            if (_thisOperation == LockOperation.Read)
            {
                if (_currentReadCount > 0)
                    _currentReadCount--;
            }
            else if (_thisOperation == LockOperation.Write)
            {
                if (_currentWriteCount > 0)
                    _currentWriteCount--;
            }
            else if (_thisOperation == LockOperation.Schema)
            {
                if (_currentReadCount > 0)
                    _currentSchemaCount--;
            }

            if (((int)_currentShare).HasFlagFast((int)LockShare.Shared)
                && _currentSharedCount == 0)
            {
                _currentShare -= LockShare.Shared;
            }
            else if (((int)_currentShare).HasFlagFast((int)LockShare.Exclusive)
                && _currentExclusiveCount == 0)
            {
                _currentShare -= LockShare.Exclusive;
            }

            if (((int)_currentOperation).HasFlagFast((int)LockOperation.Read)
                && _currentReadCount == 0)
            {
                _currentOperation -= LockOperation.Read;
            }
            else if (((int)_currentOperation).HasFlagFast((int)LockOperation.Write)
                && _currentWriteCount == 0)
            {
                _currentOperation -= LockOperation.Write;
            }
            else if (((int)_currentOperation).HasFlagFast((int)LockOperation.Schema)
                && _currentSchemaCount == 0)
            {
                _currentOperation -= LockOperation.Schema;
            }

            Thread.EndCriticalRegion();
        }

        public static SharedLockContext Lock(SharedLockGlobals globals, LockShare share, LockOperation operation)
        {
            SharedLock theLock = new SharedLock(globals, share, operation);
            return new SharedLockContext(theLock);
        }

        public static SharedLockGlobals CreateGlobals(string name)
        {
            return new SharedLockGlobals() { Name = name, Lock = new object() };
        }

        [DllImport("kernel32", ExactSpelling = true)]
        private static extern void SwitchToThread();
        private static void StallThread()
        {
            // On a single-CPU system, spinning does no good
            if (IsSingleCpuMachine) SwitchToThread();

            // Multi-CPU system might be hyper-threaded, let other thread run
            else Thread.SpinWait(1);
        }

        public struct SharedLockContext : IDisposable
        {
            public SharedLockContext(SharedLock theLock)
            {
                Disposed = false;
                SharedLock = theLock;
                if (theLock._thisShare == LockShare.Exclusive
                    || theLock._thisShare == LockShare.EscalatedExclusive)
                {
                    Logger.LogInfo("Exclusive Lock Taken on " + theLock.Name);
                }
            }
            private bool Disposed;
            public SharedLock SharedLock;
            public void Dispose()
            {
                if (!Disposed)
                {
                    SharedLock.ReleaseLock();
                    Disposed = true;
                }
            }

            public override string ToString()
            {
                return string.Format("{0}:{1} [{2}:{3}]",
                    SharedLock._currentShare.ToString(), SharedLock._currentOperation,
                    this.SharedLock._thisShare.ToString(), this.SharedLock._thisOperation.ToString());
            }
        }
    }
}
