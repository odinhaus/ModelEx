using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;

namespace Altus.Core.Exceptions
{
    [Serializable()]
    public class RuntimeException : Exception
    {
        protected RuntimeException() { }

        public RuntimeException(ExceptionCode code, string message) : base(message)
        {
            this.Code = code;
            this._message = message;
        }

        public RuntimeException(Exception exception) : base("An unexpected error occurred: " + exception.Message, exception)
        {
            this.Code = ExceptionCode.UnknownException;
        }

        [DataMember()]
        public ExceptionCode Code { get; protected set; }

        string _message;
        public new string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
            }
        }
    }
}
