using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using SpreadsheetGear;

namespace AcmeCo.Workflow.SpreadsheetGearActivities
{
    [Designer(typeof(TransferDataDesigner))]
    public sealed class TransferData : CodeActivity
    {
        [RequiredArgument]
        public InArgument<IWorkbook> SourceWorkbook { get; set; }

        [RequiredArgument]
        public InArgument<string> SourceRange { get; set; }

        [RequiredArgument]
        public InArgument<IWorkbook> TargetWorkbook { get; set; }

        [RequiredArgument]
        public InArgument<string> TargetRange { get; set; }


        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            IWorkbook sourceWorkbook = context.GetValue(this.SourceWorkbook);
            string sourceRange = context.GetValue(this.SourceRange);

            IWorkbook targetWorkbook = context.GetValue(this.TargetWorkbook);
            string targetRange = context.GetValue(this.TargetRange);

            object sourceData = sourceWorkbook.Names[sourceRange].RefersToRange.Value;

            targetWorkbook.Names[targetRange].RefersToRange.Value = sourceData;
        }
    }
}
