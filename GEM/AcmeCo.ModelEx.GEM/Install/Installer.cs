using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Altus.Core;
using Altus.Core.Component;
using Altus.Core.Configuration;
using Altus.Core.Diagnostics;
using Altus.Core.Licensing;

namespace Altus.GEM.Install
{
    public partial class Installer : LicensedInstallerComponent
    {
        Regex _rPlatform = new Regex(@"(?<Prod>GEM)_(?<Code>[\w\.]+)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        Regex _rApp = new Regex(@"(?<Prod>GEM)_(?<App>[\w\.]+)_(?<Code>[\w\.]+)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        protected override bool OnInstall(DeclaredApp app)
        {
            try
            {
                Validator = OnCreateValidator(app);
                IEnumerable<ConfigMessage> configErrors = null;
                DiscoveryFileElement configFile = LicensedAppInstaller.Instance.GetAppConfig(app.Name);
                if (configFile != null 
                    && !Validator.Validate(configFile.CodeBase, out configErrors))
                {
                    foreach (ConfigMessage msg in configErrors)
                    {
                        if (msg.Level == ConfigMessageLevel.Information)
                        {
                            Logger.LogInfo(msg.Message);
                        }
                        else if (msg.Level == ConfigMessageLevel.Warning)
                        {
                            Logger.LogWarn(msg.Message);
                        }
                        else if (msg.Level == ConfigMessageLevel.Error)
                        {
                            Logger.LogError(msg.Message);
                        }
                    }
                    return false;
                }

                bool ret = true;
                if (app.Name == "GEM")
                {
                    if (IsServer)
                    {
                        ret = OnPlatformServerInstall(Validator.Config);
                    }
                    if (IsClient)
                    {
                        ret = ret & OnPlatformClientInstall(Validator.Config);
                    }
                }
                else
                {
                    foreach (ILicense lic in app.Licenses)
                    {
                        DeclaredApp appFound;
                        if (lic.TryGetToken<DeclaredApp>("//App", out appFound))
                        {
                            Match m = _rPlatform.Match(lic.Key);
                            if (m.Success)
                            {
                                string product;
                                if (lic.TryGetToken<string>("//Product", out product))
                                {
                                    if ( product.Trim().Equals("ModelexHost", StringComparison.InvariantCultureIgnoreCase))
                                        OnAppServerInstall(lic, app, Validator.Config);
                                    else
                                        OnAppClientInstall(lic, app, Validator.Config);
                                }
                                break;
                            }
                        }
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                Logger.Log(ex, app.Name + " Installation Failed");
                return false;
            }
        }

        protected virtual ConfigValidator OnCreateValidator(DeclaredApp app) { return new ConfigValidator(app, true); }

        public ConfigValidator Validator { get; private set; }

        bool _isLicensed = false;
        protected override void OnApplyLicensing(ILicense[] licenses, params string[] args)
        {
            foreach (ILicense lic in licenses)
            {
                DeclaredApp app;
                if (lic.TryGetToken<DeclaredApp>("//App", out app))
                {
                    Match m = _rPlatform.Match(lic.Key);
                    if (m.Success)
                    {
                        _isLicensed = true;
                        App = app;
                        string product;
                        if (lic.TryGetToken<string>("//Product", out product))
                        {
                            IsServer = product.Trim().Equals("ModelexHost", StringComparison.InvariantCultureIgnoreCase);
                            IsClient = !IsServer;
                        }
                        break;
                    }
                }
            }
        }

        protected override bool OnIsLicensed(object component)
        {
            return _isLicensed;
        }

        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }

        public DeclaredApp App { get; private set; }

        protected override void OnCreateAppNodeIdentity(DeclaredApp app)
        {
            
        }

        private class EmbeddedCodeReader
        {
            Dictionary<string, string> _codeCache = new Dictionary<string, string>();
            public EmbeddedCodeReader()
            { }


            public string LoadResource(string resourceName)
            {
                string code;
                if (!TryGetEmbeddedResource(resourceName, out code))
                {
                    Logger.LogWarn(string.Format("Resource not found: {0}",
                        resourceName));
                }
                return code;
            }


            private bool TryGetEmbeddedResource(string resourceName, out string code)
            {
                bool found = false;
                code = null;
                try
                {
                    HashSet<Assembly> checkedAssemblies = new HashSet<Assembly>();
                    Assembly assembly = Context.CurrentContext.CurrentApp.PrimaryAssembly
                        ?? typeof(EmbeddedCodeReader).Assembly;
                    string resourceKey = GetResourceKey(assembly, resourceName);

                    found = TryGetManifestResource(resourceKey, assembly, out code);
                    if (!found)
                    {
                        found = false;
                        checkedAssemblies.Add(assembly);
                        assembly = this.GetType().Assembly;
                        resourceKey = GetResourceKey(assembly, resourceName);
                        found = TryGetManifestResource(resourceKey, assembly, out code);
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
                                        resourceKey = GetResourceKey(assembly, resourceName);
                                        found = TryGetManifestResource(resourceKey, assembly, out code);
                                        if (found)
                                            break;
                                        checkedAssemblies.Add(assembly);
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
 
                return found;
            }


            private string GetResourceKey(Assembly assembly, string resourceName)
            {
                return assembly.GetName().Name + "." + resourceName.Replace(@"/", @"\").Replace(@"\", ".");
            }

            private bool TryGetManifestResource(string resourceName, Assembly assembly, out string resource)
            {
                if (!_codeCache.TryGetValue(resourceName, out resource))
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
                                    {
                                        _codeCache.Add(resourceName, resource);
                                        return true;
                                    }
                                }

                            }
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                resource = reader.ReadToEnd();
                                _codeCache.Add(resourceName, resource);
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
                    using (Stream resStream = assembly.GetManifestResourceStream(resName))
                    {
                        using (ResourceReader rdr = new ResourceReader(resStream))
                        {
                            string resType;
                            byte[] resData;
                            resourceName = resourceName.Replace(resName.Replace(".g.resources", "") + ".", "").ToLowerInvariant().Replace(".", "/");
                            int lastSlash = resourceName.LastIndexOf('/');
                            if (lastSlash > 0)
                            {
                                resourceName = resourceName.Substring(0, lastSlash) 
                                    + "." + 
                                    resourceName.Substring(lastSlash+1, resourceName.Length - lastSlash - 1);
                            }
                            rdr.GetResourceData(resourceName,
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
        }
    }
}
