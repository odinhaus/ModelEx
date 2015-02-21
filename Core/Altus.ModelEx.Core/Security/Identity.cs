using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Principal;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Altus.Core.Security
{

    public class Identity : IIdentity
    {

        protected Identity() { }

        // public properties
        public bool IsAuthenticated { get; internal set; }
        public string AuthenticationType
        {
            get
            {
                return "Altus Authentication";
            }
        }

        
        public int Id { get; protected set; }
       
        public string Name { get; set; }
       
        public string PasswordEncrypted { get; set; }
       
        public string SecretKey { get; set; }

        public string Created { get; private set; }

        public Identity(int id, string name) { this.Id = id; this.Name = name; }
    }
}
