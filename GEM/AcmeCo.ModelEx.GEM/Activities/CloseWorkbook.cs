using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using SpreadsheetGear;
using System.ComponentModel;

namespace AcmeCo.Workflow.SpreadsheetGearActivities
{
    [Designer(typeof(CloseWorkbookDesigner))]
    public sealed class CloseWorkbook : CodeActivity
    {
        [RequiredArgument]
        public InArgument<IWorkbook> Workbook { get; set; }

        [RequiredArgument]
        public InArgument<Boolean> Save { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            // Obtain the runtime value of the Text input argument
            IWorkbook workbook = context.GetValue(this.Workbook);
            Boolean save = context.GetValue(this.Save);

            if (save) { workbook.Save(); }

            workbook.Close();
        }
    }
}
