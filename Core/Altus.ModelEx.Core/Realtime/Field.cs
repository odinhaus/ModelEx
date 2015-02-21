using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using Altus.Core.Serialization.Binary;
using Altus.Core.Data;
using Altus.Core.Security;
using Altus.Core.Component;
using Altus.Core.Serialization;
using Altus.Core;
using Altus.Core.Diagnostics;

namespace Altus.Core.Realtime
{
    public enum FieldValueType
    {
        Unknown = -1,
        Analog = 0,
        Discrete = 1,
        Complex = 2
    }


    [DataContract()]    
    public class Field : IComparable, IComparable<Field>
    {

        public ushort Id;
        public string Object;
        public string Name;
        public string SourceNode;
        public DateTime TimeStamp;
        public IComparable Value;
        
        private string _qn;
        [ScriptIgnore()]
        public string QualifiedName 
        { 
            get 
            { 
                if (_qn == null)
                    _qn = string.Concat(
                        (string.IsNullOrEmpty(this.SourceNode) ? NodeIdentity.NodeAddress : this.SourceNode), 
                        "::", 
                        this.Object, 
                        ".", 
                        this.Name);
                return _qn;
            } 
        }


        public int ValueSize
        {
            get
            {
                int size = 0;

                if (Value != null)
                {
                    if (Value is byte
                        || Value is bool)
                    {
                        size = 1;
                    }
                    else if (Value is char
                        || Value is short
                        || Value is ushort)
                    {
                        size = 2;
                    }
                    else if (Value is uint
                        || Value is int
                        || Value is float)
                    {
                        size = 4;
                    }
                    else if (Value is long
                        || Value is ulong
                        || Value is double
                        || Value is decimal
                        || Value is DateTime)
                    {
                        size = 8;
                    }
                    else if (Value is string)
                    {
                        size = 4 + Encoding.Unicode.GetByteCount(Value as string);
                    }
                }

                return size;
            }
        }

        private double _vn;

        public double ValueNumeric
        {
            get
            {
                if (Value == null) return 0;
                
                Type DataType = Value.GetType();
                if (DataType == typeof(int))
                {
                    _vn = (double)(int)Value;
                }
                else if (DataType == typeof(uint))
                {
                    _vn = (double)(uint)Value;
                }
                else if (DataType == typeof(long))
                {
                    _vn = (double)(long)Value;
                }
                else if (DataType == typeof(ulong))
                {
                    _vn = (double)(ulong)Value;
                }
                else if (DataType == typeof(double))
                {
                    _vn = (double)Value;
                }
                else if (DataType == typeof(float))
                {
                    _vn = (double)(float)Value;
                }
                else if (DataType == typeof(bool))
                {
                    _vn = (bool)Value ? 1 : 0;
                }
                else if (DataType == typeof(byte))
                {
                    _vn = (double)(byte)Value;
                }
                else if (DataType == typeof(decimal))
                {
                    _vn = (double)(decimal)Value;
                }
                else if (DataType == typeof(DateTime))
                {
                    _vn = (double)((DateTime)Value).ToBinary();
                }
                else if (DataType == typeof(short))
                {
                    _vn = (double)(short)Value;
                }
                else if (DataType == typeof(ushort))
                {
                    _vn = (double)(ushort)Value;
                }
                else if (DataType == typeof(string))
                {
                    _vn = (double)((string)Value).GetHashCode();
                }
                else
                {
                    _vn = 0;
                }
                
                return _vn;
            }
        }

        public object ValueObject
        {
            get
            {
                if (Value == null) return null;

                Type DataType = Value.GetType();
                if (DataType == typeof(int))
                {
                    return (object)(int)Value;
                }
                else if (DataType == typeof(uint))
                {
                    return (object)(uint)Value;
                }
                else if (DataType == typeof(long))
                {
                    return (object)(long)Value;
                }
                else if (DataType == typeof(ulong))
                {
                    return (object)(ulong)Value;
                }
                else if (DataType == typeof(double))
                {
                    return (object)Value;
                }
                else if (DataType == typeof(float))
                {
                    return (object)(float)Value;
                }
                else if (DataType == typeof(bool))
                {
                    return (object)(bool)Value;
                }
                else if (DataType == typeof(byte))
                {
                    return (object)(byte)Value;
                }
                else if (DataType == typeof(decimal))
                {
                    return (object)(decimal)Value;
                }
                else if (DataType == typeof(DateTime))
                {
                    return (object)(DateTime)Value;
                }
                else if (DataType == typeof(short))
                {
                    return (object)(short)Value;
                }
                else if (DataType == typeof(ushort))
                {
                    return (object)(ushort)Value;
                }
                else if (DataType == typeof(string))
                {
                    return (object)(string)Value;
                }
                else if (DataType == typeof(byte))
                {
                    return (object)(byte)Value;
                }
                else return null;
            }
        }



        public void SetValue(IComparable value)
        {
            this.Value = value;
        }

        public void SetTimestamp(DateTime timestamp)
        {
            this.TimeStamp = timestamp;
        }

