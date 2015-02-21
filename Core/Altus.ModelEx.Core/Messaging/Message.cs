using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Altus.Core.Messaging;
using Altus.Core.Compression;
using Altus.Core.Streams;
using Altus.Core.Component;
using Altus.Core.Serialization;
using Altus.Core.Diagnostics;
using Altus.Core.Configuration;
using Altus.Core.Security;
using Altus.Core;
using System.Reflection;
using Altus.Core.Processing;
using System.Net;
using Altus.Core.Messaging.Http;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using Altus.Core.Data;
using Altus.Core.Topology;

namespace Altus.Core.Messaging
{
    [System.Serializable]
    public abstract class IdentifiedMessage
    {
        public IdentifiedMessage(string payloadFormat)
        {
            this.Headers = new Dictionary<string, string>();
            this.Id = Guid.NewGuid().ToString();
            this.MessageType = OnGetMessageType();
            this.Timestamp = CurrentTime.Now;
            this.TTL = TimeSpan.FromSeconds(90);
            this.PayloadFormat = payloadFormat;
            //this.Parameters = new ServiceParameterCollection();
            this.CorrelationId = string.Empty;
        }
        public IdentifiedMessage(string payloadFormat, string id)
        {
            this.Headers = new Dictionary<string, string>();
            this.Id = id;
            this.CorrelationId = "";
            this.MessageType = OnGetMessageType();
            this.Timestamp = CurrentTime.Now;
            this.TTL = TimeSpan.FromSeconds(90);
            this.PayloadFormat = payloadFormat;
            //this.Parameters = new ServiceParameterCollection();
            this.CorrelationId = string.Empty;
        }
        [HttpResponseHeader]
        public string Id { get; set; }
        [HttpResponseHeader]
        public string Sender { get; set; }
        private string _cid = string.Empty;
        [HttpResponseHeader]
        public string CorrelationId { get { return _cid; } set { _cid = value == null ? string.Empty : value; }}
        [HttpResponseHeader]
        public DateTime Timestamp { get; set; }
        [HttpResponseHeader]
        public TimeSpan TTL { get; set; }
        [HttpResponseHeader]
        public byte MessageType { get; set; }
        [HttpResponseHeader]
        public string PayloadFormat { get; protected set; }
        [ScriptIgnore()]
        public MemoryStream PayloadStream { get; protected set; }
        [HttpResponseHeader]
        public ServiceType ServiceType { get; protected set; }
        [HttpResponseHeader]
        public string ServiceUri { get; protected set; }
        public Dictionary<string, string> Headers { get; private set; }

