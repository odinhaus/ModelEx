using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Presentation.ViewModels
{
    public delegate void UpdateTextStatusHandler(string message);
    public delegate void UpdateProgressStatusHandler(int min, int max, int value);
    public delegate void UpdateIndeterminateProgressStatusHandler(bool isOn);

    public interface ISupportsStatus
    {
        void RegisterMainStatus(UpdateTextStatusHandler callback);
        void RegisterSecondaryStatus(UpdateTextStatusHandler callback);
        void RegisterProgressStatus(UpdateProgressStatusHandler callback);
        void RegisterIndeterminateProgressStatus(UpdateIndeterminateProgressStatusHandler callback);
    }
}
