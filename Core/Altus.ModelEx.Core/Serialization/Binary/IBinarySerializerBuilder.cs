using System;
using System.ComponentModel;
namespace Altus.Core.Serialization.Binary
{
    public interface IBinarySerializerBuilder : IComponent
    {
        ISerializer CreateSerializerType(Type type);
        ISerializer<T> CreateSerializerType<T>();
    }
}
