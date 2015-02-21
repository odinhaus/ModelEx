using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Presentation.ViewModels
{
    public delegate void NavigatedHandler(string viewName);

    public interface ISupportsNavigation
    {
        void RegisterNavigated(NavigatedHandler callback);
        void NavigateTo(string viewName);
        void NavigateBack();
        void NavigateForward();
    }
}
