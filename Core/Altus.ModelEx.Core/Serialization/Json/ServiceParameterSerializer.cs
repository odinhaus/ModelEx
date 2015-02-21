using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Processing;
using System.Web.Script.Serialization;
using Altus.Core.Streams;
using Altus.Core.Component;
using Altus.Core;
using Altus.Core.Serialization.Json;

[assembly: Component(ComponentType=typeof(ServiceParameterSerializer))]

namespace Altus.Core.Serialization.Json
{
    public class ServiceParameterSerializer : JsonSerializer<ServiceParameter>
    {
        protected override IDictionary<string, object> OnSerialize(object obj, JavaScriptSerializer serializer)
        {
            Dictionary<string, object> props = new Dictionary<string, object>();
            ServiceParameter sp = obj as ServiceParameter;

            props.Add("name", sp.Name);
            props.Add("type", sp.Type);
            props.Add("direction", sp.Direction);
            if (sp.Value == null || sp.Value.GetType().IsValueType || sp.Value is string)
                props.Add("value", sp.Value);
            else
            {
                ISerializer ser = App.Instance.Shell.GetComponents<ISerializer>().Where(s =>
                    s.SupportsType(sp.Value.GetType()) && s.SupportsFormat(StandardFormats.JSON)).FirstOrDefault();
                if (ser == null) throw (new SerializationException("A serializer for type " + sp.Type + " in " + StandardFormats.JSON + " format could not be found."));
                props.Add("value", SerializationContext.TextEncoding.GetString(ser.Serialize(sp.Value)));
            }

            return props;
        }

        protected override object OnDeserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            ServiceParameter sp = new ServiceParameter(
                dictionary["name"].ToString(), 
                dictionary["type"].ToString(),
                (ParameterDirection)dictionary["direction"]);

            Type t = TypeHelper.GetType(sp.Type);
            ISerializer ser = null;
            if (t != null
                && !t.IsValueType
                && dictionary["value"] != null)
            {
                 ser = App.Instance.Shell.GetComponents<ISerializer>().Where(s =>
                    s.SupportsFormat(StandardFormats.JSON) && s.SupportsType(t)).FirstOrDefault();

                 if (ser == null) throw (new SerializationException("A serializer for type " + sp.Type + " in " + StandardFormats.JSON + " format could not be found."));

                 sp.Value = ser.Deserialize(SerializationContext.TextEncoding.GetBytes(dictionary["value"].ToString()), t);
            }
            else if (t.IsValueType || dictionary["value"] == null)
            {
                sp.Value = dictionary["value"];
            }
            else
            {
                throw (new TypeAccessException("The specified type " + dictionary["type"].ToString() + " could not be found."));
            }

            
            return sp;
        }
    }
}
