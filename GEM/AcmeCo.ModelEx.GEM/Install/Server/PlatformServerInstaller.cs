using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.GEM.Install
{
    public partial class Installer
    {
        protected virtual bool OnPlatformServerInstall(Altus.GEM.Schema.ModelEx config)
        {
            return true;
        }
    }
}
