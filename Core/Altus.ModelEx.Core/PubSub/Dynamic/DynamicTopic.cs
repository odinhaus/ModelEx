using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.ComponentModel;
using Altus.Core.PubSub;
using Altus.Core.Data;
using Altus.Core.Realtime;
using Altus.Core.Dynamic;
using System.Reflection;
using Altus.Core.Diagnostics;
using Altus.Core.Security;

namespace Altus.Core.PubSub.Dynamic
{
    public delegate void DynamicSubscriptionHandler(object sender, DynamicSubscriptionHandlerEventArgs e);

    public class DynamicSubscriptionHandlerEventArgs : EventArgs
    {
        public DynamicSubscriptionHandlerEventArgs(SubscriptionHandlerArgs baseArgs, IEnumerable<DynamicField> dynamicFields)
        {
            Subscription = baseArgs.Subscription;
            Fields = dynamicFields;
        }

        public Subscription Subscription { get; private set; }
        public IEnumerable<DynamicField> Fields { get; private set; }
    }

    [StorageMapping("DynamicTopic")]
    public class DynamicTopic : DynamicObject, INotifyPropertyChanged
    {
        public event DynamicSubscriptionHandler Subscribe;

        public Topic FrameworkTopic { get; private set; }
        private DynamicOrgUnit FrameworkFields;

        public event PropertyChangedEventHandler PropertyChanged;


        public DynamicTopic(string topicName) : this(DataContext.Default.Get<Topic>(new { name = topicName })) {}        
        
        public DynamicTopic(Topic frameworkTopic)
        {
            this.FrameworkTopic = frameworkTopic;
            this.FrameworkFields = new DynamicOrgUnit(this) { Name = "" };
            this.Fields = new List<DynamicField>();
            if (frameworkTopic != null)
            {
                IEnumerable<Field> fields = DataContext.Default.GetTopicFields(this.FrameworkTopic.Name);
                foreach (Field f in fields)
                {
                    DynamicField df = new DynamicField(this, f);
                    AddField(df);
                }

                frameworkTopic.Subscribe += frameworkTopic_Subscribe;
            }
        }

        public void AddField(DynamicField df)
        {
            string objectName = df.FrameworkField.QualifiedName;
            DynamicOrgUnit n = this.FrameworkFields.CreateNode(objectName);
            ((List<DynamicField>)this.Fields).Add(df);
            _fieldsIdx.Add(df.FrameworkField.Id, df);
            n.AddChild(new DynamicOrgUnit(this) { Name = df.FrameworkField.Name, Instance = df });
            foreach (string alias in df.Aliases)
            {
                DynamicOrgUnit aliasNode = this.FrameworkFields.CreateNode(NodeIdentity.Application + ":" + alias);
                aliasNode.Instance = df;
            }
        }

        Dictionary<ulong, DynamicField> _fieldsIdx = new Dictionary<ulong, DynamicField>();
        void frameworkTopic_Subscribe(object sender, SubscriptionHandlerArgs e)
        {
            List<DynamicField> changes = new List<DynamicField>();
            foreach (Field f in e.Fields)
            {
                if (_fieldsIdx.ContainsKey(f.Id))
                {
                    DynamicField df = _fieldsIdx[f.Id];
                    df.Update(f.Value, f.TimeStamp, false);
                    changes.Add(df);
                }
            }
            this.CurrentChanges = changes;
            if (Subscribe != null)
            {
                Subscribe(this, new DynamicSubscriptionHandlerEventArgs(e, changes));
            }
        }

        public string Name { get { return this.FrameworkTopic.Name; } }
        public IEnumerable<DynamicField> Fields { get; private set; }
        public IEnumerable<DynamicField> CurrentChanges { get; private set; }

        private string FieldName(Field f)
        {
            return f.Name;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this.FrameworkFields.FindNode(binder.Name, true);
            bool ret = result != null;
            if (!ret)
            {
                Logger.LogError("The UI is looking to dynamically bind a Topic to an object called " + binder.Name + " which could not be found.");
            }
            return ret;
        }

        public bool TryGetField(string qualifiedFieldName, out DynamicField field)
        {
            field = null;
            DynamicOrgUnit dos = this.FrameworkFields.FindNode(qualifiedFieldName, true);
            if (dos != null)
                field = dos.Instance as DynamicField;
            return dos != null;
        }

        protected void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public static implicit operator DynamicTopic(Topic topic)
        {
            return new DynamicTopic(topic);
        }
    }
}
