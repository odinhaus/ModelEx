using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Data;
using System.Net;
using Altus.Core.Serialization;
using Altus.Core.Topology;
using Altus.Core.Processing;
using Altus.Core.Processing.Rpc;
using Altus.Core.Messaging.Udp;
using Altus.Core.Component;
using Altus.Core.Messaging;
using Altus.Core.Security;
using Altus.Core;
using Altus.Core.Licensing;
using Altus.Core.Realtime;

namespace Altus.Core.PubSub
{
    [StorageMapping("Topic")]
    public class Topic 
    {
        private static Dictionary<Topic, Topic.TopicSubscription> _subscriptions = new Dictionary<Topic, Topic.TopicSubscription>();
        public event SubscriptionHandler Subscribe
        {
            add
            {
                lock (_subscriptions)
                {
                    SubscriptionHandler handler;
                    if (_subscriptions.ContainsKey(this))
                    {
                        _subscriptions[this].Handler += value;
                    }
                    else
                    {
                        handler = new SubscriptionHandler(value);
                        _subscriptions.Add(this, CreateSubscription(this, handler));
                    }
                }
            }
            remove
            {
                lock (_subscriptions)
                {
                    if (_subscriptions.ContainsKey(this))
                    {
                        _subscriptions[this].Handler -= value;
                        if (_subscriptions[this].Handler.GetInvocationList().Length == 0)
                        {
                            RemoveSubscription(this);
                        }
                    }
                }
            }
        }

        private static TopicSubscription CreateSubscription(Topic topic, SubscriptionHandler handler)
        {
            //topic.Internal.CheckLocalObjectWiring();
            TopicSubscriber ts = new TopicSubscriber(topic);
            Subscription sub = new Subscription(
                new Subscriber(NodeAddress.Current.Address, ts),
                topic,
                new Publisher(NodeAddress.Any, ts));

            return new TopicSubscription() { Subscription = sub, Handler = handler };
        }

        public void Publish()
        {
            this.Internal.Update();
            ServiceParameter sp = new ServiceParameter("Data",
                typeof(Field[]).FullName, ParameterDirection.In)
                {
                    Value = this.IsPublisherSpecific ? this.Internal[NodeIdentity.NodeAddress] : this.Internal.Fields.ToArray()
                };
            ServiceOperation.Publish(this.Definition, sp);
        }

        public IEnumerable<Field> Fields
        {
            get
            {
                return this.Internal.Fields.ToArray();
            }
        }

        private static void RemoveSubscription(Topic topic)
        {

        }

        private class TopicSubscription
        {
            public SubscriptionHandler Handler { get; set; }
            public Subscription Subscription { get; set; }
        }

        private class TopicSubscriber
        {
            public TopicSubscriber(Topic topic)
            {
                this.Topic = topic;
                OnInitialize();
            }

            public Topic Topic { get; private set; }
            public Subscription Subscription { get; private set; }

            protected void OnInitialize(params string[] args)
            {
                if (this.Topic.IsMulticast)
                {
                    CreateMulticastSubscription();
                }
                else
                {
                    CreateUnicastSubscription();
                }
            }

            private void CreateUnicastSubscription()
            {
                throw new NotImplementedException();
            }

            private void CreateMulticastSubscription()
            {
                ServiceEndPointManager sem = App.Instance.Shell.GetComponent<ServiceEndPointManager>();
                if (sem == null)
                {
                    App.Instance.Shell.ComponentChanged += Shell_ComponentChanged;
                }
                else
                    RegisterMulticastSubscription(sem);
            }

            void Shell_ComponentChanged(object sender, CompositionContainerComponentEventArgs e)
            {
                if (e.Component is ServiceEndPointManager
                    && e.Change == CompositionContainerComponentChange.Add)
                {
                    RegisterMulticastSubscription((ServiceEndPointManager)e.Component);
                }
            }

            private void RegisterMulticastSubscription(ServiceEndPointManager sem)
            {
                RpcOperationAttribute op = new RpcOperationAttribute(this.Topic.Name, new Action<Field[]>(Subscribe));
                op.SingletonTarget = true;

                op.ServiceEndPoint = new RpcEndPointAttribute(string.Format(@"udp://[a-zA-Z0-9\:\.\*]+/[\w\._\-\+]+/[a-zA-Z0-9\:\.\*]+\({0}\)", this.Topic.Name));
                sem.CreateServiceOperation(op, op.ServiceEndPoint.Routes[0]);

                UdpHost host = App.Instance.Shell.GetComponent<UdpHost>();
                host.JoinGroup(this.Topic);
            }

            private void Subscribe(Field[] data)
            {
                ServiceContext ctx = ServiceContext.Current;
                if (this.Topic.IsPublisherSpecific)
                {
                    this.Topic.Internal[ctx.Sender] = data;
                }
                else
                {
                    this.Topic.Internal.CurrentChanges = data;
                }
            }
        }


        //private Topic() { this.Definition = new PublicationDefinition() { Topic = this, Public = false, Format = StandardFormats.BINARY }; }

