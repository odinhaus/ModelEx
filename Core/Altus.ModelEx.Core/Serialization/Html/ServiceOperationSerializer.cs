using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Processing;
using Altus.Core.Streams;
using System.Reflection;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Html.ServiceOperationSerializer))]
namespace Altus.Core.Serialization.Html
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
            return format.Equals(StandardFormats.HTML);
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
            return ReflectionSerializer.Serialize(source);
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
