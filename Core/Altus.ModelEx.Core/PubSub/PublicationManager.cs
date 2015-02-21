using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WoodMac.ModelEx.Core.Component;
using WoodMac.ModelEx.Core.PubSub;
using System.ComponentModel;
using System.Reflection;
using WoodMac.ModelEx.Core.Data;
using WoodMac.ModelEx.Core.Security;

[assembly: Component(ComponentType=typeof(PublicationManager))]
namespace WoodMac.ModelEx.Core.PubSub
{
    public class PublicationManager : InitializableComponent
    {
        Dictionary<IComponent, List<ReflectedPublicationDefinition>> _componentPubs = new Dictionary<IComponent, List<ReflectedPublicationDefinition>>();
        Dictionary<Topic, List<ReflectedPublicationDefinition>> _topicPubs = new Dictionary<Topic, List<ReflectedPublicationDefinition>>();

        protected override bool OnInitialize(params string[] args)
        {
            foreach (IComponent c in Connect.Instance.Shell.GetComponents<IComponent>())
            {
                AddPublications(c);
            }

            Connect.Instance.Shell.ComponentChanged += new CompositionContainerComponentChangedHandler(Shell_ComponentChanged);
            return true;
        }

        void Shell_ComponentChanged(object sender, CompositionContainerComponentEventArgs e)
        {
            if (e.Change == CompositionContainerComponentChange.Add)
            {
                AddPublications(e.Component);
            }
            else
            {
                RemovePublications(e.Component);
            }

            if (!attachedSubs && e.Component is SubscriptionManager)
            {
                AttachSubscriptions((SubscriptionManager)e.Component);
            }
        }

        private void AttachSubscriptions(SubscriptionManager subscriptionManager)
        {
            foreach (Topic topic in this._topicPubs.Keys)
            {
                foreach (SubscriptionDefinition sub in subscriptionManager.GetSubscriptions(topic))
                {
                    foreach (ReflectedPublicationDefinition pubDef in _topicPubs[topic])
                    {
                        //pubDef.AddSubscription(sub);
                        sub.AddPublication(pubDef);
                    }
                }
            }
        }

        private void RemovePublications(IComponent component)
        {
            if (_componentPubs.ContainsKey(component))
            {
                _componentPubs.Remove(component);
            }
        }
        private bool attachedSubs = false;
        private void AddPublications(IComponent component)
        {
            lock (_componentPubs)
            {
                if (_componentPubs.ContainsKey(component)) return;

                MemberInfo[] members = component.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                SubscriptionManager sm = Connect.Instance.Shell.GetComponent<SubscriptionManager>();
                attachedSubs = sm != null;
                foreach (MemberInfo member in members)
                {
                    PublicationAttribute[] pubs;
                    MemberInfo targetMember = member;
                    if (targetMember is MethodInfo || targetMember is EventInfo)
                    {
                        pubs = (PublicationAttribute[])member.GetCustomAttributes(typeof(PublicationAttribute), true);
                    }
                    else continue;

                    if (pubs != null && pubs.Length > 0)
                    {
                        if (!targetMember.DeclaringType.Equals(component.GetType()))
                            targetMember = targetMember.DeclaringType.GetMember(targetMember.Name, targetMember.MemberType, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).First();

                        foreach (PublicationAttribute pub in pubs)
                        {
                            ReflectedPublicationDefinition pubDef = new ReflectedPublicationDefinition(pub, member, new Publisher(NodeIdentity.NodeAddress, component));

                            if (!_componentPubs.ContainsKey(component))
                                _componentPubs.Add(component, new List<ReflectedPublicationDefinition>());

                            _componentPubs[component].Add(pubDef);

                            lock (_topicPubs)
                            {
                                if (!_topicPubs.ContainsKey(pubDef.Topic))
                                    _topicPubs.Add(pubDef.Topic, new List<ReflectedPublicationDefinition>());

                                _topicPubs[pubDef.Topic].Add(pubDef);
                            }
                            if (sm != null)
                            {
                                IEnumerable<SubscriptionDefinition> subscriptions = sm.GetSubscriptions(pubDef.Topic);
                                foreach (SubscriptionDefinition sub in subscriptions)
                                    sub.AddPublication(pubDef);
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<ReflectedPublicationDefinition> GetPublishers(Topic topic)
        {
            try
            {
                return _topicPubs[topic];
            }
            catch
            {
                return new ReflectedPublicationDefinition[0];
            }
        }
    }
}
