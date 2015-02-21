using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Pipeline;
using Altus.Core.Messaging.Http;
using Altus.Core.Messaging;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using Altus.Core;
using Altus.Core.Serialization;
using Altus.Core.Component;
using Altus.Core.Diagnostics;
using Altus.Core.Streams;

namespace Altus.Core.Processing.Msp
{
    public class MspOperationProxy : ServiceOperationProxy
    {
        public MspOperationProxy(Message message,  ServiceOperationAttribute attrib, IConnection connection) : base(message, attrib, connection) { }

        protected override object OnCreateTarget(ServiceContext request)
        {
            Page target = (Page)this.Attribute.Target;
            target.MspContext = (MspContext)this.ServiceContext;
            this.SetPageTemplates(target);
            target.Initialize(this.Attribute.Name);
            return target;
        }

        protected override object OnProcess(ServiceContext request, object target, object[] args)
        {
            object ret = base.OnProcess(request, target, args);
            ((MspContext)this.ServiceContext).Response.OperationResponse =
                OnBuildResponse(ret, args, Attribute.Method.ReturnType.Equals(typeof(void)));
            return ret;
        }

        protected override ServiceOperation OnBuildResponse(object returnValue, object[] inputArgs, bool returnIsVoid)
        {
            ServiceOperation operation = base.OnBuildResponse(returnValue, inputArgs, returnIsVoid);
            object result = ((MspContext)this.ServiceContext).Response.HtmlEncoded;
            operation.Parameters.Add(new ServiceParameter("Response", result.GetType().FullName, ParameterDirection.Out) { Value = result });
            return operation;
        }

        protected void SetPageTemplates(Page page)
        {
            string pagePath = "";
            string errorPath = "";

            Type pageType = page.GetType();
            pagePath = string.IsNullOrEmpty(((MspEndPointAttribute)this.Attribute.ServiceEndPoint).HtmlTemplatePath) 
                ? pageType.Namespace 
                : ((MspEndPointAttribute)this.Attribute.ServiceEndPoint).HtmlTemplatePath;
            errorPath = string.IsNullOrEmpty(((MspEndPointAttribute)this.Attribute.ServiceEndPoint).HtmlErrorTemplatePath) 
                ? pageType.Namespace 
                : ((MspEndPointAttribute)this.Attribute.ServiceEndPoint).HtmlErrorTemplatePath;

            Assembly pageAsm = App.Instance.API.EntryAssembly;
            string rootNs = pageAsm.GetName().Name;
            
            pagePath = pagePath.ToLower().Replace(rootNs.ToLower(),"");
            if (pagePath.StartsWith(".")) pagePath = pagePath.Substring(1, pagePath.Length - 1);
            pagePath = pagePath.Replace(".", @"\");
            pagePath = Path.Combine(Context.GlobalContext.CodeBase, pagePath);
            

            errorPath = Path.Combine(pagePath, pageType.Name + ".error.msp");
            pagePath = Path.Combine(pagePath, pageType.Name + ".msp");

            //<!--#include file = "filename.ext" -->
            string html = File.Exists(pagePath) ? File.ReadAllText(pagePath) : "";
            string error = File.Exists(errorPath) ? File.ReadAllText(errorPath) : "";

            Regex r = new Regex(@"<!--\s*#include\s+file\s*=\s*\""{0,1}\s*(?<file>[\w\.]+)\s*\""{0,1}\s*-->", 
                RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);

            if (!string.IsNullOrEmpty(html))
            {
                // look for server-side includes
                Match m = r.Match(html);
                while (m.Success)
                {
                    string file = m.Groups["file"].Value;
                    if (!File.Exists(file))
                    {
                        file = Path.Combine(Path.GetDirectoryName(pagePath), file);
                    }

                    if (File.Exists(file))
                    {
                        html = html.Replace(m.ToString(), File.ReadAllText(file));
                    }
                    else
                    {
                        html = html.Replace(m.ToString(), "<!-- SSIFile=\"" + m.Groups["file"].Value + "\" Status=\"NotFound\"-->");
                    }

                    m = r.Match(html); // this will call nested includes that are pulled in from the replacement
                }
            }

            if (!string.IsNullOrEmpty(error))
            {
                // look for server-side includes
                Match m = r.Match(error);
                while (m.Success)
                {
                    string file = m.Groups["file"].Value;
                    if (!File.Exists(file))
                    {
                        file = Path.Combine(Path.GetDirectoryName(pagePath), file);
                    }

                    if (File.Exists(file))
                    {
                        error = error.Replace(m.ToString(), File.ReadAllText(file));
                    }
                    else
                    {
                        error = error.Replace(m.ToString(), "<!-- SSIFile=\"" + m.Groups["file"].Value + "\" Status=\"NotFound\"-->");
                    }

                    m = r.Match(error); // this will call nested includes that are pulled in from the replacement
                }
            }

            page.Response.LoadPageTemplates(html, error);
        }

        protected override IPipeline<ServiceContext> OnCreatePipeline()
        {
            return new MspPipeline(this);
        }

        protected override ServiceContext OnCreateServiceContext(Message message, ServiceOperation operation, ServiceOperationAttribute attrib, IPipeline<ServiceContext> pipeline, IConnection connection)
        {
            return new MspContext(message, operation, attrib, (MspPipeline)pipeline, connection, new MspRequest(), new MspResponse((HttpConnection)connection));
        }
    }
}
