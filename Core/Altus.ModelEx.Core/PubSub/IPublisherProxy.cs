using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Processing;

namespace Altus.Core.PubSub
{
    public interface IPublisherProxy
    {
        Subscription Subscription { get; }
        void Respond(params ServiceParameter[] parms);
        void Update();
    }
}
