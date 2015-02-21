using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Altus.Core.Processing.Msp
{
    public class MspProcessorsSection : ConfigurationSection
    {
        [ConfigurationProperty("processors", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(MspProcessorElement), AddItemName = "add")]
        public MspProcessorCollection Processors
        {
            get
            {
                return (MspProcessorCollection)this["processors"];
            }
            set
            {
                this["processors"] = value;
            }
        }
    }

    public class MspProcessorCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MspProcessorElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            MspProcessorElement service = (MspProcessorElement)element;

            return getKey(service);
        }

        public void Sort()
        {
            List<MspProcessorElement> sorted = new List<MspProcessorElement>();
            foreach (MspProcessorElement e in this)
            {
                sorted.Add(e);
            }
            sorted.Sort();
            this.Clear();
            foreach (MspProcessorElement e in sorted)
            {
                this.Add(e);
            }
        }

        /// <summary>
        /// Gets or sets the named service element for the given index.
        /// </summary>
        /// <param name="index">The index of the named service element to get or set.</param>
        /// <returns>The named service element.</returns>
        public MspProcessorElement this[int index]
        {
            get
            {
                return (MspProcessorElement)BaseGet(index);
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
        public new MspProcessorElement this[string name]
        {
            get
            {
                return (MspProcessorElement)BaseGet(name);
            }
        }

        /// <summary>
        /// Gets the number of named service elements in this instance.
        /// </summary>
        public new int Count
        {
            get { return base.Count; }
        }

        public int IndexOf(MspProcessorElement service)
        {
            return BaseIndexOf(service);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Add(MspProcessorElement item)
        {
            BaseAdd(item);
        }

        public void Clear()
        {
            BaseClear();
        }

        public bool Contains(MspProcessorElement item)
        {
            return BaseIndexOf(item) >= 0;
        }

        public void CopyTo(MspProcessorElement[] array, int arrayIndex)
        {
            base.CopyTo(array, arrayIndex);
        }

        public new bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(MspProcessorElement item)
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
        private string getKey(MspProcessorElement service)
        {
            return service.TypeName;
        }
    }

    public class MspProcessorElement : ConfigurationElement, IComparable<MspProcessorElement>
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

        public int CompareTo(MspProcessorElement other)
        {
            return this.Priority.CompareTo(other.Priority);
        }
    }
}
