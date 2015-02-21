using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Data;
using Altus.Core.PubSub;
using Altus.Core.PubSub.Dynamic;
using Altus.Core.Dynamic;
using Altus.Core.Licensing;
using Altus.Core.Presentation.ViewModels;
using Altus.Core.Collections;

[assembly: Component(ComponentType = typeof(DynamicTopicManager), Dependencies=new string[]{"RTDB"})]
namespace Altus.Core.PubSub.Dynamic
{
    public class DynamicTopicManager : InitializableComponent
    {
        static Dictionary<string, DynamicTopic> _topics = new Dictionary<string, DynamicTopic>();
        static HashSet<DeclaredApp> _loadedApps = new HashSet<DeclaredApp>();
        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        public static DynamicTopic GetTopic(string topicName)
        {
            return _topics[topicName];
        }

        public static void RegisterView(View view)
        {
            view.Topics.CollectionChanged -= Topics_CollectionChanged;
            view.Topics.CollectionChanged += Topics_CollectionChanged;
        }

        static void Topics_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (DynamicTopic t in (SafeObservableCollection<DynamicTopic>)sender)
            {
                if (!_topics.ContainsKey(t.Name))
                    _topics.Add(t.Name, t);
            }
        }

        static Dictionary<string, DynamicField> _cachedFields = new Dictionary<string, DynamicField>();
        public static bool TryGetField(string topicQualifiedFieldName, out DynamicField field)
        {
            lock (_cachedFields)
            {
                string tqfn = topicQualifiedFieldName;
                if (_cachedFields.ContainsKey(tqfn))
                {
                    field = _cachedFields[tqfn];
                }
                else
                {
                    string[] split = topicQualifiedFieldName.Split('.');
                    DynamicTopic dt = null;
                    field = null;
                    if (TryGetTopic(split[0], out dt))
                    {
                        topicQualifiedFieldName = topicQualifiedFieldName.Replace(split[0] + ".", "");
                        if (dt.TryGetField(topicQualifiedFieldName, out field))
                        {
                            _cachedFields.Add(tqfn, field);
                        }
                    }
                }
            }
            return field != null;
        }

        public static bool TryGetTopic(string topicName, out DynamicTopic topic)
        {
            if (!_loadedApps.Contains(Context.CurrentContext.CurrentApp))
            {
                foreach (Topic t in DataContext.Default.Select<Topic>())
                {
                    if (!_topics.ContainsKey(t.Name))
                        _topics.Add(t.Name, t);
                }
                _loadedApps.Add(Context.CurrentContext.CurrentApp);
            }
            if (_topics.ContainsKey(topicName))
            {
                topic = _topics[topicName];
                return true;
            }
            topic = null;
            return false;
        }
    }
}
