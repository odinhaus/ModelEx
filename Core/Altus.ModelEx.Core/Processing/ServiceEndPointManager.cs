using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using System.ComponentModel;
using System.Reflection;
using Altus.Core.Processing.Rpc;
using Altus.Core.Processing.Msp;
using System.Text.RegularExpressions;
using Altus.Core.Diagnostics;
using Altus.Core.Serialization;
using Altus.Core.Messaging;

[assembly: Component(
    ComponentType = typeof(Altus.Core.Processing.ServiceEndPointManager),
    Name = "ServiceEndPointManager",
    Dependencies = new string[]{"UdpHost, TcpHost", "HttpHost"})]
namespace Altus.Core.Processing
{
    public class ServiceEndPointManager : InitializableComponent
    {
        Dictionary<string, ServiceOperationAttribute> _mappedOperations = new Dictionary<string, ServiceOperationAttribute>();
        List<ServiceOperationAttribute> _services = new List<ServiceOperationAttribute>();
        
        protected override bool OnInitialize(params string[] args)
        {
            match = new Regex(reg);
            foreach (IComponent component in App.Instance.Shell.Components)
            {
                CreateServiceEndPoints(component);
            }
            App.Instance.Shell.ComponentChanged += new CompositionContainerComponentChangedHandler(Shell_ComponentChanged);
            return true;
        }

        void Shell_ComponentChanged(object sender, CompositionContainerComponentEventArgs e)
        {
            if (e.Change == CompositionContainerComponentChange.Add)
            {
                this.CreateServiceEndPoints(e.Component);
            }
            else
            {

            }
        }


        static string reg = @"(?<protocol>[\w\.\*]*)://(?<node>[\w\.\*]*)(?<portExists>:{0,1}(?<port>[\d\*]*))/(?<application>[\w\.\*]*)/(?<object>[^\(\)\[\]]*)";
        static Regex uriReg = new Regex(reg + @"(?<hasOperation>\({0,1}(?<operation>[\w\.\*]*)\){0,1})(?<hasFormat>\[{0,1}(?<format>[\w\.\*]*)\]{0,1})");

        public static bool TryParseServiceUri(string uri, out string protocol, out string node, out int port, out string application, out string objectPath, out string operation, out string format)
        {
            Match matched = uriReg.Match(uri);

            protocol = node = application = objectPath = operation = format = string.Empty;
            port = 0;

            if (matched.Success)
            {
                protocol = matched.Groups["protocol"].Value;
                node = matched.Groups["node"].Value;
                application = matched.Groups["application"].Value;
                objectPath = matched.Groups["object"].Value;
                if (string.IsNullOrEmpty(matched.Groups["hasOperation"].Value))
                {
                    operation = "*";
                }
                else
                {
                    operation = matched.Groups["operation"].Value;
                }

                if (!string.IsNullOrEmpty(matched.Groups["hasFormat"].Value))
                {
                    format = matched.Groups["format"].Value;
                }
                else
                {
                    format = StandardFormats.PROTOCOL_DEFAULT; // use default for protocol
                }

                if (!string.IsNullOrEmpty(matched.Groups["portExists"].Value))
                {
                    int.TryParse(matched.Groups["port"].Value, out port);
                }
            }

            return matched.Success;
        }

        Regex match;
        public virtual void CreateServiceEndPoints(object component)
        {
            List<ServiceEndPointAttribute> attribs = new List<ServiceEndPointAttribute>();
            List<object> attribsObj = component.GetType().GetCustomAttributes(true).Where(o => o.GetType().IsSubclassOf(typeof(ServiceEndPointAttribute))).ToList();

            foreach (object o in attribsObj)
            {
                attribs.Add((ServiceEndPointAttribute)o);
            }

            if (attribs != null && attribs.Count > 0)
            {
                foreach (ServiceEndPointAttribute attrib in attribs)
                {
                    this.CreateServiceEndPoint(component, attrib);
                    foreach(ServiceRoute route in attrib.Routes)
                    {
                        foreach (ServiceOperationAttribute op in attrib.Operations)
                        {
                            CreateServiceOperation(op, route);
                        }
                    }
                }
            }
        }

        public virtual void CreateServiceEndPoint(object component, ServiceEndPointAttribute endPoint)
        {
            endPoint.Target = component;
            MethodInfo[] operations = component.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (operations != null && operations.Length > 0)
            {
                List<ServiceOperationAttribute> list = new List<ServiceOperationAttribute>();
                foreach (MethodInfo operation in operations)
                {
                    List<ServiceOperationAttribute> attribs = new List<ServiceOperationAttribute>();
                    List<object> attribsObj = operation.GetCustomAttributes(true).Where(o => o.GetType().IsSubclassOf(typeof(ServiceOperationAttribute))).ToList();

                    foreach (object o in attribsObj)
                    {
                        attribs.Add((ServiceOperationAttribute)o);
                    }

                    if (attribs != null && attribs.Count > 0)
                    {
                        foreach (ServiceOperationAttribute attrib in attribs)
                        {
                            attrib.Method = operation;
                            attrib.Target = component;
                            attrib.ServiceEndPoint = endPoint;
                            if (attrib.Name == null)
                                attrib.Name = operation.Name;
                        }
                        list.AddRange(attribs);
                    }
                }
                endPoint.Operations = list.ToArray();
            }
        }

