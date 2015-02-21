using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Altus.Core.Pipeline
{
    public interface IProcessor<T> : IComponent where T: class
    {
        void Process(T request);
    }
}
