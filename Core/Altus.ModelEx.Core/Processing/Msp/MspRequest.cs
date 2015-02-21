using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Messaging;
using Altus.Core.Messaging.Http;
using System.Net;
using System.Collections.Specialized;

namespace Altus.Core.Processing.Msp
{
    public class MspRequest : ServiceRequest
    {
        public MspRequest()
        {
            this.Form = new HttpForm();
            this.Cookies = new CookieCollection();
            this.QueryString = new NameValueCollection();
        }

        public MspRequest(HttpForm form)
        {
            this.Form = form;
            this.Cookies = new CookieCollection();
            this.QueryString = new NameValueCollection();
        }

        public HttpForm Form { get; private set; }
        public CookieCollection Cookies { get; private set; }
        public NameValueCollection QueryString { get; private set; }
    }
}
