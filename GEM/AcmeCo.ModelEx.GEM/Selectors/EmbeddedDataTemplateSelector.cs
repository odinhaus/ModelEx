using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using Altus.Core;
using Altus.Core.Diagnostics;
using Altus.Core.Presentation.ViewModels;
using Altus.Core.Presentation.Wpf.Runtime;
using Altus.Core.Streams;
using Altus.GEM.ViewModels;
using Altus.Core.Presentation.Wpf;
using System.Windows.Controls;

namespace Altus.GEM.Selectors
{
    public class EmbeddedDataTemplateSelector : ViewTemplateSelector
    {
        public EmbeddedDataTemplateSelector()
        {
        }

        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            if (item is WPFView)
            {
                IDataTemplateLoader dtl = new EmbeddedDataTemplateXamlReader(this);
                string xaml = dtl.LoadViewTemplate((WPFView)item, container);

                ((WPFView)item).SetDependencyObject(container);

                return CreateTemplate(xaml, (WPFView)item, container);
            }
            else if (item is Entity)
            {
                EmbeddedDataTemplateXamlReader dtl = new EmbeddedDataTemplateXamlReader(this);
                string xaml = dtl.LoadEntityTemplate((Entity)item, container);

                ((Entity)item).SetDependencyObject(container);

                return CreateTemplate(xaml, (Entity)item, container);

            }
            return base.SelectTemplate(item, container);
        }

        public DataTemplate CreateTemplate(string xaml, Entity item, System.Windows.DependencyObject container)
        {
            foreach (IXAMLTemplateProcessor<Entity> processor in Altus.Core.Component.App.Instance.Shell.GetComponents<IXAMLTemplateProcessor<Entity>>()
                .Where(p => p.Enabled))
            {
                xaml = processor.PreProcess(xaml, (Entity)item);
            }

            DataTemplate dt = string.IsNullOrEmpty(xaml) ? null : XamlReader.Load(new TextStream(xaml)) as DataTemplate;
            FrameworkElement element = container as FrameworkElement;
            element.Loaded += new RoutedEventHandler(VisualElement_EntityLoaded);

            return dt;
        }

        protected override string OnGetItemWindowName(object item)
        {
            if (item is Entity) return ((Entity)item).Explorer.WindowName;
            return base.OnGetItemWindowName(item);
        }

        protected void VisualElement_EntityLoaded(object sender, RoutedEventArgs e)
        {
            Entity item = ((FrameworkElement)sender).DataContext as Entity;
            foreach (IXAMLTemplateProcessor<Entity> processor in Altus.Core.Component.App.Instance.Shell.GetComponents<IXAMLTemplateProcessor<Entity>>()
                .Where(p => p.Enabled))
            {
                int children = VisualTreeHelper.GetChildrenCount((DependencyObject)sender);
                for (int i = 0; i < children; i++)
                {
                    processor.PostProcess(VisualTreeHelper.GetChild((DependencyObject)sender, 0), item);
                }
            }
        }

        private class EmbeddedDataTemplateXamlReader : IDataTemplateLoader
        {
            static Dictionary<string, string> _xamlCache = new Dictionary<string, string>();
            public EmbeddedDataTemplateXamlReader(ViewTemplateSelector selector)
            {
                this.Selector = selector;
            }

            public string LoadViewTemplate(View view, System.Windows.DependencyObject container)
            {
                string xaml;
                if (!TryGetEmbeddedScript(Selector.GetItemWindowName(view), view.Name, view.CurrentSize, out xaml))
                {
                    Logger.LogWarn(string.Format("View template not defined for Window: {0} View: {1} Size: {2}",
                        view.WindowName,
                        view.Name,
                        view.CurrentSize));
                }
                return xaml;
            }

            public string LoadEntityTemplate(Entity item, System.Windows.DependencyObject container)
            {
                string xaml = null;

  
                string key = Selector.GetItemWindowName(item) + item.InstanceType + item.CurrentSize;

                if (_xamlCache.ContainsKey(key)) return _xamlCache[key];

                if (!TryGetEmbeddedScript(Selector.GetItemWindowName(item), item.InstanceType, item.CurrentSize, out xaml))
                {
                    if (!TryGetEmbeddedScript(Selector.GetItemWindowName(item), "Entity", item.CurrentSize, out xaml))
                    {
                        Logger.LogWarn(string.Format("View template not defined for Window: {0} View: {1} Size: {2}",
                            Selector.GetItemWindowName(item),
                            item.Name,
                            item.CurrentSize));
                    }
                }
                _xamlCache.Add(key, xaml);
                
                
                return xaml;
            }


