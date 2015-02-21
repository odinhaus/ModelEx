using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Altus.Core.Security;
using Altus.Core.Scheduling;
using Altus.Core;
using System.Linq.Expressions;
using Altus.Core.Processing;

namespace Altus.Core.PubSub
{
    internal class ReflectionPublicationContext : PublicationContext
    {
        internal ReflectionPublicationContext(Topic topic, 
            Publisher publisher, 
            MemberInfo publisherMember, 
            MethodInfo successMethod, 
            MethodInfo definitionMethod,
            int defaultPubInterval,
            ISubscriberProxy subscriberProxy)
        {
            this.SuccessMethod = successMethod;
            this.DefinitionMethod = definitionMethod;
            this.AddProxy(subscriberProxy);
            this.Member = publisherMember;
            this.Publisher = publisher;
            this.IsMethod = publisherMember is MethodInfo;
            this.Topic = topic;
            this.DefaultInterval = defaultPubInterval;
            if (!this.IsMethod)
            {
                AttachEvent();
            }
        }

        protected override bool OnInitialize(params string[] args)
        {
            if (this.DefinitionMethod != null)
            {
                PublicationDefinitionArgs e = new PublicationDefinitionArgs(this.Subscription)
                {
                     Schedule = this.Subscription.Schedule == null 
                        ? new PeriodicSchedule(new DateRange(DateTime.MinValue, DateTime.MaxValue), int.MaxValue) 
                        : this.Subscription.Schedule,
                     Success = CreateDynamicDelegate(this.SuccessMethod)
                };

                PublicationContext.Current = this;
                this.DefinitionMethod.Invoke(this.Publisher.Target, new object[] { this, e });
                if (e.Cancel)
                {
                    Altus.Core.Component.App.Instance.Shell.Remove(this);
                }
                else
                {
                    this.Schedule = e.Schedule;
                    this.Success = e.Success;
                    this.SuccessMethod = e.Success.Method;
                    //this.Error = e.Error;
                }
            }
            else if (this.DefaultInterval >= 0)
            {
                this.Schedule = new PeriodicSchedule(new DateRange(DateTime.MinValue, DateTime.MaxValue), this.DefaultInterval);
            }
            base.OnInitialize(args);
            return true;
        }

        private Delegate CreateDynamicDelegate(MethodInfo method)
        {
            if (method == null) return null;
            List<Type> args = new List<Type>(
            method.GetParameters().Select(p => p.ParameterType));
            Type delegateType;
            if (method.ReturnType == typeof(void))
            {
                delegateType = Expression.GetActionType(args.ToArray());
            }
            else
            {
                args.Add(method.ReturnType);
                delegateType = Expression.GetFuncType(args.ToArray());
            }
            Delegate d = Delegate.CreateDelegate(delegateType, null, method);
            return d;
        }

        private void AttachEvent()
        {
            this.PublishEvent = this.Member as EventInfo;
            Func<object[], object> del = HandleEvent;
            this.PublishEvent.AddEventHandler(this.Publisher.Target, Delegate.CreateDelegate(this.PublishEvent.EventHandlerType, del.Target, del.Method));
            MethodInfo methodInfo = this.PublishEvent.GetRaiseMethod(true);
            this.Member = methodInfo;
            this.IsMethod = true;
        }

        private object HandleEvent(object[] args)
        {
            if (RaisedByMe) return null;
            MethodInfo method = this.PublishEvent.EventHandlerType.GetMethod("Invoke");
            ParameterInfo[] parms = method.GetParameters();
            if (args.Length != parms.Length) throw (new InvalidOperationException("Parameter count mismatch"));
            List<ServiceParameter> pubParms = new List<ServiceParameter>();
            for (int i = 0; i < args.Length; i++)
            {
                pubParms.Add(new ServiceParameter(parms[i].Name, parms[i].ParameterType.FullName, ParameterDirection.In) { Value = args[i] });
            }
            HandlePublish(pubParms.ToArray());
            return null;
        }

        private void HandlePublish(ServiceParameter[] args)
        {
            foreach (ISubscriberProxy proxy in this.SubscriberProxies)
            {
                proxy.PublishData(args);
            }
        }

        private MemberInfo Member;
        private Publisher Publisher;
        private bool IsMethod;
        private bool RaisedByMe;
        private MethodInfo SuccessMethod;
        private MethodInfo DefinitionMethod;
        private EventInfo PublishEvent;
        private int DefaultInterval;

        protected override ServiceParameter[] OnExecute(params object[] args)
        {
            RaisedByMe = true;
            try
            {
                ServiceParameter[] result = Invoke((MethodInfo)this.Member, this.Publisher.Target).ToArray();
                this.HandlePublish(result);
                return result;
            }
            finally
            {
                RaisedByMe = false;
            }
        }

        private ServiceParameter[] Invoke(MethodInfo theMethod, object theTarget, params ServiceParameter[] spParms)
        {
            ParameterInfo[] parms = (theMethod).GetParameters();
            List<int> pubParms = new List<int>();
            object[] args = new object[parms.Length];
            for (int i = 0; i < parms.Length; i++ )
            {
                ParameterInfo parm = parms[i];
                ServiceParameter sp = this.Subscription.Parameters.Where(p => p.Name.Equals(parm.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (sp == null && parm.IsOut)
                {
                    args[i] = null;
                }
                else if (sp == null && parm.IsOptional)
                {
                    args[i] = parm.DefaultValue;
                }
                else if (sp == null)
                {
                    args[i] = spParms[i].Value;
                }
                else
                {
                    args[i] = sp.Value;
                }
                if (parm.GetCustomAttributes(typeof(PublicationDataAttribute), true).Length > 0)
                {
                    pubParms.Add(i);
                }
            }

            object result = ((MethodInfo)theMethod).Invoke(theTarget, args);

            bool pubRetVal = ((MethodInfo)theMethod).ReturnParameter.GetCustomAttributes(typeof(PublicationDataAttribute), true).Length > 0;
            List<ServiceParameter> retArgs = new List<ServiceParameter>();
            if (pubRetVal)
            {
                retArgs.Add(new ServiceParameter("result", result.GetType().FullName, ParameterDirection.Return) { Value = result });
            }
            for (int i = 0; i < pubParms.Count; i++)
            {
                retArgs.Add(new ServiceParameter(parms[pubParms[i]].Name, parms[pubParms[i]].ParameterType.FullName, ParameterDirection.In) { Value = args[pubParms[i]] });
            }

            return retArgs.ToArray();
        }

        protected override void OnSuccess(params ServiceParameter[] args)
        {
            if (this.Success != null)
            {
                this.Invoke(this.Success.Method, this.Success.Target, args);
            }
            else if (this.SuccessMethod != null)
            {
                this.Invoke(this.SuccessMethod, this.Publisher.Target, args);
            }
        }

        protected override void OnError(ServiceParameter error)
        {
            //this.SubscriberProxy.PublishError(ex, args);
        }
    }
}
