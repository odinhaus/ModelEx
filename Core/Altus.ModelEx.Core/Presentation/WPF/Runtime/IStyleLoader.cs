using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Altus.Core.Presentation.ViewModels;

namespace Altus.Core.Presentation.WPF.Runtime
{
    public interface IStyleLoader
    {
        string LoadStyle(View view, System.Windows.DependencyObject container);
    }
}
