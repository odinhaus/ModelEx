using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;

namespace Altus.Core.Processing
{
    public static class ServiceTypes
    {
        public const string RPC = "Rpc";
        public const string MSP = "Msp";
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public abstract class ServiceEndPointAttribute : Attribute
    {
        public ServiceEndPointAttribute(Type invocationProxyType, string type, params string[] routes)
        {
            ServiceRoute[] sRoutes = new ServiceRoute[routes.Length];
            for (int i = 0; i < routes.Length; i++)
            {
                sRoutes[i] = new ServiceRoute(routes[i]);
            }
            Routes = sRoutes;
            ServiceType = type;
            this.InvocationProxyType = invocationProxyType;
        }

        public ServiceRoute[] Routes { get; private set; }
        internal ServiceOperationAttribute[] Operations { get; set; }
        internal object Target { get; set; }
        public string ServiceType { get; private set; }
        public Type InvocationProxyType { get; private set; }
    }
}
