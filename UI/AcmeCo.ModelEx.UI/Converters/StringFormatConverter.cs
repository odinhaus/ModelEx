using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Altus.UI.Converters
{ 
    public class StringFormatConverter : DependencyObject, IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return Binding.DoNothing;
            if (value.Length == 1) return value[0];

            double dval;
            if (value[0] == null) value[0] = 0d;
            if (value[1] == null) value[1] = 0;
            if (double.TryParse(value[0].ToString(), out dval))
            {
                int digits;
                if (int.TryParse(value[1].ToString(), out digits))
                {
                    return dval.ToString("G" + digits.ToString());
                }
                else
                {
                    return dval.ToString(value[1].ToString());
                }
            }
            else return value[0];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
