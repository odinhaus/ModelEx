using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Exceptions;
using Altus.Core.Streams;

[assembly: Component(ComponentType=typeof(Altus.Core.Serialization.Html.GenericExceptionSerializer))]

namespace Altus.Core.Serialization.Html
{
    public class GenericExceptionSerializer : InitializableComponent, ISerializer<RuntimeException>
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
            return type.Equals(typeof(RuntimeException));
        }

        public byte[] Serialize(object source)
        {
            return Serialize((RuntimeException)source);
        }

        public byte[] Serialize(RuntimeException source)
        {
            return ReflectionSerializer.Serialize(source);
        }

        public void Serialize(RuntimeException source, System.IO.Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        public RuntimeException Deserialize(byte[] source)
        {
            throw new NotImplementedException();
        }

        public RuntimeException Deserialize(System.IO.Stream inputSource)
        {
            throw new NotImplementedException();
        }



        object ISerializer.Deserialize(byte[] source, Type targetType)
        {
            throw new NotImplementedException();
        }
    }
}
