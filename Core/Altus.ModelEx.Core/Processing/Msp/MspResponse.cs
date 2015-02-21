using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Messaging;
using Altus.Core.Messaging.Http;
using Altus.Core.Serialization;
using Altus.Core.Streams;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using Altus.Core.Component;
using System.Net;

namespace Altus.Core.Processing.Msp
{
    public class MspResponse : ServiceResponse
    {
        private string _valueTemplateReg = @"<%\s*=\s*(?<object>[\w\.]+?).(?<value>\w+)\s*%>";
        
        private bool _useTemplate = true;
        private bool _useEncoding = true;
        private bool _allowBinaryWrite = true;
        private bool _allowTextWrite = true;
        private bool _errorOccurred = false;
        private MemoryStream _binaryData = new MemoryStream();
        private MemoryStream _textData = new MemoryStream();
        Regex _valueR;

        public MspResponse(HttpConnection connection)
        {
            this.Connection = connection;
            this._valueR = new Regex(_valueTemplateReg, RegexOptions.IgnoreCase);
            this.Cookies = new CookieCollection();
        }

        /// <summary>
        /// This method adds binary data to the output buffer with no character encoding.  Any HTML page template content will be 
        /// ignored.
        /// </summary>
        /// <param name="data"></param>
        public void WriteBinary(byte[] data)
        {
            if (_allowBinaryWrite)
            {
                this._binaryData.Write(data, 0, data.Length);
                _useTemplate = false;
                _useEncoding = false;
                _allowTextWrite = false;
            }
            else
                throw (new InvalidOperationException("Binary writing cannot be used in combination with writing HTML to the output stream."));
        }

        /// <summary>
        /// This method writes formatted HTML directly to the output buffer.  Any formatted HTML page template content
        /// will be ignored and replaced by this data.
        /// </summary>
        /// <param name="html"></param>
        public void WriteText(string html)
        {
            if (_allowTextWrite)
            {
                byte[] data = SerializationContext.TextEncoding.GetBytes(html);
                this._textData.Write(data, 0, data.Length);
                _useTemplate = false;
                _allowBinaryWrite = false;
            }
            else
                throw (new InvalidOperationException("HTML writing cannot be used in combination with writing binary content to the output stream."));
        }

        /// <summary>
        /// This method serializes the provided source object according the current ServiceContext ResponseFormat specified, and adds the result
        /// to the output stream.  Any formatted HTML page template content will be ignored and replaced by the serialized data.
        /// </summary>
        /// <param name="source"></param>
        public void WriteObject(object source)
        {
            if (_allowTextWrite)
            {
                ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>()
                    .Where(s => s.SupportsFormat(ServiceContext.Current.ResponseFormat) && s.SupportsType(source.GetType())).FirstOrDefault();
                byte[] serialized;
                if (serializer == null)
                    serialized = SerializationContext.TextEncoding.GetBytes(source.ToString());
                else
                    serialized = serializer.Serialize(source);
                this._textData.Write(serialized, 0, serialized.Length);
                _useTemplate = false;
                _allowBinaryWrite = false;
            }
            else
                throw (new InvalidOperationException("HTML writing cannot be used in combination with writing binary content to the output stream."));
        }


        /// <summary>
        /// redirects the response to url specified, and then immediately ends the current connection
        /// </summary>
        /// <param name="url"></param>
        public void Redirect(string url)
        {
            ((HttpConnection)this.Connection).Response.Redirect(url);
            ServiceContext.Current.Terminate();
        }

        public string Html
        {
            get
            {
                if (_useTemplate)
                {
                    string html = "";
                    if (this.UseErrorTemplate)
                    {
                        html = ReplaceElements(this.HtmlErrorTemplate, this.HtmlErrorElements);
                    }
                    else
                    {
                        html = ReplaceElements(this.HtmlTemplate, this.HtmlElements);
                    }
                    
                    html = ReplaceValues(html);

                    return html;
                }
                else
                {
                    this._binaryData.Position = 0;
                    string html = SerializationContext.TextEncoding.GetString(StreamHelper.GetBytes(this._binaryData));
                    return html;
                }
            }
        }

