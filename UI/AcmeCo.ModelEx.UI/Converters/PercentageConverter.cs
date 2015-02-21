using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Altus.UI.Converters
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DoubleConverter dc = new DoubleConverter();
            double v = (double)dc.Convert(value, typeof(double), 0, culture);
            double p = (double)dc.Convert(parameter, typeof(double), 0, culture);
            return v * p;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
