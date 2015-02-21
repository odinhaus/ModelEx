using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core.Component
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public class DiscoverableAssemblyAttribute : Attribute
    {
        public DiscoverableAssemblyAttribute(string key)
        {
            Key = key;
        }

        public string Key { get; private set; }
    }
}
