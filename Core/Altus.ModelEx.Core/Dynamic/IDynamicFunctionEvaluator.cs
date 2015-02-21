using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Dynamic
{
    public interface IDynamicFunctionEvaluator
    {
        object Execute(string methodName, object[] args);
    }
}
