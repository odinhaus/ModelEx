//************************************************************************************************************
//  
//  COPYRIGHT ALTUS SERVICES, LLC 2006, All Rights Reserved     
// 
//
//  Class History
//============================================================================================================
//
//  Developer       Date            Comments
//------------------------------------------------------------------------------------------------------------
//  BILLBL          06/17/2006      Created
//
//
//
//
//************************************************************************************************************

#region References and Aliases

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
using System.ComponentModel;
using Altus.Core.Diagnostics;
using Altus.Core.Configuration;
using System.ServiceProcess;
using Altus.Core;
using System.Text.RegularExpressions;
using System.Configuration;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Web.Hosting;

#endregion References and Aliases

namespace Altus.Core.Component
{
    //========================================================================================================//
    /// <summary>
    /// Class name:  Application
    /// Class description:
    /// Usage:
    /// <example></example>
    /// <remarks></remarks>
    /// </summary>
    //========================================================================================================//
    [Serializable]
    public partial class App : MarshalByRefObject
    {
        static App()
        {
            APIs = new Dictionary<string, AppAPI>();
        }

        private static Dictionary<string, AppAPI> APIs { get; set; }
        private static Context Context { get; set; }
        private static bool Hosted { get; set; }
        private static string[] Args { get; set; }
        
