using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Altus.Core.Component;

namespace Altus.Core.Licensing
{
    public interface ILicenseManager : IInitialize, IComponent
    {
        ILicense[] GetLicenses();
    }
}
