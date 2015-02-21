using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Altus.Core.Messaging.Http
{
    public class HttpForm : IEnumerable<HttpFormEntry>
    {
        Dictionary<string, object> _values = new Dictionary<string, object>();
        public object this[string name]
        {
            get 
            {
                if (_values.ContainsKey(name))
                    return _values[name];
                else
                    return null;
            }
            set 
            {
                if (_values.ContainsKey(name))
                    _values[name] = value;
                else
                    _values.Add(name, value);
            }
        }

        public bool Contains(string name)
        {
            return _values.ContainsKey(name);
        }

        public object[] Values
        {
            get
            {
                return _values.Values.ToArray();
            }
        }

        public IEnumerator<HttpFormEntry> GetEnumerator()
        {
            Dictionary<string, object>.Enumerator en = this._values.GetEnumerator();
            while (en.MoveNext())
            {
                yield return new HttpFormEntry(en.Current.Key, en.Current.Value);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public struct HttpFormEntry
    {
        public HttpFormEntry(string key, object value) : this()
        {
            this.Key = key;
            this.Value = value;
        }

        public string Key { get; private set; }
        public object Value { get; private set; }
    }
}
