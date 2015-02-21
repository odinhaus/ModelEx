using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Processing;
using Altus.Core.Streams;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using Altus.Core;
using System.Collections;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.ServiceOperationSerializer))]
namespace Altus.Core.Serialization.Json
{
    public class ServiceOperationSerializer : JsonSerializer<ServiceOperation>
    {
        protected override object OnDeserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            ServiceOperation op = new ServiceOperation(
                (OperationType)dictionary["type"],
                (ServiceType)dictionary["serviceType"],
                (string)dictionary["serviceUri"]);

            ISerializer<ServiceParameter> ser = App.Instance.Shell.GetComponents<ISerializer<ServiceParameter>>().Where(s =>
                s.SupportsFormat(StandardFormats.JSON) && s.SupportsType(typeof(ServiceParameter))).FirstOrDefault();

            if (ser == null) throw (new SerializationException("A serializer for type " + typeof(ServiceParameter).FullName + " in " + StandardFormats.JSON + " format could not be found."));

            foreach (Dictionary<string, object> parm in (ArrayList)dictionary["parameters"])
            {
                op.Parameters.Add((ServiceParameter)((JsonSerializer)ser).Deserialize(parm, typeof(ServiceParameter), serializer));
            }

            return op;
        }

        protected override IDictionary<string, object> OnSerialize(object obj, JavaScriptSerializer serializer)
        {
            Dictionary<string, object> jsonProps = new Dictionary<string, object>();
            ServiceOperation op = obj as ServiceOperation;
            if (op != null)
            {
                jsonProps.Add("operation", op.Operation);
                jsonProps.Add("format", op.Format);
                jsonProps.Add("type", op.Type);
                jsonProps.Add("serviceType", op.ServiceType);
                
                jsonProps.Add("serviceUri", op.ServiceUri);
                jsonProps.Add("protocol", op.Protocol);
                jsonProps.Add("nodeAddress", op.NodeAddress);
                jsonProps.Add("application", op.Application);
                jsonProps.Add("port", op.Port);
                jsonProps.Add("objectPath", op.ObjectPath);
                
                //jsonProps.Add("responseUri", op.ResponseUri);
                jsonProps.Add("endPoint", op.EndPoint.ToString());
                
                
                jsonProps.Add("parameters", op.Parameters.Where(sp => sp.Direction != ParameterDirection.Aspect).ToArray());
                jsonProps.Add("additionalPayload", op.AdditionalPayload);
            }
            return jsonProps;
        }
    }
}
