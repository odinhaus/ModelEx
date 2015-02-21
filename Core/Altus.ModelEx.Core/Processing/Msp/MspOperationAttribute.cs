using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Serialization;

namespace Altus.Core.Processing.Msp
{
    public class MspOperationAttribute : ServiceOperationAttribute
    {
        public MspOperationAttribute(string methodName) : base( StandardFormats.PROTOCOL_DEFAULT, methodName) { }
    }
}