        public virtual void CreateServiceOperation(ServiceOperationAttribute op, ServiceRoute route)
        {
            string opRoute = route.RoutePattern + op.Name;

            Match m = match.Match(route.RoutePattern);

            if (m.Success)
            {
                string prot = m.Groups["protocol"].Value.Replace("*", @"[\w\.]*");
                string node = m.Groups["node"].Value.Replace("*", @"[\w\.]*");
                string port = "";

                if (!string.IsNullOrEmpty(m.Groups["portExists"].Value)
                    && !m.Groups["port"].Value.Equals("*"))
                {
                    port = "(?<portExists>:{1}(?<port>" + m.Groups["port"].Value.Replace("*", @"[\d]*") + "))";
                }
                else
                {
                    port = @"(?<portExists>:{0,1}(?<port>[\d]*))";
                }

                string obj = m.Groups["object"].Value.Replace("*", @"[\w\./]*");
                string application = m.Groups["application"].Value.Replace("*", @"[\w\.]*");
                string oper = op.Name;
                string operReg;
                if (op.IsDefault)
                {
                    operReg = @"(?<hasOperation>($|\((?<operation>(\*|" + oper + @"))\)))($|\[|\?)";
                }
                else
                {
                    operReg = @"\((?<operation>" + oper + @")\)";
                }
                string fmt = op.ReturnFormat;
                string pat = @"(?<protocol>"
                    + prot
                    + @")://(?<node>"
                    + node
                    + @")"
                    + port
                    + @"/(?<application>"
                    + application
                    + @")"
                    + @"/(?<object>"
                    + obj
                    + @")"
                    + operReg;
                if (fmt != StandardFormats.PROTOCOL_DEFAULT)
                {
                    pat += @"\[(?<format>"
                    + fmt
                    + @")\]";
                }
                op.MatchExpression = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);
                _services.Add(op);
                _services.Sort(new Comparison<ServiceOperationAttribute>(
                    delegate(ServiceOperationAttribute att1, ServiceOperationAttribute att2)
                    {
                        return att1.Priority.CompareTo(att2.Priority);
                    }));
            }
            else
            {
                //throw (new InvalidOperationException("Invalid route syntax provided: " + route.RoutePattern));
                op.MatchExpression = new Regex(route.RoutePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);
                _services.Add(op);
                _services.Sort(new Comparison<ServiceOperationAttribute>(
                    delegate(ServiceOperationAttribute att1, ServiceOperationAttribute att2)
                    {
                        return att1.Priority.CompareTo(att2.Priority);
                    }));
            }
        }

        public ServiceOperationProxy GetProxy(Message message, IConnection connection)
        {
            ServiceOperationProxy proxy;
            if (TryGetProxy(message, connection, out proxy)) return proxy;
            else throw (new InvalidOperationException("Service end point handler not found for Service Uri: " + message.ServiceUri));
        }
        public bool TryGetProxy(Message message, IConnection connection, out ServiceOperationProxy proxy)
        {
            proxy = null;
            ServiceOperationAttribute opAttrib = null;
            string p, n, app, obj, op, fmt;
            int port;
            ServiceEndPointManager.TryParseServiceUri(message.ServiceUri, out p, out n, out port, out app, out obj, out op, out fmt);
            lock (_mappedOperations)
            {
                if (_mappedOperations.ContainsKey(message.ServiceUri))
                {
                    opAttrib = _mappedOperations[message.ServiceUri];
                }
                else
                {
                    List<ServiceOperationAttribute>.Enumerator en = _services.GetEnumerator();
                    while (en.MoveNext())
                    {
                        if (en.Current.MatchExpression.Match(message.ServiceUri).Success
                            && (en.Current.ReturnFormat.Equals(StandardFormats.PROTOCOL_DEFAULT) ||
                            en.Current.ReturnFormat.Equals(fmt, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            opAttrib = en.Current;
                            _mappedOperations.Add(message.ServiceUri, opAttrib);
                            break;
                        }
                    }
                }
            }

            if (opAttrib != null)
            {
                try
                {
                    proxy = (ServiceOperationProxy)Activator.CreateInstance(opAttrib.ServiceEndPoint.InvocationProxyType, message, opAttrib, connection);
                }
                catch
                {
                    lock(_mappedOperations)
                    {
                        _mappedOperations[message.ServiceUri] = null;
                    }
                }
            }

            return proxy != null;
        }

        public bool HasProxy(string serviceUri, out ServiceOperationAttribute provider)
        {
            ServiceOperationAttribute opAttrib = null;
            provider = null;
            string p, n, app, obj, op, fmt;
            int port;
            ServiceEndPointManager.TryParseServiceUri(serviceUri, out p, out n, out port, out app, out obj, out op, out fmt);
            lock (_mappedOperations)
            {
                if (_mappedOperations.ContainsKey(serviceUri))
                {
                    return _mappedOperations[serviceUri] != null;
                }
                else
                {
                    List<ServiceOperationAttribute>.Enumerator en = _services.GetEnumerator();
                    while (en.MoveNext())
                    {
                        if (en.Current.MatchExpression.Match(serviceUri).Success
                            && (en.Current.ReturnFormat.Equals(StandardFormats.PROTOCOL_DEFAULT) ||
                            en.Current.ReturnFormat.Equals(fmt, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            opAttrib = en.Current;
                            _mappedOperations.Add(serviceUri, opAttrib);
                            provider = opAttrib;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
