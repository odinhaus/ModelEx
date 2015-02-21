using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using System.Reflection;
using System.Threading;
using Altus.Core.Diagnostics;

namespace Altus.Core.Component
{
    [Serializable]
    public class ServiceContainer : CompositionContainer
    {
        /// <summary>
        /// Gets a list of Windows Services to run on shell startup
        /// </summary>
        public ServiceBase[] Services { get; protected set; }

        /// <summary>
        /// Gets a boolean value indicating whether the container should
        /// be run as a windows service (true), or a windows executable (false)
        /// </summary>
        public bool RunAsService { get; protected set; }


        protected virtual ServiceBase[] OnCreateServices() { return new ServiceBase[0]; }

        protected virtual bool OnSetRunAsService()
        {
            return (this.StartupArgs.Contains("svc"));
        }

        protected override void OnLoad()
        {
            this.RunAsService = OnSetRunAsService();
            this.Services = OnCreateServices();

            if (this.RunAsService)
            {
                Thread loader = new Thread(new ThreadStart(OnBackgroundLoad));
                loader.IsBackground = true;
                loader.Name = "Service Component Background Loader";
                loader.Start();
            }
            else
            {
                this.ComponentLoader.LoadComponents(this.StartupArgs);

                if (Services == null) return;

                foreach (ServiceBase service in Services)
                {
                    MethodInfo start = service.GetType().GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    start.Invoke(service, new object[] { this.StartupArgs });
                }
            }
        }

        protected virtual void OnBackgroundLoad()
        {
            Context.CurrentContext = Context.GlobalContext;
            Logger.Log("Loading Background Components");
            this.ComponentLoader.LoadComponents();
        }
    }
}

