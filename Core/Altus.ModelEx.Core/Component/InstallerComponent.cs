using Altus.Core.Diagnostics;
using Altus.Core.Licensing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Altus.Core.Reflection;

namespace Altus.Core.Component
{
    public abstract class InstallerComponent : InitializableComponent, IInstaller
    {
        public bool IsInstalled
        {
            get;
            protected set;
        }

        public void Install(params string[] appNames)
        {
            OnInstallBegin();
            bool isInstalled = true;
            DeclaredApp current = Context.CurrentContext.CurrentApp;
            if (appNames != null && appNames.Length > 0)
            {
                foreach (string appName in appNames)
                {
                    DeclaredApp app = App.Instance[appName];
                    if (app != null)
                    {
                        Context.CurrentContext.CurrentApp = app;
                        isInstalled &= OnInstall(app);
                    }
                }
            }
            else
            {
                foreach (DeclaredApp app in App.Instance.Apps)
                {
                    if (app != null)
                    {
                        Context.CurrentContext.CurrentApp = app;
                        isInstalled &= OnInstall(app);
                    }
                }
            }
            Context.CurrentContext.CurrentApp = current;
            IsInstalled = isInstalled;
            OnInstallEnd();
        }

        protected virtual void OnInstallBegin() { }
        protected virtual void OnInstallEnd() { }

        /// <summary>
        /// Derived types should override to implement installation behavior.  This method will be called 
        /// for each target app context that the installer is configured to run for using its InstallerComponentAttribute.
        /// </summary>
        /// <returns>true when install was successful</returns>
        protected abstract bool OnInstall(DeclaredApp app);

        protected bool RunInstallers(params Assembly[] assemblies)
        {
            bool success = true;
            foreach (Assembly assembly in assemblies)
            {
                Type[] installers = assembly.GetTypes().Where(t => typeof(Installer).IsAssignableFrom(t)).ToArray();
                foreach (Type installer in installers)
                {
                    RunInstallerAttribute attrib = installer.GetCustomAttribute<RunInstallerAttribute>(true);
                    if (attrib != null
                        && attrib.RunInstaller)
                    {
                        Installer i = (Installer)Activator.CreateInstance(installer, new object[] { });
                        IDictionary stateSaver = new Hashtable();
                        try
                        {
                            i.Install(stateSaver);
                            i.Commit(stateSaver);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Failed to install " + i.GetType().Name + " in assembly " + assembly.FullName);
                            i.Rollback(stateSaver);
                            success = false;
                        }
                    }
                }
            }
            return success;
        }
    }
}
