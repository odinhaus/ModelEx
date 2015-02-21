using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Messaging.Http
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public class HttpResponseHeaderAttribute : Attribute
    {
    }
}
