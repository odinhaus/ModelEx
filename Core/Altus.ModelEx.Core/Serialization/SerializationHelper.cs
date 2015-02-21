using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using Altus.Core.Streams;

namespace Altus.Core.Serialization
{
    public enum SerializationFormat
    {
        Binary,
        Xml
    }

    public class XmlTextWriterFormattedNoDeclaration : System.Xml.XmlTextWriter
    {
        public XmlTextWriterFormattedNoDeclaration(System.IO.TextWriter w)
            : base(w)
        {
            Formatting = System.Xml.Formatting.Indented;
        }

        public override void WriteStartDocument() { } // suppress
    }

    public static class SerializationHelper
    {
        /// <summary>
        /// Converts the provided source object to an Xml string.  Source object must be
        /// serialization compatible.  If the object contains unspecified types as properties (
        /// properties of type Object), you must also specify the types that those objects
        /// consist of and contain.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="extraTypes"></param>
        /// <returns></returns>
        public static string ToXml(object source, params Type[] extraTypes)
        {
            SmartStream ms = new SmartStream();
            ToXml(source, ms, extraTypes);
            return ASCIIEncoding.ASCII.GetString(ms.ToArray());
        }


        /// <summary>
        /// Converts the provided source object to Xml and writes the Xml to the provided
        /// Stream.  Source object must be
        /// serialization compatible.  If the object contains unspecified types as properties (
        /// properties of type Object), you must also specify the types that those objects
        /// consist of and contain.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="extraTypes"></param>
        public static void ToXml(object source, Stream destination, params Type[] extraTypes)
        {
            XmlSerializer serializer = null;
            //<?xml version="1.0" encoding="utf-8"?>
            if (extraTypes == null || extraTypes.Length == 0)
            {
                serializer = new XmlSerializer(source.GetType());
            }
            else
            {
                serializer = new XmlSerializer(source.GetType(), extraTypes);
            }

            serializer.Serialize(destination, source);
            destination.Position = 0;
        }

        /// <summary>
        /// Converts the provided source object to Xml (With out start document and namespaces) 
        /// and writes the Xml to the provided
        /// Stream.  Source object must be
        /// serialization compatible.  If the object contains unspecified types as properties (
        /// properties of type Object), you must also specify the types that those objects
        /// consist of and contain.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="extraTypes"></param>
        public static string ToXmlWithOutNamespaces(object source, params Type[] extraTypes)
        {
            XmlSerializer serializer = null;
            if (extraTypes == null || extraTypes.Length == 0)
            {
                serializer = new XmlSerializer(source.GetType());
            }
            else
            {
                serializer = new XmlSerializer(source.GetType(), extraTypes);
            }

            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            StringWriter sw = new StringWriter();
            XmlWriter writer = new XmlTextWriterFormattedNoDeclaration(sw);

            serializer.Serialize(writer, source, ns);

            return sw.ToString();
        }

        /// <summary>
        /// Converts the provided source object to Xml and writes the Xml into the
        /// XmlReader returned.  Source object must be xml serialization compatible.
        /// If the object contains unspecified types as properties (
        /// properties of type Object), you must also specify the types that those objects
        /// consist of and contain.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="xmlReader"></param>
        /// <param name="extraTypes"></param>
        public static void ToXml(object source, out XmlReader xmlReader, out Stream xmlStream, params Type[] extraTypes)
        {
            xmlStream = new SmartStream();
            ToXml(source, xmlStream, extraTypes);
            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ConformanceLevel = ConformanceLevel.Fragment;
            xmlReader = XmlReader.Create(xmlStream, xrs, new XmlParserContext(null, null, "en", XmlSpace.Default, Encoding.UTF8));
        }

        /// <summary>
        /// Converts and ASCII formatted XML string into an instance of the provided type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlSource"></param>
        /// <param name="extraTypes"></param>
        /// <returns></returns>
        public static T FromXml<T>(string xmlSource, params Type[] extraTypes)
        {
            SmartStream stream = new SmartStream(ASCIIEncoding.ASCII.GetBytes(xmlSource));
            return FromXml<T>(stream, extraTypes);
        }

        /// <summary>
        /// Converts the ASCII formatted string data in the provided stream to
        /// an instance of the provided type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlSourceStream"></param>
        /// <param name="extraTypes"></param>
        /// <returns></returns>
        public static T FromXml<T>(Stream xmlSourceStream, params Type[] extraTypes)
        {
            try
            {
                XmlSerializer serializer = null;
                if (extraTypes == null || extraTypes.Length == 0)
                {
                    serializer = new XmlSerializer(typeof(T));
                }
                else
                {
                    serializer = new XmlSerializer(typeof(T), extraTypes);
                }
                return (T)serializer.Deserialize(xmlSourceStream);
            }
            finally
            {
            }
        }

        /// <summary>
        /// Converts the ASCII formatted byte data in the provided byte array to an
        /// instance of the specified type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlSourceBytes"></param>
        /// <param name="extraTypes"></param>
        /// <returns></returns>
        public static T FromXml<T>(byte[] xmlSourceBytes, params Type[] extraTypes)
        {
            return FromXml<T>(new SmartStream(xmlSourceBytes), extraTypes);
        }

        /// <summary>
        /// Deserializes the provided xml into the type specified by sourceType.
        /// If any additional types are required for deserialization, they can be specified by extraTypes
        /// </summary>
        /// <param name="xmlSource"></param>
        /// <param name="sourceType"></param>
        /// <param name="extraTypes"></param>
        /// <returns></returns>
        public static object FromXml(string xmlSource, Type sourceType, params Type[] extraTypes)
        {
            return FromXml(ASCIIEncoding.ASCII.GetBytes(xmlSource), sourceType, extraTypes);
        }

