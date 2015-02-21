using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Messaging.Tcp
{
    public class TcpFault
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}
