using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Altus.Core.Serialization;
using Altus.Core.Streams;
using Altus.Core.Component;
using System.Reflection;
using Altus.Core.Processing;
using Altus.Core.Exceptions;

namespace Altus.Core.Messaging.Http
{
    public class HttpConnection : IConnection
    {
        public bool IsDisconnected { get; protected set; }
        public event EventHandler Disconnected;
        protected void OnDisconnected()
        {
            IsDisconnected = true;
            if (Disconnected != null)
                Disconnected(this, new EventArgs());
        }
        public static void ProcessRequest(HttpListenerResponse response, HttpListenerRequest request)
        {
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Method", "GET, POST");
            response.AddHeader("Access-Control-Allow-Headers", AltusHeaders.All + ", Content-Type, Content-Length");
            if (request.HttpMethod.Equals("OPTIONS", StringComparison.InvariantCultureIgnoreCase))
            {
                response.Close();
            }
            else
            {
                SerializationContext.TextEncoding = request.ContentEncoding;
                HttpConnection connection = new HttpConnection(response, request);
                Message message = new Message(request, connection);
                connection.ProcessInboundMessage(message);
            }
        }

        public HttpConnection(HttpListenerResponse response, HttpListenerRequest request)
        {
            this.Action = (Altus.Core.Processing.Action)Enum.Parse(typeof(Altus.Core.Processing.Action), request.HttpMethod);
            this.Response = response;
            EndPoint = request.RemoteEndPoint;
            this.ConnectionAspects = new Dictionary<string, object>();
            this.ContentType = StandardFormats.GetContentType("form");
            this.TextEncoding = request.ContentEncoding;
        }

        public HttpConnection(HttpWebRequest request, EndPoint endPoint)
        {
            this.Action = Processing.Action.POST;
            this.Request = request;
            EndPoint = endPoint;
            this.ConnectionAspects = new Dictionary<string, object>();
        }

        public void Send(byte[] data)
        {
            if (this.Response == null)
            {
                this.WriteStream = this.Request.GetRequestStream();
                this.Request.ContentType = this.ContentType + "; charset=" + this.TextEncoding.HeaderName;
            }
            else
            {
                this.WriteStream = this.Response.OutputStream;
                this.ContentLength = data.Length;
                this.Response.ContentType = this.ContentType + "; charset=" + this.TextEncoding.HeaderName;
            }

            WriteStream.Write(data, 0, data.Length);

            if (this.Request == null)
            {
                this.Response.Close();
            }
            else
            {
                Message msg = null;
                HttpWebResponse resp = null;
                try
                {
                     resp = (HttpWebResponse)this.Request.GetResponse();
                }
                catch (WebException wex)
                {
                    resp = (HttpWebResponse)wex.Response;
                }
                this.ContentType = resp.ContentType;
                msg = new Message(resp, this);
                this.TextEncoding = Encoding.GetEncoding(msg.Encoding);
                this.ProcessInboundMessage(msg);
            }
        }

        public void Send(Message message)
        {
            if (SerializationContext.TextEncoding == null) SerializationContext.TextEncoding = this.TextEncoding;
            message.Encoding = SerializationContext.TextEncoding.EncodingName;

            ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>()
                .Where(s => s.SupportsFormat(message.PayloadFormat) && s.SupportsType(message.GetType())).FirstOrDefault();

            if (serializer == null) throw (new SerializationException("Serializer for type " + message.GetType() + " could not be found."));

            byte[] data = serializer.Serialize(message);

            

            foreach (KeyValuePair<string, string> header in message.Headers)
            {
                if (this.Response == null)
                {
                    if (header.Key.Equals("Content-Type"))
                    {
                        this.Request.ContentType = header.Value;
                    }
                    else
                    {
                        this.Request.Headers.Add(header.Key, header.Value);
                    }
                }
                else
                {
                    if (header.Key.Equals("Content-Type"))
                    {
                        this.Response.ContentType = header.Value;
                    }
                    else
                    {
                        this.Response.AddHeader(header.Key, header.Value);
                    }
                }
            }

            foreach (PropertyInfo pi in message.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                object[] headers = pi.GetCustomAttributes(typeof(HttpResponseHeaderAttribute), true);
                if (pi.CanRead && headers != null && headers.Length > 0)
                {
                    object value = pi.GetValue(message, null);
                    if (value == null)
                        value = "";
                    else
                        value = value.ToString();
                    if (this.Response == null)
                    {
                        this.Request.Headers.Add("Altus-" + pi.Name, (string)value);
                    }
                    else
                    {
                        this.Response.AddHeader("Altus-" + pi.Name, (string)value);
                    }
                }
            }

            if (this.Response == null)
            {
                this.ContentType = StandardFormats.GetContentType(message.PayloadFormat);
            }
            else
            {
                this.TextEncoding = SerializationContext.TextEncoding;
                this.Response.StatusCode = message.StatusCode;
            }
            
            this.Send(data);
        }

