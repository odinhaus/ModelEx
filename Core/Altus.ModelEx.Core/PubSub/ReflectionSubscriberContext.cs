using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Altus.Core.Processing;

namespace Altus.Core.PubSub
{
    public class ReflectionSubscriberContext : SubscriptionContext, ISubscriberProxy
    {
        public ReflectionSubscriberContext(Subscription subscription, MethodInfo successHandler, MethodInfo errorHandler, MethodInfo defintionHandler)
            :base(subscription)
        {
            this.Subscription = subscription;
            this.SuccessHandler = successHandler;
            this.ErrorHandler = errorHandler;
            this.DefinitionHandler = defintionHandler;
        }

        private Subscription Subscription;
        private MethodInfo SuccessHandler;
        private MethodInfo ErrorHandler;
        private MethodInfo DefinitionHandler;

        /// <summary>
        /// Calls the subscribing components definition method (if set), and return a bool indicating whether the 
        /// definition method cancelled the subscription operation (true to cancel, false to proceed with subscribing).
        /// </summary>
        /// <returns></returns>
        public bool RequestDefinition()
        {
            SubscriptionContext.Current = this;
            bool cancelled = false;
            if (this.DefinitionHandler != null)
            {
                SubscriptionDefinitionArgs e = new SubscriptionDefinitionArgs(this.Subscription);
                this.DefinitionHandler.Invoke(this.Subscription.Subscriber.Target, new object[] { this, e });
                cancelled = e.Cancel;
            }
            return cancelled;
        }

        public void PublishData(params ServiceParameter[] parameters)
        {
            try
            {
                SubscriptionContext.Current = this;
                if (this.SuccessHandler != null)
                {
                    ParameterInfo[] parms = this.SuccessHandler.GetParameters();
                    object[] args = new object[parms.Length];
                    for (int i = 0; i < parms.Length; i++)
                    {
                        args[i] = parameters.Where(p => p.Name.Equals(parms[i].Name, StringComparison.InvariantCultureIgnoreCase)).First().Value;
                    }
                    this.SuccessHandler.Invoke(this.Subscription.Subscriber.Target, args);
                }
            }
            catch (Exception ex)
            {
                PublishError(new ServiceParameter("Error", ex.GetType().FullName, ParameterDirection.Error) { Value = ex.InnerException });
            }
        }

        public void PublishError(params ServiceParameter[] parameters)
        {
            if (this.ErrorHandler != null)
            {
                Exception e = null;
                ServiceParameter sp = parameters.Where(p => p.Direction == ParameterDirection.Error).FirstOrDefault();
                if (sp != null)
                    e = sp.Value as Exception;

                SubscriptionErrorArgs arg = new SubscriptionErrorArgs(this.Subscription, e);
                this.ErrorHandler.Invoke(this.Subscription.Subscriber.Target, new object[] { this, arg });
            }
        }
    }
}
