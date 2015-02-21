using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;

namespace Altus.Core.Dynamic.Components
{
    public class DynamicHost : ServiceContainer
    {
        public DynamicHost()
            : base()
        {
        }

        protected override void OnInitialize(CompositionContainerAttribute shellAttribute, params string[] startupArgs)
        {
            bool runAsAService = false;
            this.RunAsService = runAsAService;
            //this.Services = new System.ServiceProcess.ServiceBase[] { new NodeService() };
            base.OnInitialize(shellAttribute, startupArgs);
        }
    }
}