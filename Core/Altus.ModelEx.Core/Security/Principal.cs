using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Principal;
using System.Collections.Generic;

namespace Altus.Core.Security
{
    public class Principal : GenericPrincipal
    {
        // ctor
        public Principal(IIdentity identity, string[] roles)
            : base(identity, roles)
        {
            if (roles == null) roles = new string[0];
            this.Roles = roles;
        }

        public string[] Roles { get; private set; }

        // private methods
        public override bool IsInRole(string role)
        {
            return this.Roles.Contains(role, new RoleNameComparer());
        }

        private class RoleNameComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return x.Equals(y, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
