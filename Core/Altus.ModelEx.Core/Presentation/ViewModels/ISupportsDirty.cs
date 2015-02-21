using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Presentation.ViewModels
{
    public class HandleDirtyArgs : EventArgs
    {
        public bool Cancel { get; set; }
    }

    public interface ISupportsDirty
    {
        bool IsDirty { get; }
        void HandleDirty(HandleDirtyArgs e);
    }
}