        private object _payload;
        public object Payload
        {
            get { return _payload; }
            set
            {
                _payload = value;
                _payloadType = value.GetType().FullName;
            }
        }
        //public ServiceParameterCollection Parameters { get; private set; }
        [HttpResponseHeader]
        public int StatusCode { get; set; }
        [HttpResponseHeader]
        public Altus.Core.Processing.Action Action { get; protected set; }
        string _payloadType = string.Empty;
        [ScriptIgnore()]
        [HttpResponseHeader]
        public string PayloadType { get { return _payloadType; } protected set { _payloadType = value; } }
        [HttpResponseHeader()]
        public bool IsReponse { get; set; }
        [HttpResponseHeader()]
        public string Encoding 
        {
            get
            {
                if (this.Headers.ContainsKey("Content-Type"))
                {
                    string charset = this.Headers["Content-Type"].Split(';')[1].Trim().Split('=')[1].Trim();
                    return charset;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                string ctype = StandardFormats.GetContentType(this.PayloadFormat);
                ctype += "; charset=" + value;
                if (this.Headers.ContainsKey("Content-Type"))
                {
                    this.Headers["Content-Type"] = ctype;
                }
                else
                {
                    this.Headers.Add("Content-Type", ctype);
                }
            }
        }

        string _lastUri = null;
        IPEndPoint _serviceEP;
        [ScriptIgnore()]
        public IPEndPoint ServiceEndPoint
        {
            get
            {
                if (!ServiceUri.Equals(_serviceEP))
                {
                    string protocol, node, application, obj, op, fmt;
                    int port;
                    if (ServiceEndPointManager.TryParseServiceUri(this.ServiceUri, out protocol, out node, out port, out application, out obj, out op, out fmt))
                    {
                        _serviceEP = DataContext.Default.GetNodeEndPoint(node, protocol);
                    }   
                }

                if (_serviceEP == null) throw (new InvalidOperationException("Service URI is not of the correct format."));
                return _serviceEP;
            }
        }

        public void SerializeParameters(Stream outputStream)
        {
            this.OnSerializePayload(outputStream);
        }

        protected abstract byte OnGetMessageType();

        //protected virtual void OnDeserializeParameters(string typeName, Stream inputStream)
        //{
            
        //}

        public byte[] ToByteArray()
        {
            byte[] body;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter br = new BinaryWriter(ms);
                br.Write(this.Headers.Count);
                foreach (KeyValuePair<string, string> h in this.Headers)
                    br.Write(h.Key + ":" + h.Value);
                br.Write(this.PayloadFormat);
                br.Write(this.PayloadType);
                br.Write(this.Id);
                br.Write(this.CorrelationId);
                br.Write(this.Sender);
                br.Write(this.Timestamp.Ticks);
                br.Write(this.TTL.Ticks);
                br.Write((int)this.ServiceType);
                br.Write(this.ServiceUri);
                br.Write(this.StatusCode);
                MemoryStream pms = new MemoryStream();
                OnSerializePayload(pms);
                OnSerialize(ms);

                br.Write(pms.Length);
                pms.Position = 0;
                StreamHelper.Copy(pms, ms);

                ms.Position = 0;
                body = StreamHelper.GetBytes(ms);
            }
            byte prefix = 1;
            prefix = (byte)(prefix | (MessageType << 1));
            //bool compressed = body.Length > 20000;
            uint originalLength = (uint)body.Length;
            //if (compressed)
            //{
            //    byte[] cBody = ZipBody(body);
            //    if (cBody.Length < originalLength)
            //    {
            //        body = cBody;
            //        prefix = (byte)(1 + (1 << 7));
            //    }
            //}
            byte[] dest = new byte[1 + 4 + body.Length];
            dest[0] = prefix;
            BitConverter.GetBytes(body.Length).CopyTo(dest, 1); // length prefixer
            body.CopyTo(dest, 5);
            return dest;
        }

        static Dictionary<Type, ISerializer> _serializers = new Dictionary<Type, ISerializer>();
        protected virtual void OnSerializePayload(Stream outputStream)
        {
            ISerializer serializer = null;
            Type t = this.Payload.GetType();
            //try
            //{
            //    serializer = _serializers[t];
            //}
            //catch
            //{
            //    serializer = Altus.component.Application.Instance.Shell.GetComponents<ISerializer>()
            //    .Where(ims => ims.SupportsFormat(this.PayloadFormat) && ims.SupportsType(t)).FirstOrDefault();
            //    try
            //    {
            //        _serializers.Add(t, serializer);
            //    }
            //    catch { }
            //}
            lock (_serializers)
            {
                if (_serializers.ContainsKey(t))
                {
                    serializer = _serializers[t];
                }
                else
                {
                    serializer = Altus.Core.Component.App.Instance.Shell.GetComponents<ISerializer>()
                    .Where(ims => ims.SupportsFormat(this.PayloadFormat) && ims.SupportsType(t)).FirstOrDefault();
                    _serializers.Add(t, serializer);
                }
            }

            if (serializer == null)
                Logger.LogError(new SerializationException("Serializer could not be located for current MessagingContext"));
            else
            {
                StreamHelper.Copy(serializer.Serialize(this.Payload), outputStream);
            }
        }

