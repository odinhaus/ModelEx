using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Entities;
using Altus.Core.Serialization;
using System.Runtime.Serialization;
using Altus.Core.Serialization.Binary;

namespace Altus.Core.Processing
{
    public enum ParameterDirection
    {
        /// <summary>
        /// Value will be sent as an input parameter to the mapped operation
        /// </summary>
        In,
        /// <summary>
        /// Value will be available as a return parameter to the mapped operation
        /// </summary>
        Out,
        /// <summary>
        /// Value will be mapped to a matching ServiceContext property
        /// </summary>
        Aspect,
        /// <summary>
        /// Value is the result of a service operation
        /// </summary>
        Return,
        /// <summary>
        /// Value is an exception that occurred as a result of the service operation
        /// </summary>
        Error
    }

    [DataContract()]
    [System.Serializable]
    public class ServiceParameter : AbstractEntity
    {
        protected ServiceParameter() { }

        public ServiceParameter(string name, string type)
        {
            this.Name = name;
            this.Type = type.Replace("&", "");
            this.Direction = ParameterDirection.In;
        }

        public ServiceParameter(string name, string type, ParameterDirection direction)
        {
            this.Name = name;
            this.Type = type.Replace("&","");
            this.Direction = direction;
        }
        [DataMember()]
        [BinarySerializable(1)]
        public string Name { get; protected set; }

        [DataMember()]
        [BinarySerializable(2)]
        public string Type { get; protected set; }

        object _value = null;
        [DataMember()]
        [BinarySerializable(3)]
        public object Value 
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        [DataMember()]
        [BinarySerializable(4)]
        public ParameterDirection Direction { get; protected set; }
    }

    public class SerializedServiceParameter
    {

    }
}
