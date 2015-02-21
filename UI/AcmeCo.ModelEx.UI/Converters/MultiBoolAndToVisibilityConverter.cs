using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Altus.UI.Converters
{
    public class MultiBoolAndToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null || values.Length == 0) return Binding.DoNothing;

            bool ret = true;
            foreach (object value in values)
            {
                if (value == null
                    || !(value is bool)
                    || !(bool)value)
                {
                    ret = false;
                    break;
                }
            }

            if (ret) return Visibility.Visible;
            return Visibility.Hidden;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