        protected virtual void OnSerialize(Stream stream)
        {

        }

        public static IdentifiedMessage FromStream(Stream source)
        {
            IdentifiedMessage msg = null;
            byte msgType = (byte)source.ReadByte();
            msgType = (byte)(msgType >> 1);
            if (msgType == 1)
            {
                msg = new Message();
            }
            //else if (prefix == 2)
            //{
            //    msg = new ReplicationMessage();
            //}
            msg.Deserialize(source);
            return msg;
        }

        public void Deserialize(Stream source)
        {
            source.Position = 5;
            BinaryReader br = new BinaryReader(source);
            int headerCount = br.ReadInt32();
            for (int i = 0; i < headerCount; i++)
            {
                string header = br.ReadString();
                int idx = header.IndexOf(':');
                this.Headers.Add(header.Substring(0, idx), header.Substring(idx));
            }
            this.PayloadFormat = br.ReadString();
            this.PayloadType = br.ReadString();
            this.Id = br.ReadString();
            this.CorrelationId = br.ReadString();
            this.Sender = br.ReadString();
            this.Timestamp = new DateTime().AddTicks(br.ReadInt64());
            this.TTL = TimeSpan.FromTicks(br.ReadInt64());
            this.ServiceType = (ServiceType)br.ReadInt32();
            this.ServiceUri = br.ReadString();
            this.StatusCode = br.ReadInt32();
            OnDeserialize(source);
            long payloadLength = br.ReadInt64();
            MemoryStream ms = new MemoryStream(br.ReadBytes((int)payloadLength));
            ms.Position = 0;
            this.PayloadStream = ms;
        }

        public void FromByteArray(byte[] tcpBytes)
        {
            using (MemoryStream ms = new MemoryStream(tcpBytes))
            {
                this.Deserialize(ms);
            }
        }

        protected virtual void OnDeserialize(Stream source)
        {

        }
    }

    //public class ReplicationMessage : IdentifiedMessage
    //{
    //    public ReplicationMessage() : base("bin") { }
    //    public ReplicationMessage(string id) : base(id) { }

    //    protected override byte OnGetMessageType()
    //    {
    //        return 2;
    //    }
    //}
    [System.Serializable]
    public class Message : IdentifiedMessage
    {
        static byte[] _netId;
        static string _empty21 = string.Empty.PadRight(21);
        
        static Message()
        {
            try
            {
                _netId = ASCIIEncoding.ASCII.GetBytes(NodeAddress.Current.Network);
            }
            catch
            {
                _netId = ASCIIEncoding.ASCII.GetBytes("UNKNOWN");
            }
        }

        internal Message() : this("") {}

        public Message(string payloadFormat) : base(payloadFormat, Guid.NewGuid().ToString())
        {
            Recipients = new string[0];
            Sender = "";
            DeliveryGuaranteed = false;
            ServiceType = ServiceType.Directed;
            ReceivedBy = _empty21;
        }

        public Message(ServiceOperation sep)
            : base(sep.Format, Guid.NewGuid().ToString())
        {
            ServiceType = sep.ServiceType;
            Recipients = new string[0];
            Sender = NodeIdentity.NodeAddress;
            DeliveryGuaranteed = sep.ServiceType == ServiceType.RequestResponse;
            ReceivedBy = _empty21;
            ServiceUri = sep.ServiceUri;
            this.Payload = sep;
        }

        public Message(string payloadFormat, string serviceUri, ServiceType type, string sender) : base(payloadFormat)
        {
            ServiceType = type;
            Recipients = new string[0];
            Sender = sender;
            DeliveryGuaranteed = false;
            ReceivedBy = _empty21;
            ServiceUri = serviceUri;
        }

        public Message(string payloadFormat, string id, string serviceUri, ServiceType type, string sender)
            : base(payloadFormat)
        {
            Id = id;
            ServiceType = type;
            Recipients = new string[0];
            Sender = sender;
            DeliveryGuaranteed = false;
            ReceivedBy = _empty21;
            ServiceUri = serviceUri;
        }

