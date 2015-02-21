using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core;
using Altus.Core.Streams;
using System.IO;
using Altus.Core.Component;
using System.Web.Script.Serialization;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<string>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<byte>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<char>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<ushort>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<short>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<uint>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<int>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<ulong>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<long>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<float>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<double>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.PrimitiveSerializer<bool>))]
namespace Altus.Core.Serialization.Json
{
    public class PrimitiveSerializer<T> : InitializableComponent, ISerializer<T>
    {
        public bool IsScalar { get { return true; } }
        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        public int Priority { get; private set; }
        public byte[] Serialize(T source)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            return SerializationContext.TextEncoding.GetBytes(jss.Serialize(source));
        }

        public byte[] Serialize(object source)
        {
            return Serialize((T)source);
        }

        object ISerializer.Deserialize(byte[] source, Type targetType)
        {
            return Deserialize(source);
        }

        public void Serialize(T source, Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        public T Deserialize(byte[] source)
        {
            string textValue = SerializationContext.TextEncoding.GetString(source);
            if (textValue.StartsWith("\""))
            {
                textValue = textValue.Substring(1);
            }
            if (textValue.EndsWith("\""))
            {
                textValue = textValue.Substring(0, textValue.Length - 1);
            }
            if (typeof(T).Equals(typeof(string)))
                return (T)(object)textValue;
            else if (typeof(T).Equals(typeof(bool)))
            {
                return (T)(object)bool.Parse(textValue);
            }
            else if (typeof(T).Equals(typeof(char)))
            {
                return (T)(object)char.Parse(textValue);
            }
            else if (typeof(T).Equals(typeof(ushort)))
            {
                return (T)(object)ushort.Parse(textValue);
            }
            else if (typeof(T).Equals(typeof(short)))
            {
                return (T)(object)short.Parse(textValue);
            }
            else if (typeof(T).Equals(typeof(uint)))
            {
                return (T)(object)uint.Parse(textValue);
            }
            else if (typeof(T).Equals(typeof(int)))
            {
                return (T)(object)int.Parse(textValue);
            }
            else if (typeof(T).Equals(typeof(ulong)))
            {
                return (T)(object)ulong.Parse(textValue);
            }
            else if (typeof(T).Equals(typeof(long)))
            {
                return (T)(object)long.Parse(textValue);
            }
            else if (typeof(T).Equals(typeof(float)))
            {
                return (T)(object)float.Parse(textValue);
            }
            else if (typeof(T).Equals(typeof(double)))
            {
                return (T)(object)double.Parse(textValue);
            }
            else if (typeof(T).Equals(typeof(decimal)))
            {
                return (T)(object)decimal.Parse(textValue);
            }
            else
            {
                throw (new InvalidOperationException("Deserialization is not supported for non-primitive type " + typeof(T).Name));
            }
        }

        public T Deserialize(Stream inputStream)
        {
            return Deserialize(StreamHelper.GetBytes(inputStream));
        }

        public bool SupportsFormat(string format)
        {
            return format.Equals("json", StringComparison.InvariantCultureIgnoreCase);
        }

        public bool SupportsType(Type type)
        {
            return type == typeof(T);
        }
    }
}
