using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Altus.Core.Dynamic
{
    public interface IDynamicPropertyEvaluator : INotifyPropertyChanged
    {
        dynamic Instance { get; }
        Func<object> Gettor { get; }
        Action<Object> Settor { get; }
    }
}
