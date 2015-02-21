using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using Altus.Core.Presentation.Wpf.Runtime;

namespace Altus.Core.Presentation.Wpf.Converters
{
    public class ViewTemplateSelectorConverter : IValueConverter
    {
        Dictionary<string, DataTemplateSelector> _selectors = new Dictionary<string, DataTemplateSelector>();
        public virtual object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string key = value.ToString() + "_" + parameter.ToString();
            if (!_selectors.ContainsKey(key))
            {
                _selectors.Add(key, CreateViewTemplateSelector(key, value, parameter, culture));
            }
            return _selectors[key];
        }

        protected virtual DataTemplateSelector CreateViewTemplateSelector(string key, object value, object parameter, System.Globalization.CultureInfo culture)
        {
            return new ViewTemplateSelector();
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
