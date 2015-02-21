using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Pipeline;
using Altus.Core.Component;
using Altus.Core.Configuration;
using Altus.Core;
using Altus.Core.Diagnostics;

namespace Altus.Core.Processing.Msp
{
    public class MspPipeline : InitializableComponent, IPipeline<ServiceContext>
    {
        static List<IProcessor<ServiceContext>> _processors = new List<IProcessor<ServiceContext>>();
        static List<Type> _procTypes = new List<Type>();
        static bool _firstRun = true;

        public MspPipeline(IProcessor<ServiceContext> handler)
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
                    try
                    {
                        MspProcessorsSection section = ConfigurationManager.GetSection("mspPipeline", Context.CurrentContext) as MspProcessorsSection;
                        if (section != null)
                        {
                            section.Processors.Sort();

                            foreach (MspProcessorElement e in section.Processors)
                            {
                                try
                                {
                                    _procTypes.Add(TypeHelper.GetType(e.TypeName, true));
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogWarn(ex, "MspPipeline processor type not found and will be ignored.");
                                }
                            }
                            ResetProcessors();
                        }
                    }
                    catch { }
                    _firstRun = false;
                }
            }
            return true;
        }

        private static void ResetProcessors()
        {
            lock (_procTypes)
            {
                _processors.Clear();
                foreach (Type t in _procTypes)
                {
                    _processors.Add((IProcessor<ServiceContext>)Activator.CreateInstance(t, new object[] { }));
                }
            }
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
            ResetProcessors();
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
