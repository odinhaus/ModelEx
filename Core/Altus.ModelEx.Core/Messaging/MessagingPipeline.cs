using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.component;
using Altus.pipeline;
using Altus.processing;

namespace Altus.messaging
{
    public class MessagingPipeline : InitializableComponent, IPipeline<ServiceContext>, IComparer<IProcessor<ServiceContext>>
    {
        protected override void OnInitialize(params string[] args)
        {
            List<IProcessor<ServiceContext>> procs = new List<IProcessor<ServiceContext>>(Application.Instance.Shell.GetComponents<IProcessor<ServiceContext>>())
                .Where(proc => proc != this).ToList();
            procs.Sort(this);
            Processors = procs.ToArray();
        }

        public IProcessor<ServiceContext>[] Processors
        {
            get;
            private set;
        }

        public ServiceContext Process(ServiceContext request)
        {
            ServiceContext mctx = request;
            foreach (IProcessor<ServiceContext> proc in this.Processors.Where(p => p.CanProcess(request)))
            {
                mctx = proc.Process(mctx);
            }
            return mctx;
        }

        public int Priority { get { return 0; } }

        public int Compare(IProcessor<ServiceContext> x, IProcessor<ServiceContext> y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return (x.Priority.CompareTo(y.Priority));
        }

        public bool CanProcess(ServiceContext request)
        {
            return request.ServiceProtocol.Equals("http", StringComparison.InvariantCultureIgnoreCase)
                || request.ServiceProtocol.Equals("tcp", StringComparison.InvariantCultureIgnoreCase)
                || request.ServiceProtocol.Equals("udp", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
