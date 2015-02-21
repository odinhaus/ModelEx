using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Messaging.Tcp
{
    public class Header
    {
        public Header(string name, string value) { Name = name; Value = value; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
