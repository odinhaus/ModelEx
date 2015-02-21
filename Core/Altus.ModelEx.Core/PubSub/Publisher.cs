using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Topology;

namespace Altus.Core.PubSub
{
    public class Publisher
    {
        public Publisher(string address, object target)
        {
            this.NodeAddress = address;
            this.Target = target;
        }

        public object Target { get; private set; }
        public NodeAddress NodeAddress { get; set; }
    }
}
