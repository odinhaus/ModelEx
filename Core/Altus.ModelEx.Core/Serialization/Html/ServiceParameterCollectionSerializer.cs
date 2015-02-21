using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Processing;
using Altus.Core.Streams;
using System.Web.Script.Serialization;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.ServiceParameterCollectionSerializer))]

namespace Altus.Core.Serialization.Html
{
    public class ServiceParameterCollectionSerializer : InitializableComponent, ISerializer<ServiceParameterCollection>
    {
        SerializationContext _sContext = null;
        public bool IsScalar { get { return false; } }
        protected override bool OnInitialize(params string[] args)
        {
            _sContext = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();
            return true;
        }
        public int Priority { get; private set; }
        public bool SupportsFormat(string format)
        {
            return format.Equals(StandardFormats.HTML, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool SupportsType(Type type)
        {
            return type.Equals(typeof(ServiceParameterCollection));
        }


        public byte[] Serialize(ServiceParameterCollection source)
        {
            if (_sContext == null)
                _sContext = App.Instance.Shell.GetComponent<SerializationContext>();

            if (_sContext == null)
                throw (new InvalidOperationException("Serialization manager could not be found"));

            if (SerializationContext.TextEncoding == null)
                SerializationContext.TextEncoding = System.Text.Encoding.Unicode;

            StringBuilder sb = new StringBuilder();
            sb.Append("<ServiceParameterCollection>");
            foreach (ServiceParameter p in source)
            {
                ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>().Where(
                    c => c.SupportsFormat(StandardFormats.HTML) && c.SupportsType(p.Value.GetType())).FirstOrDefault();
                sb.Append("<ServiceParameter>");
                sb.Append("<ServiceParameterAttribute name=\"" + p.Name + "\" type=\"" + p.Type + "\" direction=\"" + p.Direction.ToString() + "\">");
                if (serializer == null)
                {
                    sb.Append(p.Value.ToString());
                }
                else
                {
                    sb.Append(SerializationContext.TextEncoding.GetString(serializer.Serialize(p.Value)));
                }
                sb.Append("</ServiceParameterAttribute>");
                sb.Append("</ServiceParameter>");
            }
            sb.Append("</ServiceParameterCollection>");

            return SerializationContext.TextEncoding.GetBytes(sb.ToString());
        }

        public void Serialize(ServiceParameterCollection source, System.IO.Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        public byte[] Serialize(object source)
        {
            return this.Serialize((ServiceParameterCollection)source);
        }

        public ServiceParameterCollection Deserialize(byte[] source)
        {
            if (_sContext == null)
                _sContext = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();

            if (_sContext == null)
                throw (new InvalidOperationException("Serialization manager could not be found"));

            if (SerializationContext.TextEncoding == null)
                SerializationContext.TextEncoding = System.Text.Encoding.Unicode;


            return null;
        }

        public ServiceParameterCollection Deserialize(System.IO.Stream inputSource)
        {
            return this.Deserialize(StreamHelper.GetBytes(inputSource));
        }

        object ISerializer.Deserialize(byte[] source, Type targetType)
        {
            return this.Deserialize(source);
        }
    }
}
