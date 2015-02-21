using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Altus.Core.Messaging
{
    public interface IConnection : IDisposable
    {
        event EventHandler Disconnected;
        event EventHandler Disposing;
        event EventHandler Disposed;

        Protocol Protocol { get; }
        EndPoint EndPoint { get; }
        Encoding TextEncoding { get; set; }
        long ContentLength { get; set; }
        string ContentType { get; set; }
        string DefaultFormat { get; }
        Dictionary<string, object> ConnectionAspects { get; }

        void Send(byte[] data);
        void Send(Message message);
        Message Call(Message msg);
        Message Call(Message msg, TimeSpan timeout);
        Message Call(Message msg, int timespan);

        void SendError(Message message, Exception ex);

        Processing.Action Action { get; }
        void ResetProperties();

        bool IsDisconnected { get; }
    }
}
