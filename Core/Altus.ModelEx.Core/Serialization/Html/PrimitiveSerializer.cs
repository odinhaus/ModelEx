using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Altus.Core.Streams;
using Altus.Core.Component;
using System.Text.RegularExpressions;
using Altus.Core;
using System.ComponentModel;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<string>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<byte>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<char>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<ushort>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<short>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<uint>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<int>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<ulong>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<long>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<float>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<double>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.PrimitiveSerializer<DateTime>))]
namespace Altus.Core.Serialization.Html
{
    public class PrimitiveSerializer<T> : InitializableComponent, ISerializer<T>
    {
        public bool IsScalar { get { return true; } }
        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        public int Priority { get; private set; }
        public byte[] Serialize(object source)
        {
            return Serialize((T)source);
        }

        object ISerializer.Deserialize(byte[] source, Type targetType)
        {
            return Deserialize(source);
        }

        public byte[] Serialize(T source)
        {
            if (typeof(T).Equals(typeof(string)))
            {
                return SerializationContext.TextEncoding.GetBytes(source.ToString());
            }
            else
            {
                return SerializationContext.TextEncoding.GetBytes("<" + source.GetType().FullName + ">" + source.ToString() + "</" + source.GetType().FullName + ">");
            }
        }

        public void Serialize(T source, Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        public T Deserialize(byte[] source)
        {
            Regex r = new Regex(@"<(?<type>[\w\.]+)>(?<value>.*)</\1>");
            string html = SerializationContext.TextEncoding.GetString(source);
            string typeName = "System.String";
            string value = html;

            Match m = r.Match(html);

            if (m.Success)
            {
                typeName = m.Groups["type"].Value;
                value = m.Groups["value"].Value;
            }

            Type t = TypeHelper.GetType(typeName);

            if (t == null)
            {
                if (typeof(T).Equals(typeof(string)))
                    return (T)(object)html;
                else
                    throw (new InvalidCastException("The provided type is not a supported primitive type."));
            }

            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter == null
                || !converter.CanConvertFrom(value.GetType()))
                throw (new InvalidCastException("The provided type is not a supported primitive type."));

            return (T)converter.ConvertFrom(value);
        }

        public T Deserialize(Stream inputSource)
        {
            return Deserialize(inputSource.GetBytes(8));
        }

        public bool SupportsFormat(string format)
        {
            return format.Equals(StandardFormats.HTML, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool SupportsType(Type type)
        {
            return type == typeof(T);
        }
    }
}
