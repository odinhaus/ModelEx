using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Altus.Core.Messaging.Udp;

namespace Altus.Core.Messaging
{
    public static class ConnectionManager
    {
        static Dictionary<string, IConnection> _mcastConnections = new Dictionary<string, IConnection>();

        public static IConnection CreateMulticastConnection(IPEndPoint endPoint, bool listen)
        {
            lock (_mcastConnections)
            {
                if (!_mcastConnections.ContainsKey(endPoint.ToString()))
                {
                    IConnection connection = new MulticastConnection(endPoint, listen);
                    connection.Disposed += connection_Disposed;
                    _mcastConnections.Add(endPoint.ToString(), connection);
                }
                return _mcastConnections[endPoint.ToString()];
            }
        }

        static void connection_Disposed(object sender, EventArgs e)
        {
            lock (_mcastConnections)
            {
                _mcastConnections.Remove(((IConnection)sender).EndPoint.ToString());
            }
        }
    }
}
