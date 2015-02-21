using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using Altus.Core.Data;
using Altus.Core.Security;

namespace Altus.Core.PubSub
{
    public class SubscriptionDefinition 
    {
        //public SubscriptionDefinition(SubscriptionAttribute attrib, MethodInfo subscribingMethod, Subscriber subscriber)
        //{
        //    this.Topic = DataContext.Default.GetTopic(attrib.TopicName);
        //    this.Subscriber = subscriber;
        //    this.SubscribingMethod = subscribingMethod;
        //    this.ErrorMethod = GetErrorMethod();
        //    this.DefinitionMethod = GetDefinitionMethod();
        //}

        

        private MethodInfo GetDefinitionMethod()
        {
            MethodInfo def = this.Subscriber.Target.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.GetCustomAttributes(typeof(SubscriptionDefinitionAttribute), true)
                    .Where(pd => ((SubscriptionDefinitionAttribute)pd).TopicName.Equals(this.Topic.Name, StringComparison.InvariantCultureIgnoreCase)).Count() > 0).FirstOrDefault();
            return def;
        }

        private MethodInfo GetErrorMethod()
        {
            MethodInfo def = this.Subscriber.Target.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.GetCustomAttributes(typeof(SubscriptionErrorAttribute), true)
                    .Where(pd => ((SubscriptionErrorAttribute)pd).TopicName.Equals(this.Topic.Name, StringComparison.InvariantCultureIgnoreCase)).Count() > 0).FirstOrDefault();
            return def;
        }

        public Topic Topic { get; private set; }
        public Subscriber Subscriber { get; private set; }
        public MethodInfo SubscribingMethod { get; private set; }
        public MethodInfo ErrorMethod { get; private set; }
        public MethodInfo DefinitionMethod { get; private set; }

        //public void AddPublication(ReflectedPublicationDefinition publication)
        //{
        //    ISubscriberProxy proxy = CreateSubscriberProxy(new Subscription(this, publication.Publisher));

        //    if (!proxy.RequestDefinition()) // user didn't cancel, so wire it up
        //    {
        //        publication.AddSubscription(proxy);
        //    }
        //}

        //private ISubscriberProxy CreateSubscriberProxy(Subscription subscription)
        //{
        //    ReflectionSubscriberContext rsp = new ReflectionSubscriberContext(
        //           subscription,
        //           this.SubscribingMethod,
        //           this.ErrorMethod,
        //           this.DefinitionMethod);
        //    return rsp;
        //}
    }
}