        public Topic(int id, string name, bool isDeliveryGuaranteed, bool isSubscriberSpecific, bool isPublisherSpecific, 
            string multiCastIp = null, int multicastPort = 0, string defaultFormat = StandardFormats.BINARY)
        {
            this.Id = id;
            this.Name = name;
            this.IsDeliveryGuaranteed = isDeliveryGuaranteed;
            this.IsSubscriberSpecific = isSubscriberSpecific;
            this.IsPublisherSpecific = isPublisherSpecific;
            this.MulticastIP = multiCastIp;
            this.MulticastPort = multicastPort;
            this.DefaultFormat = defaultFormat;
            this.Definition = new PublicationDefinition() { Topic = this, Public = false, Format = defaultFormat };
            //this.Internal = CreateInternal(this);
        }

        static Dictionary<string, TopicInternal> _topics = new Dictionary<string, TopicInternal>();
        private static TopicInternal CreateInternal(Topic topic)
        {
            if (string.IsNullOrEmpty(topic.Name)) return null;

            lock (_topics)
            {
                if (!_topics.ContainsKey(topic.Name))
                {
                    _topics.Add(topic.Name, new TopicInternal(topic));
                }
                return _topics[topic.Name];
            }
        }

        public PublicationDefinition Definition { get; private set; }

        public int Id { get; private set; }

        private string _name;

