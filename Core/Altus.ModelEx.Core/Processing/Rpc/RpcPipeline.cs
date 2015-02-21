using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Pipeline;
using Altus.Core.Component;
using Altus.Core.Configuration;
using Altus.Core;

namespace Altus.Core.Processing.Rpc
{
    public class RpcPipeline : InitializableComponent, IPipeline<ServiceContext>
    {
        static List<IProcessor<ServiceContext>> _processors = new List<IProcessor<ServiceContext>>();
        static bool _firstRun = true;

        public RpcPipeline(IProcessor<ServiceContext> handler)
        {
            OnInitialize();
            List<IProcessor<ServiceContext>> processors = new List<IProcessor<ServiceContext>>(_processors);
            processors.Add(handler);
            this.Processors = processors.ToArray();
        }

        protected override bool OnInitialize(params string[] args)
        {
            lock (_processors)
            {
                if (_firstRun)
                {
                    RpcProcessorsSection section = ConfigurationManager.GetSection("rpcProcessors", Context.CurrentContext) as RpcProcessorsSection;
                    if (section != null)
                    {
                        section.Processors.Sort();
                        foreach (RpcProcessorElement e in section.Processors)
                        {
                            _processors.Add((IProcessor<ServiceContext>)TypeHelper.CreateType(e.TypeName, new object[] { }));
                        }
                    }
                    _firstRun = false;
                }
            }
            return true;
        }

        public IProcessor<ServiceContext>[] Processors
        {
            get;
            private set;
        }

        public void Process(ServiceContext request)
        {
            foreach (IProcessor<ServiceContext> processor in this.Processors)
            {
                if (processor != null)
                    processor.Process(request);
                if (request.IsCanceled) break;
            }
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
