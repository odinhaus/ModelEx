using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Security
{
    public class Role
    {
        public Role() { }

        public Role(string name) { this.Name = name; }
        public int Id { get; private set; }
        public string Name { get; set; }
    }
}
