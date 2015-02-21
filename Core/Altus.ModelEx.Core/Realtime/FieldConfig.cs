using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Irony.Parsing;
using Altus.Core;
using System.Linq.Expressions;
using System.IO;
using Altus.Core.Realtime;
using Altus.Core.Data;

namespace Altus.Core.Realtime
{
    public enum DataTypeEnum
    {
        Unsupported = -1,
        Unknown = 0,
        Boolean,
        Byte,
        Char,
        UInt16,
        Int16,
        UInt32,
        Int32,
        UInt64,
        Int64,
        Single,
        Double,
        Decimal,
        DateTime,
        String
    }

    [StorageMapping("Field")]
    public class FieldConfig 
    {
        private ushort _fieldId;
        private ushort _objectId;
        private int _fieldValueTypeId;
        public FieldConfig() : this(typeof(int)) { }

        protected FieldConfig(Type dataType)
        {
            DataType = dataType; 
            ValueType = FieldValueType.Analog;

            if (DataType == typeof(string)
                    || DataType == typeof(bool)
                    || DataType == typeof(char)
                    )
                _isNumeric = false;
            else
                _isNumeric = true;

        }

        public static ulong Composite(ushort fieldId, ushort objectId, uint orgUnitId)
        {
            return (ulong)((long)fieldId + ((long)orgUnitId << 4 * 8) + ((long)objectId << 2 * 8));
        }

        public static byte FieldTypeValFromPoint(Field dp)
        {
            return FieldTypeValFromValue(dp.Value);
        }

        public static byte FieldTypeValFromValue(object value)
        {
            if (value is int)
            {
                return 1;
            }
            else if (value is uint)
            {
                return 2;
            }
            else if (value is long)
            {
                return 3;
            }
            else if (value is ulong)
            {
                return 4;
            }
            else if (value is double)
            {
                return 5;
            }
            else if (value is float)
            {
                return 6;
            }
            else if (value is bool)
            {
                return 7;
            }
            else if (value is byte)
            {
                return 8;
            }
            else if (value is decimal)
            {
                return 9;
            }
            else if (value is DateTime)
            {
                return 10;
            }
            else if (value is string)
            {
                return 11;
            }
            else if (value is ushort)
            {
                return 12;
            }
            else if (value is short)
            {
                return 13;
            }
            else if (value is char)
            {
                return 14;
            }
            else if (value is byte)
            {
                return 15;
            }
            else
            {
                return 0;
            }
        }

        public static Type TypeValFromPointTypeVal(byte val)
        {
            if (val == 1)
            {
                return typeof(int);
            }
            else if (val == 2)
            {
                return typeof(uint);
            }
            else if (val == 3)
            {
                return typeof(long);
            }
            else if (val == 4)
            {
                return typeof(ulong);
            }
            else if (val == 5)
            {
                return typeof(double);
            }
            else if (val == 6)
            {
                return typeof(float);
            }
            else if (val == 7)
            {
                return typeof(bool);
            }
            else if (val == 8)
            {
                return typeof(byte);
            }
            else if (val == 9)
            {
                return typeof(decimal);
            }
            else if (val == 10)
            {
                return typeof(DateTime);
            }
            else if (val == 11)
            {
                return typeof(string);
            }
            else if (val == 12)
            {
                return typeof(ushort);
            }
            else if (val == 13)
            {
                return typeof(short);
            }
            else if (val == 14)
            {
                return typeof(char);
            }
            else if (val == 15)
            {
                return typeof(byte);
            }
            else
            {
                return typeof(object);
            }
        }

        public int Index { get; set; }

        [XmlAttribute()]
        public ulong CompositeId { get; set; }

        public ushort FieldId
        {
            get
            {
                _fieldId = (ushort)(((ulong)CompositeId << 6 * 8) >> 6 * 8);
                return _fieldId;
            }
            set { _fieldId = value; }
        }

        public ushort ObjectId
        {
            get
            {
                return (ushort)((ulong)(CompositeId << 4 * 8) >> 6 * 8);                
            }
            set { _objectId = value; }
        }

        public uint OrgUnitId
        {
            get
            {
                return (ushort)((ulong)CompositeId >> 4 * 8);
            }
        }

