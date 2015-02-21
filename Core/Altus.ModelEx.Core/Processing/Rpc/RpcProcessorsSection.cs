using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Altus.Core.Processing.Rpc
{
    public class RpcProcessorsSection : ConfigurationSection
    {
        [ConfigurationProperty("processors", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(RpcProcessorElement), AddItemName = "add")]
        public RpcProcessorCollection Processors
        {
            get
            {
                return (RpcProcessorCollection)this["processors"];
            }
            set
            {
                this["processors"] = value;
            }
        }
    }

    public class RpcProcessorCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new RpcProcessorElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            RpcProcessorElement service = (RpcProcessorElement)element;

            return getKey(service);
        }

        public void Sort()
        {
            List<RpcProcessorElement> sorted = new List<RpcProcessorElement>();
            foreach (RpcProcessorElement e in this)
            {
                sorted.Add(e);
            }
            sorted.Sort();
            this.Clear();
            foreach (RpcProcessorElement e in sorted)
            {
                this.Add(e);
            }
        }

        /// <summary>
        /// Gets or sets the named service element for the given index.
        /// </summary>
        /// <param name="index">The index of the named service element to get or set.</param>
        /// <returns>The named service element.</returns>
        public RpcProcessorElement this[int index]
        {
            get
            {
                return (RpcProcessorElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemove(index);
                }
                BaseAdd(index, value);
            }
        }

        /// <summary>
        /// Gets or sets the named service element for the given name.
        /// </summary>
        /// <param name="name">The name of the named service element to get or set.</param>
        /// <returns>The named service element.</returns>
        public new RpcProcessorElement this[string name]
        {
            get
            {
                return (RpcProcessorElement)BaseGet(name);
            }
        }

        /// <summary>
        /// Gets the number of named service elements in this instance.
        /// </summary>
        public new int Count
        {
            get { return base.Count; }
        }

        public int IndexOf(RpcProcessorElement service)
        {
            return BaseIndexOf(service);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Add(RpcProcessorElement item)
        {
            BaseAdd(item);
        }

        public void Clear()
        {
            BaseClear();
        }

        public bool Contains(RpcProcessorElement item)
        {
            return BaseIndexOf(item) >= 0;
        }

        public void CopyTo(RpcProcessorElement[] array, int arrayIndex)
        {
            base.CopyTo(array, arrayIndex);
        }

        public new bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(RpcProcessorElement item)
        {
            if (BaseIndexOf(item) >= 0)
            {
                BaseRemove(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the key by which named service elements are mapped in the base class.
        /// </summary>
        /// <param name="service">The named service element to get the key from.</param>
        /// <returns>The key.</returns>
        private string getKey(RpcProcessorElement service)
        {
            return service.TypeName;
        }
    }

    public class RpcProcessorElement : ConfigurationElement, IComparable<RpcProcessorElement>
    {
        [ConfigurationProperty("typeName", IsRequired = true)]
        public string TypeName
        {
            get
            {
                return (string)this["typeName"];
            }
            set
            {
                this["typeName"] = value;
            }
        }

        [ConfigurationProperty("priority", IsRequired = true)]
        public int Priority
        {
            get
            {
                return (int)this["priority"];
            }
            set
            {
                this["priority"] = value;
            }
        }

        public int CompareTo(RpcProcessorElement other)
        {
            return this.Priority.CompareTo(other.Priority);
        }
    }
}
