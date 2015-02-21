using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Dynamic;

namespace Altus.Core.Dynamic
{
    public class Derrick_IDynamicFunctionEvaluator : DynamicObject, IDynamicFunctionEvaluator
    {
        public Derrick_IDynamicFunctionEvaluator(dynamic instance, string instanceName)
        {
            this.InstanceName = instanceName;
            this.Instance = instance;
        }

        public string InstanceName { get; private set; }
        public dynamic Instance { get; private set; }

        public object Execute(string methodName, object[] args)
        {
            MethodInfo[] methods = this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.Name.Equals(methodName, StringComparison.InvariantCultureIgnoreCase)).ToArray();

            if (methods != null && methods.Length > 0)
            {
                MethodInfo method = MatchMethodToArgs(methods, args); ;

                if (method != null)
                    return method.Invoke(this, args);
            }

            throw (new NotImplementedException("The function " + methodName + " is not implemented on instance " + this.InstanceName));
        }

        private MethodInfo MatchMethodToArgs(MethodInfo[] methods, object[] args)
        {
            foreach (MethodInfo mi in methods)
            {
                ParameterInfo[] parms = mi.GetParameters();
                if (parms.Length == args.Length)
                {
                    for (int i = 0; i < parms.Length; i++)
                    {
                        if (!parms[i].ParameterType.Equals(typeof(object))
                            && !(parms[i].ParameterType.Equals(args[i].GetType())
                            || args[i].GetType().IsSubclassOf(parms[i].ParameterType)))
                        {
                            continue;
                        }
                        return mi;
                    }
                }
            }

            return null;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return this.Instance.TryGetMember(binder, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return this.Instance.TryGetMember(binder, value);
        }

        public void HelloWorld(object sender, EventArgs e)
        {
            ((dynamic)this).Message = "you clicked me";
        }

    }
}