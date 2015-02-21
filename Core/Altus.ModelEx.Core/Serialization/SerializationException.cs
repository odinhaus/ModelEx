using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Serialization
{
    public class SerializationException : Exception
    {
        public SerializationException(string message) : base(message) { }
    }
}
