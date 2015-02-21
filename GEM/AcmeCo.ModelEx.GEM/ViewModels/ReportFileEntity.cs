using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xaml;

namespace Altus.GEM.ViewModels
{
    public class ReportFileEntity : FileEntity
    {
        protected ReportFileEntity(string file, string name, EntityType type)
            : base(file, name, type)
        {
        }

        public ReportFileEntity(string file, string name, EntityType type, Entity parent)
            : base(file, name, type, parent)
        {
        }

        protected ReportFileEntity(DbDataReader rdr)
            : base(rdr)
        {}


        protected override EntityBehavior OnHandleReportEntityExecute()
        {
            OnExecuteWorkflow();
            return base.OnHandleDataEntityExecute();
        }

        protected virtual void OnExecuteWorkflow()
        {
            //WorkflowExtensions.InvokeActivity(OnGetXamlStream(), OnGetWorkflowArgs());
        }

        protected virtual IDictionary<string, object> OnGetWorkflowArgs()
        {
            throw new NotImplementedException();
        }

        protected virtual Stream OnGetXamlStream()
        {
            return new MemoryStream(ASCIIEncoding.ASCII.GetBytes(OnGetXamlString()));
        }

        protected virtual string OnGetXamlString()
        {
            throw new NotImplementedException();
        }
    }
}
