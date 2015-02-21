using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using System.Threading;
using System.Diagnostics;
using Altus.Core.Scheduling;
using Altus.Core.Diagnostics;
using Altus.Core.Threading;
using Altus.Core;
using System.Reflection;
using System.Runtime.ExceptionServices;

[assembly: Component(
    ComponentType = typeof(Scheduler),
    Name = "Scheduler")]

namespace Altus.Core.Scheduling
{
    public class Scheduler : InitializableComponent
    {
        List<IScheduledTask> _tasks = new List<IScheduledTask>();
        List<IScheduledTask> _queue = new List<IScheduledTask>();
        Thread _taskRunner;
        Thread _queueRunner;
        bool _running = false;
        string[] _args;
        Context _ctx;
        public static Scheduler Instance
        {
            get
            {
                Scheduler instance = App.Instance.Shell.GetComponent<Scheduler>();
                if (instance == null)
                {
                    instance = new Scheduler();
                    App.Instance.Shell.Add(instance, "Scheduler");
                }
                return instance;
            }
        }

        protected override bool OnInitialize(params string[] args)
        {
            _args = args;
            _tasks.AddRange(App.Instance.Shell.GetComponents<IScheduledTask>());
            App.Instance.Shell.ComponentChanged += new CompositionContainerComponentChangedHandler(Shell_ComponentChanged);
            _running = true;
            _ctx = Context.GlobalContext.Copy();
            _taskRunner = new Thread(new ThreadStart(RunTasks));
            _taskRunner.IsBackground = true;
            _taskRunner.Name = "Scheduled Task Scheduler";
            _taskRunner.Start();

            _queueRunner = new Thread(new ThreadStart(RunQueue));
            _queueRunner.IsBackground = true;
            _queueRunner.Name = "Queued Task Scheduler";
            _queueRunner.Start();

            return true;
        }

