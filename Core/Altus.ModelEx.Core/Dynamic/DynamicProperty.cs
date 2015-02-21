using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Altus.Core;
using System.Reflection;
using Altus.Core.Data;

namespace Altus.Core.Dynamic
{  
    public class DynamicProperty<T> : INotifyPropertyChanged
    {
        public DynamicProperty() { }

        public DynamicProperty(T target, string propertyName, object scalarValue)
            : this(target, propertyName, scalarValue.GetType())
        {
            this.Settor(scalarValue);
        }

        public DynamicProperty(T target, string propertyName, string type) : this(target, propertyName, TypeHelper.GetType(type)) { }

        public DynamicProperty(T target, string propertyName, Type type)
        {
            this.TargetInstance = target;
            this.Name = propertyName;
            this.Type = type;

            MemberInfo prop = null;
            object bi = null;
            if (target.GetType().BaseType.IsGenericType
                && target.GetType().BaseType.GetGenericTypeDefinition().Equals(typeof(Extendable<>)))
            {
                bi = target.GetType().GetProperty("BackingInstance").GetValue(target, null);
                if (bi == null)
                {
                    bi = target;
                }
            }
            if (bi != null)
            {
                prop = bi.GetType().GetMember(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
                if (prop != null
                    && !prop.DeclaringType.Equals(bi.GetType()))
                    prop = prop.DeclaringType.GetMember(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
            }

            if (prop == null)
            {
                this.Gettor = new Func<object>(() => { return _value; });
                this.Settor = new Action<object>((object value) => { _value = value; OnPropertyChanged(this.Name); });
            }
            else
            {
                if (prop is PropertyInfo)
                {
                    this.Gettor = new Func<object>(delegate() { return ((PropertyInfo)prop).GetValue(bi, null); });
                    this.Settor = new Action<object>(delegate(object value) { ((PropertyInfo)prop).SetValue(bi, value, null); });
                }
                else if (prop is FieldInfo)
                {
                    this.Gettor = new Func<object>(delegate() { return ((FieldInfo)prop).GetValue(bi); });
                    this.Settor = new Action<object>(delegate(object value) { ((FieldInfo)prop).SetValue(bi, value); });
                }
            }
        }

        public DynamicProperty(T target, string instanceName, string propertyName, string type, string gettorCS, string settorCS, string bodyCS, string references)
            : this(target, propertyName, TypeHelper.GetType(type), DynamicPropertyEvaluatorBuilder.Create(target, instanceName, propertyName, gettorCS, settorCS, bodyCS, references)) { }

        public DynamicProperty(T target, string propertyName, Type type, IDynamicPropertyEvaluator evaluator)
        {
            this.TargetInstance = target;
            this.Name = propertyName;
            this.Type = type;
            this.Gettor = evaluator.Gettor;
            this.Settor = evaluator.Settor;
            evaluator.PropertyChanged += new PropertyChangedEventHandler(evaluator_PropertyChanged);
        }

        void evaluator_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, e);
            }
        }

        public T TargetInstance { get; private set; }       
        public string Name { get; private set; }
        public Type Type { get; private set; }
        
        private Func<object> Gettor;
        private Action<System.Object> Settor;

        object _value;        
        public object Value { get { return this.Gettor(); } set { this.Settor(value); } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

    }
}