        public Message(HttpListenerRequest request, IConnection connection) : base(StandardFormats.HTML, Guid.NewGuid().ToString())
        {
            this.Id = request.Headers["Altus-Id"];
            this.CorrelationId = request.Headers["Altus-CorrelationId"];
            this.ServiceUri = request.Url.ToString();
            this.Action = (Altus.Core.Processing.Action)Enum.Parse(typeof(Altus.Core.Processing.Action), request.HttpMethod);

            string mt = request.Headers["Altus-ServiceType"];
            if (!string.IsNullOrEmpty(mt))
                this.ServiceType = (ServiceType)Enum.Parse(typeof(ServiceType), mt);
            else
                this.ServiceType = ServiceType.RequestResponse;

            this.Sender = request.Headers["Altus-Sender"];

            List<string> recipients = new List<string>();
            for (int i = 0; i < request.Headers.Count; i++)
            {
                if (request.Headers.GetKey(i) == "Altus-Recipient")
                {
                    recipients.Add(request.Headers.Get(i));
                }
            }
            this.Recipients = recipients.ToArray();
           
            string gd = request.Headers["Altus-GuaranteedDelivery"];
            if (!string.IsNullOrEmpty(gd))
                this.DeliveryGuaranteed = bool.Parse(gd);
            else
                this.DeliveryGuaranteed = true;

            this.Headers.Add("Content-Type", connection.ContentType + "; charset=" + request.ContentEncoding.EncodingName);
            string format = request.Headers["Altus-PayloadFormat"];
            
            if (!string.IsNullOrEmpty(format))
                this.PayloadFormat = format;
            else
                this.PayloadFormat = StandardFormats.HTML;

            string payloadType = request.Headers["Altus-PayloadType"];
            if (string.IsNullOrEmpty(payloadType)
                && this.PayloadFormat.Equals(StandardFormats.HTML))
            {
                payloadType = typeof(HttpForm).FullName;
            }
            else if (string.IsNullOrEmpty(payloadType))
            {
                payloadType = typeof(ServiceParameterCollection).FullName;
            }

            this.PayloadType = payloadType;
            this.PayloadStream = new MemoryStream();
            if (this.Action == Processing.Action.POST)
            {
                StreamHelper.Copy(request.InputStream, this.PayloadStream);
            }
            else
            {
                string qs = "";
                bool isFirst = true;
                foreach (string key in request.QueryString.Keys)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        qs += "&";
                    qs += key + "=" + request.QueryString[key];
                }
                StreamHelper.Copy(request.ContentEncoding.GetBytes(qs), this.PayloadStream);
            }

            this.PayloadStream.Position = 0;
            connection.ConnectionAspects.Add("Cookies", request.Cookies);
            connection.ConnectionAspects.Add("QueryString", request.QueryString);
        }

