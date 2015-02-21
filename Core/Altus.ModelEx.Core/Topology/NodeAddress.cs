using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Altus.Core.Security;

namespace Altus.Core.Topology
{
    public class NodeAddress
    {
        public const string Any = "*";

        static Regex r = new Regex(@"(?<node>\w+).(?<network>\w+).(?<organization>\w+).(?<application>.+)");
        public NodeAddress(string address)
        {
            if (address == null) address = string.Empty;
            if (address.Equals("local", StringComparison.InvariantCultureIgnoreCase))
            {
                this.Address = NodeIdentity.NodeAddress;
            }
            else
            {
                this.Address = address;
            }

            Match m = r.Match(this.Address);
            if (m.Success)
            {
                this.IsValid = true;
                this.Node = m.Groups["node"].Value;
                this.Network = m.Groups["network"].Value;
                this.Organization = m.Groups["organization"].Value;
                this.Platform = m.Groups["application"].Value;
            }
            else
            {
                this.IsValid = false;
            }
        }
        public static NodeAddress Current { get { return NodeIdentity.NodeAddress; } }

        private ulong _id = 0;
        public ulong Id 
        {
            get
            {
                if (_id == 0)
                {
                    NodeIdentity.TryGetNodeId(this.Address, out _id);
                }
                return _id;
            }
        }
        public string Node { get; private set; }
        public string Network { get; private set; }
        public string Organization { get; private set; }
        public string Platform { get; set; }
        public string Address { get; private set; }

        public bool IsValid { get; private set; }

        public override string ToString()
        {
            return Address;
        }

        public override bool Equals(object obj)
        {
            return (obj is NodeAddress || obj is string)
                && ((NodeAddress)(string)obj).Address.Equals(this.Address, StringComparison.InvariantCultureIgnoreCase);
        }

        public static implicit operator NodeAddress(string address)
        {
            return new NodeAddress(address);
        }
    }
}
