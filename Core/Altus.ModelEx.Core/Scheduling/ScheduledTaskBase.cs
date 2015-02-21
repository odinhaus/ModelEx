using Altus.Core.Component;
using Altus.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Altus.Core.Licensing;

namespace Altus.Core.Scheduling
{
    /// <summary>
    /// Abstract base component that allows implementers to define a delegate to be called according to a 
    /// specifyable schedule.  By default, the delegate is called once per second with no expiration.
    /// </summary>
    public abstract class ScheduledTaskBase : InitializableComponent, IScheduledTask
    {
        protected ScheduledTaskBase() 
        {
            this.Name = OnGetName();
            this.Callback = OnGetCallback();
            this.Schedule = OnGetSchedule();
            this.Priority = OnGetPriority();
            this.ProcessorAffinityMask = OnGetProcessorAffinityMask();
            this.App = Context.CurrentContext.CurrentApp;
        }

        protected virtual string OnGetName()
        {
            return Guid.NewGuid().ToString();
        }
        protected abstract Delegate OnGetCallback();
        protected virtual Schedule OnGetSchedule()
        {
            return new PeriodicSchedule(DateRange.Forever, 1000);
        }
        protected virtual ThreadPriority OnGetPriority()
        {
            return ThreadPriority.Normal;
        }
        protected virtual byte OnGetProcessorAffinityMask()
        {
            return 0;
        }

        public object Execute(params object[] args)
        {
            return Callback.DynamicInvoke();
        }

        public Delegate Callback { get; private set; }
        public byte ProcessorAffinityMask { get; private set; }

        public DeclaredApp App { get; set; }

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
            this.Args = args;
            return true;
        }

        public object[] Args { get; private set; }

        public override bool Equals(object obj)
        {
            return obj is ScheduledTaskBase
                && ((ScheduledTaskBase)obj).Name.Equals(this.Name);
        }

        public void Kill()
        {
            this.Schedule = new PeriodicSchedule(new DateRange(DateTime.MinValue, DateTime.MinValue.AddTicks(1)), 0);
        }
    }
}
