using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using System.Web.Script.Serialization;
using Altus.Core.Streams;

namespace Altus.Core.Serialization.Json
{
    public abstract class JsonSerializer<T> : JsonSerializer, ISerializer<T>
    {
        public bool IsScalar { get { return false; } }
        public byte[] Serialize(T source)
        {
            JavaScriptSerializer jss = this.GetSerializer<T>();
            return SerializationContext.TextEncoding.GetBytes(jss.Serialize(source));
        }
        public int Priority { get; private set; }
        public void Serialize(T source, System.IO.Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        protected override byte[] OnSerialize(object source)
        {
            if (source.GetType().Equals(typeof(T))
                || source.GetType().IsSubclassOf(typeof(T)))
            {
                return Serialize((T)source);
            }
            else
                throw (new SerializationException("Type not supported"));
        }

        public T Deserialize(byte[] source)
        {
            JavaScriptSerializer jss = this.GetSerializer<T>();
            return jss.Deserialize<T>(SerializationContext.TextEncoding.GetString(source));
        }

        public T Deserialize(System.IO.Stream inputSource)
        {
            return Deserialize(StreamHelper.GetBytes(inputSource));
        }

        protected override object OnDeserialize(byte[] source, Type targetType)
        {
            if (targetType.Equals(typeof(T))
                || targetType.IsSubclassOf(typeof(T)))
            {
                return Deserialize(source);
            }
            else
                throw (new SerializationException("Type not supported"));
        }

        protected override IEnumerable<Type> OnGetSupportedTypes()
        {
            return new Type[] { typeof(T) };
        }
    }

    public abstract class JsonSerializer : JavaScriptConverter, IInitialize, ISerializer, IJsonSerializer
    {
        protected abstract object OnDeserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer);
        protected abstract IDictionary<string, object> OnSerialize(object obj, JavaScriptSerializer serializer);
        protected abstract IEnumerable<Type> OnGetSupportedTypes();
        protected abstract byte[] OnSerialize(object source);
        protected abstract object OnDeserialize(byte[] source, Type targetType);
        public int Priority { get; private set; }
        public bool IsScalar { get { return false; } }
        protected JavaScriptSerializer GetSerializer<T>()
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            IJsonSerializer[] jsss = App.Instance.Shell.GetComponents<IJsonSerializer>().Where(js => js.SupportsFormat(StandardFormats.JSON)).ToArray();
            if (jsss != null && jsss.Length > 0)
            {
                jss.RegisterConverters(jsss.OfType<JavaScriptConverter>());
            }
            jss.MaxJsonLength = int.MaxValue;
            return jss;
        }

        protected virtual void OnDispose()
        {
        }

        protected virtual bool OnSupportsFormats(string format)
        {
            return format.Equals(StandardFormats.JSON, StringComparison.InvariantCultureIgnoreCase);
        }

        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            return OnDeserialize(dictionary, type, serializer);
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            return OnSerialize(obj, serializer);
        }

       
        IEnumerable<Type> _types = null;
        public override IEnumerable<Type> SupportedTypes
        {
            get { return _types; }
        }

        public void Initialize(string name, params string[] args)
        {
            this.Name = name;
            this._types = OnGetSupportedTypes();
        }

        public bool IsInitialized
        {
            get;
            private set;
        }

        public bool IsEnabled
        {
            get;
            set;
        }

        public string Name { get; private set; }

        public bool SupportsFormat(string format)
        {
            return OnSupportsFormats(format);
        }

        public bool SupportsType(Type type)
        {
            return _types != null && _types.Contains(type);
        }

        public byte[] Serialize(object source)
        {
            return OnSerialize(source);
        }

        public object Deserialize(byte[] source, Type targetType)
        {
            return OnDeserialize(source, targetType);
        }

        public event EventHandler Disposed;

        public System.ComponentModel.ISite Site
        {
            get;
            set;
        }

        public void Dispose()
        {
            this.OnDispose();
            if (Disposed != null)
                Disposed(this, new EventArgs());
        }

        
    }
}