        public void SendError(Message message, Exception ex)
        {
            try
            {
                ServiceOperation so = new ServiceOperation(message, OperationType.Response);
                ex = new RuntimeException(ex);
                so.Parameters.Add(new ServiceParameter("Error", ex.GetType().FullName, ParameterDirection.Error) { Value = ex });
                message.Payload = so;
                Send(message);
            }
            catch
            {
                Send(ASCIIEncoding.ASCII.GetBytes(ex.ToString()));
            }
        }

        public System.Net.EndPoint EndPoint
        {
            get;
            private set;
        }

        public Dictionary<string, object> ConnectionAspects { get; private set; }
        public Stream WriteStream { get; set; }
        public HttpListenerResponse Response { get; private set; }
        public HttpWebRequest Request { get; private set; }
        public Protocol Protocol { get { return Messaging.Protocol.Http; } }
        public Altus.Core.Processing.Action Action { get; private set; }

        public void ResetProperties()
        {
            ConnectionAspects.Clear();
        }

        public Encoding TextEncoding
        {
            get;
            set;
        }

        public long ContentLength
        {
            get 
            {
                if (this.Response == null)
                {
                    return this.Request.ContentLength;
                }
                else
                {
                    return Response.ContentLength64;
                }
            }
            set 
            {
                if (this.Response == null)
                {
                    this.Request.ContentLength = value;
                }
                else
                {
                    Response.ContentLength64 = value;
                }
            }
        }

        public string ContentType
        {
            get;
            set;
        }

        public string DefaultFormat { get { return StandardFormats.HTML; } }


        public Message Call(Message message)
        {
            return this.Call(message, 30000);   
        }

        public Message Call(Message message, int timespan)
        {
            return this.Call(message, TimeSpan.FromMilliseconds(timespan));
        }

        public Message Call(Message message, TimeSpan timeSpan)
        {
            AsyncRequest async = new AsyncRequest(message, timeSpan);

            if (async.CanHaveResponse)
            {
                lock (_receivers)
                {
                    _receivers.Add(message.Id, async.ResponseCallback);
                }
            }

            this.Send(message);

            return async.GetResponse();
        }

        static Dictionary<string, MessageReceivedHandler> _receivers = new Dictionary<string, MessageReceivedHandler>();
        private void ProcessInboundMessage(Message message)
        {
            if (_receivers.ContainsKey(message.CorrelationId))
            {
                MessageReceivedHandler callback = _receivers[message.CorrelationId];
                try
                {
                    callback(this, message);
                }
                catch { }
                finally
                {
                    _receivers.Remove(message.CorrelationId);
                }
            }
            else
            {
                ServiceContext ctx;
                ServiceContextFactory factory = App.Instance.Shell.GetComponents<ServiceContextFactory>().Where(scf => scf.CanProcess(message)).FirstOrDefault();
                if (factory != null
                    && factory.CreateContext(message, this, out ctx))
                {
                    ServiceContext.Current = ctx;
                    ctx.ProcessingContext.Process(ctx);
                    if (ctx.ServiceType == ServiceType.RequestResponse
                        && !ctx.IsCanceled)
                        ProcessResponse(ctx);
                    ctx.Dispose();
                }
                else
                {
                    throw (new InvalidOperationException("Context Factory either could not be found, or could not process the request: " + message.ServiceUri));
                }
            }
        }

        private void ProcessResponse(ServiceContext ctx)
        {
            Message message = ctx.ToResponseMessage();
            this.Send(message);
        }

        #region IDisposable Members
        
        bool disposed = false;
        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue 
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }


        public event EventHandler Disposing;
        public event EventHandler Disposed;
        //========================================================================================================//
        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the 
        // runtime from inside the finalizer and you should not reference 
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                if (this.Disposing != null)
                    this.Disposing(this, new EventArgs());
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    this.OnDisposeManagedResources();
                }

                // Call the appropriate methods to clean up 
                // unmanaged resources here.
                // If disposing is false, 
                // only the following code is executed.
                this.OnDisposeUnmanagedResources();
                if (this.Disposed != null)
                    this.Disposed(this, new EventArgs());
            }
            disposed = true;
        }

        /// <summary>
        /// Dispose managed resources
        /// </summary>
        protected virtual void OnDisposeManagedResources()
        {
            if (this.Response != null)
            {
                try
                {
                    this.Response.Close();
                }
                catch { }
            }


            if (this.Request != null)
            {
                try
                {
                    this.Request.Abort();
                }
                catch { }
            }
        }

        /// <summary>
        /// Dispose unmanaged (native resources)
        /// </summary>
        protected virtual void OnDisposeUnmanagedResources()
        {
        }

        #endregion
    }
}
