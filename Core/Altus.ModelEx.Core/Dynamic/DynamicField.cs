using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Realtime;
using Altus.Core.Processing;
using Altus.Core.Processing.Rpc;
using Altus.Core.Messaging.Udp;
using System.Dynamic;
using System.Threading;
using System.ComponentModel;
using Altus.Core.Data;
using Altus.Core.PubSub.Dynamic;
using Altus.Core.Presentation.Commands;
using Altus.Core;
using Altus.Core.Licensing;

namespace Altus.Core.Dynamic
{
    public class DynamicField : Extendable<DynamicField>
    {
        private DateTime RefTime = CurrentTime.Now;
        private Dictionary<string, RelatedField> RelatedFieldBag = new Dictionary<string, RelatedField>();
        

        public DynamicField(DynamicTopic topic, Field field) : base(field.QualifiedName, field)
        {
            this.Topic = topic;
        }

        public DynamicField(DynamicTopic topic, Field field, bool isExtendable)
            : base(field.QualifiedName, field, isExtendable)
        {
            this.Topic = topic;
        }

        public DynamicField(DynamicTopic topic, Field field, bool isExtendable, MemberResolutionHandler memberResolutionFailedCallback)
            : base(field.QualifiedName, field, isExtendable, memberResolutionFailedCallback)
        {
            this.Topic = topic;
        }

        protected override string OnGetInstanceType()
        {
            return "Field";
        }

        protected override void OnExtend()
        {
            OnExtendField();
            base.OnExtend();
        }

        protected virtual void OnExtendField()
        {
            if (!base._extensionsLoaded.Contains(Context.CurrentContext.CurrentApp))
            {
                foreach (RelatedField rf in OnGetRelatedFields())
                {
                    RelatedFieldBag.Add(rf.RelationshipType.ToLowerInvariant(), rf);
                    string cmdPrefix = string.Concat("OnSet", rf.RelationshipType);
                    if (this.FunctionBag.ContainsKey(string.Concat(cmdPrefix, "Value")))
                    {
                        CreateCommand(this.FunctionBag[string.Concat(cmdPrefix, "Value")]);
                    }
                    if (this.FunctionBag.ContainsKey(string.Concat(cmdPrefix, "ValueNumeric")))
                    {
                        CreateCommand(this.FunctionBag[string.Concat(cmdPrefix, "ValueNumeric")]);
                    }
                }
            }
        }

        protected virtual IEnumerable<RelatedField> OnGetRelatedFields()
        {
            return DataContext.Default.Select<RelatedField>(new { field = this });            
        }

