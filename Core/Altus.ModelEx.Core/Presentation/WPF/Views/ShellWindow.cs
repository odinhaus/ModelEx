using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Presentation.ViewModels;
using Altus.Core.Data;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Altus.Core.Licensing;
using Altus.Core.Component;

namespace Altus.Core.Presentation.Wpf.Views
{
    public class ShellWindow : System.Windows.Window, IDynamicMetaObjectProvider, ILicensedComponent
    {
        public static readonly System.Windows.DependencyProperty WindowNameProperty = System.Windows.DependencyProperty.Register("WindowName", typeof(string), typeof(ShellWindow));

        public ShellWindow()
        {
            this.Loaded += ShellWindow_Loaded;
            App.Instance.Shell.ComponentChanged += Shell_ComponentChanged;
        }

        void Shell_ComponentChanged(object sender, CompositionContainerComponentEventArgs e)
        {
            if (e.Component is View)
            {
                if (e.Change == CompositionContainerComponentChange.Add)
                {
                    OnViewAdded((View)e.Component);
                }
                else
                {
                    OnViewRemoved((View)e.Component);
                }
            }
        }

        protected virtual void OnViewAdded(View view)
        {

        }

        protected virtual void OnViewRemoved(View view)
        {

        }

        void ShellWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ILicenseManager lmgr = App.Instance.Shell.GetComponent<ILicenseManager>();
            if (lmgr != null)
            {
                this.ApplyLicensing(lmgr.GetLicenses(), App.Instance.API.Args);
            }
        }

        public string WindowName
        {
            get { return (string)GetValue(WindowNameProperty); }
            set { SetValue(WindowNameProperty, value); }
        }

        protected string[] GetViewTypes()
        {
            List<string> viewTypes = new List<string>();
            DeclaredApp current = Context.CurrentContext.CurrentApp;

            foreach (View view in App.Instance.Shell.GetComponents<View>())
            {
                if (!viewTypes.Contains(view.ViewType))
                    viewTypes.Add(view.ViewType);
            }

            foreach (DeclaredApp app in App.Instance.Apps)
            {
                Context.CurrentContext.CurrentApp = app;

                foreach(string vt in Altus.Core.Data.DataContext.Default.GetViewTypes(this.WindowName, "wpf"))
                {
                    if (!viewTypes.Contains(vt))
                        viewTypes.Add(vt);
                }
            }
            Context.CurrentContext.CurrentApp = current;
            return viewTypes.ToArray();
        }

