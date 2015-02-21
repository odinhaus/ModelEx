using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Messaging.Http;
using System.Reflection;
using System.IO;

namespace Altus.Core.Processing.Msp
{
    public abstract class Page : InitializableComponent
    {
        protected Page()
        {
        }

        protected virtual void OnLoadTemplate()
        {
            
        }

        protected override bool OnInitialize(params string[] args)
        {
            if (this.MspContext != null)
                InitializePage();
            return true;
        }

        protected virtual void InitializePage()
        {
            this.OnLoadTemplate();
        }

        [MspOperation("Load", IsDefault=true)]
        protected abstract void ProcessRequest();

        public MspContext MspContext { get; internal set; }
        public MspRequest Request { get { return this.MspContext.Request; } }
        public MspResponse Response { get { return this.MspContext.Response; } }
        public HtmlElementList NamedElements { get { return this.MspContext.Response.HtmlElements; } }

        public string HtmlTemplate { get; private set; }
        public string HtmlErrorTemplate { get; private set; }

        internal virtual void OnUnhandledException(object sender, Exception exception)
        {
            this.Error = exception.GetType().Name;
            this.ErrorMessage = exception.Message;
            this.ErrorSource = exception.Source;
            this.ErrorTrace = exception.StackTrace;
            this.Response.UseErrorTemplate = true;
        }

        public string Error { get; protected set; }
        public string ErrorMessage { get; set; }
        public string ErrorSource { get; protected set; }
        public string ErrorTrace { get; protected set; }
    }
}
