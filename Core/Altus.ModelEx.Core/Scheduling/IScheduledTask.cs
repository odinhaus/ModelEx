using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Altus.Core.Licensing;

namespace Altus.Core.Scheduling
{
    public interface IScheduledTask : IComponent
    {
        object Execute(params object[] args);
        Schedule Schedule { get; set; }
        string Name { get; }
        void Kill();
        System.Threading.ThreadPriority Priority { get; }
        byte ProcessorAffinityMask { get; }
        DeclaredApp App { get; }
    }

    public interface IIsolatedScheduledTask : IScheduledTask { }
}