        /// <summary>
        /// Deserializes the provided xml into the type specified by sourceType.
        /// If any additional types are required for deserialization, they can be specified by extraTypes
        /// </summary>
        /// <param name="xmlSource"></param>
        /// <param name="sourceType"></param>
        /// <param name="extraTypes"></param>
        /// <returns></returns>
        public static object FromXml(byte[] xmlSource, Type sourceType, params Type[] extraTypes)
        {
            return FromXml(new SmartStream(xmlSource), sourceType, extraTypes);
        }

        /// <summary>
        /// Deserializes the provided xml into the type specified by sourceType.
        /// If any additional types are required for deserialization, they can be specified by extraTypes
        /// </summary>
        /// <param name="xmlSource"></param>
        /// <param name="sourceType"></param>
        /// <param name="extraTypes"></param>
        /// <returns></returns>
        public static object FromXml(Stream xmlSource, Type sourceType, params Type[] extraTypes)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(sourceType, extraTypes);
                return serializer.Deserialize(xmlSource);
            }
            finally
            {
            }
        }

        /// <summary>
        /// Converts the provided Serializable type to a Base64 encoded binary string.
        /// The source type provided (as well as all internally contained types) must
        /// be marked with the SerializableAttribute class.
        /// If toBase64=true, then resulting string will be base64 encoded, otherwise it will be 
        /// ASCII encoded.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static void ToBinary(object source, bool toBase64, out string result)
        {
            SmartStream ms = new SmartStream();
            ToBinary(source, ms);
            ms.Position = 0;
            result = (toBase64 ? Convert.ToBase64String(ms.ToArray()) : ASCIIEncoding.ASCII.GetString(ms.ToArray()));
        }

        /// <summary>
        /// Converts the provided Serializable type to binary format in the provided stream.
        /// The source type provided (as well as all internally contained types) must
        /// be marked with the SerializableAttribute class.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void ToBinary(object source, Stream destination)
        {
            try
            {
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(destination, source);
            }
            finally
            {
            }
        }

        /// <summary>
        /// Converts the provided Serializable type to binary format as a byte[].
        /// The source type provided must be marked Serializable.
        /// If a toBase64 parameter value of true is specified, the resulting
        /// byte[] will be converted to the equivalent base64 encoded ASCII byte[].
        /// </summary>
        /// <param name="source"></param>
        /// <param name="toBase64"></param>
        /// <returns></returns>
        public static byte[] ToBinary(object source, bool toBase64)
        {
            SmartStream ms = new SmartStream();
            ToBinary(source, ms);
            ms.Position = 0;
            byte[] ret = null;

            if (toBase64)
            {
                ret = ASCIIEncoding.ASCII.GetBytes(Convert.ToBase64String(ms.ToArray()));
            }
            else
            {
                ret = ms.ToArray();
            }

            return ret;
        }


        /// <summary>
        /// Creates an instance of type T from the string provided.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="base64binarySource"></param>
        /// <returns></returns>
        public static T FromBinary<T>(string binarySource, bool isBase64)
        {
            SmartStream stream = new SmartStream((isBase64 ? Convert.FromBase64String(binarySource) : ASCIIEncoding.ASCII.GetBytes(binarySource)));
            return FromBinary<T>(stream);
        }

        /// <summary>
        /// Creates an instance of type T from the contents of the provided stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binarySource">stream containing raw binary data - NOT assumed to be base 64 encoded</param>
        /// <returns></returns>
        public static T FromBinary<T>(Stream binarySource)
        {
            try
            {
                BinaryFormatter serializer = new BinaryFormatter();
                return (T)serializer.Deserialize(binarySource);
            }
            finally
            {
            }
        }

        /// <summary>
        /// Creates an instance of type T from the contents of the provided byte array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="base64BinarySource">the base 64 encoded byte array to be deserialized</param>
        /// <returns></returns>
        public static T FromBinary<T>(byte[] binarySource, bool isBase64)
        {
            byte[] rawSource = (isBase64 ? Convert.FromBase64String(ASCIIEncoding.ASCII.GetString(binarySource)) : binarySource);
            return FromBinary<T>(new MemoryStream(rawSource));
        }

        /// <summary>
        /// Returns an instance of an object specified by the binarySource provided.
        /// </summary>
        /// <param name="binarySource">string representing the serialized source for the object</param>
        /// <param name="isBase64">if true, binarySource is assumed to be base64 encoded, 
        /// and will be base64 decoded prior to deserialization</param>
        /// <returns></returns>
        public static object FromBinary(string binarySource, bool isBase64)
        {
            SmartStream stream = new SmartStream((isBase64 ? Convert.FromBase64String(binarySource) : ASCIIEncoding.ASCII.GetBytes(binarySource)));
            return FromBinary(stream);
        }

        /// <summary>
        /// Returns an instance of an object specified by the binarySource provided.
        /// </summary>
        /// <param name="binarySource">string representing the serialized source for the object</param>
        /// <param name="isBase64">if true, binarySource is assumed to be base64 encoded, 
        /// and will be base64 decoded prior to deserialization</param>
        /// <returns></returns>
        public static object FromBinary(byte[] binarySource, bool isBase64)
        {
            byte[] rawSource = (isBase64 ? Convert.FromBase64String(ASCIIEncoding.ASCII.GetString(binarySource)) : binarySource);
            return FromBinary(new SmartStream(rawSource));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="binarySource"></param>
        /// <returns></returns>
        public static object FromBinary(Stream binarySource)
        {
            try
            {
                BinaryFormatter serializer = new BinaryFormatter();
                return serializer.Deserialize(binarySource);
            }
            finally
            {
            }
        }
    }
}
