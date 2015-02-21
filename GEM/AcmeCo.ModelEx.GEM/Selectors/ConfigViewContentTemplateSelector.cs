using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Altus.Core.Presentation.ViewModels;
using Altus.GEM.ViewModels;

namespace Altus.GEM.Selectors
{
    public class ConfigViewContentTemplateSelector : EmbeddedDataTemplateSelector
    {
        public ConfigViewContentTemplateSelector()
        {
            this.WindowName = "Config";
        }
        public ConfigViewContentTemplateSelector(string windowName)
        {
            this.WindowName = windowName;
        }
        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            if (item is WPFView)
            {
                string size = ((WPFView)item).CurrentSize;
                try
                {
                    ((WPFView)item).CurrentSize = "Config";
                    return base.SelectTemplate(item, container);
                }
                finally
                {
                    ((WPFView)item).CurrentSize = size;
                }
            }
            else if (item is Entity)
            {
                string size = ((Entity)item).CurrentSize;
                try
                {
                    ((Entity)item).CurrentSize = "Config";
                    return base.SelectTemplate(item, container);
                }
                finally
                {
                    ((Entity)item).CurrentSize = size;
                }
            }
            else
                return ((FrameworkElement)container).FindResource("ConfigDefault") as DataTemplate;
        }

        public string WindowName { get; private set; }

        protected override string OnGetItemWindowName(object item)
        {
            return WindowName;
        }
    }
}
