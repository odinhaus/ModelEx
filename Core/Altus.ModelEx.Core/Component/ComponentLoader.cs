using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.ComponentModel;
using Altus.Core;
using Altus.Core.Data;
using Altus.Core.Security;
using Altus.Core.Serialization;
using Altus.Core.Configuration;
using Altus.Core.Diagnostics;
using System.Xml.Serialization;
using Altus.Core.Licensing;
using System.Threading;

namespace Altus.Core.Component
{
    /// <summary>
    /// Simple class that uses reflection to detect ComponentAttribute declarations
    /// at the assembly level for all .exe and .dll files in the execution
    /// directory that define components to inject into the shell
    /// </summary>
    public class ComponentLoader : IComponentLoader
    {
        List<IComponent> _components = new List<IComponent>();
        List<ComponentAttribute> _attribs = new List<ComponentAttribute>();

        #region IComponentLoader Members

        public event ComponentLoadStatusHandler LoadStatus;

        public event ComponentLoadCompleteHandler LoadComplete;

        public event ComponentLoadBeginHandler LoadBegin;

        public event ComponentLoadCoreBegin LoadCoreBegin;
        public event ComponentLoadCoreComplete LoadCoreComplete;
        public event ComponentLoadExtensionsBegin LoadExtensionsBegin;
        public event ComponentLoadExtensionsComplete LoadExtensionsComplete;


        public void LoadComponents(params string[] args)
        {
            string _root = AppDomain.CurrentDomain.ShadowCopyFiles 
                ? Path.Combine(AppDomain.CurrentDomain.SetupInformation.CachePath, AppDomain.CurrentDomain.SetupInformation.ApplicationName)
                : AppDomain.CurrentDomain.BaseDirectory;

            LoaderConfig = ConfigurationManager.GetSection("componentLoader", Context.CurrentContext) as ComponentLoaderSection;
            List<ComponentAttribute> loaded = new List<ComponentAttribute>();

            if (this.LoadBegin != null)
                this.LoadBegin();

            // load core components first
            LoadCoreAssemblies(LoaderConfig.CoreAssemblies);
            LoadCoreComponentsLoop(LoaderConfig, loaded);

            LoadDiscoverableAssemblies(LoaderConfig.DiscoveryPaths);
            LoadExtensionsComponentsLoop(LoaderConfig, loaded);
            

            IsComplete = true;
            if (this.LoadComplete != null)
            {
                this.LoadComplete();
            }
        }

        public ComponentLoaderSection LoaderConfig { get; private set; }

        private void LoadCoreComponentsLoop(ComponentLoaderSection section,  List<ComponentAttribute> loaded)
        {
            bool isFirstPass = true;
            IEnumerable<ComponentAttribute> components = null;
            
            do
            {
                IEnumerable<ComponentAttribute> dbComponents = DataContext.Default.Select<ComponentAttribute>();
                components = GetComponentList(section, loaded, dbComponents, false);

                Dictionary<string, ComponentDependency> cd = ComponentDependency.BuildDependencyGraph(components.ToArray());

                _count += components.Count();

                if (isFirstPass)
                {
                    if (this.LoadCoreBegin != null)
                        this.LoadCoreBegin();
                    isFirstPass = false;
                }

                LoadComponentRecurse(cd);
                loaded.AddRange(components);

                if (_injectedAttribs.Count > 0)
                {
                    cd = ComponentDependency.BuildDependencyGraph(_injectedAttribs.ToArray());
                    LoadComponentRecurse(cd);
                    loaded.AddRange(_injectedAttribs);
                }

                if (_injectedComponents.Count > 0)
                {
                    Dictionary<string, IComponent>.Enumerator en = _injectedComponents.GetEnumerator();
                    int index = loaded.Count();
                    while (en.MoveNext())
                    {
                        NotifyComponentLoaded(en.Current.Value, en.Current.Key, index, _count);
                        index++;
                    }
                    _injectedComponents.Clear();
                }

            } while (components.Count() > 0);
            if (this.LoadCoreComplete != null)
                this.LoadCoreComplete();
        }

