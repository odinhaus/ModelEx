using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core.Data
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=false, Inherited=true)]
    public class DataContextScriptsAttribute : Attribute
    {
        public DataContextScriptsAttribute(string resourceKey)
        {
            ResourceKey = resourceKey;
        }

        public string ResourceKey { get; private set; }
    }
}
