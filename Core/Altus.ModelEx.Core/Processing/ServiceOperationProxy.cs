using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Pipeline;
using Altus.Core.Component;
using System.Reflection;
using Altus.Core.Processing.Rpc;
using Altus.Core.Messaging;
using Altus.Core.Serialization;
using System.ComponentModel;
using Altus.Core;
using Altus.Core.Diagnostics;
using Altus.Core.Messaging.Http;
using Altus.Core.Streams;

namespace Altus.Core.Processing
{
    public abstract class ServiceOperationProxy : InitializableComponent, IProcessor<ServiceContext>
    {
        protected ServiceOperationProxy(Message message, ServiceOperationAttribute attrib, IConnection connection)
        {
            try
            {
                //this.Operation = operation;
                this.Attribute = attrib;
                this.Operation = this.OnDeserializePayload(message);
                this.ServiceContext = OnCreateServiceContext(message, this.Operation, attrib, OnCreatePipeline(), connection);
                this.Attribute.Target = OnCreateTarget(this.ServiceContext);
                this.OnSetAspectValues(this.ServiceContext);
                this.Args = OnBuildInputParams(this.ServiceContext);
                connection.ContentLength = 0;
                connection.ContentType = StandardFormats.GetContentType(this.ServiceContext.ResponseFormat.Equals(StandardFormats.PROTOCOL_DEFAULT) ? connection.DefaultFormat : this.ServiceContext.ResponseFormat);
                CanProcess = true;
            }
            catch (Exception ex)
            {
                CanProcess = false;
                OnUnhandledException(this.ServiceContext, ex);
            }
        }

        public ServiceOperation Operation { get; private set; }
        public ServiceOperationAttribute Attribute { get; private set; }
        public ServiceContext ServiceContext { get; private set; }
        public bool CanProcess { get; private set; }
        private object[] Args;

        public void Process(ServiceContext request)
        {
            try
            {
                if (CanProcess)
                {
                    this.OnProcess(request, this.Attribute.Target, this.Args);
                }
                //else
                //    throw (new InvalidOperationException("The Service Pipeline can not process requests due to an error during initial construction."));
            }
            catch (Exception ex)
            {
                OnUnhandledException(request, ex);
            }
        }

