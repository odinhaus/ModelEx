using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Presentation.Wpf.Runtime;
using Altus.Core.Presentation.ViewModels;
using System.Windows;
using System.Xml;
using System.Reflection;
using System.Text.RegularExpressions;
using Altus.Core;
using Altus.Core.Dynamic;
using System.Dynamic;

[assembly:Component(ComponentType=typeof(XAMLTemplateEventProcessor<View>))]
namespace Altus.Core.Presentation.Wpf.Runtime
{
    public class XAMLTemplateEventProcessor<T> : InitializableComponent, IXAMLTemplateProcessor<T>
        where T : Extendable<T>
    {
        protected override bool OnInitialize(params string[] args)
        {
            this.Enabled = true;
            this.PendingEvents = new Dictionary<string, PendingEventAssignment>();
            return true;
        }

        public string PreProcess(string xaml, T item)
        {
            this.Item = item;
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(xaml);
            LoadReferences(xdoc.FirstChild);
            foreach(XmlNode xChild in xdoc.FirstChild.ChildNodes)
                WalkXmlTree(xChild);
            return xdoc.OuterXml;
        }

        private void LoadReferences(XmlNode xmlNode)
        {
            Dictionary<string, XamlReference> assemblies = new Dictionary<string, XamlReference>();
            XmlAttributeCollection nss = xmlNode.Attributes;
            Regex rNS = new Regex(@"(?<ns>(xmlns)):(?<name>\w+)");
            Regex rAssembly = new Regex(@"clr-namespace:(?<clrNs>[\w\.]+);assembly=(?<assembly>[\w\.]+)");
            foreach (XmlAttribute ns in nss)
            {
                Match m = rNS.Match(ns.Name);
                if (m.Success)
                {
                    if (ns.Value.Equals("http://schemas.microsoft.com/winfx/2006/xaml"))
                    {
                        XamlReference xr = new XamlReference()
                        { 
                            Key = m.Groups["name"].Value, 
                            Assembly = typeof(DependencyObject).Assembly, 
                            Namespace = typeof(DependencyObject).Namespace
                        };
                        assemblies.Add( xr.Key, xr );
                    }
                    else
                    {
                        Match asm = rAssembly.Match(ns.Value);
                        if (asm.Success)
                        {
                            XamlReference xr = new XamlReference()
                            {
                                Key = m.Groups["name"].Value,
                                Assembly = Assembly.Load(asm.Groups["assembly"].Value),
                                Namespace = asm.Groups["clrNs"].Value
                            };
                            assemblies.Add(xr.Key, xr);
                        }
                    }
                }
                else if (ns.Value.Equals("http://schemas.microsoft.com/winfx/2006/xaml/presentation"))
                {
                    XamlReference xr = new XamlReference()
                    {
                        Key = "",
                        Assembly = typeof(FrameworkElement).Assembly,
                        Namespace = typeof(FrameworkElement).Namespace
                    };
                    assemblies.Add(xr.Key, xr);
                }
            }

            this.References = assemblies;
        }

        private void WalkXmlTree(XmlNode xmlNode)
        {
            Type type;
            XamlReference xref;
            if (TryGetNodeType(xmlNode, out type, out xref))
            {
                EventInfo[] events = type.GetEvents(BindingFlags.Public | BindingFlags.Instance);
                List<string> found = new List<string>();
                string name = "n" + Guid.NewGuid().ToString().Replace("-","");
                string xName = this.References
                    .Where(xr => xr.Value.Namespace.Equals(typeof(DependencyObject).Namespace) && !xr.Value.Key.Equals(string.Empty)).First().Key + ":Name";
                bool hasName = false;

                if (((XmlElement)xmlNode).HasAttribute("Name"))
                {
                    name = ((XmlElement)xmlNode).GetAttribute("Name");
                    hasName = true;
                }
                else if (((XmlElement)xmlNode).HasAttribute(xName))
                {
                    name = ((XmlElement)xmlNode).GetAttribute(xName);
                    hasName = true;
                }

                foreach (XmlAttribute attr in xmlNode.Attributes)
                {
                    EventInfo theEvent = events.Where(e => e.Name.Equals(attr.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    if (theEvent != null)
                    {
                        this.PendingEvents.Add(name, new PendingEventAssignment()
                        {
                            Reference = xref,
                            Event = theEvent,
                            SourceType = type,
                            TargetHandler = attr.Value,
                            SourceName = name
                        });

                        found.Add(attr.Name);
                    }
                }

                if (!hasName && found.Count > 0)
                    ((XmlElement)xmlNode).SetAttribute(xName, name);

                foreach (string attName in found)
                {
                    ((XmlElement)xmlNode).RemoveAttribute(attName);
                }
            }

            foreach(XmlNode xChild in xmlNode.ChildNodes)
                WalkXmlTree(xChild);
        }

        private bool TryGetNodeType(XmlNode node, out Type type, out XamlReference xref)
        {
            try
            {
                string[] split = node.Name.Split(':');
                string typeName;

                if (split.Length == 1)
                {
                    xref = this.References[""];
                    typeName = split[0];
                }
                else
                {
                    xref = this.References[split[0]];
                    typeName = split[1];
                }

                return TryFindTypeByName(xref.Namespace, typeName, xref.Assembly, out type);// xref.Assembly.GetType(string.Format("{0}.{1}", xref.Namespace, typeName));
                //return type != null;
            }
            catch
            {
                type = null;
                xref = null;
                return false;
            }
        }

        static Dictionary<string, Type> _resolved = new Dictionary<string, Type>();

        private bool TryFindTypeByName(string rootNS, string typeName, Assembly source, out Type type)
        {
            type = null;
            lock (_resolved)
            {
                if (_resolved.TryGetValue(typeName, out type))
                {
                    return type != null;
                }
                else
                {
                    foreach (Type t in source.GetTypes().Where(t => !string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith(rootNS)))
                    {
                        if (t.Name.Equals(typeName))
                        {
                            type = t;
                            return true;
                        }
                    }
                    _resolved.Add(typeName, type);
                    return false;
                }
            }
        }

        public void PostProcess(DependencyObject root, T item)
        {
            this.Item = item;
            this.WalkLogicalTree(root as FrameworkElement);
        }

        private void WalkLogicalTree(DependencyObject obj)
        {
            if (obj == null) return;

            AttachEvents(obj);

            foreach (object elm in LogicalTreeHelper.GetChildren(obj))
                WalkLogicalTree(elm as DependencyObject);
        }

        private void AttachEvents(DependencyObject obj)
        {
            FrameworkElement fe = obj as FrameworkElement;
            if (fe == null) return;

            string name = fe.Name;
            if (this.PendingEvents.ContainsKey(name))
            {
                this.AttachEvents(fe, this.PendingEvents[name]);
            }
        }

        private void AttachEvents(FrameworkElement fe, PendingEventAssignment pendingEventAssignment)
        {
            MethodInfo mi;
            object target;
            if (this.Item.TryGetEventMethod((object)fe, pendingEventAssignment.Event, pendingEventAssignment.TargetHandler, out mi, out target))
            {
                Delegate del = Delegate.CreateDelegate(pendingEventAssignment.Event.EventHandlerType, target, mi);
                pendingEventAssignment.Event.AddEventHandler(fe, del);
            }
        }

        public bool Enabled { get; set; }

        public T Item { get; private set; }

        private Dictionary<string, XamlReference> References { get; set; }

        private Dictionary<string, PendingEventAssignment> PendingEvents { get; set; }

        private class XamlReference
        {
            public string Key;
            public string Namespace;
            public Assembly Assembly;
        }

        private class PendingEventAssignment
        {
            public XamlReference Reference;
            public Type SourceType;
            public EventInfo Event;
            public string TargetHandler;
            public string SourceName;
        }
    }
}
