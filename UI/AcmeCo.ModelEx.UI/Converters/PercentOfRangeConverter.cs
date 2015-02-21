using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Altus.UI.Converters
{
    public class PercentOfRangeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null) return Binding.DoNothing;
            if (values.Length == 0) return Binding.DoNothing;
            if (values.Length == 1) return values[0];

            double min, max, current, minstop, maxstop;
            if (values.Length == 2)
            {
                minstop = min = 0;
                try
                {
                    maxstop = max = double.Parse(values[1].ToString());
                }
                catch { maxstop = max = 100; }
                try
                {
                    current = double.Parse(values[0].ToString());
                }
                catch { current = 0; }
            }
            else
            {
                try
                {
                    min = double.Parse(values[0].ToString());
                }
                catch { min = 0; }
                try
                {
                    max = double.Parse(values[2].ToString());
                }
                catch { max = 100; }
                try
                {
                    current = double.Parse(values[1].ToString());
                }
                catch { current = 0; }
                if (values.Length == 5)
                {
                    try
                    {
                        minstop = double.Parse(values[3].ToString());
                    }
                    catch { minstop = min; }
                    try
                    {
                        maxstop = double.Parse(values[4].ToString());
                    }
                    catch { maxstop = max; }
                }
                else
                {
                    minstop = min;
                    maxstop = max;
                }
            }

            if (current > maxstop) current = maxstop;
            else if (current < minstop) current = minstop;

            double val = (current - min) / (max - min);
            return val;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return new object[] { 0, value, 1 };
        }
    }
}
