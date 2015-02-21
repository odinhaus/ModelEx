using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Messaging;
using System.IO;
using Altus.Core.Component;
using Altus.Core.Pipeline;
using Altus.Core.Security;
using System.Threading;
using Altus.Core.Serialization;
using Altus.Core;

namespace Altus.Core.Processing
{
    public delegate void ExceptionHandler(object sender, Exception exception);

    public abstract partial class ServiceContext
    {
        #region Instance Members
        protected ServiceContext() { }
        //public Action Action { get; set; }
        public string Id { get; set; }
        public string CorrelationId { get; set; }
        public IConnection Connection { get; protected set; }
        public string Sender { get; set; }
        public string[] Recipients { get; set; }
        public ServiceType ServiceType { get; internal set; }
        public CompositionContainer ShellContext { get { return App.Instance.Shell; } }
        public IPipeline<ServiceContext> ProcessingContext { get; protected set; }
        public bool DeliveryGuaranteed { get; set; }
        public DateTime Timestamp { get; set; }
        public string RequestFormat { get; protected set; }
        public string ResponseFormat { get; protected set; }
        public Message Message { get; protected set; }

        public void SetServiceUri(string uri)
        {
            if (!string.IsNullOrEmpty(uri))
            {
                string[] segments = uri.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                _protocol = segments[0].Split(':')[0];
                _meth = segments[3];
                _auth = segments[1];
            }
            _svcUri = uri;
        }
        string _svcUri;
        public string ServiceUri
        {
            get { return _svcUri; }
        }
        //public string ResponseUri { get; internal set; }
        string _protocol;
        public string ServiceProtocol
        {
            get
            {
                return _protocol;
            }
        }
        string _auth;
        public string ServiceHost
        {
            get
            {
                return _auth;
            }
        }
        string _meth;
        public string ServiceMethod
        {
            get
            {
                return _meth;
            }
        }

        public Message ToResponseMessage()
        {
            Message message = new Message(
               this.ResponseFormat.Equals(StandardFormats.PROTOCOL_DEFAULT) ? this.Connection.DefaultFormat : this.ResponseFormat,
               this.ServiceUri,
               this.ServiceType,
               NodeIdentity.NodeAddress);
            message.StatusCode = this.StatusCode;
            message.CorrelationId = this.Id;
            message.DeliveryGuaranteed = this.DeliveryGuaranteed;
            message.Recipients = new string[] { this.Sender };
            message.Payload = this.OnSetResponseMessagePayload();
            foreach (KeyValuePair<string, string> header in this.OnSetResponseHeaders())
            {
                message.Headers.Add(header.Key, header.Value);
            }
            message.Timestamp = CurrentTime.Now;
            message.IsReponse = true;
            return message;
        }
        public bool Terminated { get; private set; }
        public void Terminate()
        {
            this.Terminated = true;
            Dispose();
            Thread.CurrentThread.Abort(this);
        }

        protected abstract object OnSetRequestMessagePayload();
        protected abstract object OnSetResponseMessagePayload();
        protected abstract List<KeyValuePair<string, string>> OnSetResponseHeaders();
        public int StatusCode { get; internal set; }
        protected abstract void OnUnhandledException(object sender, Exception exception);
        #endregion
    }
}
