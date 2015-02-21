using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using SpreadsheetGear;
using System.ComponentModel;

namespace AcmeCo.Workflow.SpreadsheetGearActivities
{
    [Designer(typeof(ReadFromExcelFileDesigner))]
    public sealed class ReadFromExcelFile : CodeActivity<object>
    {
        [RequiredArgument]
        public InArgument<IWorkbook> Source { get; set; }

        [RequiredArgument]
        public InArgument<string> RangeName { get; set; }

        protected override object Execute(CodeActivityContext context)
        {
            IWorkbook source = context.GetValue(this.Source);
            string rangeName = context.GetValue(this.RangeName);

            object data = source.Names[rangeName].RefersToRange.Value;
            
            return data;
        }
    }
}