            private bool TryGetEmbeddedScript(string windowName, string viewName, string viewSize, out string xaml)
            {
                bool found = false;
                xaml = null;
                //xaml = string.Format(xaml, viewName, viewSize);
                try
                {
                    HashSet<Assembly> checkedAssemblies = new HashSet<Assembly>();
                    Assembly assembly = Context.CurrentContext.CurrentApp.PrimaryAssembly 
                        ?? typeof(EmbeddedDataTemplateXamlReader).Assembly;
                    string resourceName = GetResourceKey(assembly, windowName, viewName, viewSize);
                    found = TryGetManifestResource(resourceName, assembly, out xaml);
                    if (!found)
                    {
                        found = false;
                        checkedAssemblies.Add(assembly);
                        assembly = this.GetType().Assembly;
                        resourceName = GetResourceKey(assembly, windowName, viewName, viewSize);
                        found = TryGetManifestResource(resourceName, assembly, out xaml);
                        if (!found)
                        {
                            checkedAssemblies.Add(assembly);
                            StackTrace trace = new StackTrace(0);

                            for (int i = 0; i < trace.FrameCount; i++)
                            {
                                StackFrame frame = trace.GetFrame(i);
                                if (frame.GetMethod().DeclaringType != null)
                                {
                                    assembly = frame.GetMethod().DeclaringType.Assembly;
                                    if (!checkedAssemblies.Contains(assembly))
                                    {
                                        resourceName = GetResourceKey(assembly, windowName, viewName, viewSize);
                                        found = TryGetManifestResource(resourceName, assembly, out xaml);
                                        if (found)
                                            break;
                                        checkedAssemblies.Add(assembly);
                                    }
                                }
                            }
                        }
                    }
                }
                catch {}
                if (found)
                    xaml = ExtractXaml(xaml, viewName, viewSize);
                return found;
            }

            private string ExtractXaml(string xaml, string viewName, string viewSize)
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(xaml);
                XmlNamespaceManager xnsm = new XmlNamespaceManager(xDoc.NameTable);
                XmlElement xResources = xDoc.FirstChild as XmlElement;

                AddNamespaces(xResources, xnsm);

                XmlElement xTemplate = xDoc.SelectSingleNode("p:DataTemplate[@x:Key='" + viewName + "_" + viewSize + "']", xnsm) as XmlElement;
                if (xTemplate == null)
                {
                    xTemplate = xDoc.SelectSingleNode("p:DataTemplate", xnsm) as XmlElement;
                }
                return xTemplate.OuterXml;
            }

            private void AddNamespaces(XmlElement xResources, XmlNamespaceManager xnsm)
            {
                foreach(XmlAttribute att in xResources.Attributes)
                {
                    if (att.Name.StartsWith("xmlns"))
                    {
                        xnsm.AddNamespace(att.Prefix == "xmlns" ? att.LocalName : att.Prefix, att.Value);
                        if (att.Value.Equals("http://schemas.microsoft.com/winfx/2006/xaml/presentation"))
                            xnsm.AddNamespace("p", att.Value);
                        else if (att.Value.Equals("http://schemas.microsoft.com/winfx/2006/xaml"))
                            xnsm.AddNamespace("x", att.Value);
                    }
                }
            }

            private string GetResourceKey(Assembly assembly, string windowName, string viewName, string viewSize)
            {
                return assembly.GetName().Name + ".Views.DataTemplates." + windowName + "." + viewName + "_" + viewSize + ".xaml";
            }

            private bool TryGetManifestResource(string resourceName, Assembly assembly, out string resource)
            {
                if (!_xamlCache.TryGetValue(resourceName, out resource))
                {
                    resource = resourceName;
                    try
                    {
                        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream == null)
                            {
                                foreach (string resName in assembly.GetManifestResourceNames())
                                {
                                    if (TryGetResourceReaderResource(resName, resourceName, assembly, out resource))
                                        return true;
                                }

                            }
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                resource = reader.ReadToEnd();
                                return true;
                            }
                        }
                    }
                    catch { return false; }
                }
                else return true;
            }

            private bool TryGetResourceReaderResource(string resName, string resourceName, Assembly assembly, out string resource)
            {
                resource = string.Empty;
                try
                {
                    using(Stream resStream = assembly.GetManifestResourceStream(resName))
                    {
                        using(ResourceReader rdr = new ResourceReader(resStream))
                        {
                            string resType;
                            byte[] resData;
                            rdr.GetResourceData(
                                resourceName.Replace(resName.Replace(".g.resources", "") + ".", "").ToLowerInvariant().Replace(".", "/").Replace("/xaml", ".xaml"), 
                                out resType, out resData);
                            int length = BitConverter.ToInt32(resData, 0);
                            using (StreamReader sRdr = new StreamReader(new MemoryStream(resData, sizeof(Int32), length)))
                            {
                                resource = sRdr.ReadToEnd();
                            }
                            return true;
                        }
                    }
                }
                catch { return false; }

            }


            #region IComponent

            public event EventHandler Disposed;

            public System.ComponentModel.ISite Site
            {
                get;
                set;
            }

            public void Dispose()
            {
            }
            #endregion

            public ViewTemplateSelector Selector { get; private set; }
        }
    }
}
