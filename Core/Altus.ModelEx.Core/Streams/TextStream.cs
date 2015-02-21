using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Altus.Core.Serialization;

namespace Altus.Core.Streams
{
    public class TextStream : MemoryStream
    {
        public TextStream(string text)
            : base(SerializationContext.TextEncoding.GetBytes(text))
        {

        }
    }
}
