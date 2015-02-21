using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Altus.Core.Processing
{
    [CollectionDataContract()]
    [System.Serializable]
    public class ServiceParameterCollection : IEnumerable<ServiceParameter>
    {
        private Dictionary<string, ServiceParameter> _innerList = new Dictionary<string, ServiceParameter>();
        public ServiceParameterCollection() { }

        public ServiceParameterCollection(ServiceParameter[] args)
        {
            this.AddRange(args);
        }

        public ServiceParameter this[int index]
        {
            get { return _innerList.Values.ToArray()[index]; }
        }

        public ServiceParameter this[string name]
        {
            get { return _innerList[name]; }
        }

        public object Result
        {
            get
            {
                ServiceParameter p = _innerList.Values.Where(sp => sp.Direction == ParameterDirection.Return).FirstOrDefault();
                if (p == null) return null;
                else return p.Value;
            }
        }

        public int Count
        {
            get { return _innerList.Count; }
        }

        public void Add(string name, string type, ParameterDirection direction)
        {
            Add(new ServiceParameter(name, type, direction));
        }

        public void Add(string name, string type, ParameterDirection direction, object value)
        {
            Add(new ServiceParameter(name, type, direction) { Value = value });
        }

        public void Add(ServiceParameter p)
        {
            _innerList.Add(p.Name, p);
        }

        public void AddRange(IEnumerable<ServiceParameter> serviceParameters)
        {
            foreach (ServiceParameter p in serviceParameters)
                Add(p);
        }

        public void Remove(ServiceParameter p)
        {
            _innerList.Remove(p.Name);
        }

        public void Remove(string name)
        {
            _innerList.Remove(name);
        }

        public IEnumerator<ServiceParameter> GetEnumerator()
        {
            foreach (ServiceParameter p in _innerList.Values)
                yield return p;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ServiceParameter sp in this)
            {
                if (sb.Length > 0)
                    sb.Append("&");
                sb.Append(sp.Name);
                sb.Append("=");
                sb.Append(sp.Value.ToString());
            }
            return sb.ToString();
        }
    }
}
