using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.GEM.Install
{
    public partial class Installer
    {
        protected virtual bool OnPlatformClientInstall(Altus.GEM.Schema.ModelEx config)
        {
            return true;
        }
    }
}
