using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Data
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple=false, Inherited=true)]
    public class StorageMappingAttribute : Attribute
    {
        public StorageMappingAttribute(string storageEntityName)
        {
            this.EntityName = storageEntityName;
        }

        public string EntityName { get; private set; }
    }
}
