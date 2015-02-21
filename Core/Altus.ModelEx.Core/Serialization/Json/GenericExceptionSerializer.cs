using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Exceptions;
using System.Web.Script.Serialization;
using Altus.Core.Streams;
using Altus.Core.Component;
using Altus.Core.Serialization.Json;

[assembly: Component(ComponentType=typeof(GenericExceptionSerializer))]
namespace Altus.Core.Serialization.Json
{
    public class GenericExceptionSerializer : JsonSerializer<RuntimeException>
    {
        protected override object OnDeserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            RuntimeException ex = new RuntimeException((ExceptionCode)dictionary["code"], dictionary["message"].ToString());
            return ex;
        }

        protected override IDictionary<string, object> OnSerialize(object obj, JavaScriptSerializer serializer)
        {
            Dictionary<string, object> props = new Dictionary<string, object>();
            RuntimeException ex = obj as RuntimeException;

            props.Add("message", ex.Message);
            props.Add("code", ex.Code);

            return props;
        }
    }
}
