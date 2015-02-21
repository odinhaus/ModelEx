using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Altus.Core.Diagnostics;

namespace Altus.Core.Exceptions
{
    public enum ExceptionCode : int
    {
        UnknownException,
        SystemWinCacheExpirationThreadExited,
        PerfMonCouldNotCreateCounter
    }

    public static class ExceptionHelper
    {
        public const string UnknownException = "Unknown Exception";
        public const string SystemWinCacheExpirationThreadExited = "The cache expiration thread terminated unexpectedly";
        public const string PerfMonCouldNotCreateCounter = "Performance Counter instance could not be created";


        public static void Throw(Exception ex)
        {
            throw new RuntimeException(ex);
        }

        //===========================================================================================//
        /// <summary>
        /// Creates and throws an exception for the provided exception code
        /// </summary>
        /// <param name="code"></param>
        public static void CreateAndThrowException(ExceptionCode code)
        {
            throw (CreateException(code));
        }
        //===========================================================================================//


        //===========================================================================================//
        /// <summary>
        /// Creates and throws an exception for the provided exception code and additional message
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public static void CreateAndThrowException(ExceptionCode code, string message)
        {
            Exception ex = CreateException(code, message);
            Logger.Log(ex);

            throw (ex);
        }
        //===========================================================================================//

        //===========================================================================================//
        /// <summary>
        /// Creates the appropriate exception for the ExceptionCode provided
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static Exception CreateException(ExceptionCode code)
        {
            return CreateException(code, string.Empty);
        }
        //===========================================================================================//


        //===========================================================================================//
        /// <summary>
        /// Creates the appropriate exception for the ExceptionCode provided, and appends the provided
        /// message.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Exception CreateException(ExceptionCode code, string additionalMessage)
        {
            Exception exception;
            string message = "Unknown exception";

            FieldInfo field = typeof(ExceptionHelper).GetField(code.ToString(), BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField);

            if (field != null)
            {
                message = (string)field.GetValue(null);
            }

            if (!string.IsNullOrEmpty(additionalMessage))
                message += "\r\n" + additionalMessage;

            exception = new RuntimeException(code, message);

            return exception;
        }
        //===========================================================================================//
    }
}
