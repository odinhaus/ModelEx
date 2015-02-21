using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Altus.UI.Converters
{
    public class VerticalMarginPercentOfRangeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null) return Binding.DoNothing;
            if (values.Length != 7) return Binding.DoNothing;

            double min, max, current, minstop, maxstop, heightRange, blockHeight;
            heightRange = 0;

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

            try
            {
                minstop = double.Parse(values[3].ToString());
            }
            catch { minstop = min; }
            try
            {
                maxstop = double.Parse(values[4].ToString());
            }
            catch { maxstop = max; } try
            {
                minstop = double.Parse(values[3].ToString());
            }
            catch { minstop = min; }
            try
            {
                maxstop = double.Parse(values[4].ToString());
            }
            catch { maxstop = max; }
            try
            {
                heightRange = double.Parse(values[5].ToString());
            }
            catch { heightRange = max - min; }
            try
            {
                blockHeight = double.Parse(values[6].ToString());
            }
            catch { blockHeight = 0d; }

   

            double val = 1d - ((current - min) / (max - min));
            val = val * heightRange;

            if (val > heightRange - (minstop + blockHeight)) val = heightRange - (minstop + blockHeight);
            else if (val < maxstop) val = maxstop;

            return val;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
