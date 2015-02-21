using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.GEM.ViewModels
{
    public class FileEntity : Entity
    {
        protected FileEntity(string file, string name, EntityType type)
            : base(name, type)
        {
            this.File = file;
        }

        public FileEntity(string file, string name, EntityType type, Entity parent)
            : base(name, type, parent)
        {
            this.File = file;
        }

        protected FileEntity(DbDataReader rdr)
            : base(rdr)
        {
            this.File = rdr["File"].ToString();
        }

        private string _file;
        public string File 
        {
            get { return _file; }
            set
            {
                _file = value;
                OnPropertyChanged("File");
            }
        }

        protected override EntityBehavior OnHandleDataEntityExecute()
        {
            Process.Start(this.File);
            return base.OnHandleDataEntityExecute();
        }

    }
}