        private void LoadExtensionsComponentsLoop(ComponentLoaderSection section, List<ComponentAttribute> loaded)
        {
            bool isFirstPass = true;
            IEnumerable<ComponentAttribute> components = null;
            DeclaredApp current = Context.CurrentContext.CurrentApp;
            foreach (DeclaredApp app in App.Instance.Apps)
            {
                if (app.IsCore) continue;
                Context.CurrentContext.CurrentApp = app;
                do
                {
                    IEnumerable<ComponentAttribute> dbComponents = DataContext.Default.Select<ComponentAttribute>(new { NodeId = NodeIdentity.NodeId });
                    components = GetComponentList(section, loaded, dbComponents, true);

                    Dictionary<string, ComponentDependency> cd = ComponentDependency.BuildDependencyGraph(components.ToArray());

                    _count += components.Count();

                    if (isFirstPass)
                    {
                        if (this.LoadExtensionsBegin != null)
                            this.LoadExtensionsBegin();
                        isFirstPass = false;
                    }

                    LoadComponentRecurse(cd);
                    loaded.AddRange(components);

                    if (_injectedAttribs.Count > 0)
                    {
                        cd = ComponentDependency.BuildDependencyGraph(_injectedAttribs.ToArray());
                        LoadComponentRecurse(cd);
                        loaded.AddRange(_injectedAttribs);
                    }

                    if (_injectedComponents.Count > 0)
                    {
                        Dictionary<string, IComponent>.Enumerator en = _injectedComponents.GetEnumerator();
                        int index = loaded.Count();
                        while (en.MoveNext())
                        {
                            NotifyComponentLoaded(en.Current.Value, en.Current.Key, index, _count);
                            index++;
                        }
                        _injectedComponents.Clear();
                    }

                } while (components.Count() > 0);
            }
            Context.CurrentContext.CurrentApp = current;
            if (this.LoadExtensionsComplete != null)
                this.LoadExtensionsComplete();
        }

