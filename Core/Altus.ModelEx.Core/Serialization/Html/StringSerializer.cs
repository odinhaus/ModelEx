using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Streams;

namespace Altus.Core.Serialization.Html
{
    public class StringSerializer : InitializableComponent, ISerializer<string>
    {
        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        public int Priority { get; private set; }

        public byte[] Serialize(string source)
        {
            return SerializationContext.TextEncoding.GetBytes(source);
        }

        public void Serialize(string source, System.IO.Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        public string Deserialize(byte[] source)
        {
            return SerializationContext.TextEncoding.GetString(source);
        }

        public string Deserialize(System.IO.Stream inputSource)
        {
            return Deserialize(StreamHelper.GetBytes(inputSource));
        }

        public bool SupportsFormat(string format)
        {
            return format.Equals(StandardFormats.HTML, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool SupportsType(Type type)
        {
            return type.Equals(typeof(string));
        }

        public byte[] Serialize(object source)
        {
            return Serialize((string)source);
        }

        object ISerializer.Deserialize(byte[] source, Type targetType)
        {
            return Deserialize(source);
        }

        public bool IsScalar { get { return true; } }
    }
}