        private Dictionary<string, ViewList> _viewsByType = new Dictionary<string,ViewList>();
        private ViewList _allViews = new ViewList();
        protected ViewList GetViews(string viewType, string defaultSize)
        {
            if (!_viewsByType.ContainsKey(viewType))
            {
                ViewList allAppsList = new ViewList(this.WindowName);
                foreach (View view in App.Instance.Shell.GetComponents<View>().Where(vw => vw.ViewType.Equals(viewType, StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (!allAppsList.Contains(view))
                        allAppsList.Add(view);
                }
                DeclaredApp current = Context.CurrentContext.CurrentApp;
                foreach (DeclaredApp app in App.Instance.Apps)
                {
                    Context.CurrentContext.CurrentApp = app;
                    try
                    {
                        ViewList appList = Altus.Core.Data.DataContext.Default.Get<ViewList>(
                            new 
                            { 
                                windowName = this.WindowName, 
                                viewType = viewType, 
                                uiType = "wpf", 
                                defaultSize = defaultSize 
                            });
                        if (appList != null)
                        {
                            foreach (View v in appList)
                            {
                                if (!allAppsList.Contains(v))
                                    allAppsList.Add(v);
                            }
                        }
                    }
                    catch { }
                }
                Context.CurrentContext.CurrentApp = current;

                ViewList licensedList = new ViewList();
                ILicenseManager lmgr = App.Instance.Shell.GetComponent<ILicenseManager>();
                ILicense[] licenses = lmgr.GetLicenses();
                foreach (View view in allAppsList)
                {
                    if (lmgr == null)
                    {
                        view.CurrentSize = defaultSize;
                        licensedList.Add(view);
                        _allViews.Add(view);
                    }
                    else
                    {
                        view.ApplyLicensing(licenses, App.Instance.API.Args);
                        if (view.IsLicensed(view))
                        {
                            view.Initialize(view.Name, App.Instance.API.Args);
                            view.CurrentSize = defaultSize;
                            licensedList.Add(view);
                            _allViews.Add(view);
                            App.Instance.Shell.Add(view, view.App.Name + ":" + view.Name + "_View");
                        }
                    }
                }
                _viewsByType.Add(viewType, licensedList);
            }
            else
            {
                foreach (View view in _viewsByType[viewType])
                {
                    view.CurrentSize = defaultSize;
                }
            }
            return _viewsByType[viewType];
        }

        protected View GetView(string viewName, string defaultSize)
        {
            View view = null;
            foreach (View v in _allViews)
            {
                if (v.Name.Equals(viewName, StringComparison.InvariantCultureIgnoreCase))
                {
                    view = v; 
                    break;
                }
            }
            if (view == null)
            {
                view = Altus.Core.Data.DataContext.Default.Get<View>(new { windowName = this.WindowName, viewName = viewName, uiType = "wpf", defaultSize = defaultSize });
                if (view == null)
                {
                    foreach (View v in App.Instance.Shell.GetComponents<View>())
                    {
                        if (v.Name.Equals(viewName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            view = v;
                            break;
                        }
                    }
                }

                ILicenseManager lmgr = App.Instance.Shell.GetComponent<ILicenseManager>();
                ILicense[] licenses = lmgr.GetLicenses();

                if (lmgr == null)
                {
                    view.CurrentSize = defaultSize;
                    _allViews.Add(view);
                    return view;
                }
                else
                {
                    view.ApplyLicensing(licenses, App.Instance.API.Args);
                    if (view.IsLicensed(view))
                    {
                        view.CurrentSize = defaultSize;
                        _allViews.Add(view);
                        return view;
                    }
                    else return null;
                }
            }
            else
            {
                view.CurrentSize = defaultSize;
                return view;
            }
        }

        public virtual IEnumerable<string> GetDynamicMemberNames()
        {
            return new string[0];
        }
        
        public virtual DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new MetaDynamic(parameter, this);
        }
        
        public virtual bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            result = null;
            return false;
        }
        
        public virtual bool TryConvert(ConvertBinder binder, out object result)
        {
            result = null;
            return false;
        }
        
        public virtual bool TryCreateInstance(CreateInstanceBinder binder, object[] args, out object result)
        {
            result = null;
            return false;
        }
        
        public virtual bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes)
        {
            return false;
        }
        
        public virtual bool TryDeleteMember(DeleteMemberBinder binder)
        {
            return false;
        }
        
