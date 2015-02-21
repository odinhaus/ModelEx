using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace Altus.Core.Component
{
    [Serializable]
    public class ComponentLoaderSection : ConfigurationSection
    {
        [ConfigurationProperty("coreAssemblies", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(CoreAssemblyCollection), AddItemName = "add")]
        public CoreAssemblyCollection CoreAssemblies
        {
            get
            {
                return (CoreAssemblyCollection)this["coreAssemblies"];
            }
            set
            {
                this["coreAssemblies"] = value;
            }
        }

        [ConfigurationProperty("discoveryPaths", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(DiscoveryPathCollection), AddItemName = "add")]
        public DiscoveryPathCollection DiscoveryPaths
        {
            get
            {
                return (DiscoveryPathCollection)this["discoveryPaths"];
            }
            set
            {
                this["discoveryPaths"] = value;
            }
        }
    }
    [Serializable]
    public class CoreAssemblyCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new CoreAssemblyElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            CoreAssemblyElement service = (CoreAssemblyElement)element;

            return getKey(service);
        }

        public void Sort()
        {
            List<CoreAssemblyElement> sorted = new List<CoreAssemblyElement>();
            foreach (CoreAssemblyElement e in this)
            {
                sorted.Add(e);
            }
            sorted.Sort();
            this.Clear();
            foreach (CoreAssemblyElement e in sorted)
            {
                this.Add(e);
            }
        }

        /// <summary>
        /// Gets or sets the named service element for the given index.
        /// </summary>
        /// <param name="index">The index of the named service element to get or set.</param>
        /// <returns>The named service element.</returns>
        public CoreAssemblyElement this[int index]
        {
            get
            {
                return (CoreAssemblyElement)BaseGet(index);
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
        public new CoreAssemblyElement this[string name]
        {
            get
            {
                return (CoreAssemblyElement)BaseGet(name);
            }
        }

        /// <summary>
        /// Gets the number of named service elements in this instance.
        /// </summary>
        public new int Count
        {
            get { return base.Count; }
        }

        public int IndexOf(CoreAssemblyElement service)
        {
            return BaseIndexOf(service);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Add(CoreAssemblyElement item)
        {
            BaseAdd(item);
        }

        public void Clear()
        {
            BaseClear();
        }

        public bool Contains(CoreAssemblyElement item)
        {
            return BaseIndexOf(item) >= 0;
        }

        public void CopyTo(CoreAssemblyElement[] array, int arrayIndex)
        {
            base.CopyTo(array, arrayIndex);
        }

        public new bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(CoreAssemblyElement item)
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
        private string getKey(CoreAssemblyElement service)
        {
            return Path.Combine(service.Path, service.Assembly);
        }
    }
    [Serializable]
    public class CoreAssemblyElement : ConfigurationElement, IComparable<CoreAssemblyElement>
    {
        [ConfigurationProperty("assembly", IsRequired = true)]
        public string Assembly
        {
            get
            {
                return (string)this["assembly"];
            }
            set
            {
                this["assembly"] = value;
            }
        }

        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get
            {
                return (string)this["path"];
            }
            set
            {
                this["path"] = value;
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
        [XmlIgnore]
        public string CodeBase
        {
            get
            {
                if (System.IO.Path.IsPathRooted(this.Path))
                {
                    return System.IO.Path.Combine(this.Path, this.Assembly);
                }
                else
                {
                    return System.IO.Path.Combine(System.IO.Path.Combine(Context.GlobalContext.CodeBase, this.Path), this.Assembly);
                }
            }
        }
        [XmlIgnore]
        public bool IsValid { get; set; }
        [XmlIgnore]
        public Assembly LoadedAssembly { get; set; }

        public int CompareTo(CoreAssemblyElement other)
        {
            return this.Priority.CompareTo(other.Priority);
        }

        public override string ToString()
        {
            return Assembly;
        }
    }
    [Serializable]
    public class DiscoveryPathCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new DiscoveryPathElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            DiscoveryPathElement service = (DiscoveryPathElement)element;

            return getKey(service);
        }

        public void Sort()
        {
            List<DiscoveryPathElement> sorted = new List<DiscoveryPathElement>();
            foreach (DiscoveryPathElement e in this)
            {
                sorted.Add(e);
            }
            sorted.Sort();
            this.Clear();
            foreach (DiscoveryPathElement e in sorted)
            {
                this.Add(e);
            }
        }

        /// <summary>
        /// Gets or sets the named service element for the given index.
        /// </summary>
        /// <param name="index">The index of the named service element to get or set.</param>
        /// <returns>The named service element.</returns>
        public DiscoveryPathElement this[int index]
        {
            get
            {
                return (DiscoveryPathElement)BaseGet(index);
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
        public new DiscoveryPathElement this[string name]
        {
            get
            {
                return (DiscoveryPathElement)BaseGet(name);
            }
        }

        /// <summary>
        /// Gets the number of named service elements in this instance.
        /// </summary>
        public new int Count
        {
            get { return base.Count; }
        }

        public int IndexOf(DiscoveryPathElement service)
        {
            return BaseIndexOf(service);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Add(DiscoveryPathElement item)
        {
            BaseAdd(item);
        }

        public void Clear()
        {
            BaseClear();
        }

        public bool Contains(DiscoveryPathElement item)
        {
            return BaseIndexOf(item) >= 0;
        }

        public void CopyTo(DiscoveryPathElement[] array, int arrayIndex)
        {
            base.CopyTo(array, arrayIndex);
        }

        public new bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(DiscoveryPathElement item)
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
        private string getKey(DiscoveryPathElement service)
        {
            return service.Path;
        }
    }
    [Serializable]
    public class DiscoveryPathElement : ConfigurationElement, IComparable<DiscoveryPathElement>
    {
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get
            {
                return (string)this["path"];
            }
            set
            {
                this["path"] = value;
            }
        }

        [ConfigurationProperty("recurse", IsRequired = true)]
        public bool Recurse
        {
            get
            {
                return (bool)this["recurse"];
            }
            set
            {
                this["recurse"] = value;
            }
        }

        public string CodeBase
        {
            get
            {
                if (System.IO.Path.IsPathRooted(this.Path))
                {
                    return this.Path;
                }
                else
                {
                    return System.IO.Path.Combine(Context.GlobalContext.CodeBase, this.Path);
                }
            }
        }

        public bool IsValid { get; set; }

        public int CompareTo(DiscoveryPathElement other)
        {
            return this.Path.CompareTo(other.Path);
        }

        public override string ToString()
        {
            return Path;
        }
    }
    [Serializable]
    [XmlRoot(ElementName="Manifest")]
    public class DiscoveryManifest 
    {
        public DiscoveryManifest() { Targets = new DiscoveryManifestTargetCollection(); }
        [XmlArray("Targets")]
        [XmlArrayItem(ElementName = "Target", Type = typeof(DiscoveryTarget))]
        public DiscoveryManifestTargetCollection Targets { get; set; }
        [XmlAttribute]
        public DateTime LastUpdated { get; set; }
        [XmlIgnore]
        public string LocalPath { get; set; }

        public void Save()
        {
            if (string.IsNullOrEmpty(LocalPath)) throw new InvalidOperationException("Manifest LocalPath must be set prior to calling Save.");

            XmlSerializer serializer = new XmlSerializer(typeof(DiscoveryManifest));
            
            using(StreamWriter sw = new StreamWriter(LocalPath))
            {
                serializer.Serialize(sw, this);
            }
        }
    }
    [Serializable]
    public class DiscoveryManifestTargetCollection : IList<DiscoveryTarget>
    {
        List<DiscoveryTarget> _inner = new List<DiscoveryTarget>();

        public int IndexOf(DiscoveryTarget item)
        {
            return _inner.IndexOf(item);
        }

        public void Insert(int index, DiscoveryTarget item)
        {
            _inner.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _inner.RemoveAt(index);
        }
        
        public DiscoveryTarget this[int index]
        {
            get
            {
                return _inner[index];
            }
            set
            {
                _inner[index] = value;
            }
        }

        public void Add(DiscoveryTarget item)
        {
            _inner.Add(item);
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public bool Contains(DiscoveryTarget item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(DiscoveryTarget[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _inner.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(DiscoveryTarget item)
        {
            return _inner.Remove(item);
        }

        public IEnumerator<DiscoveryTarget> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    [Serializable]
    [XmlRoot("Target")]
    public class DiscoveryTarget
    {
        public DiscoveryTarget() { Files = new DiscoveryFileCollection(); }
        [XmlAttribute]
        public string Product { get; set; }
        [XmlArray("Files")]
        [XmlArrayItem(ElementName="File", Type=typeof(DiscoveryFileElement))]
        public DiscoveryFileCollection Files { get; set; }

        public bool IsLocal 
        { 
            get
            {
                return Product != null 
                    && Product.Equals(Context.GetEnvironmentVariable<string>("Instance", ""), StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public override string ToString()
        {
            return Product;
        }
    }
    [Serializable]
    public class DiscoveryFileCollection: IList<DiscoveryFileElement>
    {
        List<DiscoveryFileElement> _inner = new List<DiscoveryFileElement>();

       
        public int IndexOf(DiscoveryFileElement item)
        {
            return _inner.IndexOf(item);
        }

        public void Insert(int index, DiscoveryFileElement item)
        {
            _inner.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _inner.RemoveAt(index);
        }

        public DiscoveryFileElement this[int index]
        {
            get
            {
                return _inner[index];
            }
            set
            {
                _inner[index] = value;
            }
        }

        public void Add(DiscoveryFileElement item)
        {
            _inner.Add(item);
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public bool Contains(DiscoveryFileElement item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(DiscoveryFileElement[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _inner.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(DiscoveryFileElement item)
        {
            return _inner.Remove(item);
        }

        public IEnumerator<DiscoveryFileElement> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    [Serializable]
    [XmlRoot("File")]
    public class DiscoveryFileElement
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public bool Reflect { get; set; }
        [XmlAttribute]
        public bool IsPrimary { get; set; }
        [XmlAttribute]
        public bool IsAppConfig { get; set; }
        [XmlAttribute]
        public bool IsDatabase { get; set; }
        [XmlAttribute]
        public string Source { get; set; }
        [XmlAttribute]
        public string Destination { get; set; }
        [XmlAttribute]
        public string Checksum { get; set; }
        [XmlIgnore]
        public string CodeBase { get; set; }
        [XmlIgnore]
        public bool IsValid { get; set; }
        [XmlIgnore]
        public Assembly LoadedAssembly { get; set; }
        [XmlIgnore]
        public bool Exists { get { return File.Exists(CodeBase); } }

        public override string ToString()
        {
            return Name;
        }
    }
}
