using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Serialization
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited= true)]
    public class SerializableAttribute : Attribute
    {
        public SerializableAttribute(string formatName, Type serializerType)
        {
            Format = formatName;
            SerializerType = serializerType;
        }

        public string Format { get; private set; }
        public Type SerializerType { get; private set; }
        internal Type TargetType { get; set; }
    }
}
