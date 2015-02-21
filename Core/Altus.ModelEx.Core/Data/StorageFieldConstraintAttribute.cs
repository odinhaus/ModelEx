using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Data
{
    public enum WriteConstraintType
    {
        Equals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        IsNotNull,
        IsNull,
        Custom
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class StorageFieldWriteConstraintAttribute : Attribute
    {
        public StorageFieldWriteConstraintAttribute(WriteConstraintType type) : this(type, null) {}

        public StorageFieldWriteConstraintAttribute(WriteConstraintType type, IComparable value)
        {
            this.ConstraintType = type;
            this.ConstraintValue = value;
        }

        public WriteConstraintType ConstraintType { get; private set; }
        public IComparable ConstraintValue { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class StorageFieldCustomWriteConstraintAttribute : StorageFieldWriteConstraintAttribute
    {
        public StorageFieldCustomWriteConstraintAttribute(string methodName) : base(WriteConstraintType.Custom, methodName) {}
    }
}
