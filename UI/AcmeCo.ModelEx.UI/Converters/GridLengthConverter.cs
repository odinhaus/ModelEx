using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace Altus.UI.Converters
{
    public class GridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DoubleConverter dc = new DoubleConverter();
            double v = (double)dc.Convert(value, typeof(double), parameter, culture);
            if (v == double.PositiveInfinity)
                v = double.MaxValue;
            else if (v == double.NegativeInfinity)
                v = double.MinValue;
            return new GridLength(v);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
