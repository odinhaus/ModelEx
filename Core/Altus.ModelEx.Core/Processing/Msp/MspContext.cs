using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Messaging;
using System.Net;
using Altus.Core.Messaging.Http;
using Altus.Core.Exceptions;

namespace Altus.Core.Processing.Msp
{
    public class MspContext : ServiceContext<MspRequest, MspResponse>
    {
        public MspContext(Message message, ServiceOperation operation, ServiceOperationAttribute attrib, MspPipeline pipeline, IConnection connection, MspRequest request, MspResponse response)
            : base()
        {
            this.Message = message;
            this.ProcessingContext = pipeline;
            this.Request = request;
            this.Response = response;
            this.Action = connection.Action;
            this.Connection = connection;
            this.ResponseFormat = operation.Format;
            //this.ResponseUri = operation.ResponseUri;
            this.SetServiceUri(operation.ServiceUri);
            this.Operation = operation;
            this.StatusCode = 200;
        }
        protected ServiceOperation Operation { get; set; }

        public Altus.Core.Processing.Action Action { get; private set; }

        protected override object OnSetRequestMessagePayload()
        {
            return this.Request.ServiceOperation;
        }

        protected override object OnSetResponseMessagePayload()
        {
            return this.Response.OperationResponse;
        }

        protected override List<KeyValuePair<string, string>> OnSetResponseHeaders()
        {
            List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
            foreach (Cookie c in this.Response.Cookies)
            {
                headers.Add(new KeyValuePair<string,string>("Set-Cookie", GetCookieString(c)));
            }
            return headers;
        }

        protected override void OnUnhandledException(object sender, Exception exception)
        {
            this.StatusCode = 500;
            if (this.Response.OperationResponse == null)
            {
                this.Response.OperationResponse = new ServiceOperation(OperationType.Response, this.Request.ServiceOperation.ServiceType, this.Operation.ServiceUri);
            }
            this.Response.OperationResponse.Parameters.Add(new ServiceParameter("Error", exception.GetType().FullName, ParameterDirection.Error) { Value = new RuntimeException(exception) });
            this.Page.OnUnhandledException(sender, exception);
        }

        private string GetCookieString(Cookie cookie)
        {
            string cs = string.Format("{0}={1}; ", cookie.Name, cookie.Value);
            if (!cookie.Expired && cookie.Expires != DateTime.MaxValue)
                cs += string.Format("Expires={0}; ", cookie.Expires.ToString("r"));
            if (!string.IsNullOrEmpty(cookie.Domain))
                cs += string.Format("Domain={0}; ", cookie.Domain);
            if (!string.IsNullOrEmpty(cookie.Path))
                cs += string.Format("Path={0}; ", cookie.Path);
            if (!string.IsNullOrEmpty(cookie.Comment))
                cs += string.Format("Comment={0}; ", cookie.Comment);
            if (cookie.CommentUri != null)
                cs += string.Format("CommentUri={0}; ", cookie.CommentUri.ToString());
            if (cookie.HttpOnly)
                cs += "HttpOnly; ";

            return cs;
        }

        public Page Page { get { return (Page)((MspOperationProxy)this.ProcessingContext.Processors.Last()).Attribute.Target; } }
    }
}
