using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Altus.Core.Configuration;
using Altus.Core.Licensing;

namespace Altus.GEM.Install
{
    public class ConfigValidator : Altus.Core.Configuration.ConfigValidator<Altus.GEM.Schema.ModelEx>
    {
        public ConfigValidator(DeclaredApp app, bool updateConfig) 
            : base(updateConfig, "modelex", "GEM_Config.xsd")
        {
            this.App = app;
        }
        public DeclaredApp App { get; private set; }

        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }

        protected override bool OnValidate(Altus.GEM.Schema.ModelEx config, out IEnumerable<Core.Configuration.ConfigMessage> errors)
        {
            List<ConfigMessage> cerr = new List<ConfigMessage>();

            OnValidate(config, cerr);

            errors = cerr.ToArray();
            return cerr.Where(cm => cm.Level != ConfigMessageLevel.Information).Count() == 0;
        }

        protected virtual void OnValidate(Altus.GEM.Schema.ModelEx config, List<ConfigMessage> cerr)
        {
            
        }
    }
}
