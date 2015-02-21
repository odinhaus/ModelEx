using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Altus.Core.Presentation.ViewModels;
using Altus.Core.Presentation.Wpf.Runtime;
using Altus.Core.Streams;

namespace Altus.Core.Presentation.WPF.Runtime
{
    public class ViewStyleSelector : StyleSelector
    {

        public ViewStyleSelector(View view)
        {
            this.View = view;
        }
        public override Style SelectStyle(object item, DependencyObject container)
        {
            return base.SelectStyle(item, container);
        }


        public Style CreateTemplate(string xaml, WPFView item, System.Windows.DependencyObject container)
        {
            foreach (IXAMLTemplateProcessor<View> processor in Altus.Core.Component.App.Instance.Shell.GetComponents<IXAMLTemplateProcessor<View>>()
                .Where(p => p.Enabled))
            {
                xaml = processor.PreProcess(xaml, (View)item);
            }

            Style dt = XamlReader.Load(new TextStream(xaml)) as Style;
            FrameworkElement element = container as FrameworkElement;
            element.Loaded += new RoutedEventHandler(VisualElement_Loaded);

            return dt;
        }

        private void VisualElement_Loaded(object sender, RoutedEventArgs e)
        {
            View view = ((FrameworkElement)sender).DataContext as View;
            foreach (IXAMLTemplateProcessor<View> processor in Altus.Core.Component.App.Instance.Shell.GetComponents<IXAMLTemplateProcessor<View>>()
                .Where(p => p.Enabled))
            {
                int children = VisualTreeHelper.GetChildrenCount((DependencyObject)sender);
                for (int i = 0; i < children; i++)
                {
                    processor.PostProcess(VisualTreeHelper.GetChild((DependencyObject)sender, 0), view);
                }
            }
        }

        public View View { get; private set; }
    }
}
