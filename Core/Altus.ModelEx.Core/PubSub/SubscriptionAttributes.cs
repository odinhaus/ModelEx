using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Topology;
using Altus.Core.Realtime;
using Altus.Core.Dynamic;

namespace Altus.Core.PubSub
{
    public delegate void SubscriptionHandler(object sender, SubscriptionHandlerArgs e);
    public delegate void SubscriptionConditionHandler(object sender, SubscriptionDefinitionArgs e);
    public delegate void SubscriptionErrorHandler(object sender, SubscriptionErrorArgs e);

    public class SubscriptionErrorArgs : EventArgs
    {
        public SubscriptionErrorArgs(Subscription subscription, Exception exception)
        {
            this.Subscription = subscription;
            this.Exception = exception;
        }

        public Subscription Subscription { get; private set; }
        public Exception Exception { get; private set; }
    }

    public class SubscriptionDefinitionArgs : EventArgs
    {
        public SubscriptionDefinitionArgs(Subscription subscription)
        {
            Subscription = subscription;
        }

        public Subscription Subscription { get; private set; }
        public bool Cancel { get; set; }
    }

    public class SubscriptionHandlerArgs : EventArgs
    {
        public SubscriptionHandlerArgs(Subscription subscription, IEnumerable<Field> fields)
        {
            this.Subscription = subscription;
            this.Fields = fields;
        }

        public Subscription Subscription { get; private set; }
        public IEnumerable<Field> Fields { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple=true, Inherited = true)]
    public class SubscriptionAttribute : Attribute
    {
        public SubscriptionAttribute(string topicName)
        {
            this.TopicName = topicName;
            this.Publisher = null;
        }

        public SubscriptionAttribute(string topicName, string publisher)
        {
            this.TopicName = topicName;
            this.Publisher = publisher;
        }

        public string TopicName { get; private set; }
        public string Publisher { get; private set; }
    }

    /// <summary>
    /// Methods declared with this attribute will be called sequentially for each publisher providing publications
    /// with the designated topic name.  Handlers can set custom Condition arguments to be passed to the publisher
    /// used to influence the publication to provide subscriber-specific data or filters.  Handlers can also choose
    /// to cancel the subscription process by setting the Cancel propert of the SubscriptionConditionArgs instance to true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public class SubscriptionDefinitionAttribute : Attribute
    {
        public SubscriptionDefinitionAttribute(string topicName)
        {
            this.TopicName = topicName;
            this.Publisher = null;
        }
        
        public SubscriptionDefinitionAttribute(string topicName, string publisher)
        {
            this.TopicName = topicName;
            this.Publisher = publisher;
        }

        public string TopicName { get; private set; }
        public string Publisher { get; private set; }
    }

    /// <summary>
    /// Methods decalred with this attribute will be called in the event that an error occurred while publishing data 
    /// for the specified topic and optional publisher.  Methods must implement the SubscriptionErrorHandler delegate 
    /// interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class SubscriptionErrorAttribute : Attribute
    {
        public SubscriptionErrorAttribute(string topicName)
        {
            this.TopicName = topicName;
            this.Publisher = null;
        }

        public SubscriptionErrorAttribute(string topicName, string publisher)
        {
            this.TopicName = topicName;
            this.Publisher = publisher;
        }

        public string TopicName { get; private set; }
        public string Publisher { get; private set; }
    }

    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple=false, Inherited=true)]
    public class SubscriptionResponseAttribute : Attribute {}
}
