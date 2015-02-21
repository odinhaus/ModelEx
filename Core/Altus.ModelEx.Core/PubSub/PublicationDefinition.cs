using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using System.Reflection;
using System.ComponentModel;
using Altus.Core.Data;
using Altus.Core.Security;
using Altus.Core.Topology;

namespace Altus.Core.PubSub
{
    public class ReflectedPublicationDefinition : PublicationDefinition, IEnumerable<PublicationContext>
    {
        Dictionary<string, PublicationContext> _contexts = new Dictionary<string, PublicationContext>();

        //public ReflectedPublicationDefinition(PublicationAttribute attribute, MemberInfo declaringMember, Publisher publisher)
        //{
        //    Topic topic = DataContext.Default.GetTopic(attribute.TopicName);
        //    if (topic == null) throw (new InvalidOperationException("Topic " + attribute.TopicName + " does not exist"));
        //    this.Topic = topic;
        //    this.PublishingMember = declaringMember;
        //    this.Publisher = publisher;
        //    this.SuccessMethod = GetSuccessMethod();
        //    this.DefinitionMethod = GetDefinitionMethod();
        //    this.DefaultInterval = attribute.Interval;
        //    this.Public = attribute.Public;
        //    this.Format = topic.DefaultFormat;
        //    this.Initialize();
        //}

        private void Initialize()
        {
            if (this.Topic.IsMulticast)
            {
                // we'll never get subscribers that we know about, so we manually create a multicast subscriber proxy
                ReflectionPublicationContext mcastCtx = new ReflectionPublicationContext(
                    this.Topic,
                    this.Publisher,
                    this.PublishingMember,
                    this.SuccessMethod,
                    this.DefinitionMethod,
                    this.DefaultInterval,
                    new MulticastSubscriberProxy(this));
                this.AddPublication(mcastCtx);
            }
        }

        private MethodInfo GetDefinitionMethod()
        {
            MethodInfo def = this.Publisher.Target.GetType().GetMethods(
                BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)
                .Where(mi => mi.GetCustomAttributes(typeof(PublicationDefinitionAttribute), true)
                    .Where(pd => ((PublicationDefinitionAttribute)pd).TopicName.Equals(this.Topic.Name, StringComparison.InvariantCultureIgnoreCase)).Count() > 0).FirstOrDefault();
            return def;
        }

        private MethodInfo GetSuccessMethod()
        {
            MethodInfo def = this.Publisher.Target.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.GetCustomAttributes(typeof(PublicationSuccessAttribute), true)
                    .Where(pd => ((PublicationSuccessAttribute)pd).TopicName.Equals(this.Topic.Name, StringComparison.InvariantCultureIgnoreCase)).Count() > 0).FirstOrDefault();
            return def;
        }
       
        public Publisher Publisher { get; set; }
        public MemberInfo PublishingMember { get; private set; }
        public MethodInfo SuccessMethod { get; private set; }
        public MethodInfo DefinitionMethod { get; private set; }
        public bool Public { get; private set; }

        public void AddSubscription(ISubscriberProxy proxy)
        {
            ReflectionPublicationContext pubCtx = new ReflectionPublicationContext(proxy.Subscription.Topic,
                this.Publisher,
                this.PublishingMember,
                this.SuccessMethod,
                this.DefinitionMethod,
                this.DefaultInterval,
                proxy);

            this.AddPublication(pubCtx);
        }

        public void RemoveSubscription(Subscription subscription)
        {

        }

        public void AddPublication(PublicationContext ctx)
        {
            if (this.Topic.IsMulticast && _contexts.ContainsKey(ctx.Subscription.Id))
            {
                foreach (ISubscriberProxy proxy in ctx.SubscriberProxies)
                    _contexts[ctx.Subscription.Id].AddProxy(proxy);
            }
            else
            {
                _contexts.Add(ctx.Subscription.Id, ctx);
                Altus.Core.Component.App.Instance.Shell.Add(ctx, ctx.GetType().FullName + "_" + ctx.Key); // this will alert the scheduler to drop the component
            }
        }

        public void RemovePublication(PublicationContext ctx)
        {
            if (_contexts.ContainsKey(ctx.Subscription.Id))
            {
                _contexts.Remove(ctx.Subscription.Id);
                Altus.Core.Component.App.Instance.Shell.Remove(ctx.GetType().FullName + "_" + ctx.Key); // this will alert the scheduler to drop the component
            }
        }


        public IEnumerator<PublicationContext> GetEnumerator()
        {
            foreach (PublicationContext ctx in this._contexts.Values)
                yield return ctx;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        
    }

    public class PublicationDefinition
    {
        public PublicationDefinition() { PublishIfChangedOnly = false; }
        public Topic Topic { get; set; }
        public string Id
        {
            get
            {
                return this.Topic.IsMulticast ? this.Topic.Name : this.Topic.Name + "|" + NodeIdentity.NodeAddress;
            }
        }
        public bool Public { get; set; }
        public string Format { get; set; }
        public int DefaultInterval { get; set; }
        public bool PublishIfChangedOnly { get; set; }

        public override string ToString()
        {
            return Id;
        }
    }

    public class PublicationComponentDefinition : PublicationDefinition
    {
        public string CLRType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Dependencies { get; set; }
        public override string ToString()
        {
            return  Name + "|" + base.ToString();
        }
    }
}
