using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WoodMac.ModelEx.Core.Component;
using WoodMac.ModelEx.Core.PubSub;
using System.ComponentModel;
using System.Reflection;
using WoodMac.ModelEx.Core.Security;

[assembly: Component(ComponentType = typeof(SubscriptionManager))]
namespace WoodMac.ModelEx.Core.PubSub
{
    public class SubscriptionManager : InitializableComponent
    {
        Dictionary<Topic, List<SubscriptionDefinition>> _topicSubscriptions = new Dictionary<Topic, List<SubscriptionDefinition>>();
        Dictionary<IComponent, List<SubscriptionDefinition>> _componentSubs = new Dictionary<IComponent, List<SubscriptionDefinition>>();


        protected override bool OnInitialize(params string[] args)
        {
            foreach (IComponent c in Connect.Instance.Shell.GetComponents<IComponent>())
            {
                AddSubscriptions(c);
            }

            Connect.Instance.Shell.ComponentChanged += new CompositionContainerComponentChangedHandler(Shell_ComponentChanged);
            return true;
        }

        void Shell_ComponentChanged(object sender, CompositionContainerComponentEventArgs e)
        {
            if (e.Change == CompositionContainerComponentChange.Add)
            {
                AddSubscriptions(e.Component);
            }
            else
            {
                RemoveSubscriptions(e.Component);
            }

            if (!attachedPubs && e.Component is PublicationManager)
            {
                AttachPublications((PublicationManager)e.Component);
            }
        }

        private void AttachPublications(PublicationManager subscriptionManager)
        {

        }

        private void RemoveSubscriptions(IComponent component)
        {

        }
        bool attachedPubs = false;
        private void AddSubscriptions(IComponent component)
        {
            lock (_componentSubs)
            {
                if (_componentSubs.ContainsKey(component)) return;

                MethodInfo[] members = component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                PublicationManager pm = Connect.Instance.Shell.GetComponent<PublicationManager>();
                attachedPubs = pm != null;
                foreach (MethodInfo member in members)
                {
                    SubscriptionAttribute[] subs;
                    MemberInfo targetMember = member;
                    if (targetMember is MethodInfo || targetMember is EventInfo)
                    {
                        subs = (SubscriptionAttribute[])member.GetCustomAttributes(typeof(SubscriptionAttribute), true);
                    }
                    else continue;

                    if (subs != null && subs.Length > 0)
                    {
                        if (!targetMember.DeclaringType.Equals(component.GetType()))
                            targetMember = targetMember.DeclaringType.GetMember(targetMember.Name, targetMember.MemberType, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).First();

                        foreach (SubscriptionAttribute sub in subs)
                        {
                            Subscriber subscriber = new Subscriber(NodeIdentity.NodeAddress, component);
                            SubscriptionDefinition subDef = new SubscriptionDefinition(sub, member, subscriber);
                                
                            if (!_componentSubs.ContainsKey(component))
                                _componentSubs.Add(component, new List<SubscriptionDefinition>());

                            _componentSubs[component].Add(subDef);

                            lock (_topicSubscriptions)
                            {
                                if (!_topicSubscriptions.ContainsKey(subDef.Topic))
                                    _topicSubscriptions.Add(subDef.Topic, new List<SubscriptionDefinition>());

                                _topicSubscriptions[subDef.Topic].Add(subDef);
                            }

                            if (pm != null)
                            {
                                IEnumerable<ReflectedPublicationDefinition> publications = pm.GetPublishers(subDef.Topic);
                                foreach (ReflectedPublicationDefinition pub in publications)
                                    subDef.AddPublication(pub);
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<SubscriptionDefinition> GetSubscriptions(Topic topic)
        {
            try
            {
                return _topicSubscriptions[topic];
            }
            catch
            {
                return new SubscriptionDefinition[0];
            }
        }
    }
}