        protected override void SetProperties()
        {
            base.SetProperties();
            this.FrameworkField.SetValueNumeric(0, typeof(float));
        }

        
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            Func<GetMemberBinder, object> pointer = null;
            result = null;
            lock (_memberPointers)
            {
                if (_memberPointers.ContainsKey(binder.Name))
                {
                    pointer = _memberPointers[binder.Name];
                }
                else
                {
                    OnExtend();

                    if (!this.IsSubscribed
                        && (binder.Name.Equals("Value", StringComparison.InvariantCultureIgnoreCase)
                        || binder.Name.Equals("ValueNumeric", StringComparison.InvariantCultureIgnoreCase)))
                        Subscribe();

                    string opName = binder.Name.ToLowerInvariant().Replace("valuenumeric", "").Replace("value", "").Replace("field", "");
                    DynamicField df = null;
                    if (this.RelatedFieldBag.ContainsKey(opName))
                    {
                        RelatedField rf = this.RelatedFieldBag[opName];
                        if (rf.IsWired)
                        {
                            df = rf.DynamicField;
                        }
                        else if (this.Topic != null)
                        {
                            if (this.Topic.TryGetField(rf.Field.QualifiedName, out df))
                            {
                                rf.DynamicField = df;
                                rf.IsWired = true;
                                df.PropertyChanged += new PropertyChangedEventHandler(RelatedField_PropertyChanged);
                            }
                        }

                        if (df != null)
                        {
                            string propName = binder.Name.ToLowerInvariant().Replace(opName, "");

                            if (!df.IsSubscribed)
                                df.Subscribe();

                            switch (propName)
                            {

                                case "value":
                                    {
                                        result = (object)df.FrameworkField.ValueObject;
                                        pointer = new Func<GetMemberBinder, object>(delegate(GetMemberBinder b)
                                        {
                                            string op = b.Name.ToLowerInvariant().Replace("valuenumeric", "").Replace("value", "").Replace("field", "");
                                            RelatedField rff = this.RelatedFieldBag[op];
                                            object ret = rff.DynamicField.FrameworkField.ValueObject;
                                            return ret;
                                        });
                                        break;
                                    }
                                case "":
                                case "valuenumeric":
                                    {
                                        result = df.FrameworkField.ValueNumeric;
                                        pointer = new Func<GetMemberBinder, object>(delegate(GetMemberBinder b)
                                        {
                                            string op = b.Name.ToLowerInvariant().Replace("valuenumeric", "").Replace("value", "").Replace("field", "");
                                            RelatedField rff = this.RelatedFieldBag[op];
                                            object ret = rff.DynamicField.FrameworkField.ValueNumeric;
                                            return ret;
                                        });
                                        break;
                                    }
                                case "field":
                                    {
                                        result = df.FrameworkField;
                                        pointer = new Func<GetMemberBinder, object>(delegate(GetMemberBinder b)
                                        {
                                            string op = b.Name.ToLowerInvariant().Replace("valuenumeric", "").Replace("value", "").Replace("field", "");
                                            RelatedField rff = this.RelatedFieldBag[op];
                                            object ret = rff.DynamicField.FrameworkField;
                                            return ret;
                                        });
                                        break;
                                    }
                                default:
                                    {
                                        result = df;
                                        pointer = new Func<GetMemberBinder, object>(delegate(GetMemberBinder b)
                                        {
                                            string op = b.Name.ToLowerInvariant().Replace("valuenumeric", "").Replace("value", "").Replace("field", "");
                                            RelatedField rff = this.RelatedFieldBag[op];
                                            object ret = rff.DynamicField;
                                            return ret;
                                        });
                                        break;
                                    }
                            }
                        }
                    }

                    if (pointer == null)
                    {
                        opName = opName.Replace("set", "");
                        if (this.RelatedFieldBag.ContainsKey(opName))
                        {
                            string cmdKey = string.Concat("set", opName);
                            cmdKey = string.Concat("on", cmdKey, binder.Name.ToLowerInvariant().Replace(cmdKey, "")); //onsetpropertyname[value|valuenumeric]
                            if (this.CommandBag.ContainsKey(cmdKey))
                            {
                                pointer = new Func<GetMemberBinder, object>(delegate(GetMemberBinder b)
                                {
                                    string op = b.Name.ToLowerInvariant().Replace("valuenumeric", "").Replace("value", "").Replace("field", "");
                                    op = op.Replace("set", "");
                                    string ck = string.Concat("set", op);
                                    object ret = this.CommandBag[ck];
                                    return ret;
                                });
                                result = this.CommandBag[cmdKey];
                            }
                        }
                    }
                    
                    if (pointer == null
                        && binder.Name.Equals("Value", StringComparison.InvariantCultureIgnoreCase)
                        && TryGetBackingMember(binder, out result))
                    {
                        pointer = new Func<GetMemberBinder, object>(delegate(GetMemberBinder b)
                        {
                            object ret;
                            TryGetBackingMember(b, out ret);
                            return (object)ret;
                        });
                    }
                    else if (pointer == null
                        && base.TryGetMember(binder, out result))
                    {
                            pointer = new Func<GetMemberBinder, object>(delegate(GetMemberBinder b)
                                {
                                    object ret;
                                    base.TryGetMember(b, out ret);
                                    return ret;
                                });
                    }
                    

                    if (pointer != null
                        && !_memberPointers.ContainsKey(binder.Name))
                    {
                        _memberPointers.Add(binder.Name, pointer);
                    }
                }

                if (pointer == null) return false;
                else
                {
                    if (result == null)
                        result = pointer(binder);
                    return true;
                }
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            OnExtend();

            if (!this.IsSubscribed
                && (binder.Name.Equals("Value", StringComparison.InvariantCultureIgnoreCase)
                || binder.Name.Equals("ValueNumeric", StringComparison.InvariantCultureIgnoreCase)))
                Subscribe();

            string opName = binder.Name.ToLowerInvariant().Replace("valuenumeric", "").Replace("value", "").Replace("field", "");
            DynamicField df = null;
            if (this.RelatedFieldBag.ContainsKey(opName))
            {
                RelatedField rf = this.RelatedFieldBag[opName];
                if (rf.IsWired)
                {
                    df = rf.DynamicField;
                }
                else if (this.Topic != null)
                {
                    if (this.Topic.TryGetField(rf.Field.QualifiedName, out df))
                    {
                        rf.DynamicField = df;
                        rf.IsWired = true;
                        df.PropertyChanged += new PropertyChangedEventHandler(RelatedField_PropertyChanged); 
                    }
                }

                if (df != null)
                {
                    string propName = binder.Name.ToLowerInvariant().Replace(opName, "");

                    if (!df.IsSubscribed)
                        df.Subscribe();

                    switch (propName)
                    {
                        case "value":
                            {
                                Field f = df.FrameworkField;
                                f.Value = (IComparable)value;
                                df.FrameworkField = f;
                                return true;
                            }
                        case "valuenumeric":
                            {
                                df.FrameworkField.SetValueNumeric((double)value, df.FrameworkField.Value.GetType());
                                return true;
                            }
                        case "field":
                            {
                                df.FrameworkField = (Field)value;
                                return true;
                            }
                        case "":
                            {
                                rf.DynamicField = (DynamicField)value;
                                break;
                            }
                    }
                }
            }

            return base.TrySetMember(binder, value);
        }

