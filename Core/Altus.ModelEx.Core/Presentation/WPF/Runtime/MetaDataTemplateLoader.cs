using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Altus.Core.Component;
using Altus.Core.Streams;
using Altus.Core.Data;
using Altus.Core.Presentation.Wpf.Runtime;

using System.IO;
using System.Windows.Markup;
using Altus.Core.Presentation.ViewModels;


[assembly:Component(ComponentType=typeof(MetaDataTemplateLoader))]
namespace Altus.Core.Presentation.Wpf.Runtime
{
    public class MetaDataTemplateLoader : InitializableComponent, IDataTemplateLoader
    {
        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        public string LoadViewTemplate(View view, System.Windows.DependencyObject container)
        {
            string xaml = DataContext.Default.LoadWPFViewTemplate(view.Name, view.CurrentSize, view.WindowName);
            return xaml;
        }
    }
}
