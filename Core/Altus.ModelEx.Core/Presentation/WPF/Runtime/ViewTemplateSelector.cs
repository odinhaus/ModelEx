using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Altus.Core.Presentation.ViewModels;
using System.Windows;
using System.Windows.Markup;
using Altus.Core.Streams;
using System.Windows.Media;

namespace Altus.Core.Presentation.Wpf.Runtime
{
    public class ViewTemplateSelector : DataTemplateSelector
    {
        public ViewTemplateSelector()
        {
        }

        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            if (item is WPFView)
            {
                IDataTemplateLoader dtl = Altus.Core.Component.App.Instance.Shell.GetComponent<IDataTemplateLoader>();
                string xaml = dtl.LoadViewTemplate((WPFView)item, container);

                ((WPFView)item).SetDependencyObject(container);

                return CreateTemplate(xaml, (WPFView)item, container);
            }
            return null;
        }

        public DataTemplate CreateTemplate(string xaml, WPFView item, System.Windows.DependencyObject container)
        {
            foreach (IXAMLTemplateProcessor<View> processor in Altus.Core.Component.App.Instance.Shell.GetComponents<IXAMLTemplateProcessor<View>>()
                .Where(p => p.Enabled))
            {
                xaml = processor.PreProcess(xaml, (View)item);
            }

            DataTemplate dt = XamlReader.Load(new TextStream(xaml)) as DataTemplate;
            FrameworkElement element = container as FrameworkElement;
            element.Loaded += new RoutedEventHandler(VisualElement_Loaded);

            return dt;
        }

        protected void VisualElement_Loaded(object sender, RoutedEventArgs e)
        {
            View view = ((FrameworkElement)sender).DataContext as View;
            foreach (IXAMLTemplateProcessor<View> processor in Altus.Core.Component.App.Instance.Shell.GetComponents<IXAMLTemplateProcessor<View>>()
                .Where(p => p.Enabled))
            {
                int children = VisualTreeHelper.GetChildrenCount((DependencyObject)sender);
                for(int i = 0; i < children; i++)
                {
                    processor.PostProcess(VisualTreeHelper.GetChild((DependencyObject)sender, 0), view);
                }
            }
        }

        public string GetItemWindowName(object item)
        {
            return OnGetItemWindowName(item);
        }

        protected virtual string OnGetItemWindowName(object item)
        {
            if (item is WPFView) return ((WPFView)item).WindowName;

            return "Main";
        }
    }
}
