using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Altus.Core.Diagnostics;
using System.Linq.Expressions;

namespace Altus.Core.Dynamic
{
    public class DynamicWrapper : DynamicObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DynamicWrapper(object wrappedInstance)
        {
            this.BackingInstance = wrappedInstance;
        }

        private object _bi;
        public object BackingInstance
        {
            get { return _bi; }
            protected set
            {
                _bi = value;
                if (_bi is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged)_bi).PropertyChanged += new PropertyChangedEventHandler(dp_PropertyChanged);
                }
                this.OnPropertyChanged("BackingInstance");
            }
        }

        void dp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(e.PropertyName);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
            }

            OnPropertyChangedInvoke(name);
        }

        protected virtual void OnPropertyChangedInvoke(string propertyName)
        {
            propertyName = "On" + propertyName + "Changed";
            this.OnInvokeByName(propertyName);
        }

        private object OnBackingInstanceChangedWrapper(object arg)
        {
            OnBackingInstanceChanged();
            return arg;
        }
        protected virtual void OnBackingInstanceChanged() {  }

        Dictionary<string, Func<object, object>> _InvokeByNameHandlers = new Dictionary<string, Func<object, object>>();
        protected virtual void OnInvokeByName(string methodName, params object[] args)
        {
            Func<object, object> handler = null;
            lock (_InvokeByNameHandlers)
            {
                if ((args == null || args.Length == 0)
                    && _InvokeByNameHandlers.ContainsKey(methodName))
                {
                    handler = _InvokeByNameHandlers[methodName];
                }
                else
                {
                    if (methodName == "OnBackingInstanceChanged")
                    {
                        handler = new Func<object, object>(this.OnBackingInstanceChangedWrapper);
                    }
                    else
                    {
                        Type[] typeArgs = new Type[args == null ? 0 : args.Length];
                        Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo[] argInfos = new Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo[1 + (args == null ? 0 : args.Length)];
                        argInfos[0] = Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.None, null);
                        System.Linq.Expressions.Expression[] dynArgs = new System.Linq.Expressions.Expression[1 + (args == null ? 0 : args.Length)];
                        dynArgs[0] = System.Linq.Expressions.Expression.Constant(this);

                        if (args != null)
                        {
                            for (int i = 0; i < args.Length; i++)
                            {
                                typeArgs[i] = args[i].GetType();
                                argInfos[i + 1] = Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.None, null);
                                dynArgs[i + 1] = System.Linq.Expressions.Expression.Constant(args[i]);
                            }
                        }
                        CallSiteBinder csb = Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(
                            Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags.None,
                            methodName,
                            typeArgs,
                            this.GetType(),
                            argInfos);
                        System.Linq.Expressions.Expression exp = System.Linq.Expressions.Expression.Dynamic(
                            csb,
                            typeof(object),
                            dynArgs);
                        System.Linq.Expressions.Expression<Func<object, object>> expr = System.Linq.Expressions.Expression.Lambda<Func<object, object>>(
                            exp,
                            System.Linq.Expressions.Expression.Parameter(typeof(object)));
                        handler = expr.Compile();
                    }
                    if (args == null || args.Length == 0)
                        _InvokeByNameHandlers.Add(methodName, handler);
                }
            }
            try
            {
                if (handler != null)
                    handler.DynamicInvoke(this);
            }
            catch (Exception ex)
            {
                if (ex.InnerException == null
                    || !(ex.InnerException is Microsoft.CSharp.RuntimeBinder.RuntimeBinderException))
                {
                    Logger.Log(ex);
                }
                else
                {
                    lock (_InvokeByNameHandlers)
                    {
                        _InvokeByNameHandlers[methodName] = null;
                    }
                }
            }
        }

        public override DynamicMetaObject GetMetaObject(System.Linq.Expressions.Expression parameter)
        {
            //return base.GetMetaObject(parameter);
            DMO meta = new DMO( parameter, this);
            return meta;
        }

        private object GetMemberValue(GetMemberBinder binder)
        {
            object result = null;
            if (this.TryGetMember(binder, out result))
            {
                return result;
            }
            else throw new Microsoft.CSharp.RuntimeBinder.RuntimeBinderException("Member " + binder.Name + " not found on type " + this.BackingInstance.GetType().Name);
        }

        private void SetMemberValue(SetMemberBinder binder, object value)
        {
            if (!this.TrySetMember(binder, value))
                throw new Microsoft.CSharp.RuntimeBinder.RuntimeBinderException("Member " + binder.Name + " not found on type " + this.BackingInstance.GetType().Name);
        }

        private object Invoke(InvokeBinder binder, object[] args)
        {
            object result = null;
            if (this.TryInvoke(binder, args, out result))
                return result;
            else
                throw new Microsoft.CSharp.RuntimeBinder.RuntimeBinderException("Invoke " + binder.ToString() + " failed on type " + this.BackingInstance.GetType().Name);
        }
        private object InvokeMember(InvokeMemberBinder binder, object[] args)
        {
            object result = null;
            if (this.TryInvokeMember(binder, args, out result))
                return result;
            else
                throw new Microsoft.CSharp.RuntimeBinder.RuntimeBinderException("Invoke " + binder.Name + " failed on type " + this.BackingInstance.GetType().Name);
        }


        private class DMO : DynamicMetaObject
        {
            public DMO(Expression parameter, DynamicWrapper parent)
                : base(parameter, BindingRestrictions.Empty, parent)
            {
                Parent = parent;
            }

            public DynamicWrapper Parent { get; private set; }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

                MethodInfo mi = typeof(DynamicWrapper).GetMethod("GetMemberValue", BindingFlags.NonPublic | BindingFlags.Instance);
                Expression[] args = new Expression[]
                {
                    Expression.Constant(binder)
                };
                Expression ret = Expression.Call(Expression.Convert(Expression, typeof(DynamicWrapper)), mi, args);
                return new DynamicMetaObject(ret, restrictions);
            }

            

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

                MethodInfo mi = typeof(DynamicWrapper).GetMethod("SetMemberValue", BindingFlags.NonPublic | BindingFlags.Instance);
                Expression[] args = new Expression[]
                {
                    Expression.Constant(binder),
                    Expression.Constant(value.Value)
                };
                Expression ret = Expression.Call(Expression.Convert(Expression, typeof(DynamicWrapper)), mi, args);
                return new DynamicMetaObject(ret, restrictions);
            }

            public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
            {
                BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

                object[] vals = new object[args.Length];
                for (int i = 0; i < args.Length; i++)
                    vals[i] = args[i].Value;

                MethodInfo mi = typeof(DynamicWrapper).GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Instance);
                Expression[] ps = new Expression[]
                {
                    Expression.Constant(binder),
                    Expression.Constant(vals)
                };
                Expression ret = Expression.Call(Expression.Convert(Expression, typeof(DynamicWrapper)), mi, ps);
                return new DynamicMetaObject(ret, restrictions);

            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

                object[] vals = new object[args.Length];
                for (int i = 0; i < args.Length; i++)
                    vals[i] = args[i].Value;

                MethodInfo mi = typeof(DynamicWrapper).GetMethod("InvokeMember", BindingFlags.NonPublic | BindingFlags.Instance);
                Expression[] ps = new Expression[]
                {
                    Expression.Constant(binder),
                    Expression.Constant(vals)
                };
                Expression ret = Expression.Call(Expression.Convert(Expression, typeof(DynamicWrapper)), mi, ps);
                return new DynamicMetaObject(ret, restrictions);
            }

        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            foreach (MemberInfo mi in this.BackingInstance.GetType().GetMembers())
                yield return mi.Name;
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            return base.TryInvoke(binder, args, out result);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            MemberInfo[] members = this.GetType().GetMember(binder.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (members != null && members.Length > 0)
            {
                MemberInfo member = members[0];
                if (!member.DeclaringType.Equals(this.GetType()))
                    member = member.DeclaringType.GetMember(binder.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];

                if (member is PropertyInfo)
                {
                    result = ((PropertyInfo)member).GetValue(this, null);
                    return true;
                }
                else if (member is FieldInfo)
                {
                    result = ((FieldInfo)member).GetValue(this);
                    return true;
                }
            }
            if (this.BackingInstance != null)
            {
                if (this.BackingInstance is DynamicObject)
                {
                    if (((DynamicObject)this.BackingInstance).TryGetMember(binder, out result))
                        return true;
                }
                else
                {
                    if (TryGetBackingMember(binder.Name, out result))
                        return true;
                }
            }
            return base.TryGetMember(binder, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (this.BackingInstance != null)
            {
                if (this.BackingInstance is DynamicObject)
                {
                    if (((DynamicObject)this.BackingInstance).TrySetMember(binder, value))
                        return true;
                }
                else
                {
                    if (TrySetBackingMember(binder.Name, value))
                        return true;
                }
            }
            return base.TrySetMember(binder, value);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            MethodInfo method = this.GetType().GetMethod(binder.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (method != null)
            {
                result = method.Invoke(this, args);
                return true;
            }

            if (this.BackingInstance != null)
            {
                if (this.BackingInstance is DynamicObject)
                {
                    if (((DynamicObject)this.BackingInstance).TryInvokeMember(binder, args, out result))
                        return true;
                }
                else
                {
                    if (TryInvokeBackingMember(binder.Name, args, out result))
                        return true;
                }
            }
            return base.TryInvokeMember(binder, args, out result);
        }

        private bool TryGetBackingMember(string memberName, out object result)
        {
            MemberInfo[] members = this.BackingInstance.GetType().GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            result = null;
            if (members != null && members.Length > 0)
            {
                MemberInfo mi = members[0]; // pick the first one
                if (!mi.DeclaringType.Equals(this.BackingInstance.GetType()))
                {
                    mi = mi.DeclaringType.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
                }
                if (mi is PropertyInfo)
                {
                    result = ((PropertyInfo)mi).GetValue(this.BackingInstance, null);
                    return true;
                }
                else if (mi is FieldInfo)
                {
                    result = ((FieldInfo)mi).GetValue(this.BackingInstance);
                    return true;
                }
            }
            return false;
        }

        private bool TryInvokeBackingMember(string memberName, object[] args, out object result)
        {
            MemberInfo[] members = this.BackingInstance.GetType().GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            result = null;
            if (members != null && members.Length > 0)
            {
                MemberInfo mi = members[0]; // pick the first one
                if (!mi.DeclaringType.Equals(this.BackingInstance.GetType()))
                {
                    mi = mi.DeclaringType.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
                }
                if (mi is MethodInfo)
                {
                    result = ((MethodInfo)mi).Invoke(this.BackingInstance, args);
                    return true;
                }
            }
            return false;
        }

        private bool TrySetBackingMember(string memberName, object value)
        {
            MemberInfo[] members = this.BackingInstance.GetType().GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (members != null && members.Length > 0)
            {
                MemberInfo mi = members[0]; // pick the first one
                if (!mi.DeclaringType.Equals(this.BackingInstance.GetType()))
                {
                    mi = mi.DeclaringType.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
                }
                if (mi is PropertyInfo)
                {
                    ((PropertyInfo)mi).SetValue(this.BackingInstance, value, null);
                    return true;
                }
                else if (mi is FieldInfo)
                {
                    ((FieldInfo)mi).SetValue(this.BackingInstance, value);
                    return true;
                }
            }
            return false;
        }
    }
}
