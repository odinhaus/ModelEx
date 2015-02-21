using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Pipeline
{
    public interface IPipeline<T> : IProcessor<T> where  T: class
    {
        IProcessor<T>[] Processors { get; }
    }
}
