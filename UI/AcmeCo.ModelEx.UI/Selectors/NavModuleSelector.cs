using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core;
using Altus.Core.Presentation.ViewModels;
using Altus.Core.Presentation.Wpf.Runtime;
using Altus.UI.ViewModels;

namespace Altus.UI.Selectors
{
    public class NavModuleSelector : ViewTemplateSelector
    {
        public NavModuleSelector() { }
        public NavModuleSelector(object parameter)
        {
            Parameter = parameter;
        }

        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            if (item is NavModule)
            {
                object view = ((NavModule)item).BackingInstance;
                if (view is View)
                {
                    ((View)view).CurrentSize = Parameter.ToString();
                }
                return base.SelectTemplate(view, container);
            }
            else if (item is WPFView)
            {
                Context.CurrentContext.CurrentApp = ((WPFView)item).App;
                ((WPFView)item).CurrentSize = Parameter.ToString();
                return ((WPFView)item).SelectTemplate(container);
            }
            else
                return base.SelectTemplate(item, container);
        }

        public object Parameter { get; private set; }
    }
}
