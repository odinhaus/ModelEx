using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Reflection;

namespace Altus.Core.Serialization
{
    public interface ISerializer : IComponent
    {
        bool SupportsFormat(string format);
        bool SupportsType(Type type);
        byte[] Serialize(object source);
        object Deserialize(byte[] source, Type targetType);
        int Priority { get; }
        bool IsScalar { get; }
    }

    public interface ISerializer<T> : ISerializer
    {
        byte[] Serialize(T source);
        void Serialize(T source, Stream outputStream);
        T Deserialize(byte[] source);
        T Deserialize(Stream inputSource);
    }

    public delegate Type ResolveTypeEventHandler(object sender, ResolveTypeEventArgs e);
    public class ResolveTypeEventArgs : EventArgs
    {
        public ResolveTypeEventArgs(int position)
        {
            this.ParameterPosition = position;
            this.CallingAssembly = Assembly.GetCallingAssembly();
        }

        public ResolveTypeEventArgs(string name)
        {
            this.ParameterName = name;
            this.CallingAssembly = Assembly.GetCallingAssembly();
        }

        public ResolveTypeEventArgs(int position, Assembly callingAssembly)
        {
            this.ParameterPosition = position;
            this.CallingAssembly = callingAssembly;
        }

        public ResolveTypeEventArgs(string name, Assembly callingAssembly)
        {
            this.ParameterName = name;
            this.CallingAssembly = callingAssembly;
        }

        public int? ParameterPosition { get; set; }
        public string ParameterName { get; set; }
        public Assembly CallingAssembly { get; private set; }
    }

    public interface ISerializerTypeResolver
    {
        void AddTypeResolver(ResolveTypeEventHandler handler);
    }
}
