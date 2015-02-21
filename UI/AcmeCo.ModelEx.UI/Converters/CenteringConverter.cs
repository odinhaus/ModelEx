using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Altus.UI.Converters
{
    public class CenteringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null || values.Length != 2) return Binding.DoNothing;
            double range = double.Parse(values[0] == null ? "0" : values[0].ToString());
            double element = double.Parse(values[1] == null ? "0" : values[1].ToString());
            return (range / 2d) - (element / 2d);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
