using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Altus.Core.Component;
using Altus.Core.Processing;
using Altus.Core.Serialization;

namespace Altus.Core.Messaging.Http
{
    public class DefaultHttpRequestProcessor : InitializableComponent, IHttpRequestProcessor
    {
        public DefaultHttpRequestProcessor(HttpListenerContext ctx)
        {
            this.ListenerContext = ctx;
        }

        protected override bool OnInitialize(params string[] args) { return true; }

        public virtual void ProcessRequest()
        {
            HttpListenerRequest request = null;
            HttpListenerResponse response = null;
            try
            {
                request = this.ListenerContext.Request;
                response = this.ListenerContext.Response;
                HttpConnection.ProcessRequest(response, request);
            }
            catch (ThreadAbortException tae)
            {
                if (tae.ExceptionState != null
                    && tae.ExceptionState is ServiceContext
                    && ((ServiceContext)tae.ExceptionState).Terminated)
                {
                    Thread.ResetAbort();
                }
            }
            catch (SerializationException se)
            {
                try
                {
                    response.StatusCode = 415;
                    byte[] errorMessage = request.ContentEncoding.GetBytes(se.ToString());
                    response.ContentLength64 = errorMessage.Length;
                    response.ContentEncoding = request.ContentEncoding;
                    response.OutputStream.Write(errorMessage, 0, errorMessage.Length);
                }
                catch { }
            }
            catch (Exception ex)
            {
                try
                {
                    response.StatusCode = 500;
                    byte[] errorMessage = request.ContentEncoding.GetBytes(ex.ToString());
                    response.ContentLength64 = errorMessage.Length;
                    response.ContentEncoding = request.ContentEncoding;
                    response.OutputStream.Write(errorMessage, 0, errorMessage.Length);
                }
                catch { }
            }
        }

        public HttpListenerContext ListenerContext { get; private set; }
    }
}