        public Message(HttpWebResponse response, HttpConnection connection)
            : base(StandardFormats.HTML, Guid.NewGuid().ToString())
        {
            this.Id = response.Headers[AltusHeaders.Id];
            this.CorrelationId = response.Headers[AltusHeaders.CorrelationId];
            this.ServiceUri = response.ResponseUri.ToString();
            this.Action = (Altus.Core.Processing.Action)Enum.Parse(typeof(Altus.Core.Processing.Action), response.Method);

            string mt = response.Headers[AltusHeaders.MessageType];
            if (!string.IsNullOrEmpty(mt))
                this.ServiceType = (ServiceType)Enum.Parse(typeof(ServiceType), mt);
            else
                this.ServiceType = ServiceType.RequestResponse;

            this.Sender = response.Headers[AltusHeaders.Sender];

            List<string> recipients = new List<string>();
            for (int i = 0; i < response.Headers.Count; i++)
            {
                if (response.Headers.GetKey(i) == AltusHeaders.Recipient)
                {
                    recipients.Add(response.Headers.Get(i));
                }
            }
            this.Recipients = recipients.ToArray();

            string gd = response.Headers[AltusHeaders.GuaranteedDelivery];
            if (!string.IsNullOrEmpty(gd))
                this.DeliveryGuaranteed = bool.Parse(gd);
            else
                this.DeliveryGuaranteed = true;

            this.Headers.Add("Content-Type", connection.ContentType);

            string format = response.Headers[AltusHeaders.PayloadFormat];
            if (!string.IsNullOrEmpty(format))
                this.PayloadFormat = format;
            else
                this.PayloadFormat = StandardFormats.HTML;

            string payloadType = response.Headers[AltusHeaders.PayloadType];
            if (string.IsNullOrEmpty(payloadType)
                && this.PayloadFormat.Equals(StandardFormats.HTML))
            {
                payloadType = typeof(HttpForm).FullName;
            }
            else if (string.IsNullOrEmpty(payloadType))
            {
                payloadType = typeof(ServiceParameterCollection).FullName;
            }

            this.PayloadType = payloadType;
            this.PayloadStream = new MemoryStream();
            if (this.Action == Processing.Action.POST)
            {
                StreamHelper.Copy(response.GetResponseStream(), this.PayloadStream);
            }

            this.PayloadStream.Position = 0;
            connection.ConnectionAspects.Add("Cookies", response.Cookies);
        }

        

        protected override byte OnGetMessageType()
        {
            return 1;
        }

        internal string ReceivedBy { get; set; }
        public string[] Recipients { get; set; }
        [HttpResponseHeader]
        public bool DeliveryGuaranteed { get; set; }


        protected override void  OnSerialize(Stream stream)
        {
 	            base.OnSerialize(stream);

                BinaryWriter br = new BinaryWriter(stream);
                br.Write(ReceivedBy);
                br.Write(this.Recipients.Length);
                foreach (string r in Recipients)
                {
                    br.Write(r);
                }
                br.Write(Sender);
                br.Write(DeliveryGuaranteed);                 
        }

        protected override void OnDeserialize(Stream source)
        {
            base.OnDeserialize(source);

            BinaryReader br = new BinaryReader(source);
            this.ReceivedBy = br.ReadString().Trim();
            int rCount = br.ReadInt32();
            string[] recipients = new string[rCount];
            for (int i = 0; i < rCount; i++)
            {
                recipients[i] = br.ReadString();
            }
            this.Recipients = recipients;
            this.Sender = br.ReadString();
            this.DeliveryGuaranteed = br.ReadBoolean();
        }
    }

    public class AltusHeaders
    {
        public const string Id = "Altus-Id";
        public const string CorrelationId = "Altus-CorrelationId";
        public const string PayloadFormat = "Altus-PayloadFormat";
        public const string MessageType = "Altus-MessageType";
        public const string Sender = "Altus-Sender";
        public const string Recipient = "Altus-Recipient";
        public const string GuaranteedDelivery = "Altus-GuaranteedDelivery";
        public const string PayloadType = "Altus-PayloadType";
        public const string Timestamp = "Altus-Timestamp";
        public const string ServiceType = "Altus-ServoiceType";
        public const string ServiceUri = "Altus-ServiceUri";
        public const string StatusCode = "Altus-StatusCode";
        public const string TTL = "Altus-TTL";
        public const string Encoding = "Altus-Encoding";
        public const string Action = "Altus-Action";
        public const string All = 
            Id + ", " + 
            CorrelationId + ", " + 
            PayloadFormat + ", " + 
            MessageType + ", " + 
            Sender + ", " + 
            Recipient + ", " + 
            GuaranteedDelivery + ", " + 
            Timestamp + ", " + 
            ServiceType + ", " + 
            ServiceUri + ", " + 
            StatusCode + ", " + 
            TTL + ", " + 
            Encoding + ", " + 
            Action;
    }
}
