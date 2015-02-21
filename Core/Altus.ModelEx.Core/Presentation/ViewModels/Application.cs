using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Dynamic;
using Altus.Core.Topology;
using Altus.Core.Data;
using System.ComponentModel;
using Altus.Core.Component;
using Altus.Core.Presentation.ViewModels;
using Altus.Core.Security;


namespace Altus.Core.Presentation.ViewModels
{
    public class Application : Extendable<Application>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="backingInstance"></param>
        public Application(string name, NodeAddress backingInstance)
            : base(name, backingInstance)
        {

        }

        public NodeAddress NodeAddress 
        {
            get { return (NodeAddress)this.BackingInstance; }
            private set
            {
                this.BackingInstance = value;
            }
        }

        protected override string OnGetInstanceType()
        {
            return "Node";
        }

        protected override IEnumerable<string> OnGetAliases()
        {
            return DataContext.Default.GetApplicationAliases(this);
        }

        protected override IEnumerable<DynamicProperty<Application>> OnGetProperties()
        {
            return DataContext.Default.GetApplicationProperties(this);
        }

        protected override IEnumerable<DynamicFunction<Application>> OnGetFunctions()
        {
            return DataContext.Default.GetApplicationFunctions(this);
        }

        private static Application _instance;
        public static Application Instance 
        { 
            get
            {
                if (_instance == null)
                {
                    _instance = new Application(Context.GetEnvironmentVariable("Instance").ToString(), (NodeAddress)NodeIdentity.NodeAddress);
                }
                return _instance;
            }
        }
    }
}
