using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Processing
{
    public struct ServiceRoute
    {
        public ServiceRoute(string routePattern)
            : this()
        {
            this.RoutePattern = routePattern;
            this.Protocol = "";
            this.Port = 0;
            this.NodeAddress = "";
            this.OrgUnit = "";
        }
        public string Protocol { get; private set; }
        public int Port { get; private set; }
        public string NodeAddress { get; private set; }
        public string OrgUnit { get; private set; }
        public string RoutePattern { get; private set; }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
