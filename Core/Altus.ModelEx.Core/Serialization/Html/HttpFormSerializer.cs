using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Serialization;
using Altus.Core.Streams;
using System.IO;
using Altus.Core.Messaging.Http;
using System.Web;


[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.HttpFormSerializer))]
namespace Altus.Core.Serialization.Html
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
            return format.Equals(StandardFormats.HTML, StringComparison.InvariantCultureIgnoreCase);
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
                _sContext = App.Instance.Shell.GetComponent<SerializationContext>();

            if (_sContext == null)
                throw (new InvalidOperationException("Serialization manager could not be found"));

            if (SerializationContext.TextEncoding == null)
                SerializationContext.TextEncoding = System.Text.Encoding.Unicode;

            MemoryStream ms = new MemoryStream();
            foreach (HttpFormEntry entry in source)
            {
                ms.Write(entry.Key);
                ms.Write("=");
                ISerializer serializer = _sContext.GetSerializer(entry.Value.GetType(), StandardFormats.HTML);
                byte[] data;
                if (serializer == null)
                {
                    data = SerializationContext.TextEncoding.GetBytes(entry.Value.ToString());
                }
                else
                {
                    data = serializer.Serialize(entry.Value);
                }
                ms.Write(data);
                ms.Write("&");
            }
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
            HttpForm form = new HttpForm();
            string name = string.Empty;
            string value = string.Empty;
            bool lookForValue = false;
            int charCount = 0;

            foreach (var c in formData)
            {
                if (c == '=')
                {
                    lookForValue = true;
                }
                else if (c == '&')
                {
                    lookForValue = false;
                    form[name] = HttpUtility.UrlDecode(value);
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
                    form[name] = HttpUtility.UrlDecode(value);
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
