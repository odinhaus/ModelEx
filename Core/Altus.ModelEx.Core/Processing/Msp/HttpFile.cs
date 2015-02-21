using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Altus.Core.Component;
using System.Reflection;
using System.IO;
using Altus.Core.Messaging;
using Altus.Core.Configuration;
using Altus.Core.Serialization;

[assembly: Component(ComponentType = typeof(Altus.Core.Processing.Msp.HttpFile))]
namespace Altus.Core.Processing.Msp
{
    [MspEndPoint("", "HttpFile.error.msp", @"http://.*")]
    public class HttpFile : Page
    {
        /// <summary>
        /// Handle all HTTP file requests, other than .msp pages
        /// NOTE: the regex below will prevent matches against any
        /// file extensions that BEGIN with ".msp".
        /// </summary>
        [MspOperation(@"", Priority=Int32.MaxValue)]
        protected override void ProcessRequest()
        {
            string path = Context.GlobalContext.CodeBase;
            path = Path.Combine(path, this.Request.ServiceOperation.ObjectPath.Replace("/","\\"));//, this.Request.ServiceOperation.Operation.Replace("/","\\"));
            if (System.IO.File.Exists(path))
            {
                this.Response.WriteBinary(System.IO.File.ReadAllBytes(path));
                this.Response.ServiceContext.Connection.ContentType = StandardFormats.GetContentType(Path.GetExtension(path).Replace(".",""));
            }
            else
            {
                throw (new FileNotFoundException("The backing file for " + this.Request.ServiceOperation.ServiceUri + " could not be found."));
            }
        }

        internal override void OnUnhandledException(object sender, Exception exception)
        {
            if (exception is FileNotFoundException)
            {
                this.MspContext.StatusCode = 404;
            }

            base.OnUnhandledException(sender, exception);
        }
    }
}
