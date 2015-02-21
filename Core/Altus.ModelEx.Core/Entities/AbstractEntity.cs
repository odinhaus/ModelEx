using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.ComponentModel;
using Altus.Core.Serialization;
using System.IO;
using System.Runtime.Serialization;
using Altus.Core.Serialization.Binary;
using Altus.Core.Component;

namespace Altus.Core.Entities
{
    [DataContract()]
    [System.Serializable]
    public abstract class AbstractEntity : INotifyPropertyChanged, IEqualityComparer<AbstractEntity>
    {
        public virtual void Import(AbstractEntity entity)
        {
            ; // no-op by default
        }

        protected bool Contains<T>(IList<T> list, T ent, IEqualityComparer<T> comparer)
        {
            if (comparer == null)
                return list.Contains<T>(ent);
            else
                return list.Contains<T>(ent, comparer);
        }

        public virtual AbstractEntity Clone()
        {
            ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>().Where(s
                    => s.SupportsFormat(StandardFormats.BINARY) && s.SupportsType(this.GetType())).FirstOrDefault();
            if (serializer == null) throw (new Altus.Core.Serialization.SerializationException("Serializer not found for type \"" 
                + this.GetType().FullName + "\" supporting the " + StandardFormats.BINARY + " format."));
            return (AbstractEntity)serializer.Deserialize(serializer.Serialize(this), this.GetType());
        }

        
        public override bool Equals(object obj)
        {
            if (!obj.GetType().Equals(this.GetType())) return false;
            return base.Equals(obj);
        }

        public virtual bool Equals(AbstractEntity x, AbstractEntity y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Equals(y) && y.Equals(x);
        }

        public int GetHashCode(AbstractEntity obj)
        {
            return obj.GetHashCode();
        }

        protected AbstractEntity()
        {
            AdditionalPayload = new byte[0];
        }
        
        [DataMember()]
        public byte[] AdditionalPayload { get; protected set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName, object newValue)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
