using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.component;
using System.Net;
using Altus.security;
using Altus.serialization;
using Altus.streams;
using System.IO;
using Altus.processing;

namespace Altus.messaging.http
{
    public class HttpClient : InitializableComponent
    {
        public HttpClient(ServiceOperation sep)
        {
            this.ServiceEndPoint = sep;
        }

        public ServiceOperation ServiceEndPoint { get; private set; }

        public void Send()
        {
            HttpWebRequest request = HttpWebRequest.Create(this.ServiceEndPoint.ServiceUri) as HttpWebRequest;
            request.Headers.Add("Altus-Format", "bin");
            Message msg = new Message(this.ServiceEndPoint,  ServiceType.RequestResponse, Altus.processing.Action.POST);
            //msg.SetPayload(this.ServiceEndPoint.Arguments);
            MemoryStream ms = new MemoryStream();
            msg.SerializeParameters(ms);
            StreamHelper.Copy(ms, request.GetRequestStream());
            //HttpConnection httpConnection = new HttpConnection(request, this.ServiceEndPoint);
            //MessagingContext ctx = MessagingContext.FromMessage(httpConnection, msg);
            

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            response.Close();

        }

        //public ServiceContext SendWithResponse()
        //{
        //    HttpWebRequest request = HttpWebRequest.Create(this.ServiceEndPoint.ServiceUri) as HttpWebRequest;
        //    request.Headers.Add("Altus-Format", "bin");
        //    Message msg = new Message(this.ServiceEndPoint, MessageType.RequestResponse, Action.POST);
        //    //msg.SetPayload(this.ServiceEndPoint.Arguments);
        //    StreamHelper.Copy(msg.PayloadStream, request.GetRequestStream());
           
        //    HttpConnection httpConnection = new HttpConnection(request, this.ServiceEndPoint.EndPoint);
        //    ServiceContext ctx = ServiceContext.FromMessage(httpConnection, msg);

        //    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //    StreamHelper.Copy(response.GetResponseStream(), ctx.Request.InputStream);
        //    Message respMsg = (Message)IdentifiedMessage.FromStream(ctx.Request.InputStream);

        //    response.Close();
        //    //respMsg.GetPayload(ctx);
        //    return ctx;
        //}

        protected override void OnInitialize(params string[] args)
        {
            
        }

        protected override void OnDispose()
        {
            base.OnDispose();
        }
        
    }
}
