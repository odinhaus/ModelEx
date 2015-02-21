using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core.Messaging.Http
{
    public interface IHttpRequestProcessor : IComponent
    {
        void ProcessRequest();
    }
}