        [XmlAttribute()]
        public string Name { get; set; }
        [XmlAttribute()]
        public string DataTypeName
        {
            get { return DataType.FullName; }
            set { DataType = TypeHelper.GetType(value); }
        }

        Type _dt;
        [XmlIgnore()]
        public Type DataType { get { return _dt; } protected set { _dt = value; OnDataTypeChanged(); } }

        protected virtual void OnDataTypeChanged() { _type = DataTypeEnum.Unknown; }

        private DataTypeEnum _type = DataTypeEnum.Unknown;
        public DataTypeEnum DataTypeEnum 
        {
            get
            {
                if (_type == DataTypeEnum.Unknown
                    && DataType != null)
                {
                    if (DataType == typeof(bool))
                        _type = DataTypeEnum.Boolean;
                    else if (DataType == typeof(char))
                        _type = DataTypeEnum.Char;
                    else if (DataType == typeof(byte))
                        _type = DataTypeEnum.Byte;
                    else if (DataType == typeof(ushort))
                        _type = DataTypeEnum.UInt16;
                    else if (DataType == typeof(short))
                        _type = DataTypeEnum.Int16;
                    else if (DataType == typeof(uint))
                        _type = DataTypeEnum.UInt32;
                    else if (DataType == typeof(int))
                        _type = DataTypeEnum.Int32;
                    else if (DataType == typeof(ulong))
                        _type = DataTypeEnum.UInt64;
                    else if (DataType == typeof(long))
                        _type = DataTypeEnum.Int64;
                    else if (DataType == typeof(float))
                        _type = DataTypeEnum.Single;
                    else if (DataType == typeof(double))
                        _type = DataTypeEnum.Double;
                    else if (DataType == typeof(decimal))
                        _type = DataTypeEnum.Decimal;
                    else if (DataType == typeof(DateTime))
                        _type = DataTypeEnum.DateTime;
                    else if (DataType == typeof(string))
                        _type = DataTypeEnum.String;
                    else
                        _type = DataTypeEnum.Unsupported;
                }
                return _type;
            }
        }
        private bool _isNumeric;
        [XmlIgnore()]
        public bool IsNumeric
        {
            get
            {
                return _isNumeric;
            }
        }

        [XmlAttribute()]        
        public FieldValueType ValueType { get; set; }

        [StorageFieldMapping("FieldValueTypeId")]
        [StorageFieldWriteConstraint(WriteConstraintType.IsNotNull)]
        public int FieldValueTypeId { 
            get { 
                _fieldValueTypeId = (int)this.ValueType + 1;
                return _fieldValueTypeId;
            }
            set { _fieldValueTypeId = value; }
        }

        public static implicit operator Field(FieldConfig config)
        {
            Field field = new Field()
            {
                Id = config.FieldId,
                Name = config.Name,
            };
            field.Value = DefaultValue(config);
            return field;
        }

        private static IComparable DefaultValue(FieldConfig config)
        {
            switch (config.DataTypeEnum)
            {
                case DataTypeEnum.Boolean:
                    return false;
                case DataTypeEnum.Byte:
                    return (byte)0;
                case DataTypeEnum.Char:
                    return (char)0;
                case DataTypeEnum.DateTime:
                    return DateTime.MinValue;
                case DataTypeEnum.Decimal:
                    return (decimal)0;
                case DataTypeEnum.Double:
                    return (double)0;
                case DataTypeEnum.Int16:
                    return (short)0;
                case DataTypeEnum.Int32:
                    return (int)0;
                case DataTypeEnum.Int64:
                    return (long)0;
                case DataTypeEnum.Single:
                    return (float)0;
                case DataTypeEnum.String:
                    return string.Empty;
                case DataTypeEnum.UInt16:
                    return (ushort)0;
                case DataTypeEnum.UInt32:
                    return (uint)0;
                case DataTypeEnum.UInt64:
                    return (ulong)0;
                default:
                case DataTypeEnum.Unknown:
                case DataTypeEnum.Unsupported:
                    return string.Empty;
            }
        }

        string _qn;
        public string QualifiedName
        {
            get
            {
                if (_qn == null)
                    _qn = this.Name;
                return _qn;
            }
        }

        public override string ToString()
        {
            return this.QualifiedName;
        }
    }    
}
