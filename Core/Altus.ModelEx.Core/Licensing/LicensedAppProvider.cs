using Altus.Core.Component;
using Altus.Core.Diagnostics;
using Altus.Core.Licensing;
using Altus.Core.Messaging.Tcp;
using Altus.Core.Net;
using Altus.Core.Processing.Rpc;
using Altus.Core.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core.Licensing
{
    [InstallerComponent()]
    [RpcEndPoint("*://*:*/*/Apps")]
    public class LicensedAppProvider : LicensedAppInstaller
    {
        public static LicensedAppInstaller Create()
        {
            LicensedAppProvider instance = new LicensedAppProvider();
            return instance;
        }

        protected LicensedAppProvider() : base() { }

        protected override string OnDiscoverAppProviderUri()
        {
            return null;
        }

        [RpcOperation("GetProvider", SingletonTarget=true)]
        private DiscoveryRequest GetProvider()
        {
            if (RpcContext.Current.Sender == NodeIdentity.NodeAddress)
            {
                RpcContext.Current.Terminate();
                return null;
            }

            DiscoveryRequest response = new DiscoveryRequest()
            {
                ProviderUri = string.Format("tcp://{0}/{1}/Apps(GetApp)[bin]",
                TcpHost.ListenerEndPoint.ToString(),
                Context.GetEnvironmentVariable<string>("Instance", "Altus"))
            };
            Logger.LogInfo("Advertising provider Uri " + response.ProviderUri + " to " + RpcContext.Current.Sender);
            return response;

        }

        [RpcOperation("GetApp", SingletonTarget = true)]
        public DeclaredApp GetApp(DeclaredApp app)
        {
            if (RpcContext.Current != null
                && RpcContext.Current.Sender == NodeIdentity.NodeAddress)
            {
                RpcContext.Current.Terminate();
                return null;
            }

            DeclaredApp dc = Apps.Where(c => c.Name.Equals(app.Name, StringComparison.InvariantCultureIgnoreCase)
                && c.Version.Equals(app.Version)).FirstOrDefault();

            if (dc == null)
            {
                if (RpcContext.Current != null)
                    RpcContext.Current.Cancel();
                return null;
            }
            else
            {
                
                DeclaredApp copy = new DeclaredApp()
                {
                    IncludeSourceBytes = app.IncludeSourceBytes,
                    IsLocal = app.IsLocal,
                    Name = app.Name,
                    SourceUri = app.SourceUri,
                    Version = dc.Version,
                    DeletePrevious = app.DeletePrevious,
                    CodeBase = dc.CodeBase
                };

                byte[] bytes = new byte[0];
                if (app.IncludeSourceBytes)
                {
                    OnGetLocalSource(ref copy, app.Product);
                }

                foreach (AppFile file in copy.Files)
                {
                    if (app.IncludeSourceBytes)
                    {
                        AppFile source = app.Files.Where(cf => cf.Name.Equals(file.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (source == null
                            || string.IsNullOrEmpty(source.Checksum)
                            || !source.Checksum.Equals(file.Checksum, StringComparison.InvariantCultureIgnoreCase))
                        {
                            string path = file.CodeBase;
                            string fileSource = copy.Manifest.Targets.Where(t => t.Product.Equals(app.Product, StringComparison.InvariantCultureIgnoreCase))
                                .First().Files.Where(f => f.Name.Equals(file.Name, StringComparison.InvariantCultureIgnoreCase))
                                .First().Source;
                            if (!string.IsNullOrEmpty(fileSource))
                            {
                                if (!Path.IsPathRooted(fileSource))
                                {
                                    fileSource = Path.Combine(dc.CodeBase, fileSource);
                                }
                                path = fileSource;
                            }
                            file.Data = ReadData(path);
                        }
                    }
                    file.CodeBase = file.CodeBase.Replace(dc.CodeBase, app.CodeBase);
                }

                if (RpcContext.Current == null)
                {
                    Logger.LogInfo("Updating latest " + app.Name + " local sources.");
                }
                else
                {
                    Logger.LogInfo("Sending latest " + app.Name + " sources to " + RpcContext.Current.Sender);
                }

                return copy;
            }
        }
    }
}