        //========================================================================================================//
        /// <summary>
        /// Call this method from the Main STAThread, or Application_Start.Global.asax to run the shell application
        /// </summary>
        /// <param name="context"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int Run(Context context, params string[] args)
        {
        reload:
            Context = context;
            Hosted = false;
            Args = args;
            AppAPI api = CreateAppDomain(context, args);
            App.Instance = api.Instance;
            
            api.Instance.RunChild();
            api.ExitWaitHandle.WaitOne();
            if (api.IsReloading)
            {
                string name = api.AppDomain.FriendlyName;

                if (api.RunAsService)
                {
                    Environment.Exit(2); // services can't be reloaded - need to trigger a restart at the OS level
                }
                else
                {
                    if (APIs.ContainsKey(api.Key))
                        APIs.Remove(api.Key);
                    
                    Logger.LogInfo("Unloading AppDomain " + name + ".");
                    try
                    {
                        AppDomain.Unload(api.AppDomain);
                        Logger.LogInfo("Unloaded AppDomain " + name + ".");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "AppDomain unload failed for " + name + ".");
                    }

                    Logger.LogInfo("Attempting to restart AppDomain " + name + "...");
                    goto reload;
                }
            }
            return api.ReturnCode;
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Call this method from the Main STAThread, or Application_Start.Global.asax to run the shell application
        /// </summary>
        /// <param name="context"></param>
        /// <param name="args"></param>
        public static void RunWithoutHosting(Context context, params string[] args)
        {
            Context = context;
            Hosted = true;
            Args = args;
            AppAPI api = CreateLocalAPI(context, args);
            App.Instance = api.Instance;
            api.Instance.RunWithoutHostingChild();
            Thread reloader = new Thread(new ParameterizedThreadStart(RunWithoutHostingReloader));
            reloader.IsBackground = true;
            reloader.Name = "Altus RunWithoutHosting Reloader";
            reloader.Start(api);
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Reloads the shell in the event of an Unhosted reload request
        /// </summary>
        /// <param name="api"></param>
        private static void RunWithoutHostingReloader(object api)
        {
            while (true)
            {
                ((AppAPI)api).ExitWaitHandle.WaitOne();
                if (((AppAPI)api).IsReloading)
                {
                    RunWithoutHosting(((AppAPI)api).Context, ((AppAPI)api).Args);
                }
                return;
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Creates the child app domain
        /// </summary>
        /// <param name="context"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static AppAPI CreateAppDomain(Context context, string[] args)
        {
            Context.GlobalContext = context;
            Context.CurrentContext = context;
            
            if (context.InstanceType == InstanceType.ASPNetHost)
            {
                return CreateASPNetDomain(context, args);
            }
            else return CreateDefaultAppDomain(context, args);
        }

        private static AppAPI CreateDefaultAppDomain(Context context, string[] args)
        {
            AppSettingsSection section = (AppSettingsSection)Altus.Core.Configuration.ConfigurationManager.GetSection("AppSettings", context); // primes the config manger CurrentConfig value

            Assembly appAssembly = Assembly.GetEntryAssembly();
            if (appAssembly == null)
            {
                appAssembly = Assembly.GetExecutingAssembly();
            }

            string appName = appAssembly.FullName;
            AppDomainSetup ads = new AppDomainSetup()
            {
                DisallowBindingRedirects = false,
                DisallowCodeDownload = false,
                ShadowCopyFiles = "true",
                ApplicationName = appAssembly.GetName().Name,
                CachePath = Path.Combine(Context.GlobalContext.CodeBase, "Shadow"),
                ApplicationBase = Context.GlobalContext.CodeBase,
                PrivateBinPath = Path.Combine(Context.GlobalContext.CodeBase, "Apps"),
                ShadowCopyDirectories = Context.GlobalContext.CodeBase + ";" + Path.Combine(Context.GlobalContext.CodeBase, "Apps"),
                ConfigurationFile = Altus.Core.Configuration.ConfigurationManager.CurrentConfig,
            };

            DeleteDirectory(Path.Combine(ads.CachePath, ads.ApplicationName));

            Logger.LogInfo("Creating AppDomain " + appName + "...");
            AppDomain domain = AppDomain.CreateDomain(appName, null, ads);
            AppDomain.CurrentDomain.AssemblyResolve += HostDomain_AssemblyResolve;
            domain.UnhandledException += Domain_UnhandledException;
            domain.DomainUnload += Domain_DomainUnload;
            AppAPI api = (AppAPI)domain.CreateInstanceAndUnwrap(
                typeof(AppAPI).Assembly.FullName,
                typeof(AppAPI).FullName,
                false,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null,
                new object[] { appAssembly, args, context, true, true },
                null,
                null);

            App instance = api.CreateAppInstance();
            api.Instance = instance;
            instance.API = api;
            APIs.Add(api.Key, api);
            return api;
        }

        private static AppAPI CreateASPNetDomain(Context context, string[] args)
        {
            AppSettingsSection section = (AppSettingsSection)Altus.Core.Configuration.ConfigurationManager.GetSection("AppSettings", context); // primes the config manger CurrentConfig value

            List<string> newArgs = args.ToList();

            if (!args.HasArgValue("HttpVirtual"))
            {
                string virDir = "";
                try
                {
                    virDir = section.Settings["HttpVirtual"].Value;
                }
                catch { }
                newArgs.Add("-HttpVirtual:" + virDir);
            }

            if (!args.HasArgValue("HttpPhysical"))
            {
                string physDir = "";
                try
                {
                    physDir = section.Settings["HttpPhysical"].Value;
                    if (!Path.IsPathRooted(physDir))
                        physDir = Path.Combine(Context.GlobalContext.CodeBase, physDir);
                }
                catch 
                {
                    physDir = Context.GlobalContext.CodeBase.Replace("\\bin", "");
                }
                newArgs.Add("-HttpPhysical:" + physDir);
            }

            args = newArgs.ToArray();
            

            Assembly appAssembly = Assembly.GetEntryAssembly();
            if (appAssembly == null)
            {
                appAssembly = Assembly.GetExecutingAssembly();
            }

            AppAPI api = (AppAPI)ApplicationHost.CreateApplicationHost(typeof(AppAPI), 
                args.GetArgValue("HttpVirtual", "/"), 
                args.GetArgValue("HttpPhysical", ""));
            api.Initialize(appAssembly, args, context, true, true, Altus.Core.Configuration.ConfigurationManager.CurrentConfig);
            App instance = api.CreateAppInstance();
            api.Instance = instance;
            instance.API = api;
            APIs.Add(api.Key, api);
            return api;
        }

        private static AppAPI CreateLocalAPI(Context context, string[] args)
        {
            Context.GlobalContext = context;
            Context.CurrentContext = context;
            Altus.Core.Configuration.ConfigurationManager.GetSection("AppSettings", context); // primes the config manger CurrentConfig value
            Assembly appAssembly = Assembly.GetEntryAssembly();
            if (appAssembly == null)
            {
                appAssembly = Assembly.GetExecutingAssembly();
            }

            string appName = appAssembly.FullName;

            AppDomain domain = AppDomain.CurrentDomain;
            domain.UnhandledException += Domain_UnhandledException;
            domain.DomainUnload += Domain_DomainUnload;
            AppAPI api = new AppAPI(appAssembly, args, context, false, true);

            App instance = new App();
            api.Instance = instance;
            instance.API = api;
            APIs.Add(api.Key, api);
            return api;
        }

        public static void DeleteDirectory(string target_dir)
        {
            if (!Directory.Exists(target_dir)) return;
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);
            try
            {
                foreach (string file in files)
                {
                    System.IO.File.SetAttributes(file, FileAttributes.Normal);
                    System.IO.File.Delete(file);
                }

                foreach (string dir in dirs)
                {
                    DeleteDirectory(dir);
                }
                try
                {
                    Directory.Delete(target_dir, false);
                }
                catch { }
            }
            catch { }
        }

        //static void api_Reload(object sender, ReloadEventArgs e)
        //{
        //    APIs.Remove(((AppAPI)sender).Key);
        //}

        //static void api_Unload(object sender, UnloadEventArgs e)
        //{
        //    APIs.Remove(((AppAPI)sender).Key);
        //}

        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Handles domain unload
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Domain_DomainUnload(object sender, EventArgs e)
        {
            
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Handles domain unhandled exceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Domain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// handles assembly resolution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Assembly HostDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return null;
        }
        //========================================================================================================//
    }
}
