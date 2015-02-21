using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Altus.Core.Processing
{
    public abstract class ServiceResponse
    {
        public ServiceOperation ServiceEndPoint { get; protected set; }
        public ServiceContext ServiceContext { get { return ServiceContext.Current; } }
    }
}