        public virtual bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;
            return false;
        }
        
        public virtual bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            return false;
        }
        
        public virtual bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = null;
            return false;
        }
        
        public virtual bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;
            return false;
        }
        
        public virtual bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            return false;
        }
        
        public virtual bool TrySetMember(SetMemberBinder binder, object value)
        {
            return false;
        }
        
        public virtual bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
        {
            result = null;
            return false;
        }
        
        private sealed class MetaDynamic : DynamicMetaObject
        {
            private static readonly Expression[] NoArgs = new Expression[0];

            internal MetaDynamic(Expression expression, ShellWindow value)
                : base(expression, BindingRestrictions.Empty, value)
            {
            }
            
            public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
            {
                Fallback fallback = null;
                if (!this.IsOverridden("TryBinaryOperation"))
                {
                    return base.BindBinaryOperation(binder, arg);
                }
                if (fallback == null)
                {
                    fallback = e => binder.FallbackBinaryOperation(this, arg, e);
                }
                return this.CallMethodWithResult("TryBinaryOperation", binder, GetArgs(new DynamicMetaObject[] { arg }), fallback);
            }
            
            public override DynamicMetaObject BindConvert(ConvertBinder binder)
            {
                Fallback fallback = null;
                if (!this.IsOverridden("TryConvert"))
                {
                    return base.BindConvert(binder);
                }
                if (fallback == null)
                {
                    fallback = e => binder.FallbackConvert(this, e);
                }
                return this.CallMethodWithResult("TryConvert", binder, NoArgs, fallback);
            }
            
            public override DynamicMetaObject BindCreateInstance(CreateInstanceBinder binder, DynamicMetaObject[] args)
            {
                Fallback fallback = null;
                if (!this.IsOverridden("TryCreateInstance"))
                {
                    return base.BindCreateInstance(binder, args);
                }
                if (fallback == null)
                {
                    fallback = e => binder.FallbackCreateInstance(this, args, e);
                }
                return this.CallMethodWithResult("TryCreateInstance", binder, GetArgArray(args), fallback);
            }
            
            public override DynamicMetaObject BindDeleteIndex(DeleteIndexBinder binder, DynamicMetaObject[] indexes)
            {
                Fallback fallback = null;
                if (!this.IsOverridden("TryDeleteIndex"))
                {
                    return base.BindDeleteIndex(binder, indexes);
                }
                if (fallback == null)
                {
                    fallback = e => binder.FallbackDeleteIndex(this, indexes, e);
                }
                return this.CallMethodNoResult("TryDeleteIndex", binder, GetArgArray(indexes), fallback);
            }
            
            public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
            {
                Fallback fallback = null;
                if (!this.IsOverridden("TryDeleteMember"))
                {
                    return base.BindDeleteMember(binder);
                }
                if (fallback == null)
                {
                    fallback = e => binder.FallbackDeleteMember(this, e);
                }
                return this.CallMethodNoResult("TryDeleteMember", binder, NoArgs, fallback);
            }
            
            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                Fallback fallback = null;
                if (!this.IsOverridden("TryGetIndex"))
                {
                    return base.BindGetIndex(binder, indexes);
                }
                if (fallback == null)
                {
                    fallback = e => binder.FallbackGetIndex(this, indexes, e);
                }
                return this.CallMethodWithResult("TryGetIndex", binder, GetArgArray(indexes), fallback);
            }
            
            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                Fallback fallback = null;
                if (!this.IsOverridden("TryGetMember"))
                {
                    return base.BindGetMember(binder);
                }
                if (fallback == null)
                {
                    fallback = e => binder.FallbackGetMember(this, e);
                }
                return this.CallMethodWithResult("TryGetMember", binder, NoArgs, fallback);
            }
            
            public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
            {
                Fallback fallback = null;
                if (!this.IsOverridden("TryInvoke"))
                {
                    return base.BindInvoke(binder, args);
                }
                if (fallback == null)
                {
                    fallback = e => binder.FallbackInvoke(this, args, e);
                }
                return this.CallMethodWithResult("TryInvoke", binder, GetArgArray(args), fallback);
            }
            
            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                Fallback fallback = e => binder.FallbackInvokeMember(this, args, e);
                DynamicMetaObject errorSuggestion = this.BuildCallMethodWithResult("TryInvokeMember", binder, GetArgArray(args), this.BuildCallMethodWithResult("TryGetMember", new GetBinderAdapter(binder), NoArgs, fallback(null), e => binder.FallbackInvoke(e, args, null)), null);
                return fallback(errorSuggestion);
            }
            
            public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
            {
                Fallback fallback = null;
                if (!this.IsOverridden("TrySetIndex"))
                {
                    return base.BindSetIndex(binder, indexes, value);
                }
                if (fallback == null)
                {
                    fallback = e => binder.FallbackSetIndex(this, indexes, value, e);
                }
                return this.CallMethodReturnLast("TrySetIndex", binder, GetArgArray(indexes, value), fallback);
            }
            
            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                Fallback fallback = null;
                if (!this.IsOverridden("TrySetMember"))
                {
                    return base.BindSetMember(binder, value);
                }
                if (fallback == null)
                {
                    fallback = e => binder.FallbackSetMember(this, value, e);
                }
                return this.CallMethodReturnLast("TrySetMember", binder, GetArgs(new DynamicMetaObject[] { value }), fallback);
            }
            
            public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
            {
                Fallback fallback = null;
                if (!this.IsOverridden("TryUnaryOperation"))
                {
                    return base.BindUnaryOperation(binder);
                }
                if (fallback == null)
                {
                    fallback = e => binder.FallbackUnaryOperation(this, e);
                }
                return this.CallMethodWithResult("TryUnaryOperation", binder, NoArgs, fallback);
            }
            
            private DynamicMetaObject BuildCallMethodWithResult(string methodName, DynamicMetaObjectBinder binder, Expression[] args, DynamicMetaObject fallbackResult, Fallback fallbackInvoke)
            {
                if (!this.IsOverridden(methodName))
                {
                    return fallbackResult;
                }
                ParameterExpression expression = Expression.Parameter(typeof(object), null);
                Expression[] destinationArray = new Expression[args.Length + 2];
                Array.Copy(args, 0, destinationArray, 1, args.Length);
                destinationArray[0] = Constant(binder);
                destinationArray[destinationArray.Length - 1] = expression;
                DynamicMetaObject errorSuggestion = new DynamicMetaObject(expression, BindingRestrictions.Empty);
                if (binder.ReturnType != typeof(object))
                {
                    errorSuggestion = new DynamicMetaObject(Expression.Convert(errorSuggestion.Expression, binder.ReturnType), errorSuggestion.Restrictions);
                }
                if (fallbackInvoke != null)
                {
                    errorSuggestion = fallbackInvoke(errorSuggestion);
                }
                return new DynamicMetaObject(Expression.Block(new ParameterExpression[] { expression }, new Expression[] { Expression.Condition(Expression.Call(this.GetLimitedSelf(), typeof(DynamicObject).GetMethod(methodName), destinationArray), errorSuggestion.Expression, fallbackResult.Expression, binder.ReturnType) }), this.GetRestrictions().Merge(errorSuggestion.Restrictions).Merge(fallbackResult.Restrictions));
            }
            
            private DynamicMetaObject CallMethodNoResult(string methodName, DynamicMetaObjectBinder binder, Expression[] args, Fallback fallback)
            {
                DynamicMetaObject obj2 = fallback(null);
                DynamicMetaObject errorSuggestion = new DynamicMetaObject(Expression.Condition(Expression.Call(this.GetLimitedSelf(), typeof(DynamicObject).GetMethod(methodName), args.AddFirst<Expression>(Constant(binder))), Expression.Empty(), obj2.Expression, typeof(void)), this.GetRestrictions().Merge(obj2.Restrictions));
                return fallback(errorSuggestion);
            }
            
            private DynamicMetaObject CallMethodReturnLast(string methodName, DynamicMetaObjectBinder binder, Expression[] args, Fallback fallback)
            {
                DynamicMetaObject obj2 = fallback(null);
                System.Linq.Expressions.ParameterExpression left = Expression.Parameter(typeof(object), null);
                Expression[] arguments = args.AddFirst<Expression>(Constant(binder));
                arguments[args.Length] = Expression.Assign(left, arguments[args.Length]);
                DynamicMetaObject errorSuggestion = new DynamicMetaObject(Expression.Block(new ParameterExpression[] { left }, new Expression[] { Expression.Condition(Expression.Call(this.GetLimitedSelf(), typeof(DynamicObject).GetMethod(methodName), arguments), left, obj2.Expression, typeof(object)) }), this.GetRestrictions().Merge(obj2.Restrictions));
                return fallback(errorSuggestion);
            }
            
            private DynamicMetaObject CallMethodWithResult(string methodName, DynamicMetaObjectBinder binder, Expression[] args, Fallback fallback)
            {
                return this.CallMethodWithResult(methodName, binder, args, fallback, null);
            }
            
            private DynamicMetaObject CallMethodWithResult(string methodName, DynamicMetaObjectBinder binder, Expression[] args, Fallback fallback, Fallback fallbackInvoke)
            {
                DynamicMetaObject fallbackResult = fallback(null);
                DynamicMetaObject errorSuggestion = this.BuildCallMethodWithResult(methodName, binder, args, fallbackResult, fallbackInvoke);
                return fallback(errorSuggestion);
            }
            
            private static System.Linq.Expressions.ConstantExpression Constant(DynamicMetaObjectBinder binder)
            {
                Type baseType = binder.GetType();
                while (!baseType.IsVisible)
                {
                    baseType = baseType.BaseType;
                }
                return Expression.Constant(binder, baseType);
            }
            
            private static Expression[] GetArgArray(DynamicMetaObject[] args)
            {
                return new NewArrayExpression[] { Expression.NewArrayInit(typeof(object), GetArgs(args)) };
            }
            
            private static Expression[] GetArgArray(DynamicMetaObject[] args, DynamicMetaObject value)
            {
                return new Expression[] { Expression.NewArrayInit(typeof(object), GetArgs(args)), Expression.Convert(value.Expression, typeof(object)) };
            }
            
            private static Expression[] GetArgs(params DynamicMetaObject[] args)
            {
                Expression[] expressions = GetExpressions(args);
                for (int i = 0; i < expressions.Length; i++)
                {
                    expressions[i] = Expression.Convert(args[i].Expression, typeof(object));
                }
                return expressions;
            }

            private static Expression[] GetExpressions(params DynamicMetaObject[] objects)
            {
                RequiresNotNull(objects, "objects");
                System.Linq.Expressions.Expression[] expressionArray = new System.Linq.Expressions.Expression[objects.Length];
                for (int i = 0; i < objects.Length; i++)
                {
                    DynamicMetaObject obj2 = objects[i];
                    RequiresNotNull(obj2, "objects");
                    System.Linq.Expressions.Expression expression = obj2.Expression;
                    RequiresNotNull(expression, "objects");
                    expressionArray[i] = expression;
                }
                return expressionArray;
            }

            private static void RequiresNotNull(object value, string paramName)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(paramName);
                }
            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return this.Value.GetDynamicMemberNames();
            }
            
            private Expression GetLimitedSelf()
            {
                if (base.Expression.Type.Equals(typeof(DynamicObject)))
                {
                    return base.Expression;
                }
                return Expression.Convert(base.Expression, typeof(DynamicObject));
            }
            
            private BindingRestrictions GetRestrictions()
            {
                return GetTypeRestriction(this);
            }

            private static BindingRestrictions GetTypeRestriction(DynamicMetaObject obj)
            {
                if ((obj.Value == null) && obj.HasValue)
                {
                    return BindingRestrictions.GetInstanceRestriction(obj.Expression, null);
                }
                return BindingRestrictions.GetTypeRestriction(obj.Expression, obj.LimitType);
            }
            
            private bool IsOverridden(string method)
            {
                foreach (MethodInfo info in this.Value.GetType().GetMember(method, MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance))
                {
                    if ((info.DeclaringType != typeof(DynamicObject)) && (info.GetBaseDefinition().DeclaringType == typeof(DynamicObject)))
                    {
                        return true;
                    }
                }
                return false;
            }
            
            private ShellWindow Value
            {
                get
                {
                    return (ShellWindow)base.Value;
                }
            }
            
            private delegate DynamicMetaObject Fallback(DynamicMetaObject errorSuggestion);
            
            private sealed class GetBinderAdapter : GetMemberBinder
            {
                internal GetBinderAdapter(InvokeMemberBinder binder) : base(binder.Name, binder.IgnoreCase)
                {
                }
                
                public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
                {
                    throw new NotSupportedException();
                }
            }
        }

        public void ApplyLicensing(ILicense[] licenses, params string[] args)
        {
            OnApplyLicensing(licenses, args);
        }

        protected virtual void OnApplyLicensing(ILicense[] licenses, params string[] args)
        {

        }

        public bool IsLicensed(object component)
        {
            return OnIsLicensed(component);
        }

        protected virtual bool OnIsLicensed(object component)
        {
            return true;
        }

        public event EventHandler Disposed;

        public System.ComponentModel.ISite Site
        {
            get;
            set;
        }

        public void Dispose()
        {
            if (Disposed != null)
                Disposed(this, new EventArgs());
        }
    }

    internal static class CollectionExtensions
    {
        internal static T[] AddFirst<T>(this IList<T> list, T item)
        {
            T[] array = new T[list.Count + 1];
            array[0] = item;
            list.CopyTo(array, 1);
            return array;
        }

        internal static T[] AddLast<T>(this IList<T> list, T item)
        {
            T[] array = new T[list.Count + 1];
            list.CopyTo(array, 0);
            array[list.Count] = item;
            return array;
        }
    }
}
