using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Altus.UI.Converters
{
    public class UpdateRateConverter : IValueConverter
    {
        private int _threshold = 100;
        private DateTime _lastUpdate = DateTime.MinValue;

        public int UpdateThreshold
        {
            get { return _threshold; }
            set
            {
                if (value < 0) value = 0;
                _threshold = value;
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan elapsed = DateTime.Now.Subtract(_lastUpdate);
            bool canUpdate = elapsed.TotalMilliseconds >= _threshold;

            if (canUpdate)
            {
                _lastUpdate = DateTime.Now;
                return value;
            }
            else
                return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
