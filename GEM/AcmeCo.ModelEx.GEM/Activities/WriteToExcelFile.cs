using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using SpreadsheetGear;
using System.ComponentModel;

namespace AcmeCo.Workflow.SpreadsheetGearActivities
{
    [Designer(typeof(WriteToExcelDesigner))]
    public sealed class WriteToExcelFile : CodeActivity
    {
        [RequiredArgument]
        public InArgument<IWorkbook> Target { get; set; }

        [RequiredArgument]
        public InArgument<string> RangeName { get; set; }

        [RequiredArgument]
        public InArgument<object> Data { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            IWorkbook targetWorkbook = context.GetValue(this.Target);
            string targetRange = context.GetValue(this.RangeName);

            object data = context.GetValue(this.Data);

            targetWorkbook.Names[targetRange].RefersToRange.Value = data;
        }
    }
}
