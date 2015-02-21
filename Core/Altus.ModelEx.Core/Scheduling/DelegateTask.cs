using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Licensing;

namespace Altus.Core.Scheduling
{
    public class IsolatedDelegateTask : DelegateTask, IIsolatedScheduledTask
    {
        public IsolatedDelegateTask(Delegate del, Schedule schedule, params object[] args) : this(del, schedule, System.Threading.ThreadPriority.Normal, args)
        {}
        public IsolatedDelegateTask(Delegate del, Schedule schedule, System.Threading.ThreadPriority priority, params object[] args) 
            : base(del, schedule, priority, args)
        {}

        public IsolatedDelegateTask(Delegate del, Schedule schedule, System.Threading.ThreadPriority priority, byte processorAffinityMask, params object[] args)
            : base(del, schedule, priority, processorAffinityMask, args)
        {}
    }

    public class DelegateTask : InitializableComponent, IScheduledTask
    {
        public DelegateTask(Delegate del, Schedule schedule, params object[] args) : this(del, schedule, System.Threading.ThreadPriority.Normal, args)
        {}
        public DelegateTask(Delegate del, Schedule schedule, System.Threading.ThreadPriority priority, params object[] args)
        {
            Callback = del;
            Schedule = schedule;
            Args = args;
            Name = Guid.NewGuid().ToString();
            Priority = priority;
            this.ProcessorAffinityMask = 0;
            this.App = Context.CurrentContext.CurrentApp;
        }

        public DelegateTask(Delegate del, Schedule schedule, System.Threading.ThreadPriority priority, byte processorAffinityMask, params object[] args)
        {
            Callback = del;
            Schedule = schedule;
            Args = args;
            Name = Guid.NewGuid().ToString();
            Priority = priority;
            this.ProcessorAffinityMask = processorAffinityMask;
            this.App = Context.CurrentContext.CurrentApp;
        }

        public object Execute(params object[] args)
        {
            return Callback.DynamicInvoke(Args);
        }

        public Delegate Callback { get; private set; }

        public byte ProcessorAffinityMask { get; private set; }

        public Schedule Schedule
        {
            get;
            set;
        }

        public System.Threading.ThreadPriority Priority
        {
            get;
            private set;
        }

        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        public DeclaredApp App { get; set; }

        public object[] Args { get; private set; }

        public override bool Equals(object obj)
        {
            return obj is DelegateTask
                && ((DelegateTask)obj).Name.Equals(this.Name);
        }

        public void Kill()
        {
            this.Schedule = new PeriodicSchedule(new DateRange(DateTime.MinValue, DateTime.MinValue.AddTicks(1)), 0);
            this.Schedule.IsCanceled = true;
        }
    }
}
