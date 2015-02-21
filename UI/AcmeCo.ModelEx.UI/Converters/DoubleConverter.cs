using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Altus.UI.Converters
{
    public class DoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return 0d;
            else
            {
                if (value is byte)
                {
                    return (double)(byte)value;
                }
                else if (value is char)
                {
                    return (double)(char)value;
                }
                else if (value is short)
                {
                    return (double)(short)value;
                }
                else if (value is ushort)
                {
                    return (double)(ushort)value;
                }
                else if (value is int)
                {
                    return (double)(int)value;
                }
                else if (value is uint)
                {
                    return (double)(uint)value;
                }
                else if (value is long)
                {
                    return (double)(long)value;
                }
                else if (value is ulong)
                {
                    return (double)(ulong)value;
                }
                else if (value is float)
                {
                    return (double)(float)value;
                }
                else if (value is double)
                {
                    return value;
                }
                else if (value is decimal)
                {
                    return (double)(decimal)value;
                }
                else if (value is string)
                {
                    return double.Parse(value.ToString(), NumberStyles.Float);
                }
                else
                    throw (new InvalidCastException());
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(byte))
            {
                return (byte)(double)Convert(value, typeof(double), null, System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            else if (targetType == typeof(char))
            {
                return (char)Convert(value, typeof(double), null, System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            else if (targetType == typeof(short))
            {
                return (short)Convert(value, typeof(double), null, System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            else if (targetType == typeof(ushort))
            {
                return (ushort)Convert(value, typeof(double), null, System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            else if (targetType == typeof(int))
            {
                return (int)Convert(value, typeof(double), null, System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            else if (targetType == typeof(uint))
            {
                return (uint)Convert(value, typeof(double), null, System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            else if (targetType == typeof(long))
            {
                return (long)Convert(value, typeof(double), null, System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            else if (targetType == typeof(ulong))
            {
                return (ulong)Convert(value, typeof(double), null, System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            else if (targetType == typeof(float))
            {
                return (float)Convert(value, typeof(double), null, System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            else if (targetType == typeof(double))
            {
                return Convert(value, typeof(double), null, System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            else if (targetType == typeof(decimal))
            {
                return (decimal)Convert(value, typeof(double), null, System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            else if (targetType == typeof(string))
            {
                return value.ToString();
            }
            else
                throw (new InvalidCastException());
        }
    }
}
