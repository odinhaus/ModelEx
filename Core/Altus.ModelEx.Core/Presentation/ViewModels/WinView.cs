using Altus.Core.Licensing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core.Presentation.ViewModels
{
    public class WinView : View
    {
        public WinView():base() { }
        public WinView(string windowName, string instanceName, string viewType, object backingInstance, DeclaredApp app)
            : base(windowName, instanceName, viewType, backingInstance, app)
        {}
    }
}
