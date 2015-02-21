using Altus.Core.Component;
using Altus.Core.Licensing;
using Altus.Core.Serialization.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

[assembly: Component(ComponentType=typeof(DeclaredAppSerializer))]

namespace Altus.Core.Serialization.Xml
{
    public class DeclaredAppSerializer : SerializerBase
    {
        protected override IEnumerable<Type> OnGetSupportedTypes()
        {
            return new Type[] { typeof(DeclaredApp) };
        }

        protected override byte[] OnSerialize(object source)
        {
            XmlSerializer serializer = new XmlSerializer(source.GetType());
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.Serialize(ms, source);
                return ms.ToArray();
            }
        }

        protected override object OnDeserialize(byte[] source, Type targetType)
        {
            XmlSerializer serializer = new XmlSerializer(targetType);
            using (MemoryStream ms = new MemoryStream(source))
            {
                return serializer.Deserialize(ms);
            }
        }

        protected override bool OnSupportsFormats(string format)
        {
            return format.Equals(StandardFormats.XML, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
