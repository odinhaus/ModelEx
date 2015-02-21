using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using Altus.Core.Presentation.ViewModels;

namespace Altus.Core.Presentation.Wpf.Runtime
{
    public interface IDataTemplateLoader : IComponent
    {
        string LoadViewTemplate(View view, System.Windows.DependencyObject container);
    }
}
