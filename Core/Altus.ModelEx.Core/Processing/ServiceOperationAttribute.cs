using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Serialization;
using System.Reflection;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Altus.Core.Processing
{
    //public struct ServiceOperationParameter
    //{
    //    public ServiceOperationParameter(string methodParameterName, string serviceParameterName) : this()
    //    {
    //        MethodParameterName = methodParameterName;
    //        ServiceParameterName = serviceParameterName;
    //        SerializerType = null;
    //    }

    //    public ServiceOperationParameter(string methodParameterName, string serviceParameterName, Type serializerType) : this()
    //    {
    //        MethodParameterName = methodParameterName;
    //        ServiceParameterName = serviceParameterName;
    //        SerializerType = serializerType;
    //    }

    //    public string MethodParameterName { get; private set; }
    //    public string ServiceParameterName { get; private set; }
    //    public Type SerializerType { get; set; }
    //}

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ServiceOperationAttribute : Attribute
    {
        public ServiceOperationAttribute(string returnFormat)
        {
            this.ReturnFormat = returnFormat;
        }

        public ServiceOperationAttribute(string returnFormat, string serviceOperationName)
        {
            this.ReturnFormat = returnFormat;
            this.Name = serviceOperationName;
        }

        public ServiceOperationAttribute(string returnFormat, string serviceOperationName, Delegate handler)
        {
            this.ReturnFormat = returnFormat;
            this.Name = serviceOperationName;
            this.Method = handler.Method;
            this.Target = handler.Target;
        }

        public string Name { get; set; }
        public Type ReturnSerializerType { get; private set; }
        public string ReturnFormat { get; private set; }
        internal MethodInfo Method { get; set; }
        internal object Target { get; set; }
        public ServiceEndPointAttribute ServiceEndPoint { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }
        public Regex MatchExpression { get; set; }
        public bool SingletonTarget { get; set; }

        public override string ToString()
        {
            if (MatchExpression != null)
            {
                return MatchExpression.ToString();
            }
            else
            {
                return base.ToString();
            }
        }
    }
}
