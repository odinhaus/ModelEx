using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Scheduling;
using Altus.Core.Component;
using System.Threading;
using Altus.Core.Processing;
using Altus.Core.Licensing;

namespace Altus.Core.PubSub
{
    public abstract class PublicationContext : InitializableComponent, IScheduledTask
    {
        [ThreadStatic]
        private static PublicationContext _current;
        public static PublicationContext Current 
        {
            get
            {
                return _current;
            }
            protected set
            {
                _current = value;
            }
        }

        protected PublicationContext()
        {
            Key = Guid.NewGuid().ToString();
        }

        public string Key { get; private set; }
        public Subscription Subscription { get { return this.SubscriberProxies == null ? null : this.SubscriberProxies[0].Subscription; } }
        public Schedule Schedule { get; set; }
        public bool Cancel { get; set; }
        public event PublicationErrorHandler Error;
        public Delegate Success;
        public Topic Topic { get; protected set; }
        List<ISubscriberProxy> _proxies = new List<ISubscriberProxy>();
        public ISubscriberProxy[] SubscriberProxies { get { return _proxies.ToArray(); } }
        public string Name { get { return this.Subscription == null ? "" : this.Subscription.Id; } }
        public System.Threading.ThreadPriority Priority { get { return ThreadPriority.Normal; } }
        public byte ProcessorAffinityMask { get; private set; }
        public DeclaredApp App { get; set; }

        public void Kill()
        {
            this.Schedule = new PeriodicSchedule(new DateRange(DateTime.MinValue, DateTime.MinValue.AddTicks(1)), 0);
            this.Schedule.IsCanceled = true;
        }

        public void AddProxy(ISubscriberProxy proxy)
        {
            _proxies.Add(proxy);
        }

        public void RemoveProxy(ISubscriberProxy proxy)
        {
            _proxies.Remove(proxy);
        }

        public object Execute(params object[] args)
        {
            PublicationContext.Current = this;
            try
            {
                OnSuccess(OnExecute(args));
                return args;
            }
            catch(Exception ex)
            {
                OnError(new ServiceParameter("Error", ex.GetType().FullName, ParameterDirection.Error) { Value = ex });
                return args;
            }
        }

        protected abstract ServiceParameter[] OnExecute(params object[] args);
        protected abstract void OnSuccess(params ServiceParameter[] args);
        protected abstract void OnError(ServiceParameter args);

        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }
    }
}
