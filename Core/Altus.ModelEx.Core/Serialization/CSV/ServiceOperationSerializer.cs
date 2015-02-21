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

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Csv.ServiceOperationSerializer))]
namespace Altus.Core.Serialization.Csv
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
            return format.Equals(StandardFormats.CSV, StringComparison.InvariantCultureIgnoreCase);
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
            object value = "";
            if (p == null)
            {
                p = source.Parameters.Where(sp => sp.Direction == ParameterDirection.Error).FirstOrDefault();
            }

            if (p != null)
            {
                value = p.Value;
            }

            ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>().Where(
                s => s.SupportsFormat(StandardFormats.CSV) && s.SupportsType(value.GetType())).FirstOrDefault();

            if (serializer == null)
            {
                throw (new SerializationException("A serializer could not be found for type " + value.GetType().FullName + " support the " + StandardFormats.CSV + " format."));
            }
            else
            {
                return serializer.Serialize(value);
            }
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
