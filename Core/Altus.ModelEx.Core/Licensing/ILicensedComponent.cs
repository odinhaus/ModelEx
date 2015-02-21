using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Altus.Core.Licensing
{
    public interface ILicensedComponent : IComponent
    {
        void ApplyLicensing(ILicense[] licenses, params string[] args);
        bool IsLicensed(object component);
    }
}
