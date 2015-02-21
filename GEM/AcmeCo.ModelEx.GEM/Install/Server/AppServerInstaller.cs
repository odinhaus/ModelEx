using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Altus.Core.Licensing;

namespace Altus.GEM.Install
{
    public partial class Installer
    {
        protected virtual bool OnAppServerInstall(ILicense license, DeclaredApp app, Altus.GEM.Schema.ModelEx config)
        {
            return true;
        }
    }
}
