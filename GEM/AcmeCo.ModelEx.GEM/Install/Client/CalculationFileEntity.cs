using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.GEM.ViewModels
{
    public class @Name : FileEntity
    {
        public @Name(string file, string name, EntityType type)
            : base(file, name, type)
        {
        }

        public @Name(string file, string name, EntityType type, Entity parent)
            : base(file, name, type, parent)
        {
        }
        public @Name(DbDataReader rdr) : base(rdr)
        {
            @Reader
        }

        @Props
        @Code
    }
}
