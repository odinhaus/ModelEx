using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Altus.Core.Diagnostics;
using Altus.Core.Processing;
using Altus.Core.Processing.Msp;

namespace Altus.Core.Messaging.Http
{
    public enum HttpRequestProcessingPipeline
    {
        Msp,
        Asp
    }
    public static class HttpRequestProcessorFactory
    {
        static ServiceEndPointManager _sem;
        static HttpRequestProcessorFactory()
        {
            Pipeline = Context.GlobalContext.InstanceType == InstanceType.ASPNetHost ?
                HttpRequestProcessingPipeline.Asp
                : HttpRequestProcessingPipeline.Msp;
            _sem = Altus.Core.Component.App.Instance.Shell.GetComponent<ServiceEndPointManager>();
        }

        public static HttpRequestProcessingPipeline Pipeline { get; private set; }

        public static IHttpRequestProcessor CreateProcessor(HttpListenerContext ctx)
        {
            IHttpRequestProcessor processor;
            switch(Pipeline)
            {
                case HttpRequestProcessingPipeline.Msp:
                    {
                        processor = new DefaultHttpRequestProcessor(ctx);
                        break;
                    }
                default:
                    {
                        ServiceOperationAttribute soa;
                        if (_sem.HasProxy(ctx.Request.Url.ToString(), out soa))
                        {
                            if (soa == null
                                || (soa.Target is HttpFile
                                && soa.Method.Name.Equals("ProcessRequest"))) // this is a generic MSP operation handler
                            {
                                processor = new AspHttpRequestProcessor(ctx);
                            }
                            else processor = new DefaultHttpRequestProcessor(ctx);
                        }
                        else
                            processor = new AspHttpRequestProcessor(ctx);
                        break;
                    }
            }

            Logger.LogInfo("Handling request for " + ctx.Request.Url + " with " + processor.GetType().Name);
            return processor;
        }

    }
}
