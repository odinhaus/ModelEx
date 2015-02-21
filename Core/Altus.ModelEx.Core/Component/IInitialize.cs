using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Component
{
    public interface IInitialize
    {
        void Initialize(string name, params string[] args);
        bool IsInitialized { get; }
        bool IsEnabled { get; }
    }
}

