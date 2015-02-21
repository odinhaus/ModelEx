using System;
using System.ComponentModel;
using System.IO;
using Altus.Core.Realtime;
using System.Collections.Generic;
namespace Altus.Core.Realtime
{
    public interface IFieldSource : IComponent
    {
        bool ContainsField(string fieldName);
        Field[] Current { get; }
        string Name { get; }

        Field Read(uint fieldId);
    }
}
