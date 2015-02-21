using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Security;
using Altus.Core.Topology;
using Altus.Core.Scheduling;
using Altus.Core.Processing;

namespace Altus.Core.PubSub
{
    public delegate void SubscriptionScheduleChangedHandler(object sender, SubscriptionScheduleChangedArgs e);
    public class SubscriptionScheduleChangedArgs : EventArgs
    {
        public SubscriptionScheduleChangedArgs(Subscription subscription)
        {
            this.Subscription = subscription;
        }

        public Schedule Schedule { get { return this.Subscription.Schedule; } }
        public Subscription Subscription { get; private set; }
    }

    public class Subscription
    {
        public Subscription(SubscriptionDefinition definition, Publisher publisher) : this(definition.Subscriber, definition.Topic, publisher, null) { }

        public Subscription(Subscriber subscriber, Topic topic, Publisher publisher) : this(subscriber, topic, publisher, null) { }

        public Subscription(Subscriber subscriber, Topic topic, Publisher publisher, Schedule schedule) 
        {
            this.Subscriber = subscriber;
            this.Topic = topic;
            this.Publisher = publisher;
            this.Schedule = schedule;
            this.Parameters = new ServiceParameterCollection();
            this.Format = topic.DefaultFormat;
        }

        private string _guid = Guid.NewGuid().ToString();
        public string Id 
        { 
            get 
            {
                string ret = Topic.Name;
                ret += this.Publisher == null ? "" : "|" + this.Publisher.NodeAddress;
                if (!this.Topic.IsMulticast)
                {
                    ret += "|" + Subscriber.NodeAddress.Address;
                    ret += "|" + Parameters.ToString();
                    ret += "|" + _guid;
                }
                return ret;
            } 
        }

        public string Format { get; set; }
        public Subscriber Subscriber { get; private set; }
        public Topic Topic { get; private set; }
        public Publisher Publisher { get; private set; }
        public ServiceParameterCollection Parameters { get; private set; }
        public event SubscriptionScheduleChangedHandler ScheduleChanged;
        private Schedule _schedule;
        public Schedule Schedule 
        {
            get { return _schedule; }
            set
            {
                this._schedule = value;
                if (this.ScheduleChanged != null)
                {
                    this.ScheduleChanged(this, new SubscriptionScheduleChangedArgs(this));
                }
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Subscription
                && ((Subscription)obj).Id.Equals(this.Id, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
