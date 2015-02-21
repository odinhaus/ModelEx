using Altus.Core.Licensing;
using Altus.Core.Presentation.Wpf.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;

namespace Altus.Core.Presentation.ViewModels
{
    public class WPFView : View
    {
         public WPFView():base() { }
         public WPFView(string windowName, string instanceName, string viewType, object backingInstance, DeclaredApp app)
            : base(windowName, instanceName, viewType, backingInstance, app)
        {}

         public DataTemplate SelectTemplate(DependencyObject container)
         {
             if (Dispatcher == null)
                 Dispatcher = OnGetDispatcher();
             return OnSelectTemplate(container);
         }

         protected virtual DataTemplate OnSelectTemplate(DependencyObject container)
         {
             DataTemplateSelector vts = OnCreateDataTemplateSelector(container);
             ((FrameworkElement)container).DataContext = this;

             SetDependencyObject(container);

             return vts.SelectTemplate(this, container);
         }

         protected virtual DataTemplateSelector OnCreateDataTemplateSelector(DependencyObject container)
         {
             return new ViewTemplateSelector();
         }

         Dictionary<string, DependencyObject> _dependencyObjects = new Dictionary<string, DependencyObject>();
         public DependencyObject GetDependencyObject(string size)
         {
             if (_dependencyObjects.ContainsKey(size))
             {
                 return _dependencyObjects[size];
             }
             else return null;
         }

         public void SetDependencyObject(DependencyObject container)
         {
             if (_dependencyObjects.ContainsKey(this.CurrentSize))
             {
                 _dependencyObjects[this.CurrentSize] = container;
             }
             else
             {
                 _dependencyObjects.Add(this.CurrentSize, container);
             }
         }

         protected Dispatcher OnGetDispatcher()
         {
             Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
             return dispatcher;
         }

         public Dispatcher Dispatcher { get; protected set; }

    }
}
