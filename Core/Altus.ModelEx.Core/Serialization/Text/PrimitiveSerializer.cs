using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Altus.Core.Streams;
using Altus.Core.Component;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<bool>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<byte>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<char>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<ushort>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<short>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<uint>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<int>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<ulong>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<long>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<float>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<double>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<DateTime>))]
[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Text.PrimitiveSerializer<string>))]
namespace Altus.Core.Serialization.Text
{
    public class PrimitiveSerializer : InitializableComponent, ISerializer
    {
        public int Priority { get; private set; }
        public static bool IsPrimitive(Type t)
        {
            return t == typeof(bool)
                || t == typeof(byte)
                || t == typeof(char)
                || t == typeof(ushort)
                || t == typeof(short)
                || t == typeof(uint)
                || t == typeof(int)
                || t == typeof(ulong)
                || t == typeof(long)
                || t == typeof(float)
                || t == typeof(double)
                || t == typeof(DateTime)
                || t == typeof(string);
        }

        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        public bool IsScalar { get { return true; } }

        public byte[] Serialize(object source)
        {
            Type t = source.GetType();

            if (t == typeof(DateTime))
                return SerializationContext.TextEncoding.GetBytes(((DateTime)(object)source).ToBinary().ToString());

            return SerializationContext.TextEncoding.GetBytes(source.ToString());

            throw (new InvalidCastException("The provided type is not a supported primitive type."));
        }

        public object Deserialize(byte[] source, Type targetType)
        {
            if (targetType == typeof(DateTime))
                return DateTime.Parse(SerializationContext.TextEncoding.GetString(source));
            if (targetType == typeof(string))
                return SerializationContext.TextEncoding.GetString(source);
            if (targetType == typeof(bool))
                return bool.Parse(SerializationContext.TextEncoding.GetString(source));
            if (targetType == typeof(byte))
                return byte.Parse(SerializationContext.TextEncoding.GetString(source));
            if (targetType == typeof(char))
                return char.Parse(SerializationContext.TextEncoding.GetString(source));
            if (targetType == typeof(ushort))
                return ushort.Parse(SerializationContext.TextEncoding.GetString(source));
            if (targetType == typeof(short))
                return short.Parse(SerializationContext.TextEncoding.GetString(source));
            if (targetType == typeof(uint))
                return uint.Parse(SerializationContext.TextEncoding.GetString(source));
            if (targetType == typeof(int))
                return int.Parse(SerializationContext.TextEncoding.GetString(source));
            if (targetType == typeof(ulong))
                return ulong.Parse(SerializationContext.TextEncoding.GetString(source));
            if (targetType == typeof(long))
                return long.Parse(SerializationContext.TextEncoding.GetString(source));
            if (targetType == typeof(float))
                return float.Parse(SerializationContext.TextEncoding.GetString(source));
            if (targetType == typeof(double))
                return double.Parse(SerializationContext.TextEncoding.GetString(source));

            throw (new InvalidCastException("The provided type is not a supported primitive type."));
        }

        public bool SupportsFormat(string format)
        {
            return format.Equals(StandardFormats.TEXT, StringComparison.InvariantCultureIgnoreCase)
                || format.Equals(StandardFormats.XML, StringComparison.InvariantCultureIgnoreCase);
        }

        public virtual bool SupportsType(Type type)
        {
            return PrimitiveSerializer.IsPrimitive(type);
        }

        public static byte GetByteCount(Type targetType)
        {
            if (targetType == typeof(bool)
                || targetType == typeof(byte)) return (byte)1;

            if (targetType == typeof(char)
                || targetType == typeof(ushort)
                || targetType == typeof(short)) return (byte)2;

            if (targetType == typeof(uint)
                || targetType == typeof(int)
                || targetType == typeof(float)) return (byte)4;

            if (targetType == typeof(ulong)
                || targetType == typeof(long)
                || targetType == typeof(double)
                || targetType == typeof(DateTime)) return 8;

            return 0;
        }
    }


    public class PrimitiveSerializer<T> : PrimitiveSerializer, ISerializer<T>
    {
        public static bool IsPrimitive<T>()
        {
            return typeof(T) == typeof(bool)
                || typeof(T) == typeof(byte)
                || typeof(T) == typeof(char)
                || typeof(T) == typeof(ushort)
                || typeof(T) == typeof(short)
                || typeof(T) == typeof(uint)
                || typeof(T) == typeof(int)
                || typeof(T) == typeof(ulong)
                || typeof(T) == typeof(long)
                || typeof(T) == typeof(float)
                || typeof(T) == typeof(double)
                || typeof(T) == typeof(DateTime);
        }

        public byte[] Serialize(T source)
        {
            return base.Serialize(source);
        }

        public void Serialize(T source, Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        public T Deserialize(byte[] source)
        {
            return (T)base.Deserialize(source, typeof(T));
        }

        public T Deserialize(Stream inputSource)
        {
            return Deserialize(inputSource.GetBytes(GetByteCount(typeof(T))));
        }

        public override bool SupportsType(Type type)
        {
            return type == typeof(T);
        }

        
    }
}