        protected virtual void OnUnhandledException(ServiceContext request, Exception ex)
        {
            if (request == null) throw (new InvalidOperationException("Service context is null", ex));

            MethodInfo exHandler = request.GetType().GetMethod("OnUnhandledException", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (exHandler == null) throw (new InvalidOperationException("The exception handler on type " + request.GetType().FullName + " could not be found.", ex));

            if (!exHandler.DeclaringType.Equals(request.GetType()))
            {
                exHandler = exHandler.DeclaringType.GetMethod("OnUnhandledException", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
            exHandler.Invoke(request, new object[] { this, ex.InnerException == null ? ex : ex.InnerException });
        }

        /// <summary>
        /// Inheritors should override this method to perform any custom initialization or setup of the
        /// proxied instance that will be called during service request invocation.  The default implementation
        /// simply returns the Attribute.Target instance.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual object OnCreateTarget(ServiceContext request)
        {
            if (Attribute.SingletonTarget)
            {
                return Attribute.Target;
            }
            else
            {
                return Activator.CreateInstance(Attribute.Target.GetType());
            }
        }

        /// <summary>
        /// Inheritors can override this method to perform custom target invocation.  The default implementation
        /// calls the target method via reflection, and passes in the object[] arguments defined by the OnBuildInputParams method.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="target"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual object OnProcess(ServiceContext request, object target, object[] args)
        {
            try
            {
                return Attribute.Method.Invoke(target, args);
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException == null)
                {
                    try
                    {
                        throw (new Exception("Argument exception calling method " + Attribute.Method.Name + " on type " + target.GetType().FullName + ": " + tie.Message));
                    }
                    catch
                    {
                        throw tie;
                    }
                }
                else
                {
                    try
                    {
                        throw (new Exception("Argument exception calling method " + Attribute.Method.Name + " on type " + target.GetType().FullName + ": " + tie.InnerException.Message, tie.InnerException));
                    }
                    catch
                    {
                        throw tie.InnerException;
                    }
                }
            }
            catch (Exception aex)
            {
                try
                {
                    throw (new Exception("Argument exception calling method " + Attribute.Method.Name + " on type " + target.GetType().FullName + ": " + aex.Message));
                }
                catch
                {
                    throw aex;
                }
            }
        }

        /// <summary>
        /// Inheritors must implement this method to construct the concrete Pipeline<> instance
        /// </summary>
        /// <returns></returns>
        protected abstract IPipeline<ServiceContext> OnCreatePipeline();
        /// <summary>
        /// Inheritors must implement this method to construct the associated concrete ServiceContext type that will be 
        /// associated with the service execution pipeline
        /// </summary>
        /// <param name="message"></param>
        /// <param name="operation"></param>
        /// <param name="attrib"></param>
        /// <param name="pipeline"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        protected abstract ServiceContext OnCreateServiceContext(Message message, ServiceOperation operation, ServiceOperationAttribute attrib, IPipeline<ServiceContext> pipeline, IConnection connection);

        /// <summary>
        /// Inheritors can override this method to perform any custom aspect value assignment, prior to invocation.
        /// The default implementation assigns all ServiceParameter instances in the current ServiceOperation with
        /// ParameterDirection of Aspect to associate Property, Field or single-parameter methods of the same name 
        /// on the current ServiceContext.Request instance type.
        /// </summary>
        /// <param name="request"></param>
        protected virtual void OnSetAspectValues(ServiceContext request)
        {
            PropertyInfo requestProp = request.GetType().GetProperty("Request", BindingFlags.Public | BindingFlags.Instance);
            ServiceRequest req = requestProp.GetValue(this.ServiceContext, null) as ServiceRequest;
            foreach (ServiceParameter sp in this.Operation.Parameters.Where(p => p.Direction == ParameterDirection.Aspect))
            {
                MemberInfo member = req.GetType().GetMember(sp.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();

                if (member == null) continue; // couldn't find the associated member on the destination type, so just drop the data

                if (!member.DeclaringType.Equals(req.GetType()))
                {
                    member = member.DeclaringType.GetMember(sp.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
                }
                
                if (member is PropertyInfo)
                {
                    ((PropertyInfo)member).SetValue(req, sp.Value, null);
                }
                else if (member is FieldInfo)
                {
                    ((FieldInfo)member).SetValue(req, sp.Value);
                }
                else if (member is MethodInfo)
                {
                    ((MethodInfo)member).Invoke(req, new object[] { sp.Value });
                }
                else
                    throw (new InvalidOperationException("The service context does provide an aspect member with the given name " + sp.Name));
            }

            Dictionary<string, object>.Enumerator en = request.Connection.ConnectionAspects.GetEnumerator();
            while (en.MoveNext())
            {
                MemberInfo member = req.GetType().GetMember(en.Current.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();

                if (member == null) continue; // couldn't find the associated member on the destination type, so just drop the data

                if (!member.DeclaringType.Equals(req.GetType()))
                {
                    member = member.DeclaringType.GetMember(en.Current.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
                }

                if (member is PropertyInfo)
                {
                    ((PropertyInfo)member).SetValue(req, en.Current.Value, null);
                }
                else if (member is FieldInfo)
                {
                    ((FieldInfo)member).SetValue(req, en.Current.Value);
                }
                else if (member is MethodInfo)
                {
                    ((MethodInfo)member).Invoke(req, new object[] { en.Current.Value });
                }
                else
                    throw (new InvalidOperationException("The service context does provide an aspect member with the given name " + en.Current.Key));
            }

            req.ServiceOperation = this.Operation;
        }

        /// <summary>
        /// Inheritors can override this method to perform any custom input parameter construction/mapping behavior.
        /// The default implementaion simply loops through the ServiceOperation parameters collection, and assigns
        /// existing parameters to matching input parameters, discovered via reflection.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual object[] OnBuildInputParams(ServiceContext request)
        {
            ParameterInfo[] methodParms = Attribute.Method.GetParameters();
            object[] args = new object[methodParms.Length];
            foreach (ParameterInfo pi in methodParms)
            {
                ServiceParameter sp = Operation.Parameters.Where(p => p.Name.Equals(pi.Name, StringComparison.InvariantCultureIgnoreCase)
                    || p.Name.Equals(pi.Position.ToString())).FirstOrDefault();
                
                if (sp == null)
                {
                    if (pi.IsOut)
                    {
                        sp = new ServiceParameter(pi.Name, pi.ParameterType.FullName, ParameterDirection.Out);
                        Operation.Parameters.Add(sp);
                    }
                    else
                    {
                        OptionalAttribute optional = (OptionalAttribute)pi.GetCustomAttributes(typeof(OptionalAttribute), true).FirstOrDefault();
                        if (optional == null)
                        {
                            throw (new InvalidOperationException("The service parameter corresponding to the method parameter \"" + pi.Name + "\" was not supplied."));
                        }
                        else
                        {
                            sp = new ServiceParameter(pi.Name, pi.ParameterType.FullName, ParameterDirection.In) { Value = optional.DefaultValue };
                        }
                    }
                }
                
                args[pi.Position] = sp.Value;
            }
            return args;
        }

        /// <summary>
        /// Maps the results of the proxied method call to matching service parameters
        /// </summary>
        /// <param name="returnValue"></param>
        /// <param name="inputArgs"></param>
        /// <param name="returnIsVoid"></param>
        /// <returns></returns>
        protected virtual ServiceOperation OnBuildResponse(object returnValue, object[] inputArgs, bool returnIsVoid)
        {
            List<ServiceParameter> returnParms = new List<ServiceParameter>();
            if (!returnIsVoid)
            {
                returnParms.Add(new ServiceParameter("result", returnValue == null ? "Null" : returnValue.GetType().FullName, ParameterDirection.Return) { Value = returnValue });
            }

            ParameterInfo[] methodParms = Attribute.Method.GetParameters();
            for (int i = 0; i < methodParms.Length; i++)
            {
                ParameterInfo pi = methodParms[i];
                if (pi.IsOut)
                {
                    ServiceParameter sp = Operation.Parameters.Where(p => p.Name.Equals(pi.Name, StringComparison.InvariantCultureIgnoreCase)
                        || p.Name.Equals(pi.Position.ToString())).FirstOrDefault();

                    if (sp == null)
                    {
                        sp = new ServiceParameter(pi.Name, pi.ParameterType.FullName, ParameterDirection.Out);
                        Operation.Parameters.Add(sp);
                    }

                    sp.Value = inputArgs[pi.Position];                       

                    returnParms.Add(sp);
                }
            }

            foreach (ServiceParameter sp in Operation.Parameters)
            {
                if (returnParms.Count(rp => rp.Name.Equals(sp.Name)) == 0)
                    returnParms.Add(sp);
            }

            return new ServiceOperation(OperationType.Response, Operation.ServiceType, this.Operation.ServiceUri, returnParms.ToArray());
        }

        protected virtual ServiceOperation OnDeserializePayload(Message request)
        {
            Type t = TypeHelper.GetType(request.PayloadType);
            if (t != null
                && (t.Equals(typeof(ServiceOperation))
                || t.IsSubclassOf(typeof(ServiceOperation))))
            {
                ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>().Where(
                    ims => ims.SupportsFormat(request.PayloadFormat)
                    && ims.SupportsType(t)).FirstOrDefault();

                if (serializer is ISerializerTypeResolver)
                {
                    ((ISerializerTypeResolver)serializer).AddTypeResolver(new ResolveTypeEventHandler(this.OnResolveParameterType));
                }

                if (serializer == null)
                    throw (new SerializationException("Serializer could not be located for current MessagingContext"));
                else
                {
                    //this.Operation.Parameters.AddRange(
                    //    (ServiceParameterCollection)serializer.Deserialize(StreamHelper.GetBytes(this.ServiceContext.Message.PayloadStream), t));
                    return (ServiceOperation)serializer.Deserialize(StreamHelper.GetBytes(request.PayloadStream), t);
                }
            }
            else if (t != null
                && (t.Equals(typeof(ServiceParameterCollection))
                || t.IsSubclassOf(typeof(ServiceParameterCollection))))
            {
                ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>().Where(
                    ims => ims.SupportsFormat(request.PayloadFormat)
                    && ims.SupportsType(t)).FirstOrDefault();

                if (serializer is ISerializerTypeResolver)
                {
                    ((ISerializerTypeResolver)serializer).AddTypeResolver(new ResolveTypeEventHandler(this.OnResolveParameterType));
                }

                if (serializer == null)
                    throw (new SerializationException("Serializer could not be located for current MessagingContext"));
                else
                {
                    //this.Operation.Parameters.AddRange(
                    //    (ServiceParameterCollection)serializer.Deserialize(StreamHelper.GetBytes(this.ServiceContext.Message.PayloadStream), t));
                    ServiceOperation operation = new ServiceOperation(request, OperationType.Request);
                    operation.Parameters.AddRange((ServiceParameterCollection)serializer.Deserialize(StreamHelper.GetBytes(request.PayloadStream), t));
                    return operation;
                }
            }
            else if (t != null
                && (t.Equals(typeof(HttpForm))
                || t.IsSubclassOf(typeof(HttpForm))))
            {
                ServiceOperation operation = new ServiceOperation(request, OperationType.Request);
                SerializationContext s = SerializationContext.Instance; //Altus.Instance.Shell.GetComponent<SerializationContext>();
                ISerializer<HttpForm> serializer = s.GetSerializer<HttpForm>(request.PayloadFormat);
                if (serializer != null)
                {
                    HttpForm form = serializer.Deserialize(request.PayloadStream);

                    foreach (HttpFormEntry fe in form)
                    {
                        ParameterInfo pi = Attribute.Method.GetParameters().Where(p => p.Name.Equals(fe.Key, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        ISerializer ps = null;
                        ParameterDirection direction = ParameterDirection.Aspect;
                        Type pType = null;

                        if (pi == null)
                        {
                            pType = typeof(string);
                            ps = App.Instance.Shell.GetComponents<ISerializer>().Where(
                               ims => ims.SupportsFormat(request.PayloadFormat)
                               && ims.SupportsType(typeof(string))).FirstOrDefault();
                        }
                        else
                        {
                            pType = pi.ParameterType;
                            ps = App.Instance.Shell.GetComponents<ISerializer>().Where(
                                ims => ims.SupportsFormat(request.PayloadFormat)
                                && ims.SupportsType(pi.ParameterType)).FirstOrDefault();
                            direction = (pi.IsOut ? ParameterDirection.Out : ParameterDirection.In);
                        }

                        if (ps is ISerializerTypeResolver)
                        {
                            ((ISerializerTypeResolver)serializer).AddTypeResolver(new ResolveTypeEventHandler(this.OnResolveParameterType));
                        }

                        if (ps == null)
                            Logger.LogError(new SerializationException("Serializer could not be located for current MessagingContext"));
                        else
                        {
                            object value = ps.Deserialize(SerializationContext.TextEncoding.GetBytes(fe.Value.ToString()), pType);
                            ServiceParameter sp = new ServiceParameter(fe.Key, fe.Value.GetType().FullName, direction) { Value = value };
                            operation.Parameters.Add(sp);
                        }
                    }

                    operation.Parameters.Add(new ServiceParameter("Form", typeof(HttpForm).FullName, ParameterDirection.Aspect) { Value = form });
                }
                return operation;
            }
            else
                throw (new SerializationException("The provided Payload type, \"" + request.PayloadType + "\" could not be found, or could not be handled by this pipeline."));
        }

        protected virtual Type OnResolveParameterType(object sender, ResolveTypeEventArgs e)
        {
            ParameterInfo[] methodParms = Attribute.Method.GetParameters();
            ParameterInfo parm = null;
            if (e.ParameterPosition.HasValue)
            {
                parm = methodParms[e.ParameterPosition.Value];
                e.ParameterName = methodParms[e.ParameterPosition.Value].Name;
            }
            else
            {
                parm = methodParms.Where(p => p.Name.Equals(e.ParameterName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            }

            if (parm == null)
            {
                return null;
            }
            else
            {
                return parm.ParameterType;
            }
        }

        public int Priority
        {
            get { return int.MaxValue; }
        }

        protected override bool OnInitialize(params string[] args)
        {
            return true;
        }
    }
}
