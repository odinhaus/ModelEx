using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Altus.Core.Component;

namespace Altus.Core.Licensing
{
    public class XmlFileLicenseManager : InitializableComponent, ILicenseManager
    {
        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        public ILicense[] GetLicenses()
        {
            List<ILicense> licenses = new List<ILicense>();
            foreach (string licFile in Directory.GetFiles(App.Instance["Core"].CodeBase, "*.lic"))
            {
                XmlFileLicense lic = new XmlFileLicense(licFile);
                if (lic.IsValid)
                    licenses.Add(lic);
            }
            return licenses.ToArray();
        }
    }
}
