using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace Altus.UI.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        private static readonly IDictionary<Color, SolidColorBrush> _cachedBrushes = new Dictionary<Color, SolidColorBrush>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color color = (Color)value;
            SolidColorBrush brush;
            if (_cachedBrushes.TryGetValue(color, out brush))
            {
                return brush;
            }
            brush = new SolidColorBrush(color);
            _cachedBrushes.Add(color, brush);

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((SolidColorBrush)value).Color;
        }
    }
}
