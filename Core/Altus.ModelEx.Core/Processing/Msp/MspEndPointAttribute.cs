using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Processing.Msp
{
    public class MspEndPointAttribute : ServiceEndPointAttribute
    {
        public MspEndPointAttribute(params string[] routes) : base(typeof(MspOperationProxy), ServiceTypes.MSP, routes) { }

        public MspEndPointAttribute(string htmlTemplatePath, string htmlErrorTemplatePath, params string[] routes)
            : base(typeof(MspOperationProxy), ServiceTypes.MSP, routes)
        {
            this.HtmlTemplatePath = htmlTemplatePath;
            this.HtmlErrorTemplatePath = htmlErrorTemplatePath;
        }

        public string HtmlTemplatePath { get; private set; }
        public string HtmlErrorTemplatePath { get; private set; }
    }
}
