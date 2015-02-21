using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using Altus.Core.Topology;
using Altus.Core.Serialization;
using Altus.Core.Exceptions;

namespace Altus.Core.Processing.Rpc
{
    public class RpcProxy : DynamicObject
    {
        private RpcProxy(string format)
        {
            this.Format = format;
        }
        public RpcProxy(NodeAddress address, string application) : this(StandardFormats.BINARY)
        {
            this.Address = address;
            this.Application = application;
        }
        public RpcProxy(NodeAddress address, string application, string format)
            : this(format)
        {
            this.Address = address;
            this.Application = application;
        }
        public RpcProxy(string nodeAddress, string application)
            : this((NodeAddress)nodeAddress, application){}
        public RpcProxy(string nodeAddress, string application, string format)
            : this((NodeAddress)nodeAddress, application, format) { }

        public NodeAddress Address { get; private set; }
        public string Application { get; private set; }
        public string ObjectPath { get; private set; }
        public string Format { get; private set; }
        public object[] OutParameters { get; private set; }
        public object ReturnParameter { get; private set; }
        public object ErrorParameter { get; private set; }

        Dictionary<string, object> _props = new Dictionary<string, object>();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (_props.ContainsKey("prop_" + binder.Name.ToLowerInvariant()))
            {
                result = _props["prop_" + binder.Name.ToLowerInvariant()];
            }
            else
            {
                if (this.ObjectPath != null)
                    this.ObjectPath += ".";
                this.ObjectPath += binder.Name;
                result = this;
            }
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            try
            {
                _props.Clear();
                ReturnParameter = null;
                OutParameters = new object[0];
                ServiceParameter[] parms = new ServiceParameter[args.Length];

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == null)
                    {
                        parms[i] = new ServiceParameter(i.ToString(), typeof(object).FullName, ParameterDirection.In) { Value = args[i] };
                    }
                    else
                    {
                        parms[i] = new ServiceParameter(i.ToString(), args[i].GetType().FullName, ParameterDirection.In) { Value = args[i] };
                    }
                }

                ServiceOperation opRet = ServiceOperation.Call(this.Address.Address, this.Application, this.ObjectPath, binder.Name, this.Format, parms);
                result = opRet.Parameters.Result;

                List<object> outs = new List<object>();

                foreach (ServiceParameter sp in opRet.Parameters)
                {
                    _props.Add("prop_" + sp.Name.ToLowerInvariant(), sp.Value);
                    if (sp.Direction == ParameterDirection.Out)
                    {
                        outs.Add(sp.Value);
                    }
                    else if (sp.Direction == ParameterDirection.Return)
                    {
                        this.ReturnParameter = sp.Value;
                    }
                    else if (sp.Direction == ParameterDirection.Error)
                    {
                        throw (Exception)sp.Value;
                    }
                }

                this.ReturnParameter = result;

                return true;
            }
            catch (RuntimeException)
            {
                throw;
            }
            catch
            {
                result = null;
                return false;
            }
            finally
            {
                this.ObjectPath = null;
            }
        }
    }
}
