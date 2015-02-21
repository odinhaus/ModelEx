using Altus.Core.Component;
using Altus.Core.Configuration;
using Altus.Core.Diagnostics;
using Altus.Core.Licensing;
using Altus.Core.Messaging.Udp;
using Altus.Core.Net;
using Altus.Core.Processing;
using Altus.Core.Processing.Rpc;
using Altus.Core.Security;
using Altus.Core.Serialization;
using Altus.Core.Serialization.Xml;
using Altus.Core.Streams;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace Altus.Core.Licensing
{
    public interface IAppSource : IComponent
    {
        IEnumerable<DeclaredApp> Apps { get; }
        DeclaredApp this[string appName] { get; }
    }

    [InstallerComponent()]
    public class LicensedAppInstaller : LicensedInstallerComponent, IAppSource
    {
        protected override void OnInstallBegin()
        {
            Altus.Core.Component.App.Instance.Shell.ComponentLoader.LoadCoreComplete += OnLoadCoreAppsComplete;
        }

        protected override bool OnInstall(DeclaredApp app)
        {
            return OnGetLocalSource(ref app, Product);
        }

        protected virtual void OnLoadCoreAppsComplete()
        {
            OnCreateMCastConnection();
            _appProviderUri = OnDiscoverAppProviderUri();
            foreach (DeclaredApp app in Apps)
            {
                DeclaredApp rapp = app;
                OnInstallApp(ref rapp);
            }
        }

        protected LicensedAppInstaller() { }

        public static LicensedAppInstaller Create()
        {
            LicensedAppInstaller instance = new LicensedAppInstaller();
            return instance;
        }

        public static LicensedAppInstaller Instance
        {
            get
            {
                if (Altus.Core.Component.App.Instance != null)
                {
                    return Altus.Core.Component.App.Instance.Shell.GetComponent<LicensedAppInstaller>("LicensedAppInstaller");
                }
                else return null;
            }
        }

        private void OnInstallApp(ref DeclaredApp app)
        {
            if (OnGetLatest(ref app))
            {
                app.SourceUri = IPEndPointEx.LocalAddress(true).ToString();
            }
        }

        protected virtual Assembly OnLoadAssembly(byte[] assemblyBytes)
        {
            return Assembly.Load(assemblyBytes);
        }

        protected virtual string OnGetAppVersionDirectory(ref DeclaredApp app)
        {
            if (app.CodeBase != null)
            {
                return app.CodeBase;
            }
            else
            {
                string directory = Path.Combine(OnGetAppDirectory(ref app),
                    app.Version.Trim());
                return directory;
            }
        }

        protected virtual string OnGetAppDirectory(ref DeclaredApp app)
        {
            string directory = null;
            foreach (DiscoveryPathElement path in LoaderConfig.DiscoveryPaths)
            {
                 directory = Path.Combine(
                    Path.Combine(Context.CurrentContext.CodeBase, path.Path),
                    app.Name.Trim().Replace(" ", "_"));
                 if (Directory.Exists(directory))
                 {
                     path.IsValid = true;
                     break;
                 }
            }
            return directory;
        }

        protected virtual bool OnGetLocalSource(ref DeclaredApp app, string targetInstance)
        {
            if (app.Manifest != null) return true;
            string directory = OnGetAppVersionDirectory(ref app);

            Directory.CreateDirectory(directory);
            app.CodeBase = directory;

            MD5 hasher = MD5.Create();
                
            List<AppFile> files = new List<AppFile>();
            string manifest = Directory.GetFiles(directory, "*.manifest", SearchOption.TopDirectoryOnly).FirstOrDefault();
            bool foundAllFiles = false;
            if (!string.IsNullOrEmpty(manifest))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DiscoveryManifest));
                app.Manifest = null;

                using (StreamReader sr = new StreamReader(manifest))
                {
                    try
                    {
                        app.Manifest = (DiscoveryManifest)serializer.Deserialize(sr);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, app.Name + " manifest file could not be deserialized.  The system will use a default file.");
                        app.Manifest = new DiscoveryManifest();
                        return false;
                    }
                    app.Manifest.LocalPath = manifest;
                }

                int fileCount = 0;
                int targetCount = 0;
                foreach (DiscoveryTarget target in app.Manifest.Targets)
                {
                    foreach (DiscoveryFileElement file in target.Files)
                    {
                        if (string.IsNullOrEmpty(file.Destination))
                        {
                            file.CodeBase = Path.Combine(directory, file.Name);
                        }
                        else
                        {
                            if (Path.IsPathRooted(file.Destination))
                            {
                                file.CodeBase = Path.Combine(file.Destination, file.Name);
                            }
                            else
                            {
                                file.CodeBase = Path.Combine(Path.Combine(directory, file.Destination), file.Name);
                            }
                        }
                        
                        try
                        {
                            byte[] data = ReadData(file.CodeBase);
                            byte[] checksum = data.Length > 0 ? hasher.ComputeHash(data) : data;
                            AppFile source = new AppFile()
                            {
                                Name = Path.GetFileName(file.CodeBase),
                                Checksum = Convert.ToBase64String(checksum),
                                CodeBase = file.CodeBase
                            };
                            if (target.Product.Equals(targetInstance, StringComparison.InvariantCultureIgnoreCase))
                            {
                                fileCount++;
                                targetCount = target.Files.Count;
                                files.Add(source);
                            }
                            file.IsValid = string.IsNullOrEmpty(file.Checksum)
                                || file.Checksum.Equals(source.Checksum);
                            file.Checksum = source.Checksum;
                        }
                        catch
                        {
                            file.IsValid = false;
                        }
                    }
                    app.Files = files;
                }
                foundAllFiles = fileCount == targetCount;
                using (StreamWriter sw = new StreamWriter(manifest))
                {
                    serializer.Serialize(sw, app.Manifest);
                }
            }
            return foundAllFiles;
        }

        protected byte[] ReadData(string path)
        {
            if (System.IO.File.Exists(path))
            {
                using (FileStream fs = System.IO.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return StreamHelper.GetBytes(fs);
                }
            }
            return new byte[0];
        }

        private string _product = null;
        public string Product 
        {
            get
            {
                if (_product == null)
                    _product = Context.GetEnvironmentVariable<string>("Instance", "");
                return _product;
            }
        }
       
        protected virtual bool OnGetLatest(ref DeclaredApp app)
        {
            try
            {
                app.IncludeSourceBytes = true;
                DeclaredApp hostApp = OnGetHostApp(app);
                bool reload = false;
                foreach (AppFile file in hostApp.Files.Where(cf => cf.Data != null && cf.Data.Length > 0))
                {
                    string destination = file.CodeBase;
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    if (System.IO.File.Exists(destination))
                        System.IO.File.Delete(destination);
                    System.IO.File.WriteAllBytes(destination, file.Data);
                    Logger.LogInfo(app.Name + " file " + file.Name + " updated to size: " + file.Data.Length + " bytes, checksum: " + file.Checksum);
                    reload = true;
                }

                if (reload)
                {
                    app.WasUpdated = true & !hostApp.IsLocal;
                    OnGetLocalSource(ref app, this.Product);
                }

                if (hostApp.DeletePrevious)
                {
                    foreach (string verDir in Directory.GetDirectories(OnGetAppDirectory(ref app)))
                    {
                        string verDirName = Path.GetFileName(verDir);
                        if (verDirName.CompareTo(hostApp.Version) < 0)
                        {
                            try
                            {
                                Directory.Delete(verDir);
                                Logger.LogInfo("Previous " + app.Name + " version deleted: " + verDir);
                            }
                            catch(Exception ex)
                            {
                                Logger.LogWarn(ex, "Previous " + app.Name + " version delete failed: " + verDir);
                            }
                        }
                    }
                }

                return true;
            }
            catch { return false; }

        }

        protected override void OnCreateAppNodeIdentity(DeclaredApp app) { }

        protected virtual DeclaredApp OnGetHostApp(DeclaredApp app)
        {
            try
            {
                if (app.IsLocal)
                {
                    LicensedAppProvider provider = (LicensedAppProvider)LicensedAppProvider.Create();
                    provider.Initialize("LicensedAppProvider");
                    return provider.GetApp(app);
                }
                else
                {
                    if (string.IsNullOrEmpty(_appProviderUri))
                    {
                        Logger.LogWarn("App update provider does not exist.  Aborting update attempt.");
                        return app;
                    }
                    ServiceOperation result = ServiceOperation.Call(
                       _appProviderUri,
                       TimeSpan.FromSeconds(2),
                       new ServiceParameter("app", typeof(DeclaredApp).FullName, ParameterDirection.In) { Value = app });
                    DeclaredApp hostApp = (DeclaredApp)result.Parameters.Where(p => p.Direction == ParameterDirection.Return).First().Value;
                    return hostApp;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to retrieve update sources for App: " + app.Name + ".");
                return app;
            }
        }

        MulticastConnection _connection;
        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        public ComponentLoaderSection LoaderConfig { get; private set; }

        protected virtual void OnCreateMCastConnection()
        {
            string[] split = Context.GetEnvironmentVariable<string>("AppUpdateGroup", "239.10.10.10:9999").Split(':');

            try
            {
                _connection = new MulticastConnection(new IPEndPoint(IPAddress.Parse(split[0]), int.Parse(split[1])),
                   true,
                   true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not join App update multicast group: " + Context.GetEnvironmentVariable<string>("AppUpdateGroup", "239.10.10.10:9999"));
            }
        }

        string _appProviderUri;
        protected virtual string OnDiscoverAppProviderUri()
        {
            int count = 0;
            while(count < Context.GetEnvironmentVariable<int>("UpdateRetry", 5))
            {
                try
                {
                    ServiceOperation result = _connection.Call(Product,
                        "Apps",
                        "GetProvider",
                        StandardFormats.BINARY,
                        TimeSpan.FromSeconds(3));
                    return ((DiscoveryRequest)result.Parameters.Where(p => p.Direction == ParameterDirection.Return).First().Value).ProviderUri;
                }
                catch (TimeoutException)
                {
                }
                count++;
            }
            Logger.LogWarn("No app update providers responded to the app provider discovery group message.  App updates will be off for this session.");
            return null;
        }

        protected override void OnApplyLicensing(ILicense[] licenses, params string[] args)
        {
            try
            {
                LoaderConfig = ConfigurationManager.GetSection("componentLoader", Context.CurrentContext) as ComponentLoaderSection;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ComponentLoader section could not be loaded.");
                return;
            }

            if (Altus.Core.Component.App.Instance.Shell.GetComponent<DeclaredAppSerializer>() == null)
            {
                Altus.Core.Component.App.Instance.Shell.Add(new DeclaredAppSerializer());
            }
            List<DeclaredApp> apps = new List<DeclaredApp>();
            foreach (ILicense license in licenses)
            {
                DeclaredApp[] appsTokens = license.GetTokens<DeclaredApp>("//App").ToArray();
                if (appsTokens.Length == 0)
                {
                    ILicense[] lics = new ILicense[Context.CurrentContext.CurrentApp.Licenses.Length + 1];
                    Context.CurrentContext.CurrentApp.Licenses.CopyTo(lics, 0);
                    lics[lics.Length - 1] = license;
                    Context.CurrentContext.CurrentApp.Licenses = lics;
                }
                else 
                { 
                    foreach(DeclaredApp app in appsTokens)
                    {
                        app.Licenses = new ILicense[] { license };
                    }
                }
                apps.AddRange(appsTokens);
            }
            foreach (DeclaredApp app in apps)
            {
                app.Product = Product;
                DeclaredApp rapp = app;
                app.CodeBase = OnGetAppVersionDirectory(ref rapp);
            }
            _apps.AddRange(apps);
        }

        protected override bool OnIsLicensed(object app)
        {
            return true;
        }

        List<DeclaredApp> _apps = new List<DeclaredApp>();
        public IEnumerable<DeclaredApp> Apps { get { return _apps; } }
        public DeclaredApp this[string appName]
        {
            get { return _apps.Where(a => a.Name.Equals(appName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault(); }
        }

        public DiscoveryFileElement GetFile(string appName, string fileName)
        {
            DeclaredApp app = Apps.Where(a => a.Name.Equals(appName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (app != null
                && app.Manifest != null)
            {
                DiscoveryTarget target = app.Manifest.Targets.Where(t => t.Product.Equals(Product, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (target != null)
                {
                    return target.Files.Where(f => f.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                }
            }
            return null;
        }

        public DiscoveryFileElement GetAppConfig(string appName)
        {
            DeclaredApp app = Apps.Where(a => a.Name.Equals(appName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (app != null
                && app.Manifest != null)
            {
                DiscoveryTarget target = app.Manifest.Targets.Where(t => t.Product.Equals(Product, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (target != null)
                {
                    return target.Files.Where(f =>f.IsAppConfig).FirstOrDefault();
                }
            }
            return null;
        }

        public DiscoveryFileElement GetDatabase(string appName)
        {
            DeclaredApp app = Apps.Where(a => a.Name.Equals(appName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (app != null
                && app.Manifest != null)
            {
                DiscoveryTarget target = app.Manifest.Targets.Where(t => t.Product.Equals(Product, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (target != null)
                {
                    return target.Files.Where(f => f.IsDatabase).FirstOrDefault();
                }
            }
            return null;
        }

        public IEnumerable<Assembly> GetReflectables(string appName)
        {
            DeclaredApp app = Apps.Where(a => a.Name.Equals(appName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (app != null
                && app.Manifest != null)
            {
                DiscoveryTarget target = app.Manifest.Targets.Where(t => t.Product.Equals(Product, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (target != null)
                {
                    foreach (DiscoveryFileElement file in target.Files.Where(f => f.Reflect))
                    {
                        yield return file.LoadedAssembly;
                    }
                }
            }
        }
    }
}
