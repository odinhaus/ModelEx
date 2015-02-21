using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Altus.Core.Presentation.ViewModels;
using System.Windows;

namespace Altus.Core.Presentation.Wpf.Runtime
{
    public interface IXAMLTemplateProcessor<T> : IComponent
    {
        string PreProcess(string xaml, T item);
        void PostProcess(DependencyObject root , T item);
        bool Enabled { get; }
    }
}
