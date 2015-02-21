using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core.Processing;
using Altus.Core.Streams;
using System.Web.Script.Serialization;
using System.Collections;
using Altus.Core;
using Altus.Core.Diagnostics;
using System.Reflection;

[assembly: Component(ComponentType = typeof(Altus.Core.Serialization.Json.ServiceParameterCollectionSerializer))]
namespace Altus.Core.Serialization.Json
{
    public class ServiceParameterCollectionSerializer : InitializableComponent, ISerializer<ServiceParameterCollection>, ISerializerTypeResolver
    {
        SerializationContext _sContext = null;
        public bool IsScalar { get { return false; } }
        protected override bool OnInitialize(params string[] args)
        {
            _sContext = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();
            return true;
        }
        public int Priority { get; private set; }
        public bool SupportsFormat(string format)
        {
            return format.Equals(StandardFormats.JSON, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool SupportsType(Type type)
        {
            return type.Equals(typeof(ServiceParameterCollection));
        }


        public byte[] Serialize(ServiceParameterCollection source)
        {
            if (_sContext == null)
                _sContext = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();

            if (_sContext == null)
                throw (new InvalidOperationException("Serialization manager could not be found"));

            if (SerializationContext.TextEncoding == null)
                SerializationContext.TextEncoding = System.Text.Encoding.Unicode;

            StringBuilder sb = new StringBuilder();

            sb.Append("{ \"parameters\": [");
            bool isFirst = true;

            foreach (ServiceParameter p in source)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sb.Append(",");
                sb.Append("\n\t{");
                sb.Append("\n\t\t\"name\": \"" + p.Name + "\",");
                sb.Append("\n\t\t\"type\": \"" + p.Type + "\",");
                sb.Append("\n\t\t\"direction\": \"" + p.Direction + "\",");
                if (p.Value == null)
                {
                    sb.Append("\n\t\t\"value\": \"null\"");
                }
                else
                {
                    ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>().Where(
                        c => c.SupportsFormat(StandardFormats.JSON) && c.SupportsType(p.Value.GetType())).FirstOrDefault();
                    if (serializer == null)
                    {
                        sb.Append("\n\t\t\"value\": \"" + p.Value.ToString() + "\"");
                    }
                    else
                    {
                        sb.Append("\n\t\t\"value\": " + SerializationContext.TextEncoding.GetString(serializer.Serialize(p.Value)));
                    }
                }
                sb.Append("\n\t}");
            }

            sb.Append("]}");

            return SerializationContext.TextEncoding.GetBytes(sb.ToString());
        }

        public void Serialize(ServiceParameterCollection source, System.IO.Stream outputStream)
        {
            StreamHelper.Copy(Serialize(source), outputStream);
        }

        public byte[] Serialize(object source)
        {
            return this.Serialize((ServiceParameterCollection)source);
        }

        public ServiceParameterCollection Deserialize(byte[] source)
        {
            if (_sContext == null)
                _sContext = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();

            if (_sContext == null)
                throw (new InvalidOperationException("Serialization manager could not be found"));

            if (SerializationContext.TextEncoding == null)
                SerializationContext.TextEncoding = System.Text.Encoding.Unicode;

            ServiceParameterCollection collection = new ServiceParameterCollection();

            string jsonBody = SerializationContext.TextEncoding.GetString(source);
            Hashtable decoded = (Hashtable)JsonParser.JsonDecode(jsonBody);
            IDictionaryEnumerator en = decoded.GetEnumerator();
            while (en.MoveNext())
            {
                if (en.Key.ToString().Equals("parameters", StringComparison.InvariantCultureIgnoreCase))
                {
                    int index = 0;
                    foreach (Hashtable parameterJson in (ArrayList)en.Value)
                    {
                        string name = "";
                        string type = "";
                        ParameterDirection direction = ParameterDirection.In;
                        object value = null;
                        string valueJson = "";

                        IDictionaryEnumerator pen = parameterJson.GetEnumerator();
                        while (pen.MoveNext())
                        {
                            if (pen.Key.ToString().Equals("name", StringComparison.InvariantCultureIgnoreCase))
                            {
                                name = pen.Value.ToString();
                            }
                            else if (pen.Key.ToString().Equals("type", StringComparison.InvariantCultureIgnoreCase))
                            {
                                type = pen.Value.ToString();
                            }
                            else if (pen.Key.ToString().Equals("direction", StringComparison.InvariantCultureIgnoreCase))
                            {
                                direction = (ParameterDirection)Enum.Parse(typeof(ParameterDirection),pen.Value.ToString(), true);
                            }
                            else if (pen.Key.ToString().Equals("value", StringComparison.InvariantCultureIgnoreCase))
                            {
                                valueJson = pen.Value.ToString();
                            }
                        }

                        Type valueType = null;
                        valueType = TypeHelper.GetType(type);
                        
                        if (string.IsNullOrEmpty(type)
                            && _resolveHandler != null)
                        {
                            ResolveTypeEventArgs e;
                            if (string.IsNullOrEmpty(name))
                            {
                                e = new ResolveTypeEventArgs(index, Assembly.GetCallingAssembly());
                                valueType = _resolveHandler(this, e);
                                name = e.ParameterName;
                            }
                            else
                            {
                                e = new ResolveTypeEventArgs(name, Assembly.GetCallingAssembly());
                                valueType = _resolveHandler(this, e);
                            }
                        }
                        
                        if (valueType == null)
                        {
                            Logger.LogWarn("Could not resolve parameter type: " + type);
                            value = valueJson;
                        }
                        else
                        {
                            ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>().Where(
                                s => s.SupportsType(valueType) && s.SupportsFormat(StandardFormats.JSON)).FirstOrDefault();
                            if (serializer == null)
                            {
                                Logger.LogWarn("Could not resolve serilaizer for type: " + type + "; format: " + StandardFormats.JSON);
                                value = valueJson;
                            }
                            else
                            {
                                value = serializer.Deserialize(SerializationContext.TextEncoding.GetBytes(valueJson), valueType);
                            }
                        }

                        ServiceParameter sp = new ServiceParameter(name, type, direction) { Value = value };
                        collection.Add(sp);
                        index++;
                    }

                    break;
                }
            }

            return collection;
        }

        public ServiceParameterCollection Deserialize(System.IO.Stream inputSource)
        {
            return this.Deserialize(StreamHelper.GetBytes(inputSource));
        }

        object ISerializer.Deserialize(byte[] source, Type targetType)
        {
            return this.Deserialize(source);
        }

        private ResolveTypeEventHandler _resolveHandler = null;
        public void AddTypeResolver(ResolveTypeEventHandler handler)
        {
            _resolveHandler = handler;
        }
    }
}
