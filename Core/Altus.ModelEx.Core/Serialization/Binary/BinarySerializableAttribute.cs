using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Serialization.Binary
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class BinarySerializableAttribute : Attribute
    {
        public BinarySerializableAttribute(int sortOrder) { SortOrder = sortOrder; }
        public BinarySerializableAttribute(int sortOrder, Type serializeAs) : this(sortOrder) { SerializationType = serializeAs; }
        public Type SerializationType { get; set; }
        public int SortOrder { get; private set; }
    }
}
