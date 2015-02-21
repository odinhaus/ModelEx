using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Exceptions;

namespace Altus.Core.Diagnostics
{
    public static partial class Logger
    {
        public static void Log(ExceptionCode code, string additionalMessage)
        {
            if (TraceLevel < System.Diagnostics.TraceLevel.Info) return;
            Exception ex = ExceptionHelper.CreateException(code, additionalMessage);
            Log(ex);
        }

        public static void LogWarn(ExceptionCode code, string additionalMessage)
        {
            if (TraceLevel < System.Diagnostics.TraceLevel.Warning) return;
            Exception ex = ExceptionHelper.CreateException(code, additionalMessage);
            LogWarn(ex);
        }

        public static void LogError(ExceptionCode code, string additionalMessage)
        {
            if (TraceLevel < System.Diagnostics.TraceLevel.Error) return;
            Exception ex = ExceptionHelper.CreateException(code, additionalMessage);
            LogError(ex);
        }
    }
}
