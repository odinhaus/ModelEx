using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Altus.Core.Processing
{
    public abstract class ServiceRequest
    {
        public Stream InputStream { get; private set; }
        public ServiceOperation ServiceOperation{ get; internal set; }
        public ServiceContext ServiceContext { get { return ServiceContext.Current; } } 
    }
}
