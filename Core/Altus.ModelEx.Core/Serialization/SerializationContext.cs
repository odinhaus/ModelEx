using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;


namespace Altus.Core.Serialization
{
    public class SerializationContext : InitializableComponent
    {
        [ThreadStatic()]
        public static Encoding TextEncoding;

        static SerializationContext _ctx = null; 
        public static SerializationContext Instance
        {
            get
            {
                if (_ctx == null)
                {
                    _ctx = new SerializationContext();
                    if (App.Instance != null)
                        App.Instance.Shell.Add(_ctx);
                }
                return _ctx;
            }
        }

        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        Dictionary<string, ISerializer> _serializers = new Dictionary<string, ISerializer>();
        public ISerializer<T> GetSerializer<T>(string format)
        {
            return (ISerializer<T>)GetSerializer(typeof(T), format);
        }

        public ISerializer GetSerializer(Type type, string format)
        {
            lock (_serializers)
            {
                string key = format + "::" + type.AssemblyQualifiedName;
                if (_serializers.ContainsKey(key))
                {
                    return _serializers[key];
                }
                else
                {
                    ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>()
                        .Where(s => s.SupportsFormat(format) && s.SupportsType(type))
                        .OrderBy(s => s.Priority)
                        .FirstOrDefault();
                    if (serializer != null)
                    {
                        _serializers.Add(key, serializer);
                    }
                    return serializer;
                }
            }
        }

        public static string ToString(byte[] serialized)
        {
            return TextEncoding.GetString(serialized);
        }

        public static string ToString(object instance, string format)
        {
            SerializationContext sc = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();
            if (sc == null)
                sc = new SerializationContext();
            ISerializer s = sc.GetSerializer(instance.GetType(), format);
            if (s == null)
            {
                return instance.ToString();
            }
            else
            {
                return ToString(s.Serialize(instance));
            }
        }

        public static string ToString<T>(T instance, string format)
        {
            SerializationContext sc = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();
            if (sc == null)
                sc = new SerializationContext();
            ISerializer<T> s = sc.GetSerializer<T>(format);
            if (s == null)
            {
                return instance.ToString();
            }
            else
            {
                return ToString(s.Serialize(instance));
            }
        }
    }
}
