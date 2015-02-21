using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Altus.Core.Serialization.Json
{
    public interface IJsonSerializer : ISerializer
    {
        IEnumerable<Type> SupportedTypes { get; }
        IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer);
        object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer);
    }
}
