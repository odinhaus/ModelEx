using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using Altus.Core.Presentation.ViewModels;
using Altus.Core.Presentation.Wpf.Runtime;
using Altus.Core.Presentation.WPF.Runtime;

namespace Altus.Core.Presentation.Wpf.Converters
{
    public class ViewStyleSelectorConverter : IValueConverter
    {
        Dictionary<string, StyleSelector> _selectors = new Dictionary<string, StyleSelector>();
        public virtual object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(parameter is View)) return null;

            string key = value.ToString() + "_" + parameter.ToString();
            if (!_selectors.ContainsKey(key))
            {
                _selectors.Add(key, CreateStyleSelector(key, value, (View)parameter, culture));
            }
            return _selectors[key];
        }

        protected virtual StyleSelector CreateStyleSelector(string key, object value, View parameter, System.Globalization.CultureInfo culture)
        {
            return new ViewStyleSelector(parameter);
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