        public string Name 
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    //this.Internal = CreateInternal(this);
                }
            }
        }

        bool _delivery = false;
        public bool IsDeliveryGuaranteed
        {
            get
            {
                return _delivery && !IsMulticast;
            }
            set
            {
                _delivery = value;
            }
        }

        public bool IsSubscriberSpecific { get; private set; }
        public bool IsPublisherSpecific { get; private set; }
        public bool IsMulticast { get { return !IsSubscriberSpecific; } }
        public string MulticastIP { get; private set; }
        public int MulticastPort { get; private set; }
        public string DefaultFormat { get; private set; }
        //public long OrganizationId { get; set; }
        //public long PlatformId { get; set; }

        IPEndPoint _ep;
        public IPEndPoint MulticastEndPoint
        {
            get
            {
                if (string.IsNullOrEmpty(this.MulticastIP))
                {
                    return new IPEndPoint(0, 0);
                }
                else
                {
                    if (_ep == null)
                    {
                        IPAddress[] addresses = (IPAddress[])Context.Cache[this.MulticastIP];
                        if (addresses == null)
                        {
                            addresses = Dns.GetHostAddresses(this.MulticastIP);
                            Context.Cache.Add(this.MulticastIP, addresses, DateTime.MinValue, TimeSpan.FromHours(12), new string[0]);
                        }
                        _ep = new IPEndPoint(addresses[0], this.MulticastPort);

                        foreach (IPAddress address in addresses)
                        {
                            if (IPAddress.Loopback.Equals(addresses))
                            {
                                _ep = new IPEndPoint(address, this.MulticastPort);
                                break;
                            }
                        }
                    }
                    return _ep;
                }
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Topic && ((Topic)obj).Id.Equals(this.Id);
        }

        public override int GetHashCode()
        {
            return this.Id;
        }

        //public static implicit operator Topic(string topicName)
        //{
        //    Topic topic = DataContext.Default.GetTopic(topicName);
        //    if (topic == null) throw (new InvalidCastException("The topic does not exist."));
        //    return topic;
        //}

        private bool CheckNameIsLongEnough(object entity)
        {
            return ((Topic)entity).Name.Length > 2;
        }

        public DateTime LastUpdated { get { return Internal.LastUpdated; } }

        private TopicInternal Internal { get; set; }

        
        private class TopicInternal
        {
            private class FieldSource
            {
                public FieldSource(IFieldSource obj, IEnumerable<Field> fields) 
                { 
                    Object = obj; 
                    IndexedFields = new IndexedField[fields.Count()];
                    _fields = new Field[IndexedFields.Length];
                    int index = 0;

                    foreach (Field f in fields)
                    {
                        IndexedFields[index] = new IndexedField() { FieldId = f.Id, Index = index };
                        _fields[index] = obj.Read(f.Id);
                        index++;
                    }
                }
                public IFieldSource Object { get; private set; }

                Field[] _fields;
                public IEnumerable<Field> Fields { get { return _fields; } }
                private IndexedField[] IndexedFields;

                public IEnumerable<Field> GetLatest()
                {
                    foreach (IndexedField idxF in IndexedFields)
                    {
                        _fields[idxF.Index] = Object.Read(idxF.FieldId);
                    }
                    return Fields;
                }

                private class IndexedField
                {
                    public ushort FieldId { get; set; }
                    public int Index { get; set; }
                }
            }

            public TopicInternal(Topic topic)
            {
                this.Topic = topic;
            }

            public Topic Topic { get; private set; }
            public DateTime LastUpdated { get; private set; }

            Dictionary<ulong, Field> _myFieldsIdx = new Dictionary<ulong, Field>();
            HashSet<FieldSource> _sources = null;

            public void Update()
            {
                if (_sources == null)
                {
                    SetSources();
                }
                List<Field> fields = new List<Field>();
                foreach (FieldSource source in _sources)
                {
                    fields.AddRange(source.GetLatest());
                }

                CurrentChanges = fields;
            }

            private void SetSources()
            {
                _sources = new HashSet<FieldSource>();
                foreach (var group in Fields.GroupBy(f => f.Object))
                {
                    IFieldSource obj = App.Instance.Shell.GetComponents<IFieldSource>()
                        .Where(ifs => ifs.Name.Equals(group.Key)).FirstOrDefault();
                    if (obj != null)
                    {
                        FieldSource fs = new FieldSource(obj, group.ToArray());
                        _sources.Add(fs);
                    }
                }
            }

            HashSet<DeclaredApp> _loadedApps = new HashSet<DeclaredApp>();
            public IEnumerable<Field> Fields
            {
                get
                {
                    LoadFields();
                    return _myFieldsIdx.Values.ToArray();
                }
            }

            private void LoadFields()
            {
                lock (_loadedApps)
                {
                    if (!_loadedApps.Contains(Context.CurrentContext.CurrentApp))
                    {
                        foreach (Field field in DataContext.Default.GetTopicFields(this.Topic.Name))
                        {
                            if (!_myFieldsIdx.ContainsKey(field.Id))
                                _myFieldsIdx.Add(field.Id, field);
                        }
                        _loadedApps.Add(Context.CurrentContext.CurrentApp);
                    }
                }
            }

            public IEnumerable<Field> CurrentChanges
            {
                private get
                {
                    lock (_data)
                    {
                        if (!_data.ContainsKey(this))
                        {
                            _data.Add(this, new Field[0]);
                        }
                        return _data[this];
                    }
                }
                set
                {
                    lock (_data)
                    {

                        LoadFields();

                        List<Field> changes = new List<Field>();
                        foreach (Field f in value)
                        {
                            if (_myFieldsIdx.ContainsKey(f.Id)
                                && (!this.Topic.Definition.PublishIfChangedOnly
                                ||
                                (
                                    (_myFieldsIdx[f.Id].Value != null && f.Value != null && !_myFieldsIdx[f.Id].Value.Equals(f.Value))
                                    || (_myFieldsIdx[f.Id].Value == null && f.Value != null)
                                )))
                            {
                                _myFieldsIdx[f.Id] = f;
                                changes.Add(f);
                            }
                        }

                        if (_data.ContainsKey(this))
                        {
                            _data[this] = changes;
                        }
                        else
                        {
                            _data.Add(this, changes);
                        }
                    }
                    OnDataChanged();
                }
            }

            private void OnDataChanged()
            {
                lock (_subscriptions)
                {
                    LastUpdated = CurrentTime.Now;
                    TopicSubscription ts;
                    if (_subscriptions.TryGetValue(this.Topic, out ts))
                    {
                        ts.Handler(this.Topic, new SubscriptionHandlerArgs(ts.Subscription, this.CurrentChanges));
                    }
                }
            }

            private static Dictionary<TopicInternal, IEnumerable<Field>> _data = new Dictionary<TopicInternal, IEnumerable<Field>>();
            private static Dictionary<TopicInternal, Dictionary<string, IEnumerable<Field>>> _publisherData = new Dictionary<TopicInternal, Dictionary<string, IEnumerable<Field>>>();

            public IEnumerable<Field> this[string publisher]
            {
                get
                {
                    lock (_publisherData)
                    {
                        if (!_publisherData.ContainsKey(this))
                            _publisherData.Add(this, new Dictionary<string, IEnumerable<Field>>());

                        Dictionary<string, IEnumerable<Field>> dict = _publisherData[this];
                        string key = publisher.Trim().ToLower();

                        if (!dict.ContainsKey(key))
                            dict.Add(key, new Field[0]);

                        return dict[key];
                    }
                }
                set
                {
                    lock (_publisherData)
                    {
                        //value = (from f in value
                        //         join ff in Fields on f.CompositeId equals ff.CompositeId
                        //         select f).ToArray();
                        int setVals = 0;
                        int count = Fields.Count();
                        List<Field> current = new List<Field>();
                        foreach (Field f in value)
                        {
                            if (_myFieldsIdx.ContainsKey(f.Id))
                            {
                                current.Add(f);
                                setVals++;
                                if (setVals == count)
                                    break;
                            }
                            if (setVals == count)
                                break;
                        }

                        if (!_publisherData.ContainsKey(this))
                            _publisherData.Add(this, new Dictionary<string, IEnumerable<Field>>());

                        Dictionary<string, IEnumerable<Field>> dict = _publisherData[this];
                        string key = publisher.Trim().ToLower();

                        if (!dict.ContainsKey(key))
                            dict.Add(key, value);
                        else
                            dict[key] = value;

                        CurrentChanges = value;
                    }
                }
            }
        }
    }
}
