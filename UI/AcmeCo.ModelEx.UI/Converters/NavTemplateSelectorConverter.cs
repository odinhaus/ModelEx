using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Altus.Core.Presentation.Wpf.Converters;
using Altus.UI.Selectors;


namespace Altus.UI.Converters
{
    public class NavModuleGroupSelectorConverter : ViewTemplateSelectorConverter
    {
        protected override DataTemplateSelector CreateViewTemplateSelector(string key, object value, object parameter, System.Globalization.CultureInfo culture)
        {
            return new NavModuleSelector(parameter);
        }
    }
}
