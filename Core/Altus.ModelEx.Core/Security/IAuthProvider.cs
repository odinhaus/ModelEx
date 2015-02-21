using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;

namespace Altus.Core.Security
{
    public enum RoleMembership
    {
        Any,
        All
    }
    public interface IAuthProvider : IComponent
    {
        bool Authenticate(string user, string password);
        bool Authorize(RoleMembership membership, params string[] roles);
    }
}
