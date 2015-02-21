using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using SpreadsheetGear;
using System.ComponentModel;

namespace AcmeCo.Workflow.SpreadsheetGearActivities
{
    [Designer(typeof(CalculateWorkbookDesigner))]
    public sealed class CalculateWorkbook : CodeActivity
    {
        [RequiredArgument]
        public InArgument<IWorkbook> Workbook { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            IWorkbook workbook = context.GetValue(this.Workbook);
            workbook.WorkbookSet.Calculate();   
        }
    }
}
