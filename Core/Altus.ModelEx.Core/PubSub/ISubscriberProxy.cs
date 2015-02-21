using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Processing;

namespace Altus.Core.PubSub
{
    public interface ISubscriberProxy
    {
        Subscription Subscription { get; }
        void PublishData(params ServiceParameter[] parameters);
        void PublishError(params ServiceParameter[] parameters);
        bool RequestDefinition();
    }
}
