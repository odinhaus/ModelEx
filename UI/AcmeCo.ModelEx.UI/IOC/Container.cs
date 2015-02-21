using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Altus.Core;
using Altus.Core.Component;
using Altus.Core.Licensing;
using Altus.Core.Memory;


[assembly: CompositionContainer(
        LoaderType = "Altus.Core.Component.ComponentLoader, Core",
        ShellType = "Altus.UI.IOC.Container, ModelEx")]
[assembly: CoreAssembly("5D05ECF0-487B-49DA-97D3-A68066968818")]


namespace Altus.UI.IOC
{
    
    public class Container : WPFContainer
    {
        private static string appGuid = "9F185854-1042-4736-B6A1-6897A18E678D";
        [STAThread]
        static void Main(params string[] args)
        {
            using (Mutex mutex = new Mutex(false, appGuid))
            {
                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show("ModelEx is already running!", "Startup Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //var profileRoot = Path.Combine(
                //    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                //    "Modelex", "ProfileOptimization");
                //Directory.CreateDirectory(profileRoot);
                //// Define the folder where to save the profile files
                //ProfileOptimization.SetProfileRoot(profileRoot);
                //// Start profiling and save it in Startup.profile
                //ProfileOptimization.StartProfile("Startup.profile");

                Altus.Core.Component.App.Run(new Context(InstanceType.WPFClient, IdentityType.UserProcess), args);
            }
        }

        protected override System.Windows.Application OnCreateApplication()
        {
            return new App();
        }

        protected override System.Windows.Window OnCreateMainWidow()
        {
            return new MainWindow() { Name = "Main" };
        }

        //protected override System.Windows.Window OnCreateSplashWindow()
        //{
        //    return new Splash();
        //}

        

        protected override void OnInitialize(CompositionContainerAttribute shellAttribute, params string[] startupArgs)
        {
            MemoryManagement.SetLimits(1024 * 1024 * 256, 1024 * 1024 * 768,
                MemoryManagement.LimitFlags.QUOTA_LIMITS_HARDWS_MIN_ENABLE | MemoryManagement.LimitFlags.QUOTA_LIMITS_HARDWS_MAX_ENABLE);
            string directoryName = Context.GlobalContext.Location;
            directoryName = Path.Combine(directoryName, Context.GetEnvironmentVariable("TempDir").ToString());
            Directory.CreateDirectory(directoryName);
            foreach (string file in Directory.GetFiles(directoryName))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        protected override ILicenseManager OnCreateLicenseManager()
        {
            return new XmlFileLicenseManager();
        }

        // Uncomment to make this container be a code update service host for other app clients
        //protected override IInstaller OnCreateLicensedComponentInstaller()
        //{
        //    return LicensedAppProvider.Create();
        //}

        protected override IInstaller OnCreateLicensedComponentInstaller()
        {
            return LicensedAppInstaller.Create();
        }

        protected override void OnApplyLicensing(ILicense[] licenses)
        {
            // need to insert the WPF node entries in the Meta DB here
        }

        protected override void OnComponentChanged(CompositionContainerComponentChange change, System.ComponentModel.IComponent component, string name)
        {
            Console.WriteLine(change + "ed: " + name + " [" + component.GetType().FullName + "]");
            base.OnComponentChanged(change, component, name);
        }
    }
}
