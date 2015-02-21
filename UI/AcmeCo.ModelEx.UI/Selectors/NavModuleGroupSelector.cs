using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Altus.UI.Selectors
{
    public class NavModuleGroupSelector : DataTemplateSelector
    {
        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            // this can be made more extensible to read from configuration, db, etc
            // for now, it just returns the same default template for all items
            return base.SelectTemplate(item, container);
        }
    }
}
