using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Messaging;
using Altus.Core.Streams;
using System.IO;
using System.Web.Script.Serialization;
using Altus.Core.Processing;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.Google.MessageSerializer))]
namespace Altus.Core.Serialization.Json.Google
{
    public class MessageSerializer : InitializableComponent, ISerializer<Message>
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
            return type.Equals(typeof(Message));
        }

        public byte[] Serialize(object source)
        {
            return Serialize((Message)source);
        }

        public byte[] Serialize(Message source)
        {
            //ServiceParameter p = source.Parameters.Where(prm => prm.Direction == processing.ParameterDirection.Return).FirstOrDefault();
            //if (p == null)
            //{
            //    // return a null response???
            //    return new byte[0];
            //}
            //else
            //{
            //    ISerializer serializer = Application.Instance.Shell.GetComponents<ISerializer>()
            //        .Where(s => s.SupportsFormat("google") && s.SupportsType(p.Value.GetType())).FirstOrDefault();
            //    if (serializer == null)
            //    {
            //        // return a serialization error?
            //        throw (new SerializationException("A serializer could not be found for type " + p.Value.GetType().FullName + " support the google format."));
            //    }
            //    else
            //    {
            //        return serializer.Serialize(p.Value);
            //    }
            //}
            ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>()
                    .Where(s => s.SupportsFormat("google") && s.SupportsType(source.Payload.GetType())).FirstOrDefault();
            if (serializer == null)
            {
                // return a serialization error?
                throw (new SerializationException("A serializer could not be found for type " + source.Payload.GetType().FullName + " support the google format."));
            }
            else
            {
                return serializer.Serialize(source.Payload);
            }
        }

        public void Serialize(Message source, System.IO.Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        public Message Deserialize(byte[] source)
        {
            throw new NotImplementedException();
        }

        public Message Deserialize(System.IO.Stream inputSource)
        {
            throw new NotImplementedException();
        }

        object ISerializer.Deserialize(byte[] source, Type targetType)
        {
            throw new NotImplementedException();
        }
    }
}
