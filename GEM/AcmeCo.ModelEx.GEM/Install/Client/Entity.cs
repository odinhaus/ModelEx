using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.GEM.ViewModels
{
    public class @Name : Entity
    {
        public @Name(string name, EntityType type)
            : base(name, type) {}

        public @Name(string name, EntityType type, Entity parent)
            : base(name, type, parent) {}
        public @Name(DbDataReader rdr) : base(rdr)
        {
            @Reader
        }

        @Props
        @Code
    }
}