        public void SetValueNumeric(double value, Type dataType)
        {
            Type DataType = dataType;
            if (DataType == typeof(int))
            {
                _vn = (int)value;
                Value = (int)value;
            }
            else if (DataType == typeof(uint))
            {
                _vn = (uint)value;
                Value = (uint)value;
            }
            else if (DataType == typeof(long))
            {
                _vn = (long)value;
                Value = (long)value;
            }
            else if (DataType == typeof(ulong))
            {
                _vn = (ulong)value;
                Value = (ulong)value;
            }
            else if (DataType == typeof(double))
            {
                _vn = value;
                Value = value;
            }
            else if (DataType == typeof(float))
            {
                _vn = (float)value;
                Value = (float)value;
            }
            else if (DataType == typeof(byte))
            {
                _vn = (byte)value;
                Value = (byte)value;
            }
            else if (DataType == typeof(decimal))
            {
                _vn = (double)(decimal)value;
                Value = (decimal)value;
            }
            else if (DataType == typeof(char))
            {
                _vn = (char)value;
                Value = (char)value;
            }
            else if (DataType == typeof(short))
            {
                _vn = (short)value;
                Value = (short)value;
            }
            else if (DataType == typeof(ushort))
            {
                _vn = (ushort)value;
                Value = (ushort)value;
            }
            else
                throw new InvalidCastException("This data type does not support conversion from a numeric format.");
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} : {2}",
                Name,
                Value == null ? "NaN" : Value.ToString(),
                TimeStamp.ToString("yyyy-MM-dd H:mm:ss.ffffff zzz"));
        }

        public string ValueJson()
        {
            if (Value is string)
            {
                return "\"" + Value + "\"";
            }
            if (Value is DateTime)
            {
                DateTime dt = (DateTime)Value;
                //return dt.ToString("yyyy-MM-ddTH:mm:ss.fffZzzz");
                return string.Format("\"Date({0},{1},{2},{3},{4},{5},{6})\"",
                    dt.Year,
                    dt.Month,
                    dt.Day,
                    dt.Hour,
                    dt.Minute,
                    dt.Second,
                    dt.Millisecond);
            }
            if (Value is bool)
            {
                return (bool)Value ? "1" : "0";
            }
            return Value.ToString();
        }

        public static readonly Field Empty = new Field();

        public static void ValueWriter(object value, BinaryWriter sw)
        {
            byte code = FieldConfig.FieldTypeValFromValue(value);
            sw.Write(code);
            if (value is string)
                sw.Write((string)value);
            else if (value is bool)
                sw.Write((bool)value);
            else if (value is char)
                sw.Write((char)value);
            else if (value is byte)
                sw.Write((byte)value);
            else if (value is DateTime)
            {
                long dtBin = ((DateTime)value).ToBinary();
                sw.Write(dtBin);
            }
            else if (value is short)
                sw.Write((short)value);
            else if (value is ushort)
                sw.Write((ushort)value);
            else if (value is int)
                sw.Write((int)value);
            else if (value is uint)
                sw.Write((uint)value);
            else if (value is long)
                sw.Write((long)value);
            else if (value is ulong)
                sw.Write((ulong)value);
            else if (value is float)
                sw.Write((float)value);
            else if (value is double)
                sw.Write((double)value);
            else if (value is decimal)
                sw.Write((decimal)value);
            else
            {
                SerializationContext sCtx = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();
                ISerializer s = sCtx.GetSerializer(value.GetType(), StandardFormats.BINARY);
                sw.Write(value.GetType().FullName);
                byte[] data = s.Serialize(value);
                sw.Write(data.Length);
                sw.Write(data);
            }
        }

        public static object ValueReader(BinaryReader sr)
        {
            byte code = sr.ReadByte();
            Type value = FieldConfig.TypeValFromPointTypeVal(code);
            if (value == typeof(string))
                return sr.ReadString();
            else if (value == typeof(bool))
                return sr.ReadBoolean();
            else if (value == typeof(char))
                return sr.ReadChar();
            else if (value == typeof(byte))
                return sr.ReadByte();
            else if (value == typeof(DateTime))
            {
                long dtBin = sr.ReadInt64();
                return DateTime.FromBinary(dtBin);
            }
            else if (value == typeof(short))
                return sr.ReadInt16();
            else if (value == typeof(ushort))
                return sr.ReadUInt16();
            else if (value == typeof(int))
                return sr.ReadInt32();
            else if (value == typeof(uint))
                return sr.ReadUInt32();
            else if (value == typeof(long))
                return sr.ReadInt64();
            else if (value == typeof(ulong))
                return sr.ReadUInt64();
            else if (value == typeof(float))
                return sr.ReadSingle();
            else if (value == typeof(double))
                return sr.ReadDouble();
            else if (value == typeof(decimal))
                return sr.ReadDecimal();
            else
            {
                string tryType = sr.ReadString();
                Type type = TypeHelper.GetType(tryType);
                if (type == null)
                {
                    Logger.LogWarn("Received serialize Field value of type " + tryType + " but could not resolve the type locally.  Field value will be set to NaN by the runtime.");
                    return double.NaN;
                }
                else
                {
                    int length = sr.ReadInt32();
                    byte[] data = sr.ReadBytes(length);
                    SerializationContext sCtx = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();
                    ISerializer s = sCtx.GetSerializer(value.GetType(), StandardFormats.BINARY);
                    return s.Deserialize(data, type);
                }
            }
        }

        public static Field Create(FieldName fn)
        {
            return new Field() { Name = fn.Name, Object = fn.Object };
        }

        public int CompareTo(object obj)
        {
            if (obj is Field)
            {
                return CompareTo((Field)obj);
            }
            else
            {
                return 1;
            }
        }

        public int CompareTo(Field comp)
        {
            int v = this.Id.CompareTo(comp.Id);
            if (v == 0)
            {
                v = this.TimeStamp.Ticks.CompareTo(comp.TimeStamp.Ticks);
            }

            return v;
        }
    }
}
