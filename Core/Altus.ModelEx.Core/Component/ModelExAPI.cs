using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading;
using Altus.Core.Diagnostics;

namespace Altus.Core.Component
{
    public delegate void UnloadEventHandler(object sender, UnloadEventArgs e);
    [Serializable]
    public class UnloadEventArgs : EventArgs
    {
        public UnloadEventArgs(string appDomainName, App instance, int returnCode)
        {
            this.AppDomainName = appDomainName;
            this.Instance = instance;
            this.ReturnCode = returnCode;
        }
        public App Instance { get; private set; }
        public int ReturnCode { get; private set; }
        public string AppDomainName { get; private set; }
    }

    public delegate void ReloadEventHandler(object sender, ReloadEventArgs e);
    [Serializable]
    public class ReloadEventArgs : UnloadEventArgs
    {
        public ReloadEventArgs(string appDomainName, App instance, int returnCode)
            : base(appDomainName, instance, returnCode) { }
    }

    [Serializable]
    public class AppAPI : MarshalByRefObject, IDisposable
    {
        #region Event Declarations
        //public event ReloadEventHandler Reload;
        //public event UnloadEventHandler Unload;
        #endregion Event Declarations
        public AppAPI() { }
        public AppAPI(Assembly entryAssembly, string[] args, Context ctx, bool hosted, bool isChild)
        {
            this.Initialize(entryAssembly, args, ctx, hosted, isChild);
        }

        internal void Initialize(Assembly entryAssembly,  string[] args, Context ctx, bool hosted, bool isChild, string configFilePath = "")
        {
            this.EntryAssembly = entryAssembly;
            this.AppDomain = AppDomain.CurrentDomain;
            if (!string.IsNullOrEmpty(configFilePath))
            {
                ctx.ConfigurationFile = configFilePath;
            }
            this.Setup = AppDomain.CurrentDomain.SetupInformation;
            this.Args = args;
            this.ExitWaitHandle = new ManualResetEvent(false);
            this.IsReloading = false;
            this.Context = ctx;
            this.IsHosted = hosted;
            this.Key = Guid.NewGuid().ToString();
            this.IsChild = isChild;
            Context.GlobalContext = ctx;
        }

        public override object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService(); 
            System.Diagnostics.Debug.Assert(lease.CurrentState == LeaseState.Initial); 
            //Set lease properties 
            lease.InitialLeaseTime = TimeSpan.FromDays(20000);
            lease.RenewOnCallTime = TimeSpan.FromDays(20000);
            lease.SponsorshipTimeout = TimeSpan.FromDays(20000);
            return lease;
        }

        internal App CreateAppInstance()
        {
            return Activator.CreateInstance(typeof(App), true) as App;
        }

        public string Key { get; private set; }
        public bool IsHosted { get; private set; }
        public Assembly EntryAssembly { get; internal set; }
        public AppDomain AppDomain { get; internal set; }
        public AppDomainSetup Setup { get; internal set; }
        public string[] Args { get; internal set; }
        public Context Context { get; internal set; }
        public WaitHandle ExitWaitHandle { get; internal set; }
        public bool IsReloading { get; internal set; }
        public int ReturnCode { get; internal set; }
        public App Instance { get; internal set; }
        public bool RunAsService { get; internal set; }
        public bool IsChild { get; internal set; }

        public override bool Equals(object obj)
        {
            return obj != null
                && obj is AppAPI
                && ((AppAPI)obj).Key.Equals(this.Key);
        }

        public void RequestUnload(App instance)
        {
            UnloadChild(new ReloadEventArgs(AppDomain.CurrentDomain.FriendlyName, instance, instance.Shell.ExitCode), false);
        }

        public void RequestReload(App instance)
        {
            UnloadChild(new ReloadEventArgs(AppDomain.CurrentDomain.FriendlyName, instance, instance.Shell.ExitCode), true);
        }

        private void Release(int returnCode)
        {
            this.ReturnCode = returnCode;
            ((ManualResetEvent)this.ExitWaitHandle).Set();
        }

        private void UnloadChild(UnloadEventArgs e, bool isReloading)
        {
            IsReloading = isReloading;
            if (this.RunAsService)
            {
                if (isReloading)
                {
                    Release(0);
                    Environment.Exit(2); // bounce the service, and let recovery options restart it
                }
                else
                {
                    Release(0);
                }
            }
            else
            {
                Release(e.Instance.Shell.ExitCode);
            }
        }

        bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    this.AppDomain = null;
                }
                catch { }
                try
                {
                    this.ExitWaitHandle.Dispose();
                    this.ExitWaitHandle = null;
                }
                catch { }
                try
                {
                    this.Instance = null;
                }
                catch { }
                try
                {
                    this.Setup = null;
                }
                catch { }
            }
            disposed = true;
        }
    }
}