        private IEnumerable<ComponentAttribute> GetComponentList(ComponentLoaderSection section, 
            IEnumerable<ComponentAttribute> exclude, 
            IEnumerable<ComponentAttribute> dbComponents,
            bool allowExtensions)
        {
            List<ComponentAttribute> components = new List<ComponentAttribute>();
            if (!allowExtensions)
            {
                foreach (CoreAssemblyElement cae in section.CoreAssemblies)
                {
                    if (cae.IsValid)
                    {
                        components.AddRange(ReflectAssembly(cae.LoadedAssembly));
                    }
                }

            }
            else
            {
                foreach (DeclaredApp app in LicensedAppInstaller.Instance.Apps)
                {
                    foreach (DiscoveryTarget target in app.Manifest.Targets.Where(t => t.IsLocal))
                    {
                        foreach (DiscoveryFileElement file in target.Files.Where(f => f.Reflect && f.IsValid && f.Exists))
                        {
                            components.AddRange(ReflectAssembly(file.LoadedAssembly));
                        }
                    }
                }
            }
            
            foreach (ComponentAttribute ca in dbComponents)
            {
                ComponentAttribute found = null;
                if (allowExtensions)
                {
                    found = components.Where(c => c.Name.Equals(ca.Name, StringComparison.InvariantCultureIgnoreCase) && !IsCore(ca)).FirstOrDefault();
                    if (found == null)
                    {
                        if (ca.Enabled && !IsCore(ca))
                            components.Add(ca);
                    }
                    else
                    {
                        found.Enabled = ca.Enabled;
                    }
                }
                else
                {
                    found = components.Where(c => c.Name.Equals(ca.Name, StringComparison.InvariantCultureIgnoreCase) && IsCore(ca)).FirstOrDefault();
                    if (found == null)
                    {
                        if (ca.Enabled && IsCore(ca))
                            components.Add(ca);
                    }
                    else
                    {
                        found.Enabled = ca.Enabled;
                    }
                }
            }
            

            if (exclude != null)
            {
                foreach (ComponentAttribute ca in exclude)
                {
                    ComponentAttribute cap = components.Where(
                        c => c.Name.Equals(ca.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    components.Remove(cap);
                }
            }
            return components;
        }

        private bool IsCore(ComponentAttribute attribute)
        {
            foreach (CoreAssemblyElement coreAsm in LoaderConfig.CoreAssemblies)
            {
                if (coreAsm.Assembly.Trim().Equals(AssemblyName(attribute), StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        private string AssemblyName(ComponentAttribute attribute)
        {
            string[] split = attribute.Component.Split(',');
            return split[1].Trim();
        }

        private IEnumerable<ComponentAttribute> ReflectAssembly(Assembly asm)
        {
            List<ComponentAttribute> components = new List<ComponentAttribute>();
            object[] attribs = asm.GetCustomAttributes(typeof(ComponentAttribute), true);

            if (attribs != null && attribs.Length > 0)
            {
                components.AddRange(((ComponentAttribute[])attribs).Where(a => a.Reflect));
            }

            return components;
        }

        List<ComponentAttribute> _injectedAttribs = new List<ComponentAttribute>();
        public void Add(Assembly assembly)
        {
            IEnumerable<ComponentAttribute> attribs = ReflectAssembly(assembly);
            _count += attribs.Count();
            if (IsComplete)
            {
                Dictionary<string, ComponentDependency> cd = ComponentDependency.BuildDependencyGraph(attribs.ToArray());
                LoadComponentRecurse(cd);
            }
            else
                _injectedAttribs.AddRange(attribs);
        }

        private int _count;
        private void LoadComponentRecurse(Dictionary<string, ComponentDependency> cd)
        {
            // load all the siblings first
            Dictionary<string, ComponentDependency>.Enumerator cdEn = cd.GetEnumerator();
            while (cdEn.MoveNext())
            {
                ComponentDependency componentDependency = cdEn.Current.Value;

                if (!componentDependency.Created 
                    && componentDependency.ComponentAttribute.Enabled
                    && _attribs.Count(ca => ca.Name.Equals(componentDependency.ComponentAttribute.Name, StringComparison.InvariantCultureIgnoreCase)
                    && ca.Component.Equals(componentDependency.ComponentAttribute.Component, StringComparison.InvariantCultureIgnoreCase)) == 0)
                {
                    IComponent component = null;
                    if (componentDependency.ComponentAttribute.IsValid)
                    {
                        component = Activator.CreateInstance(componentDependency.ComponentAttribute.ComponentType,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            null,
                            componentDependency.ComponentAttribute.CtorArgs,
                            Thread.CurrentThread.CurrentCulture) as IComponent;
                    }
                    else
                    {
                        component = TypeHelper.CreateType(componentDependency.ComponentAttribute.Component, new object[] { }) as IComponent;
                    }
                    componentDependency.ComponentAttribute.Instance = component;
                    componentDependency.Created = true;
                    if (App.Instance.Shell.GetComponent(componentDependency.ComponentAttribute.Name) == null)
                    {
                        _components.Add(component);
                        _attribs.Add(componentDependency.ComponentAttribute);
                        NotifyComponentLoaded(component, componentDependency.ComponentAttribute.Name, _components.Count, _count);
                    }
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
        private void LoadCoreAssemblies(CoreAssemblyCollection coreAssemblyCollection)
        {
            List<Assembly> currentAssemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
            foreach (CoreAssemblyElement cassembly in coreAssemblyCollection)
            {
                string path = cassembly.CodeBase + ".dll";
                bool exists = true;
                if (!System.IO.File.Exists(path))
                {
                    path = cassembly.CodeBase + ".exe";
                    if (!System.IO.File.Exists(path))
                    {
                        exists = false;
                        Logger.LogWarn(new FileNotFoundException("The core assembly " + cassembly.CodeBase + " could not be found."));
                    }
                }

                if (exists)
                {
                    AssemblyName name;
                    CoreAssemblyAttribute attrib;
                    if (!currentAssemblies.Contains<CoreAssemblyAttribute>(path, out name, out attrib))
                    {
                        if (attrib != null)
                            currentAssemblies.Add(AppDomain.CurrentDomain.Load(name));
                    }
                    cassembly.IsValid = attrib != null;
                    cassembly.LoadedAssembly = currentAssemblies
                        .Where(ca => ca.GetName().FullName.Equals(name.FullName, StringComparison.InvariantCultureIgnoreCase))
                        .FirstOrDefault();
                }
            }
        }

        private void LoadDiscoverableAssemblies(DiscoveryPathCollection discoveryPathCollection)
        {
            if (LicensedAppInstaller.Instance == null) return;

            List<Assembly> currentAssemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
            
            foreach (DeclaredApp app in LicensedAppInstaller.Instance.Apps.Where(a => a.Manifest != null))
            {
                DiscoveryTarget target = app.Manifest.Targets.Where(t => t.IsLocal).FirstOrDefault();
                if (target != null)
                {
                    foreach (DiscoveryFileElement file in target.Files.Where(f => f.Reflect && f.Exists))
                    {
                        AssemblyName name;
                        DiscoverableAssemblyAttribute attrib;
                        if (!currentAssemblies.Contains<DiscoverableAssemblyAttribute>(file.CodeBase, out name, out attrib))
                        {
                            if (attrib != null)
                            {
                                currentAssemblies.Add(AppDomain.CurrentDomain.Load(name));
                                string directory = Path.GetDirectoryName(file.CodeBase);
                                if (!AppDomain.CurrentDomain.SetupInformation.PrivateBinPath.Contains(directory + ";"))
                                {
                                    AppDomain.CurrentDomain.SetupInformation.PrivateBinPath += directory + ";";
                                }
                            }
                        }
                        file.IsValid = attrib != null;
                        file.LoadedAssembly = currentAssemblies
                            .Where(ca => ca.GetName().FullName.Equals(name.FullName, StringComparison.InvariantCultureIgnoreCase))
                            .FirstOrDefault();
                    }
                }
            }
        }
    }
    

    public static class AssemblyListEx
    {
        public static bool Contains<T>(this List<Assembly> assemblies, string test, out AssemblyName name, out T attribute) where T : Attribute
        {
            attribute = null;
            
            Uri testUri = new Uri(test);
            Assembly found = assemblies.Where(
                a => !a.IsDynamic 
                    && new Uri(a.CodeBase).AbsolutePath.Equals(testUri.AbsolutePath, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (found == null)
            {
                Assembly assembly = Assembly.ReflectionOnlyLoadFrom(test);
                name = null;
                name = assembly.GetName();
                for (int i = 0; i < assemblies.Count; i++)
                {
                    if (assemblies[i].FullName == assembly.FullName)
                    {
                        attribute = assembly.GetCustomAttributeReflectionOnly<T>();
                        return true;
                    }
                }

                attribute = assembly.GetCustomAttributeReflectionOnly<T>();
                return false;
            }
            else
            {
                Assembly assembly = found;
                name = assembly.GetName();
                attribute = assembly.GetCustomAttributeReflectionOnly<T>();
                return true;
            }
        }

        public static T GetCustomAttributeReflectionOnly<T>(this Assembly reflectionOnlyAssembly) where T : Attribute
        {
            CustomAttributeData cad = CustomAttributeData.GetCustomAttributes(reflectionOnlyAssembly)
                .Where(ca => ca.Constructor.DeclaringType.FullName.Equals(typeof(T).FullName)).FirstOrDefault();
            if (cad != null)
            {
                return (T)Activator.CreateInstance(typeof(T), new object[] { cad.ConstructorArguments[0].Value });
            }
            return null;
        }

    }
}