        Dictionary<IScheduledTask, TaskRunner> _runners = new Dictionary<IScheduledTask, TaskRunner>();
        private void RunTasks()
        {
            while (_running)
            {
                IScheduledTask[] tasks;
                DateTime now = CurrentTime.Now;
                lock(this)
                {
                    tasks = _tasks.ToArray();
                }

                for (int i = 0; i < tasks.Length; i++)
                {
                    IScheduledTask task = tasks[i];
                    if (task.Schedule == null) continue;
                    if (!_runners.ContainsKey(task)
                        && !task.Schedule.IsExpired)
                    {
                        _runners.Add(task, new TaskRunner(task, _args));
                    }

                    if (task.Schedule.IsExpired)
                    {
                        if (_runners.ContainsKey(task))
                            _runners.Remove(task);
                        _tasks.Remove(task);
                    }
                }

                Thread.Sleep(1);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private void RunQueue()
        {
            List<IScheduledTask> current = new List<IScheduledTask>();
            Altus.Core.Threading.ThreadPool tp = new Threading.ThreadPool(5);
            while (_running)
            {
                lock (this)
                {
                    current.AddRange(_queue);
                    _queue.Clear();
                }

                foreach(IScheduledTask t in current.Where(st => !st.Schedule.IsCanceled 
                    && st.Schedule.DateRange.Start <= CurrentTime.Now).ToArray())
                {
                    try
                    {
                        if (t is IIsolatedScheduledTask)
                        {
                            Thread thread = new Thread(new ParameterizedThreadStart(RunTask));
                            thread.IsBackground = true;
                            thread.Name = "Isolated Enqueued Task " + t.Name;
                            thread.Start(t);
                        }
                        else
                        {
                            tp.QueueTask(new Action(delegate() { RunTask(t); }));
                        }
                        t.Dispose();
                    }
                    catch (Exception ex)
                    {
                        if (!(ex is ThreadAbortException))
                        {
                            Logger.LogError(ex, "An error occurred running a scheduled background task:");
                            if (ex is TargetInvocationException
                                && ex.InnerException != null)
                                Logger.LogError(ex.InnerException);
                        }
                    }
                    finally
                    {
                        current.Remove(t);
                    }
                }

                Thread.Sleep(1);
            }
            tp.Dispose();
        }

        [HandleProcessCorruptedStateExceptions]
        private void RunTask(object state)
        {
            try
            {
                Context.CurrentContext = _ctx;
                Context.CurrentContext.CurrentApp = ((IScheduledTask)state).App;
                ((IScheduledTask)state).Execute(((DelegateTask)state).Args);
            }
            catch (Exception ex)
            {
                if (!(ex is ThreadAbortException))
                {
                    Logger.LogError(ex, "An error occurred running a scheduled background task:");
                    if (ex is TargetInvocationException
                        && ex.InnerException != null)
                        Logger.LogError(ex.InnerException);
                }
            }
        }

        private class TaskRunner
        {
            public TaskRunner(IScheduledTask task, params string[] args)
            {
                Task = task;
                IsExpired = task.Schedule[CurrentTime.Now].IsExpired;

                Thread thread = new System.Threading.Thread(new ParameterizedThreadStart(RunTask));
                thread.IsBackground = true;
                thread.Name = "Scheduled Task Runner [" + task.Name + "]";
                thread.Priority = task.Priority;
                thread.Start(args);
                
                Thread = thread;
            }

            [HandleProcessCorruptedStateExceptions]
            private void RunTask(object args)
            {
                if (Task.ProcessorAffinityMask != 0)
                {
                    ProcessorAffinity.SetThreadAffinityMask(new UIntPtr((uint)Task.ProcessorAffinityMask));
                }
                Stopwatch sw = new Stopwatch();
                long elapsed = 0;
                sw.Start();
                Context.CurrentContext = Context.GlobalContext.Copy();
                Context.CurrentContext.CurrentApp = Task.App;

                while (!IsExpired && App.Instance.IsRunning)
                {
                    if (Task.Schedule[CurrentTime.Now].WaitNext(sw.ElapsedTicks - elapsed))
                    {
                        elapsed = sw.ElapsedTicks;// -((sw.ElapsedTicks - elapsed) % TimeSpan.TicksPerMillisecond);
                        try
                        {
                            Task.Execute((object[])(string[])args);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex);
                            if (ex is TargetInvocationException
                                && ex.InnerException != null)
                                Logger.LogError(ex.InnerException);
                        }
                    }
                    IsExpired = Task.Schedule.IsExpired;
                }
            }

            public IScheduledTask Task { get; private set; }
            public Thread Thread { get; private set; }
            public bool IsExpired { get; private set; }
        }

        protected override void OnDispose()
        {
            _running = false;

            if (_queueRunner != null)
            {
                if (!_queueRunner.Join(2000))
                    _queueRunner.Abort();
            }

            if (_taskRunner != null)
            {
                if (!_taskRunner.Join(2000))
                    _taskRunner.Abort();

                lock (this)
                {
                    if (_runners != null)
                    {
                        foreach (TaskRunner runner in _runners.Values)
                        {
                            try
                            {
                                runner.Task.Kill();
                                runner.Task.Dispose();
                                if (runner.Thread != null)
                                    runner.Thread.Join(2000);
                            }
                            catch { }
                            try
                            {
                                if (runner.Thread.ThreadState == System.Threading.ThreadState.Running
                                    || runner.Thread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                                {
                                    runner.Thread.Abort();
                                }
                            }
                            catch { }
                        }
                        _runners.Clear();
                        _runners = null;
                    }
                }
            }
            base.OnDispose();
        }

        void Shell_ComponentChanged(object sender, CompositionContainerComponentEventArgs e)
        {
            lock (this)
            {
                if (e.Component is IScheduledTask)
                {
                    if (e.Change == CompositionContainerComponentChange.Add)
                    {
                        _tasks.Add((IScheduledTask)e.Component);
                    }
                    else
                    {
                        _tasks.Remove((IScheduledTask)e.Component);
                    }
                }
            }
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided schedule with the provided arguments
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Run(int interval, Action callback)
        {
            return Run(new PeriodicSchedule(DateRange.Forever, interval), callback);
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided schedule with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Run<T>(int interval, Action<T> callback, T arg)
        {
            return Run(new PeriodicSchedule(DateRange.Forever, interval), callback, new object[] { arg });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided schedule with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Run<T, U>(int interval, Action<T, U> callback, T arg1, U arg2)
        {
            return Run(new PeriodicSchedule(DateRange.Forever, interval), callback, new object[] { arg1, arg2 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided schedule with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Run<T, U, V>(int interval, Action<T, U> callback, T arg1, U arg2, V arg3)
        {
            return Run(new PeriodicSchedule(DateRange.Forever, interval), callback, new object[] { arg1, arg2, arg3 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided schedule with the provided arguments
        /// </summary>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Run<Z>(int interval, Func<Z> callback)
        {
            return Run(new PeriodicSchedule(DateRange.Forever, interval), callback);
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided schedule with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Run<T, Z>(int interval, Func<T, Z> callback, T arg)
        {
            return Run(new PeriodicSchedule(DateRange.Forever, interval), callback, new object[] { arg });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided schedule with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Run<T, U, Z>(int interval, Func<T, U, Z> callback, T arg1, U arg2)
        {
            return Run(new PeriodicSchedule(DateRange.Forever, interval), callback, new object[] { arg1, arg2 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided schedule with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Run<T, U, V, Z>(int interval, Func<T, U, Z> callback, T arg1, U arg2, V arg3)
        {
            return Run(new PeriodicSchedule(DateRange.Forever, interval), callback, new object[] { arg1, arg2, arg3 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided schedule with the provided arguments
        /// </summary>
        /// <param name="sched"></param>
        /// <param name="callback"></param>
        /// <param name="args"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Run(Schedule sched, Delegate callback, params object[] args)
        {
            DelegateTask task = new DelegateTask(callback, sched, args );
            lock (this)
            {
                _tasks.Add(task);
            }
            return task;
        }

        /// <summary>
        /// Executes the provided delegate task with no delay with the provided arguments
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Enqueue(Action callback)
        {
            return Enqueue(new PeriodicSchedule(new DateRange(CurrentTime.Now, CurrentTime.Now), 0), callback);
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Enqueue(int interval, Action callback)
        {
            return Enqueue(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback);
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Enqueue<T>(int interval, Action<T> callback, T arg)
        {
            return Enqueue(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Enqueue<T, U>(int interval, Action<T, U> callback, T arg1, U arg2)
        {
            return Enqueue(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg1, arg2 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Enqueue<T, U, V>(int interval, Action<T, U> callback, T arg1, U arg2, V arg3)
        {
            return Enqueue(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg1, arg2, arg3 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments
        /// </summary>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Enqueue<Z>(int interval, Func<Z> callback)
        {
            return Enqueue(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback);
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Enqueue<T, Z>(int interval, Func<T, Z> callback, T arg)
        {
            return Enqueue(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Enqueue<T, U, Z>(int interval, Func<T, U, Z> callback, T arg1, U arg2)
        {
            return Enqueue(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg1, arg2 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IScheduledTask Enqueue<T, U, V, Z>(int interval, Func<T, U, Z> callback, T arg1, U arg2, V arg3)
        {
            return Enqueue(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg1, arg2, arg3 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments
        /// </summary>
        /// <param name="sched"></param>
        /// <param name="callback"></param>
        /// <param name="args"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        private IScheduledTask Enqueue(Schedule sched, Delegate callback, params object[] args)
        {
            DelegateTask task = new DelegateTask(callback, sched, args);
            lock (this)
            {
                _queue.Add(task);
            }
            return task;
        }

        /// <summary>
        /// Executes the provided delegate task with no delay with the provided arguments on its own, unique thread
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IIsolatedScheduledTask EnqueueIsolated(Action callback)
        {
            return EnqueueIsolated(new PeriodicSchedule(new DateRange(CurrentTime.Now, CurrentTime.Now), 0), callback);
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments on its own, unique thread
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IIsolatedScheduledTask EnqueueIsolated(int interval, Action callback)
        {
            return EnqueueIsolated(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback);
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments on its own, unique thread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IIsolatedScheduledTask EnqueueIsolated<T>(int interval, Action<T> callback, T arg)
        {
            return EnqueueIsolated(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments on its own, unique thread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IIsolatedScheduledTask EnqueueIsolated<T, U>(int interval, Action<T, U> callback, T arg1, U arg2)
        {
            return EnqueueIsolated(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg1, arg2 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments on its own, unique thread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IIsolatedScheduledTask EnqueueIsolated<T, U, V>(int interval, Action<T, U> callback, T arg1, U arg2, V arg3)
        {
            return EnqueueIsolated(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg1, arg2, arg3 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments on its own, unique thread
        /// </summary>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IIsolatedScheduledTask EnqueueIsolated<Z>(int interval, Func<Z> callback)
        {
            return EnqueueIsolated(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback);
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments on its own, unique thread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IIsolatedScheduledTask EnqueueIsolated<T, Z>(int interval, Func<T, Z> callback, T arg)
        {
            return EnqueueIsolated(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments on its own, unique thread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IIsolatedScheduledTask EnqueueIsolated<T, U, Z>(int interval, Func<T, U, Z> callback, T arg1, U arg2)
        {
            return EnqueueIsolated(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg1, arg2 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments on its own, unique thread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="Z"></typeparam>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        public IIsolatedScheduledTask EnqueueIsolated<T, U, V, Z>(int interval, Func<T, U, Z> callback, T arg1, U arg2, V arg3)
        {
            return EnqueueIsolated(new PeriodicSchedule(new DateRange(CurrentTime.Now.AddMilliseconds(interval), CurrentTime.Now.AddMilliseconds(interval)), 0), callback, new object[] { arg1, arg2, arg3 });
        }

        /// <summary>
        /// Executes the provided delegate task according to the provided delay with the provided arguments on its own, unique thread
        /// </summary>
        /// <param name="sched"></param>
        /// <param name="callback"></param>
        /// <param name="args"></param>
        /// <returns>the unique name of the task assigned by the platform</returns>
        private IIsolatedScheduledTask EnqueueIsolated(Schedule sched, Delegate callback, params object[] args)
        {
            IsolatedDelegateTask task = new IsolatedDelegateTask(callback, sched, args);
            lock (this)
            {
                _queue.Add(task);
            }
            return task;
        }
    }
}
