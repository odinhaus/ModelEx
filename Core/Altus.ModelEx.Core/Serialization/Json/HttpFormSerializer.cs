using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Serialization;
using Altus.Core.Streams;
using System.IO;
using Altus.Core.Messaging.Http;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.HttpFormSerializer))]
namespace Altus.Core.Serialization.Json
{
    public class HttpFormSerializer : InitializableComponent, ISerializer<HttpForm>
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
            return format.Equals(StandardFormats.JSON, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool SupportsType(Type type)
        {
            return type.Equals(typeof(HttpForm));
        }

        public byte[] Serialize(object source)
        {
            return Serialize((HttpForm)source);
        }

        object ISerializer.Deserialize(byte[] source, Type targetType)
        {
            return Deserialize(source);
        }

        public byte[] Serialize(HttpForm source)
        {
            if (_sContext == null)
                _sContext = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();

            if (_sContext == null)
                throw (new InvalidOperationException("Serialization manager could not be found"));

            if (SerializationContext.TextEncoding == null)
                SerializationContext.TextEncoding = System.Text.Encoding.Unicode;

            MemoryStream ms = new MemoryStream();
            bool isFirst = true;
            foreach (HttpFormEntry entry in source)
            {
                byte[] data;
                if (isFirst)
                {
                    ms.Write("{ ");
                }
                else
                {
                    ms.Write(",");
                }
                ms.Write(entry.Key.Trim());
                ms.Write(": ");
                ISerializer serializer = _sContext.GetSerializer(entry.Value.GetType(), StandardFormats.JSON);
                
                if (serializer == null)
                {
                    data = SerializationContext.TextEncoding.GetBytes(entry.Value.ToString().Trim());
                }
                else
                {
                    data = serializer.Serialize(entry.Value);
                }
                ms.Write(data);
                
            }

            if (!isFirst)
                ms.Write(" }");

            return ms.ToArray();
        }

        public void Serialize(HttpForm source, System.IO.Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        public HttpForm Deserialize(byte[] source)
        {
            if (_sContext == null)
                _sContext = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();

            if (_sContext == null)
                throw (new InvalidOperationException("Serialization manager could not be found"));

            if (SerializationContext.TextEncoding == null)
                SerializationContext.TextEncoding = System.Text.Encoding.Unicode;

            string formData = SerializationContext.TextEncoding.GetString(source);
            formData = formData.Substring(formData.IndexOf("{") + 1).Trim();
            formData = formData.Substring(0, formData.LastIndexOf("}")).Trim();
            HttpForm form = new HttpForm();
            string name = string.Empty;
            string value = string.Empty;
            bool lookForValue = false;
            int charCount = 0;

            foreach (var c in formData)
            {
                if (c == ':')
                {
                    lookForValue = true;
                }
                else if (c == ',')
                {
                    lookForValue = false;
                    form[name.Trim()] = value.Trim();
                    name = string.Empty;
                    value = string.Empty;
                }
                else if (!lookForValue)
                {
                    name += c;
                }
                else
                {
                    value += c;
                }

                if (++charCount == formData.Length)
                {
                    form[name.Trim()] = value.Trim();
                    break;
                }
            }

            return form;
        }

        public HttpForm Deserialize(System.IO.Stream inputSource)
        {
            return Deserialize(StreamHelper.GetBytes(inputSource));
        }
    }
}
