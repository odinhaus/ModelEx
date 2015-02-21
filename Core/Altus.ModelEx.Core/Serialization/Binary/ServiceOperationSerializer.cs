using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Messaging;
using Altus.Core.Component;
using Altus.Core.Processing;
using Altus.Core.Streams;
using System.IO;
using Altus.Core.Serialization;
using Altus.Core.Serialization.Binary;
using Altus.Core;
using Altus.Core.Data;
using System.Diagnostics;

[assembly: Component(ComponentType = typeof(ServiceOperationSerializer))]

namespace Altus.Core.Serialization.Binary
{
    public class ServiceOperationSerializer : Altus.Core.Processing.ServiceOperation, 
        ISerializer<Altus.Core.Processing.ServiceOperation>, IInitialize
    {
        const int FIELD_LENGTH_OPTIMIZED = 25;
        public int Priority { get; private set; }
        public bool IsScalar { get { return false; } }
        protected byte[] OnSerialize(object source)
        {
            ServiceOperation typed = (ServiceOperation)source;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter br = new BinaryWriter(ms);
                br.Write(typed.ObjectPath == null ? "" : typed.ObjectPath);
                br.Write(typed.NodeAddress == null ? "" : typed.NodeAddress);
                br.Write(typed.Operation == null ? "" : typed.Operation);
                br.Write(typed.Protocol == null ? "" : typed.Protocol);
                br.Write(typed.Format == null ? "" : typed.Format);
                br.Write(typed.Port);
                br.Write((System.Int32)typed.Type);
                br.Write(typed.Application == null ? "" : typed.Application);
                br.Write((System.Int32)typed.ServiceType);
                this.SerializeParameters(typed.Parameters, br);
                if (typed.AdditionalPayload.Length > 0)
                {
                    br.Write(typed.AdditionalPayload);
                }
                
                return ms.ToArray();
            }
        }

        protected object OnDeserialize(byte[] source, Type targetType)
        {
            using (MemoryStream ms = new MemoryStream(source))
            {
                BinaryReader br = new BinaryReader(ms);
                ServiceOperationSerializer typed = new ServiceOperationSerializer();
                //string typeName = br.ReadString();
                typed.ObjectPath = br.ReadString();
                typed.NodeAddress = br.ReadString();
                typed.Operation = br.ReadString();
                typed.Protocol = br.ReadString();
                typed.Format = br.ReadString();
                typed.Port = br.ReadInt32();
                typed.Type = (Altus.Core.Processing.OperationType)(br.ReadInt32());
                typed.Application = br.ReadString();
                typed.ServiceType = (Altus.Core.Processing.ServiceType)(br.ReadInt32());
                typed.Parameters = (Altus.Core.Processing.ServiceParameterCollection)this.DeserializeParameters(br);
                if (br.BaseStream.Length > br.BaseStream.Position + 1)
                {
                    typed.AdditionalPayload = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
                }
                return typed;
            }
        }

        protected bool OnSupportsFormats(string format)
        {
            return format.Equals(StandardFormats.BINARY, StringComparison.InvariantCultureIgnoreCase);
        }

        protected virtual void OnDispose()
        {
        }


        IEnumerable<Type> _types = null;
        public IEnumerable<Type> SupportedTypes
        {
            get { return _types; }
        }

        public void Initialize(string name, params string[] args)
        {
            this.Name = name;
            this._types = OnGetSupportedTypes();
        }

        public bool IsInitialized
        {
            get;
            private set;
        }

        public bool IsEnabled
        {
            get;
            set;
        }

        public string Name { get; set; }

        public bool SupportsFormat(string format)
        {
            return OnSupportsFormats(format);
        }

        public bool SupportsType(Type type)
        {
            return _types != null && _types.Contains(type);
        }

        public byte[] Serialize(object source)
        {
            return OnSerialize(source);
        }

        public object Deserialize(byte[] source, Type targetType)
        {
            return OnDeserialize(source, targetType);
        }

        public event EventHandler Disposed;

        public System.ComponentModel.ISite Site
        {
            get;
            set;
        }

        public void Dispose()
        {
            this.OnDispose();
            if (Disposed != null)
                Disposed(this, new EventArgs());
        }

        public byte[] Serialize(Altus.Core.Processing.ServiceOperation source)
        {
            return this.OnSerialize(source);
        }

