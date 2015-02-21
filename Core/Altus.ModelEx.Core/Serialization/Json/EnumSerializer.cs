using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.EnumSerializer))]
namespace Altus.Core.Serialization.Json
{
    public class EnumSerializer : InitializableComponent, ISerializer
    {
        public bool IsScalar { get { return true; } }
        public bool SupportsFormat(string format)
        {
            return format.Equals(StandardFormats.JSON, StringComparison.InvariantCultureIgnoreCase);
        }
        public int Priority { get; private set; }
        public bool SupportsType(Type type)
        {
            return type.IsEnum;
        }

        public byte[] Serialize(object source)
        {
            return SerializationContext.TextEncoding.GetBytes(source.ToString());
        }

        public object Deserialize(byte[] source, Type targetType)
        {
            return Enum.Parse(targetType, SerializationContext.TextEncoding.GetString(source));
        }

        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }
    }
}
