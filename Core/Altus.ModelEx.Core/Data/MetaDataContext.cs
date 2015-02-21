using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Data;

namespace Altus.Core.Data
{
    public abstract class MetaDataContext : DataContext 
    {
        public MetaDataContext(string name) : base(name) { }
    }
}