        private string ReplaceElements(string htmlSource, HtmlElementList elements)
        {
            foreach (HtmlElement elm in elements)
            {
                htmlSource = htmlSource.Replace(elm.OuterHtmlOriginal, elm.OuterHtml);
            }
            return htmlSource;
        }

        private string ReplaceValues(string html)
        {
            Match m = _valueR.Match(html);
            while (m.Success)
            {
                object resolvedObject = null;
                if (m.Groups["object"].Value.Equals("this", StringComparison.InvariantCultureIgnoreCase))
                {
                    Page page = ((MspContext)ServiceContext.Current).Page;
                    resolvedObject = page;
                }
                else
                {
                    resolvedObject = App.Instance.Shell.GetComponent(m.Groups["object"].Value);
                }

                if (resolvedObject != null)
                {
                    string memberName = m.Groups["value"].Value.Replace("(", "").Replace(")", "");
                    MemberInfo member = resolvedObject.GetType().GetMember(memberName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
                    if (member != null)
                    {
                        if (!member.DeclaringType.Equals(((MspContext)ServiceContext.Current).Page.GetType()))
                        {
                            member = member.DeclaringType.GetMember(memberName,
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
                        }

                        object value = null;

                        if (member is PropertyInfo)
                        {
                            value = ((PropertyInfo)member).GetValue(resolvedObject, null);
                        }
                        else if (member is FieldInfo)
                        {
                            value = ((FieldInfo)member).GetValue(resolvedObject);
                        }
                        else if (member is MethodInfo)
                        {
                            value = ((MethodInfo)member).Invoke(resolvedObject, null);
                        }

                        html = html.Replace(m.Value, value.ToString());
                    }
                }
                m = _valueR.Match(html);
            }
            return html;
        }

        public object HtmlEncoded
        {
            get
            {
                if (_useEncoding && _textData.Length == 0)
                {
                    return this.Html;
                }
                else if (_useEncoding && _textData.Length > 0)
                {
                    _textData.Position = 0;
                    return SerializationContext.TextEncoding.GetString(StreamHelper.GetBytes(_textData));
                }
                else
                {
                    _binaryData.Position = 0;
                    return _binaryData.GetBytes((int)_binaryData.Length);
                }
            }
        }
        /// <summary>
        /// Provides raw access to the Html template for the current page handler
        /// </summary>
        public string HtmlTemplate { get; private set; }
        /// <summary>
        /// Provides access to all the HTML Elements within the page template have an Html Id attribute
        /// </summary>
        public HtmlElementList HtmlElements { get; private set; }

        /// <summary>
        /// Provides raw access to the error template for the current page handler
        /// </summary>
        public string HtmlErrorTemplate { get; private set; }
        /// <summary>
        /// Provides access to all the HTML Elements within the page template have an Html Id attribute
        /// </summary>
        public HtmlElementList HtmlErrorElements { get; private set; }

        public CookieCollection Cookies { get; private set; }

        private HttpConnection Connection { get; set; }

        public string ContentType
        {
            get
            {
                return Connection.ContentType;
            }
            set
            {
                Connection.ContentType = value;
            }
        }

        public Encoding TextEncoding
        {
            get
            {
                return Connection.TextEncoding;
            }
            set
            {
                Connection.TextEncoding = value;
            }
        }

        internal void LoadPageTemplates(string htmlTemplate, string errorTemplate)
        {
            this.HtmlTemplate = htmlTemplate;
            if (!string.IsNullOrEmpty(htmlTemplate))
            {
                this.HtmlElements = new HtmlElementList(this.HtmlTemplate);
            }
            else
            {
                this.HtmlElements = new HtmlElementList("");
            }

            this.HtmlErrorTemplate = errorTemplate;
            if (!string.IsNullOrEmpty(errorTemplate))
            {
                this.HtmlErrorElements = new HtmlElementList(this.HtmlErrorTemplate);
            }
            else
            {
                this.HtmlErrorTemplate = "An error occurred.";
                this.HtmlErrorElements = new HtmlElementList("");
            }
        }

        internal bool UseErrorTemplate
        {
            get { return _errorOccurred; }
            set { _errorOccurred = value; }
        }

        public ServiceOperation OperationResponse { get; set; }

        public void Headers(string name, string value)
        {
            this.Connection.Response.Headers.Add(name, value);
        }
    }
}
