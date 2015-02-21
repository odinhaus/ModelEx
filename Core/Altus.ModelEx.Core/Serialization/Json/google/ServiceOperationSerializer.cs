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
using System.Text.RegularExpressions;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.Google.ServiceOperationSerializer))]
namespace Altus.Core.Serialization.Json.Google
{
    public class ServiceOperationSerializer : InitializableComponent, ISerializer<ServiceOperation>
    {
        public bool IsScalar { get { return false; } }
        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }
        public int Priority { get; private set; }
        public bool SupportsFormat(string format)
        {
            return format.Equals("google", StringComparison.InvariantCultureIgnoreCase);
        }

        public bool SupportsType(Type type)
        {
            return type.Equals(typeof(ServiceOperation));
        }

        public byte[] Serialize(object source)
        {
            return Serialize((ServiceOperation)source);
        }

        public byte[] Serialize(ServiceOperation source)
        {
            ServiceParameter p = source.Parameters.Where(sp => sp.Direction == ParameterDirection.Return).FirstOrDefault();
            JavaScriptSerializer jss = new JavaScriptSerializer();
            IJsonSerializer[] jsss = App.Instance.Shell.GetComponents<IJsonSerializer>().Where(js => js.SupportsFormat("google")).ToArray();
            if (jsss != null && jsss.Length > 0)
            {
                jss.RegisterConverters(jsss.OfType<JavaScriptConverter>());
            }
            jss.MaxJsonLength = int.MaxValue;
            string json;
            if (p == null)
            {
                p = source.Parameters.Where(sp => sp.Direction == ParameterDirection.Error).FirstOrDefault();
                if (p == null)
                {
                    json = jss.Serialize("");
                }
                else
                {
                    json = jss.Serialize(p.Value);
                }
            }
            else
            {
                
                json = jss.Serialize(p.Value);
            }

            ServiceParameter cbp = source.Parameters.Where(ap => ap.Name.Equals("jsoncallback", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (cbp != null)
            {
                Regex r = new Regex(@"\\/Date(?<date>.+?)\\/");
                json = r.Replace(json, "Date${date}");
                json = cbp.Value.ToString() + "(" + json + ")";
            }

            return SerializationContext.TextEncoding.GetBytes(json);
        }

        public void Serialize(ServiceOperation source, System.IO.Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        public ServiceOperation Deserialize(byte[] source)
        {
            throw new NotImplementedException();
        }

        public ServiceOperation Deserialize(System.IO.Stream inputSource)
        {
            throw new NotImplementedException();
        }

        object ISerializer.Deserialize(byte[] source, Type targetType)
        {
            throw new NotImplementedException();
        }
    }
}