        private void RelatedField_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RelatedField rf = this.RelatedFieldBag.Values.Where(r => r.DynamicField != null && r.DynamicField.Equals(sender)).FirstOrDefault();

            if (rf != null
                && !(e.PropertyName.StartsWith(rf.RelationshipType)))
            {
                this.OnPropertyChanged(rf.RelationshipType);
            }
        }

        public DynamicTopic Topic { get; private set; }
        public bool IsSubscribed { get; private set; }
        public bool IsWriteable { get; set; }
        public Field FrameworkField { get { return (Field)this.BackingInstance; } protected set { this.BackingInstance = value; } }
        public string TopicQualifiedName { get { return string.Concat(this.Topic.Name, ".", this.FrameworkField.QualifiedName); } }

        public void Update(IComparable value, DateTime timestamp, bool notifyChanges)
        {
            _field.SetValue(value);
            _field.SetTimestamp(timestamp);
            if (notifyChanges)
            {
                OnPropertyChanged("BackingInstance");
            }
        }

        private Field _field;

        protected override object OnGetBackingInstance()
        {
            return _field;
        }

        protected override void OnSetBackingInstance(object value, bool isCalledFromCtor)
        {
            _field = (Field)value;
            if (!isCalledFromCtor)
            {
                this.OnPropertyChanged("BackingInstance");
            }
        }

        private void WriteValue(IComparable value)
        {
            throw new NotImplementedException();
        }

        private void Subscribe()
        {
            IsSubscribed = true;
            if (this.Topic != null)
            {
                this.Topic.FrameworkTopic.Subscribe += FrameworkTopic_Subscribe;
            }
        }

        void FrameworkTopic_Subscribe(object sender, PubSub.SubscriptionHandlerArgs e)
        {
            Field field = e.Fields.Where(f => f.Name.Equals(this.FrameworkField.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (field.TimeStamp > DateTime.MinValue)
            {
                Field f = this.FrameworkField;
                f.Value = field.Value;
                f.TimeStamp = field.TimeStamp;
                this.FrameworkField = f;
                OnPropertyChanged("Value");
                OnPropertyChanged("ValueNumeric");
            }   
        }

        public override string ToString()
        {
            return this.FrameworkField.ToString();
        }

        protected override IEnumerable<string> OnGetAliases()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<DynamicProperty<DynamicField>> OnGetProperties()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<DynamicFunction<DynamicField>> OnGetFunctions()
        {
            throw new NotImplementedException();
        }
    }
}
