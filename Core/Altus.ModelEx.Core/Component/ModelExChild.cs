using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using Altus.Core.Diagnostics;
using Altus.Core;
using System.Resources;
using System.Globalization;
using Altus.Core.Licensing;
using System.Threading;

namespace Altus.Core.Component
{
    public partial class App : IAppSource
    {
        protected App _app;
        private CompositionContainer _shell;
        private AppAPI _api;
        private DeclaredApp _core;

        protected App()
        {
            AppDomain.CurrentDomain.FirstChanceException += ChildDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += ChildDomain_UnhandledException;
            AppDomain.CurrentDomain.AssemblyResolve += ChildDomain_AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyLoad += ChildDomain_AssemblyLoad;
            AppDomain.CurrentDomain.DomainUnload += ChildDomain_DomainUnload;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ChildDomain_ReflectionOnlyAssemblyResolve;
            AppDomain.CurrentDomain.ResourceResolve += ChildDomain_ResourceResolve;
            App.Instance = this;
        }

        
        public static App Instance
        {
            get;
            protected set;
        }

        public bool IsRunning { get; private set; }

        
        public AppAPI API 
        {
            get { return _api; }
            private set
            {
                _api = value;
            }
        }

        public IEnumerable<DeclaredApp> Apps 
        {
            get
            {
                if (Shell == null)
                {
                    return new DeclaredApp[0];
                }
                else
                {
                    IAppSource appSource = Shell.GetComponent<IAppSource>();
                    List<DeclaredApp> apps = new List<DeclaredApp>();
                    apps.Add(_core);
                    if (appSource != null)
                    {
                        apps.AddRange(appSource.Apps);
                    }
                    return apps.ToArray();
                }
            }
        }

