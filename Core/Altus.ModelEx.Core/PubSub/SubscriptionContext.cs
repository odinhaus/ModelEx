using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Topology;
using System.Threading;

namespace Altus.Core.PubSub
{
    public abstract class SubscriptionContext
    {
        protected SubscriptionContext(Subscription subscription)
        {
            this.Subscription = subscription;
            this.Publisher = subscription.Publisher;
            this.Subscription.ScheduleChanged += new SubscriptionScheduleChangedHandler(Subscription_ScheduleChanged);
        }

        void Subscription_ScheduleChanged(object sender, SubscriptionScheduleChangedArgs e)
        {
            
        }

        [ThreadStatic]
        private static SubscriptionContext _current;
        public static SubscriptionContext Current
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

        public Subscription Subscription { get; private set; }
        public Publisher Publisher { get; private set; }
    }
}
