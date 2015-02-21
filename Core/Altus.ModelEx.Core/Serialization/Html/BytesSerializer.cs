using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Streams;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.BytesSerializer))]
namespace Altus.Core.Serialization.Html
{
    public class BytesSerializer : InitializableComponent, ISerializer<byte[]>
    {
        public bool IsScalar { get { return true; } }
        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }
        public int Priority { get; private set; }
        public byte[] Serialize(byte[] source)
        {
            return source;
        }

        public void Serialize(byte[] source, System.IO.Stream outputStream)
        {
            StreamHelper.Copy(source, outputStream);
        }

        public byte[] Deserialize(byte[] source)
        {
            return source;
        }

        public byte[] Deserialize(System.IO.Stream inputSource)
        {
            return Deserialize(StreamHelper.GetBytes(inputSource));
        }

        public bool SupportsFormat(string format)
        {
            return format.Equals(StandardFormats.HTML, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool SupportsType(Type type)
        {
            return type.Equals(typeof(byte[]));
        }

        public byte[] Serialize(object source)
        {
            return Serialize((byte[])source);
        }

        object ISerializer.Deserialize(byte[] source, Type targetType)
        {
            return source;
        }
    }
}
