using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.component;
using Altus.pipeline;
using Altus.messaging;
using System.IO;

namespace Altus.dynamic
{
    public class AssembyLoader : InitializableComponent, IProcessor<MessagingContext>
    {
        protected override void OnInitialize(params string[] args)
        {
            
        }

        public bool CanProcess(MessagingContext request)
        {
            return request.ServiceMethod.Equals("load", StringComparison.InvariantCultureIgnoreCase)
                && request.RequestObject.GetType().Name.Equals("assembly", StringComparison.InvariantCultureIgnoreCase);
        }

        public MessagingContext Process(MessagingContext request)
        {
            string assembly = request.Arguments;//.ServiceUri.Segments[5];
            byte[] fileBytes = File.ReadAllBytes(assembly);
            request.ResponseObject = fileBytes;
            return request;
        }

        public int Priority
        {
            get { return 1; }
        }
    }
}
