using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using SpreadsheetGear;
using System.ComponentModel;

namespace AcmeCo.Workflow.SpreadsheetGearActivities
{
    [Designer(typeof(CreateReportWorkbookDesigner))]
    public sealed class CreateReportWorkbook : CodeActivity<IWorkbook>
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }

        public InArgument<string> Template { get; set; }
        
        protected override IWorkbook Execute(CodeActivityContext context)
        {
            string filename = context.GetValue(this.Filename);
            string template = context.GetValue(this.Template);

            IWorkbook workbook;
            if (string.IsNullOrEmpty(template))
            {
                //Create a blank workbook
                workbook = Factory.GetWorkbook();
            }
            else
            {
                //Create from a template
                workbook = Factory.GetWorkbook(template);
            }

            workbook.SaveAs(filename, FileFormat.OpenXMLWorkbook);

            return workbook;
        }
    }
}
