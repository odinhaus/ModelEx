using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoodMac.ModelEx.Core;
using WoodMac.ModelEx.Core.Presentation.ViewModels;
using WoodMac.ModelEx.Core.Presentation.Wpf.Runtime;
using WoodMac.ModelEx.GEM.ViewModels;

namespace WoodMac.ModelEx.GEM.Selectors
{
    public class TreeItemTemplateSelector : ViewTemplateSelector
    {
        public TreeItemTemplateSelector() { }

        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            if (item is ExplorerFolder)
            {
                object view = ((ExplorerFolder)item).BackingInstance;
                if (view is View)
                {
                    ((View)view).CurrentSize = "tree";
                }
                return base.SelectTemplate(view, container);
            }
            else if (item is ExplorerFile)
            {
                object view = ((ExplorerFile)item).BackingInstance;
                if (view is View)
                {
                    ((View)view).CurrentSize = "tree";
                }
                return base.SelectTemplate(view, container);
            }
            else if (item is WPFView)
            {
                Context.CurrentContext.CurrentApp = ((WPFView)item).App;
                ((WPFView)item).CurrentSize = "tree";
                return ((WPFView)item).SelectTemplate(container);
            }
            else
                return base.SelectTemplate(item, container);
        }
    }
}
