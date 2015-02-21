using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.ComponentModel;
using Altus.Core;
using Altus.Core.Component;

namespace Altus.Core.Dynamic.Components
{
    /// <summary>
    /// Simple class that uses reflection to detect ComponentAttribute declarations
    /// at the assembly level for all .exe and .dll files in the execution
    /// directory that define components to inject into the shell
    /// </summary>
    public class DynamicComponentLoader : IComponentLoader
    {
        List<IComponent> _components = new List<IComponent>();

        #region IComponentLoader Members

        public event ComponentLoadStatusHandler LoadStatus;

        public event ComponentLoadCompleteHandler LoadComplete;

        public event ComponentLoadBeginHandler LoadBegin;

        public event ComponentLoadCoreBegin LoadCoreBegin;
        public event ComponentLoadCoreComplete LoadCoreComplete;
        public event ComponentLoadExtensionsBegin LoadExtensionsBegin;
        public event ComponentLoadExtensionsComplete LoadExtensionsComplete;

        public void Add(Assembly assembly)
        {
            throw new NotImplementedException();
        }

        public void LoadComponents(params string[] args)
        {
            string _root = AppDomain.CurrentDomain.BaseDirectory;
            if (Context.CurrentContext != null && !String.IsNullOrEmpty(Context.CurrentContext.CodeBase))
                _root = Context.CurrentContext.CodeBase;

            List<ComponentAttribute> components = new List<ComponentAttribute>();

            object[] attribs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(ComponentAttribute), true);

            if (attribs != null && attribs.Length > 0)
            {
                components.AddRange((ComponentAttribute[])attribs);
            }
           
            Dictionary<string, ComponentDependency> cd = ComponentDependency.BuildDependencyGraph(components.ToArray());
            _count = components.Count;
            if (this.LoadBegin != null)
                this.LoadBegin();

            LoadComponentRecurse(cd);

            if (_injectedComponents.Count > 0)
            {
                Dictionary<string, IComponent>.Enumerator en = _injectedComponents.GetEnumerator();
                int index = components.Count;
                while (en.MoveNext())
                {
                    NotifyComponentLoaded(en.Current.Value, en.Current.Key, index, _count);
                    index++;
                }
                _injectedComponents.Clear();
            }

            IsComplete = true;
            if (this.LoadComplete != null)
            {
                this.LoadComplete();
            }
        }
        private int _count;
        private void LoadComponentRecurse(Dictionary<string, ComponentDependency> cd)
        {
            // load all the siblings first
            Dictionary<string, ComponentDependency>.Enumerator cdEn = cd.GetEnumerator();
            while (cdEn.MoveNext())
            {
                ComponentDependency componentDependency = cdEn.Current.Value;

                if (!componentDependency.Created)
                {
                    IComponent component = TypeHelper.CreateType(componentDependency.ComponentAttribute.Component, new object[] { }) as IComponent;
                    componentDependency.Created = true;
                    NotifyComponentLoaded(component, componentDependency.ComponentAttribute.Name, _components.Count + 1, _count);
                }
            }

            // now load children
            foreach (ComponentDependency dep in cd.Values)
            {
                LoadComponentRecurse(dep.Dependencies);
            }
        }

        private void NotifyComponentLoaded(IComponent component, string name, int index, int count)
        {
            if (component != null && LoadStatus != null)
            {
                LoadStatus(
                    new ComponentLoadStatusEventArgs("Discovered component " + name + ".",
                    component,
                    name,
                    index,
                    count));
            }
        }

        Dictionary<string, IComponent> _injectedComponents = new Dictionary<string, IComponent>();
        public void Add(IComponent component)
        {
            Add(component, component.GetType().FullName);
        }

        public void Add(IComponent component, string name)
        {
            _count++;
            if (IsComplete)
                NotifyComponentLoaded(component, name, _count, _count);
            else
                _injectedComponents.Add(name, component);
        }

        public bool IsComplete
        {
            get;
            private set;
        }

        public void Cancel()
        {
        }

        public System.ComponentModel.IComponent[] Components
        {
            get
            {
                return _components.ToArray();
            }
        }

        #endregion

        private void LoadLocalAssemblies(string path)
        {
            List<Assembly> currentAssemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());

            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                if (Path.GetExtension(files[i]).ToLower() == ".dll"
                    || Path.GetExtension(files[i]).ToLower() == ".exe")
                {
                    try
                    {
                        AssemblyName name;
                        if (!currentAssemblies.Contains(files[i], out name))
                        {
                            currentAssemblies.Add(AppDomain.CurrentDomain.Load(name));
                        }
                    }
                    catch { }
                }
            }
        }
    }

    public static class AssemblyListEx
    {
        public static bool Contains(this List<Assembly> assemblies, string test, out AssemblyName name)
        {
            Assembly assembly = Assembly.ReflectionOnlyLoadFrom(test);
            name = null;

            for (int i = 0; i < assemblies.Count; i++)
            {
                if (assemblies[i].FullName == assembly.FullName)
                {

                    return true;
                }
            }
            name = assembly.GetName();
            return false;
        }

    }
}

