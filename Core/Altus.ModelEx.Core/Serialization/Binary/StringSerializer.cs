using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Serialization.Binary;

[assembly: Component(ComponentType=typeof(StringSerializer))]

namespace Altus.Core.Serialization.Binary
{
    public class StringSerializer : SerializerBase<string>
    {
        protected override bool OnIsScalar()
        {
            return true;
        }

        protected override byte[] OnSerialize(object source)
        {
            byte[] textBytes = SerializationContext.TextEncoding.GetBytes(source.ToString());
            byte[] bytes = new byte[4 + textBytes.Length];
            BitConverter.GetBytes(textBytes.Length).CopyTo(bytes, 0);
            textBytes.CopyTo(bytes, 4);
            return bytes;
        }

        protected override object OnDeserialize(byte[] source, Type targetType)
        {
            int length = BitConverter.ToInt32(source, 0);
            return SerializationContext.TextEncoding.GetString(source, 4, length);
        }

        protected override bool OnSupportsFormats(string format)
        {
            return format.Equals(StandardFormats.BINARY);
        }
    }
}
