using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using SpreadsheetGear;
using System.ComponentModel;

namespace AcmeCo.Workflow.SpreadsheetGearActivities
{
    [Designer(typeof(OpenWorkbookDesigner))]
    public sealed class OpenWorkbook : CodeActivity<IWorkbook>
    {
        [RequiredArgument]
        public InArgument<string> Filename { get; set; }
        
        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override IWorkbook Execute(CodeActivityContext context)
        {
            string filename = context.GetValue(this.Filename);

            IWorkbook workbook = Factory.GetWorkbook(filename);

            return workbook;
        }
    }
}
