using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Altus.UI.Converters
{
    public class AbsoluteValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return 0d;
            else
            {
                if (value is byte)
                {
                    return (byte)value;
                }
                else if (value is char)
                {
                    return (char)value;
                }
                else if (value is short)
                {
                    return (short)Math.Abs((short)value);
                }
                else if (value is ushort)
                {
                    return (ushort)value;
                }
                else if (value is int)
                {
                    return (int)Math.Abs((int)value);
                }
                else if (value is uint)
                {
                    return (uint)value;
                }
                else if (value is long)
                {
                    return (long)Math.Abs((long)value);
                }
                else if (value is ulong)
                {
                    return (ulong)value;
                }
                else if (value is float)
                {
                    return (float)Math.Abs((float)value);
                }
                else if (value is double)
                {
                    return (double)Math.Abs((double)value);
                }
                else if (value is decimal)
                {
                    return (decimal)Math.Abs((decimal)value);
                }
                else
                    throw (new InvalidCastException());
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