        public DeclaredApp this[string appName]
        {
            get 
            { 
                return Apps.Where(a => a.Name.Equals(appName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault(); 
            }
        }

        //========================================================================================================//
        /// <summary>
        /// Called by app domain host to load the injection container
        /// </summary>
        /// <param name="args"></param>
        private int RunChild()
        {
            IsRunning = true;
            Context.GlobalContext = API.Context;
            Context.CurrentContext = API.Context;
            App.Context = Context.CurrentContext;
            //CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            //CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.DefaultThreadCurrentCulture;

            _core = new DeclaredApp()
            {
                Name = "Core",
                Version = typeof(App).Assembly.GetName().Version.Major.ToString() + "." + typeof(App).Assembly.GetName().Version.Minor.ToString(),
                CodeBase = Context.GlobalContext.CodeBase,
                IsLocal = true,
                Product = Altus.Core.Configuration.ConfigurationManager.GetAppSetting("Instance"),
                Manifest = new DiscoveryManifest(),
                DeletePrevious = false,
                SourceUri = Context.GlobalContext.CodeBase,
                IncludeSourceBytes = false,
                IsCore = true
            };
            DiscoveryTarget target = new DiscoveryTarget()
            {
                Product = _core.Product
            };
            DiscoveryFileElement file = new DiscoveryFileElement()
            {
                LoadedAssembly = typeof(App).Assembly,
                Reflect = true,
                IsPrimary = true,
                IsValid = true,
                Name = typeof(App).Assembly.GetName().Name,
                CodeBase = _core.CodeBase
            };
            target.Files.Add(file);
            _core.Manifest.Targets.Add(target);

            Context.CurrentApp = _core;

            CompositionContainer shell = OnCreateShell(
                    API.Context,
                    API.Args);

            if ((API.Context.InstanceType.HasFlag(InstanceType.WindowsFormsClient) 
                || API.Context.InstanceType.HasFlag(InstanceType.ASPNetHost))
                || (shell is ServiceContainer && !((ServiceContainer)shell).RunAsService))
            {
                System.Windows.Forms.Application.Run(shell);
            }
            else if (API.Context.InstanceType.HasFlag(InstanceType.WPFClient))
            {
                ((WPFContainer)shell).Application.Run(((WPFContainer)shell).StartWindow);
            }
            else if (API.Context.InstanceType.HasFlag(InstanceType.WindowsService)
                || API.Context.InstanceType.HasFlag(InstanceType.ASPNetHost))
            {
                API.RunAsService = true;
                ServiceBase.Run(((ServiceContainer)shell).Services);
            }
            IsRunning = false;
            return shell.ExitCode;
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Called by app domain host to load the injection container
        /// </summary>
        /// <param name="args"></param>
        private void RunWithoutHostingChild()
        {
            IsRunning = true;
            Context.GlobalContext = API.Context;
            Context.CurrentContext = API.Context;
            //CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            //CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.DefaultThreadCurrentCulture;

            _core = new DeclaredApp()
            {
                Name = "Core",
                Version = typeof(App).Assembly.GetName().Version.Major.ToString() + "." + typeof(App).Assembly.GetName().Version.Minor.ToString(),
                CodeBase = Context.GlobalContext.CodeBase,
                IsLocal = true,
                Product = Altus.Core.Configuration.ConfigurationManager.GetAppSetting("Instance"),
                Manifest = new DiscoveryManifest(),
                DeletePrevious = false,
                SourceUri = Context.GlobalContext.CodeBase,
                IncludeSourceBytes = false,
                IsCore = true
            };
            DiscoveryTarget target = new DiscoveryTarget()
            {
                Product = _core.Product
            };
            DiscoveryFileElement file = new DiscoveryFileElement()
            {
                LoadedAssembly = typeof(App).Assembly,
                Reflect = true,
                IsPrimary = true,
                IsValid = true,
                Name = typeof(App).Assembly.GetName().Name,
                CodeBase = _core.CodeBase
            };
            target.Files.Add(file);
            _core.Manifest.Targets.Add(target);

            CompositionContainer shell = OnCreateShell(
                    API.Context,
                    API.Args);
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Unloads all components, and shuts down execution for the instance
        /// </summary>
        public bool Exit()
        {
            if (IsRunning
                && !this.Shell.IsExiting)
            {
                IsRunning = false;
                bool ret = this.Shell.Exit(true);
                if (ret)
                {
                    Logger.LogInfo("Stopped AppDomain Instance " 
                        + AppDomain.CurrentDomain.FriendlyName + " with exit code " + this.Shell.ExitCode);
                }
                else
                {
                    Logger.LogWarn("Failed to stop AppDomain Instance " 
                        + AppDomain.CurrentDomain.FriendlyName + " with exit code " 
                        + this.Shell.ExitCode + ".  The AppDomain Instance will be forcibly terminated.");
                }
                
                API.RequestUnload(this);
                return ret;
            }
            return true;
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// This will causing the hosting framework to destroy the current runtime and restart a new instance
        /// </summary>
        public void Restart()
        {
            if (IsRunning
                && !this.Shell.IsExiting)
            {
                IsRunning = false;

                bool ret = this.Shell.Exit(true);

                if (ret)
                {
                    Logger.LogInfo("Stopped AppDomain Instance "
                        + AppDomain.CurrentDomain.FriendlyName + " with exit code " + this.Shell.ExitCode);
                }
                else
                {
                    Logger.LogWarn("Failed to stop AppDomain Instance "
                        + AppDomain.CurrentDomain.FriendlyName + " with exit code "
                        + this.Shell.ExitCode + ".  The AppDomain Instance will be forcibly terminated.");
                }
                
                API.RequestReload(this);
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Gets the shell reference with the provided name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public CompositionContainer Shell
        {
            get
            {
                return _shell;
            }
            protected set
            {
                _shell = value;
            }
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Handles domain unloaded event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChildDomain_DomainUnload(object sender, EventArgs e)
        {
            OnDomainUnload(sender, e);
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Derived types should overload to implement custom domain unload behavior
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnDomainUnload(object sender, EventArgs e)
        {
            
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Handles assembly resolution for the domain
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly ChildDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return OnAssemblyResolve(sender, args);
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Handles reflection only assembly resolution for the domain
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly ChildDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return OnAssemblyResolveReflectionOnly(sender, args);
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Handles resource resolution failures
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly ChildDomain_ResourceResolve(object sender, ResolveEventArgs args)
        {
            return OnResourceResolve(sender, args);
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Derived types should override to handle resource resolution failures
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual Assembly OnResourceResolve(object sender, ResolveEventArgs args)
        {
            return OnAssemblyResolve(sender, args);
        }
        //========================================================================================================//

        protected Dictionary<string, Assembly> ResolvedAssemblies = new Dictionary<string, Assembly>();
        protected Dictionary<string, Assembly> ResolvedAssembliesReflectionOnly = new Dictionary<string, Assembly>();
        //========================================================================================================//
        /// <summary>
        /// Derived types should override to implement custom assembly resolution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName an = new AssemblyName(args.Name);
            Assembly asm = null;
            if (!TryGetLoadedAssembly(an, false, out asm))
            {
                string[] split = args.Name.Split(',');
                string name = split[0];

                if (name.EndsWith(".resources"))
                {
                    return null;
                }
                bool retry = true;
                DeclaredApp app = null;
                if (Context.CurrentContext != null)
                    app = Context.CurrentContext.CurrentApp;
                else if (Context.GlobalContext != null)
                    app = Context.GlobalContext.CurrentApp;
            retrycore:
                try
                {
                    #region assembly name
                    if (split.Length == 4)
                        an = new AssemblyName(string.Format("{0}, {1}, {2}, {3}", name, split[1].Trim(), split[2].Trim(), split[3].Trim()));
                    else if (split.Length == 3)
                        an = new AssemblyName(string.Format("{0}, {1}, {2}", name, split[1].Trim(), split[2].Trim()));
                    else if (split.Length == 2)
                        an = new AssemblyName(string.Format("{0}, {1}, Culture={2}", name, split[1].Trim(), Thread.CurrentThread.CurrentCulture.DisplayName));
                    else if (split.Length == 1)
                        an = new AssemblyName(string.Format("{0}", name));
                    #endregion

                    if (TryGetLoadedAssembly(an, false, out asm)) return asm;

                    asm = Assembly.LoadFrom(Path.Combine(app.CodeBase, name) + ".dll");
                }
                catch { }

                if (asm == null)
                {
                    try
                    {
                        asm = Assembly.LoadFrom(Path.Combine(app.CodeBase, name) + ".exe");
                    }
                    catch { }
                }

                if (asm == null
                    && retry
                    && app != App.Instance["Core"])
                {
                    app = App.Instance["Core"];
                    retry = false;
                    goto retrycore;
                }

                if (asm == null
                    && !TryGetLoadedAssembly(an, true, out asm))
                {
                    Logger.LogWarn("Could not resolve assembly "
                        + args.Name
                        + " for "
                        + (args.RequestingAssembly == null ? "<null>" : args.RequestingAssembly.FullName)
                        + " in "
                        + AppDomain.CurrentDomain.FriendlyName + " AppDomain");
                }
                ResolvedAssemblies.Add(args.Name, asm);
            }
            return asm;
        }
        //========================================================================================================//


        protected bool TryGetLoadedAssembly(AssemblyName name, bool matchByNameOnly, out Assembly assembly)
        {
            bool found = false;
            assembly = null;
            if (matchByNameOnly)
            {
                List<Assembly> matches = new List<Assembly>();
                matches.AddRange(ResolvedAssemblies.Values
                    .Where(a => a != null && a.GetName().Name.Equals(name.Name, StringComparison.InvariantCultureIgnoreCase)));
                if (matches.Count == 0)
                {
                     matches.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                       .Where(a => a.GetName().Name.Equals(name.Name, StringComparison.InvariantCultureIgnoreCase)));
                }

                if (matches.Count > 0)
                {
                    matches.Sort(delegate(Assembly asm1, Assembly asm2)
                    {
                        Version v1 = asm1.GetName().Version;
                        Version v2 = asm2.GetName().Version;
                        return -v1.CompareTo(v2);
                    });
                    assembly = matches.First();
                    found = true;
                }
            }
            else
            {
                if (ResolvedAssemblies.ContainsKey(name.FullName))
                {
                    assembly = ResolvedAssemblies[name.FullName];
                    found = true;
                }
                else if (AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().FullName.Equals(name.FullName, StringComparison.InvariantCultureIgnoreCase)) > 0)
                {
                    Assembly al = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => a.GetName().FullName.Equals(name.FullName, StringComparison.InvariantCultureIgnoreCase)).First();
                    assembly = al;
                    found = true;
                }
            }
            return found;
        }

        //========================================================================================================//
        /// <summary>
        /// Derived types should override to implement custom reflection-only assembly resolution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual Assembly OnAssemblyResolveReflectionOnly(object sender, ResolveEventArgs args)
        {
            if (ResolvedAssembliesReflectionOnly.ContainsKey(args.Name))
            {
                return ResolvedAssembliesReflectionOnly[args.Name];
            }
            else
            {
                Assembly asm = null;
                try
                {
                    asm = Assembly.ReflectionOnlyLoad(args.Name);
                }
                catch
                {
                    string name = args.Name.Split(',')[0];
                    try
                    {
                        asm = Assembly.ReflectionOnlyLoadFrom(Path.ChangeExtension(Path.Combine(Context.GlobalContext.CodeBase, name), "dll"));
                    }
                    catch { }
                    if (asm == null)
                    {
                        try
                        {
                            asm = Assembly.ReflectionOnlyLoadFrom(Path.ChangeExtension(Path.Combine(Context.GlobalContext.CodeBase, name), "exe"));
                        }
                        catch { }
                    }
                }
                if (asm == null)
                {
                    Logger.LogWarn("Could not resolve assembly "
                        + args.Name
                        + " for "
                        + args.RequestingAssembly == null ? "<null>" : args.RequestingAssembly
                        + " in "
                        + AppDomain.CurrentDomain.FriendlyName + " AppDomain");
                }
                ResolvedAssembliesReflectionOnly.Add(args.Name, asm);
                return asm;
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Handles assembly load event for domain
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ChildDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            OnAssemblyLoad(sender, args);
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Derived types should override to implement custom assembly load behavior
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected virtual void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            
        }        
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Handles first chance exceptions for the App Domain
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChildDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            OnUnhandledException(sender, e);
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Handles unhandled exceptions for the App Domain
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChildDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            OnFirstChanceException(sender, e);
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// creates and registers a shell with the provided name and type
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="shellType"></param>
        protected virtual CompositionContainer OnCreateShell(Context context, params string[] args)
        {
            Context.GlobalContext = context;

            SetEnvironmentVariables(args);

            string shellAssemblyPath = Altus.Core.Configuration.ConfigurationManager.GetAppSetting("ShellAssembly");
            Assembly shellAssembly = Assembly.Load(shellAssemblyPath);

            object[] attribs = shellAssembly.GetCustomAttributes(typeof(CompositionContainerAttribute), true);

            CompositionContainerAttribute sa = null;
            // get the shell atributes, there should only be one
            if (attribs.Length == 1)
            {
                sa = attribs[0] as CompositionContainerAttribute;
            }
            else
            {
                throw (new InvalidOperationException("A Shell requires one declared ShellAttribute."));
            }

            CompositionContainer shell = (CompositionContainer)TypeHelper.CreateType(
                ((CompositionContainerAttribute)attribs[0]).ShellType,
                null);

            if (!typeof(App).Assembly.Equals(shell.GetType().Assembly))
            {
                context.CurrentApp.Manifest.Targets[0].Files.Add(new DiscoveryFileElement()
                {
                    LoadedAssembly = shell.GetType().Assembly,
                    Reflect = true,
                    IsPrimary = false,
                    IsValid = true,
                    Name = shell.GetType().Assembly.GetName().Name,
                    CodeBase = _core.CodeBase
                });
            }

            _shell = shell;
            _shell.Initialize((CompositionContainerAttribute)attribs[0], args);
            _shell.Load();
            return _shell;
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Copies startup args and config settings to context environment variables
        /// </summary>
        /// <param name="args"></param>
        public static void SetEnvironmentVariables(string[] args)
        {
            foreach (KeyValueConfigurationElement setting
                in ((AppSettingsSection)Altus.Core.Configuration.ConfigurationManager.GetSection("appSettings", Context.GlobalContext)).Settings)
            {
                Context.SetEnvironmentVariable(setting.Key, setting.Value);
            }

            if (args != null && args.Length > 0)
            {
                Regex r = new Regex(@"(?<Arg>[\w]+):(?<Value>.*)");
                foreach (string arg in args)
                {
                    Match m = r.Match(arg);
                    if (m.Success)
                    {
                        string argName = m.Groups["Arg"].Value;
                        string argVal = m.Groups["Value"].Value;
                        Context.SetEnvironmentVariable(argName, argVal);
                    }
                }
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Derived implementation can provide their own logic to perform when an Unhandled Exception occurs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Logger.LogError(e.ExceptionObject.ToString());
            }
            catch
            {
                try
                {
                    ObjectLogWriter.AppendObject(Path.Combine(Context.CurrentContext.CodeBase, "CrashLog.log"), e.ExceptionObject.ToString());
                }
                catch { }
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Derived implementation can provide their own logic to perform when a First Chance Exception occurs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnFirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            try
            {
                //Logger.LogInfo("First Chance Exception:\n" + e.Exception.ToString());
            }
            catch { }
        }
        //========================================================================================================//


        public event EventHandler Disposed;

        public System.ComponentModel.ISite Site
        {
            get;
            set;
        }

        public void Dispose()
        {
            if (Disposed != null)
                Disposed(this, new EventArgs());
        }
    }
}
