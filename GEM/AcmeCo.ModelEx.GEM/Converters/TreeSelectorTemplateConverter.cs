using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WoodMac.ModelEx.Core.Presentation.Wpf.Converters;
using WoodMac.ModelEx.GEM.Selectors;

namespace WoodMac.ModelEx.GEM.Converters
{
    public class TreeSelectorTemplateConverter : ViewTemplateSelectorConverter
    {
        protected override DataTemplateSelector CreateViewTemplateSelector(string key, object value, object parameter, System.Globalization.CultureInfo culture)
        {
            return new TreeItemTemplateSelector();
        }
    }
}
