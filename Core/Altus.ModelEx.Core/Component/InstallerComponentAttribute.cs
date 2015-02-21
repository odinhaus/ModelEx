using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core.Component
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class InstallerComponentAttribute : Attribute
    {
        /// <summary>
        /// Decorate IInstaller types and provide a list of target appNames that the 
        /// installer should run for.  Passing no app names will run the installer 
        /// for all apps.
        /// </summary>
        /// <param name="appNames"></param>
        public InstallerComponentAttribute(params string[] appNames)
        {
            Apps = appNames;
        }

        public string[] Apps { get; private set; }
    }
}
