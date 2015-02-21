using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Altus.Core.Component;
using Altus.Core.Pipeline;
using Altus.Core.Messaging.Tcp;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using Altus.Core;
using Altus.Core.Messaging.Udp;
using Altus.Core.Serialization;
using Altus.Core.Security;
using Altus.Core.Messaging;
using Altus.Core.Processing;

using Altus.Core.Diagnostics;
using Altus.Core.Streams;
using Altus.Core.Messaging.Http;


namespace Altus.Core.Processing
{
    public enum Action
    {
        POST,
        GET,
        DELETE,
        PUT,
        OPTIONS,
        ERROR
    }

    public enum ServiceType
    {
        PubSub,
        Directed,
        Broadcast,
        RequestResponse,
        Other
    }

    public enum MessagingStage
    {
        Received,
        Sent,
        Enueued,
        Dequeued,
        Delivered,
        SendError,
        DeliveryError,
        ReceiveError,
        EnqeueuError,
        DequeueError
    }

    public abstract partial class ServiceContext : IDisposable
    {
        [ThreadStatic()]
        public static ServiceContext Current;

        public void Cancel()
        {
            IsCanceled = true;
        }

        public bool IsCanceled { get; private set; }

        #region IDisposable Members
        bool disposed = false;

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue 
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        //========================================================================================================//
        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the 
        // runtime from inside the finalizer and you should not reference 
        // other objects. Only unmanaged resources can be disposed.
        bool _disposing = false;
        private void Dispose(bool disposing)
        {
            _disposing = disposing;
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    Current = null;
                    // Dispose managed resources.
                    this.OnDisposeManagedResources();
                }

                // Call the appropriate methods to clean up 
                // unmanaged resources here.
                // If disposing is false, 
                // only the following code is executed.
                this.OnDisposeUnmanagedResources();
            }
            disposed = true;
            _disposing = false;
        }

        /// <summary>
        /// Dispose managed resources
        /// </summary>
        protected virtual void OnDisposeManagedResources()
        {
        }

        /// <summary>
        /// Dispose unmanaged (native resources)
        /// </summary>
        protected virtual void OnDisposeUnmanagedResources()
        {
        }

        #endregion
    }

    public abstract class ServiceContext<T, U> : ServiceContext
        where T : ServiceRequest 
        where U : ServiceResponse
    {
        protected ServiceContext() : base() { }
        public T Request { get; protected set; }
        public U Response { get; protected set; }
    }
}
