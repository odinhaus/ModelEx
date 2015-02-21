using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Processing;
using Altus.Core.Serialization.Binary;

[assembly: Component(ComponentType=typeof(ComplexSerializer))]

namespace Altus.Core.Serialization.Binary
{
    public class ComplexSerializer : InitializableComponent, ISerializer
    {
        public bool IsScalar { get { return false; } }
        public ComplexSerializer()
        {
            Priority = int.MaxValue;
        }

        protected override bool OnInitialize(params string[] args)
        {
            Builder = App.Instance.Shell.GetComponent<IBinarySerializerBuilder>();
            if (Builder == null)
                App.Instance.Shell.ComponentChanged += new CompositionContainerComponentChangedHandler(Shell_ComponentChanged);
            return true;
        }

        void Shell_ComponentChanged(object sender, CompositionContainerComponentEventArgs e)
        {
            if (e.Change == CompositionContainerComponentChange.Add
                && e.Component is IBinarySerializerBuilder)
            {
                this.Builder = e.Component as IBinarySerializerBuilder;
            }
        }

        public int Priority { get; private set; }

        private IBinarySerializerBuilder Builder;

        public bool SupportsFormat(string format)
        {
            return format.Equals(StandardFormats.BINARY, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool SupportsType(Type type)
        {
            return !PrimitiveSerializer.IsPrimitive(type) 
                && type != typeof(ServiceOperation)
                && type != typeof(ServiceOperationSerializer)
                && type != typeof(string);
        }
        static Dictionary<Type, ISerializer> _serializers = new Dictionary<Type, ISerializer>();
        public byte[] Serialize(object source)
        {
            if (Builder == null) throw (new SerializationException("Binary serializer builder could not be found."));

            ISerializer serializer = null;
            Type t = source.GetType();
            try
            {
                serializer = _serializers[t];
            }
            catch
            {
                serializer = Builder.CreateSerializerType(source.GetType());
                try
                {
                    _serializers.Add(t, serializer);
                }
                catch { }
            }
            
            if (serializer == null) throw (new SerializationException("Serializer for type\"" + source.GetType().FullName + "\" could not be found supporting the " + StandardFormats.BINARY + " format."));

            return serializer.Serialize(source);
        }

        public object Deserialize(byte[] source, Type targetType)
        {
            IBinarySerializerBuilder builder = App.Instance.Shell.GetComponent<IBinarySerializerBuilder>();
            if (builder == null) throw (new SerializationException("Binary serializer builder could not be found."));

            ISerializer serializer = builder.CreateSerializerType(targetType);
            if (serializer == null) throw (new SerializationException("Serializer for type\"" + targetType.FullName + "\" could not be found supporting the " + StandardFormats.BINARY + " format."));

            return serializer.Deserialize(source, targetType);
        }
    }
}