        public void Serialize(Altus.Core.Processing.ServiceOperation source, System.IO.Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        public Altus.Core.Processing.ServiceOperation Deserialize(byte[] source)
        {
            return (Altus.Core.Processing.ServiceOperation)this.OnDeserialize(source, typeof(Altus.Core.Processing.ServiceOperation));
        }

        public Altus.Core.Processing.ServiceOperation Deserialize(System.IO.Stream inputSource)
        {
            return Deserialize(StreamHelper.GetBytes(inputSource));
        }

        protected IEnumerable<Type> OnGetSupportedTypes()
        {
            return new Type[] { typeof(ServiceOperation), typeof(ServiceOperationSerializer) };
        }

        protected object DeserializeType(BinaryReader br)
        {
            string tname = br.ReadString();
            if (tname.Equals("<null>", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            else
            {
                Type t = TypeHelper.GetType(tname);
                if (t == null) throw (new SerializationException("Type not found: " + tname));
                ISerializer serializer = Altus.Core.Component.App.Instance.Shell.GetComponents<ISerializer>().Where(s
                    => s.SupportsFormat(StandardFormats.BINARY) && s.SupportsType(t)).FirstOrDefault();
                if (serializer == null) throw (new SerializationException("Serializer not found for type \"" + tname + "\" supporting the " + StandardFormats.BINARY + " format."));

                if (t.IsArray)
                {
                    int count = br.ReadInt32();
                    Array list = (Array)Activator.CreateInstance(t, count);

                    for (int i = 0; i < count; i++)
                    {
                        list.SetValue(serializer.Deserialize(br.ReadBytes(br.ReadInt32()), t), i);
                    }

                    return list;
                }
                else
                {
                    return serializer.Deserialize(br.ReadBytes(br.ReadInt32()), t);
                }

            }
        }

        protected ServiceParameterCollection DeserializeParameters(BinaryReader br)
        {
            ServiceParameterCollection list = new Processing.ServiceParameterCollection();
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                ServiceParameter sp = new Processing.ServiceParameter(br.ReadString(), br.ReadString(), (Processing.ParameterDirection)br.ReadInt32());
                Type t = TypeHelper.GetType(sp.Type);
                if (PrimitiveSerializer.IsPrimitive(t))
                {
                    byte[] data = br.ReadBytes(PrimitiveSerializer.GetByteCount(t));
                    sp.Value = _ps.Deserialize(data, t);
                }
                else
                {
                    sp.Value = this.DeserializeType(br);
                }
                list.Add(sp);
            }

            return list;
        }

        private PrimitiveSerializer _ps = new PrimitiveSerializer();
        protected void SerializeParameters(ServiceParameterCollection source, BinaryWriter br)
        {
            br.Write(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                ServiceParameter sp = source[i];
                br.Write(sp.Name);
                br.Write(sp.Type);
                br.Write((int)sp.Direction);
                Type t = sp.Value.GetType();

                if (PrimitiveSerializer.IsPrimitive(t))
                {
                    byte[] data = _ps.Serialize(sp.Value);
                    br.Write(data, 0, data.Length);
                }
                else
                {
                    SerializeType(sp.Value, br);
                }
            }
        }

        private struct SourceNode
        {
            public string Name;
            public ulong Id;
            public override string ToString()
            {
                return Name;
            }
            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
        }

        static Dictionary<Type, ISerializer> _serializers = new Dictionary<Type, ISerializer>();
        protected void SerializeType(object source, BinaryWriter br)
        {
            if (source == null)
            {
                br.Write(SerializationContext.TextEncoding.GetBytes("<null>"));
            }
            else
            {
                Type t = source.GetType();
                string tname = t.FullName;
                if (typeof(ISerializer).IsAssignableFrom(t))
                {
                    Type baseType = t.BaseType;
                    Type serializerGen = typeof(ISerializer<>);
                    Type serializerSpec = serializerGen.MakeGenericType(baseType);
                    if (serializerSpec.IsAssignableFrom(t))
                    {
                        tname = baseType.FullName;
                    }
                }
               
                ISerializer serializer = null;
                try
                {
                    serializer = _serializers[t];
                }
                catch
                {
                    serializer = Altus.Core.Component.App.Instance.Shell.GetComponents<ISerializer>().Where(s
                    => s.SupportsFormat(StandardFormats.BINARY) && s.SupportsType(t)).FirstOrDefault();
                    try
                    {
                        _serializers.Add(t, serializer);
                    }
                    catch { }
                }
                if (serializer == null) throw (new SerializationException("Serializer not found for type \"" + tname + "\" supporting the " + StandardFormats.BINARY + " format."));
                if (t.IsArray)
                {
                    br.Write(((Array)source).Length);
                    foreach (object item in (Array)source)
                    {
                        byte[] data = serializer.Serialize(source);
                        br.Write(tname);
                        br.Write(data.Length);
                        br.Write(data);
                    }
                }
                else
                {
                    byte[] data = serializer.Serialize(source);
                    br.Write(tname);
                    br.Write(data.Length);
                    br.Write(data);
                }
            }
        }
    }
}