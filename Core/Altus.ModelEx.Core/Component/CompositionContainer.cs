//************************************************************************************************************
//  
//  COPYRIGHT ALTUS SERVCIES, LLC 2006, All Rights Reserved     
// 
//
//  Class History
//============================================================================================================
//
//  Developer       Date            Comments
//------------------------------------------------------------------------------------------------------------
//  BILLBL      06/18/2006 10:16:07          [your comments here]
//
//
//
//
//************************************************************************************************************

#region References and Aliases

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Altus.Core.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using Altus.Core;
using Altus.Core.Messaging;
using System.ServiceProcess;
using Altus.Core.Licensing;
using Altus.Core.Reflection;

#endregion References and Aliases


namespace Altus.Core.Component
{
    public delegate void CompositionContainerComponentChangedHandler(object sender, CompositionContainerComponentEventArgs e);

    public enum CompositionContainerComponentChange
    {
        Add,
        Delete
    }

    public class CompositionContainerComponentEventArgs : EventArgs
    {
        public CompositionContainerComponentEventArgs(
            CompositionContainerComponentChange change,
            IComponent component,
            string name)
        {
            Change = change;
            Component = component;
            Name = name;
        }

        public CompositionContainerComponentChange Change { get; private set; }
        public IComponent Component { get; private set; }
        public string Name { get; private set; }
    }

    //========================================================================================================//
    /// <summary>
    /// Class name:  Shell
    /// Class description:
    /// Usage:
    /// <example></example>
    /// <remarks></remarks>
    /// </summary>
    //========================================================================================================//
    [Serializable]
    public class CompositionContainer : ApplicationContext, IContainer
    {
        #region Fields
        #region Static Fields
        #endregion Static Fields

        #region Instance Fields
        Dictionary<string, IComponent[]> _resolved = new Dictionary<string, IComponent[]>();
        bool _shellClosed = false;
        CompositionContainerAttribute _attribute;
        IComponentLoader _loader;
        bool _mainFormClosing = false;
        Dictionary<string, IComponent> _components = new Dictionary<string, IComponent>();
        string[] _startupArgs;
        #endregion Instance Fields
        #endregion Fields

        #region Event Declarations
        public event CompositionContainerComponentChangedHandler ComponentChanged;
        #endregion Event Declarations

        #region Constructors
        #region Public
        public CompositionContainer()
        {

        }


        #endregion Public
        #endregion  Constructors

        #region Properties
        #region Public
        /// <summary>
        /// Returns whether the shell process is finished exiting
        /// </summary>
        protected bool ShellClosed
        {
            get { return _shellClosed; }
            set { _shellClosed = value; }
        }

        /// <summary>
        /// Returns whether the main form is closing
        /// </summary>
        protected bool MainFormClosing
        {
            get
            {
                return _mainFormClosing;
            }
            set
            {
                _mainFormClosing = value;
            }
        }
        /// <summary>
        /// Gets the shell starup arguments
        /// </summary>
        public string[] StartupArgs
        {
            get { return _startupArgs; }
            set { _startupArgs = value; }
        }
        /// <summary>
        /// Gets the attribute used to define the shell
        /// </summary>
        protected CompositionContainerAttribute CompositionContainerAttribute
        {
            get { return _attribute; }
            private set { _attribute = value; }
        }
        /// <summary>
        /// Gets the dictionary of components in the shell
        /// </summary>
        protected Dictionary<string, IComponent> ComponentDictionary
        {
            get { return _components; }
        }
        /// <summary>
        /// Gets the IComponentLoader used by the container to inject components at startup
        /// </summary>
        public IComponentLoader ComponentLoader
        {
            get { return _loader; }
            protected set { _loader = value; }
        }
        /// <summary>
        /// Gets/sets an optional exit code that can be returned when the application
        /// exits; default is 0 (zero).
        /// </summary>
        public int ExitCode { get; set; }
        #endregion Public

        #region Private
        #endregion Private

        #region Protected
        #endregion Protected
        #endregion Properties

        #region Methods
        #region Public

        public void Initialize(CompositionContainerAttribute shellAttribute, params string[] startupArgs)
        {
            StartupArgs = startupArgs;
            CompositionContainerAttribute = shellAttribute;
            this.ComponentLoader = (IComponentLoader)TypeHelper.CreateType(this.CompositionContainerAttribute.LoaderType, null);
            this.ComponentLoader.LoadStatus += new ComponentLoadStatusHandler(OnLoadStatus);
            this.ComponentLoader.LoadComplete += new ComponentLoadCompleteHandler(OnLoadComplete);
            this.OnInitialize(shellAttribute, startupArgs);
        }

        public bool IsExiting { get; protected set; }
        public bool Exit(bool forced)
        {
            if (this.IsExiting) return true;
            
            this.IsExiting = true;
            return OnExit(forced);
        }


        /// <summary>
        /// Unloads all views within the shell, and returns true if unload was successful, otherwise
        /// returns false.
        /// </summary>
        /// <returns></returns>
        public bool Unload(bool forced)
        {
            return this.OnUnload(forced);
        }

        /// <summary>
        /// Call this method to create and display the main shell form.  Typically,
        /// this method would be called by the startup form when it is ready to display
        /// the main shell form.
        /// </summary>
        public void Load()
        {
            ILicenseManager lm = this.OnCreateLicenseManager();
            if (lm != null)
            {
                this.Add(lm, "LicenseManager");
                this.OnApplyLicensing(lm.GetLicenses());
            }

            IInstaller lci = this.OnCreateLicensedComponentInstaller();
            if (lci != null)
            {
                this.Add(lci, "LicensedAppInstaller");
            }

            this.OnLoad();
        }
        /// <summary>
        /// Retrieves a registered component by its type
        /// </summary>
        /// <param name="componentType"></param>
        /// <returns></returns>
        public IComponent GetComponent(Type componentType)
        {
            return GetComponent(componentType.AssemblyQualifiedName);
        }
        /// <summary>
        /// Retrieves a registered component by its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IComponent GetComponent(string name)
        {
            return this.OnGetComponent(name);
        }

        /// <summary>
        /// Retrieves a registered component by its type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetComponent<T>() where T : IComponent
        {
            return (T)GetComponent(typeof(T));
        }

        /// <summary>
        /// Returns all components that are registered for the given type T.
        /// This most useful when a system may contain a collections of service components
        /// implementing a common interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T[] GetComponents<T>()// where T : IComponent
        {
            T[] list = this.OnGetComponents<T>(typeof(T).AssemblyQualifiedName);
            return list;
        }

        /// <summary>
        /// Retrieves a registered component by its name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetComponent<T>(string name) where T : IComponent
        {
            return (T)GetComponent(name);
        }

        /// <summary>
        /// Gets the name of a component instance
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public string GetName(IComponent component)
        {
            return this.OnGetName(component);
        }

        
        #endregion Public

        #region Private
        #endregion Private

        #region Protected
        protected virtual ILicenseManager OnCreateLicenseManager() { return null; }

        protected virtual IInstaller OnCreateLicensedComponentInstaller() { return null; }

        protected virtual void OnApplyLicensing(ILicense[] licenses) { }

        protected virtual bool OnExit(bool forced)
        {
            bool ret = this.OnUnload(forced);
            if (MainForm != null)
            {
                MainForm.Close();
            }
            if (this is ServiceContainer
                && ((ServiceContainer)this).RunAsService)
            {
            }
            else
            {
                this.ExitThread();
            }
            return ret;
        }

        /// <summary>
        /// Unloads all loaded modules and closes the MainForm by default.
        /// If there is not startup form, then this will also terminate the application.
        /// Iff a startup form is defined, it will be displayed if the unload completed successfully.
        /// </summary>
        /// <param name="forced"></param>
        /// <returns></returns>
        protected virtual bool OnUnload(bool forced)
        {
            bool ret = true;

            if (!_mainFormClosing && MainForm != null)
            {
                MainForm.Close();
                MainForm = null;
            }
            OnRemoveComponents();
            
            return ret;
        }

        /// <summary>
        /// Removes and Disposes all loaded components from the container
        /// </summary>
        protected virtual void OnRemoveComponents()
        {
            foreach (IComponent c in this.Components)
            {
                OnRemoveComponent(c);
            }

            this._components.Clear();
            Logger.LogInfo("All Components Removed");
        }

        /// <summary>
        /// Derived classes should override this method to provide their own custom logic to display the
        /// main shell form and then load service components.
        /// </summary>
        protected virtual void OnLoad()
        {
            this.OnLoadMainForm();
            this._loader.LoadComponents();
        }

        /// <summary>
        /// Derived classes should override this to display their own MainForm.
        /// </summary>
        protected virtual void OnLoadMainForm()
        {
            MainForm = new Form();
            MainForm.Text = "Process Shell";
            MainForm.WindowState = FormWindowState.Minimized;
            MainForm.ShowInTaskbar = false;
            MainForm.Show();
        }

        /// <summary>
        /// This method is invoked when the module loader's load operation is complete.
        /// </summary>
        protected virtual void OnLoadComplete()
        {
        }

        /// <summary>
        /// Brokers the module loader progress message to all status adapters on the shell form
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnLoadStatus(ComponentLoadStatusEventArgs e)
        {
            this.Add(e.Component, e.Name);
        }
        /// <summary>
        /// Derived classes should override to provide their own custom startup logic.  The default
        /// logic will create and display the StartupForm for the shell.  If the startup form type is
        /// the same as the shell form type, then this method will also get the regions from the startup
        /// form, as well as load its commands.  This method also initializes the module loader for the shell.
        /// </summary>
        /// <param name="shellAttribute"></param>
        /// <param name="startupArgs"></param>
        protected virtual void OnInitialize(CompositionContainerAttribute shellAttribute, params string[] startupArgs)
        {
            System.Windows.Forms.Application.ApplicationExit += new EventHandler(OnApplicationExit);
            this.StartupArgs = startupArgs;
        }


        /// <summary>
        /// This method is called when the application is exiting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnApplicationExit(object sender, EventArgs e)
        {
            // do cleanup on exit
            OnRemoveComponents();
        }

        protected virtual void OnAddComponent(IComponent component, string name)
        {
            lock (_resolved)
            {
                _resolved.Clear();
            }

            name = name.ToLowerInvariant();
            lock (ComponentDictionary)
            {
                ComponentDictionary.Add(name, component);
            }
            if (component is IInitialize && !((IInitialize)component).IsInitialized)
            {
                ((IInitialize)component).Initialize(name, StartupArgs);
            }
            if (component is IInstaller && !((IInstaller)component).IsInstalled)
            {
                InstallerComponentAttribute attrib = component.GetType().GetCustomAttribute<InstallerComponentAttribute>();
                string[] appNames = null;
                if (attrib != null)
                    appNames = attrib.Apps;
                ((IInstaller)component).Install(appNames);
            }
            this.OnHandleComponentAttributes(component, name);
            this.OnComponentChanged(CompositionContainerComponentChange.Add, component, name);
        }

        protected virtual void OnHandleComponentAttributes(IComponent component, string name){}

       

        protected virtual void OnComponentChanged(CompositionContainerComponentChange change,
            IComponent component, string name)
        {
            if (this.ComponentChanged != null)
            {
                CompositionContainerComponentEventArgs e = new CompositionContainerComponentEventArgs(
                    change, component, name);
                this.ComponentChanged(this, e);
            }
        }

        //protected virtual void OnGetPublications(IComponent component, string name)
        //{
        //    IList<EventInfo> ev = component.GetType().GetEvents(
        //        BindingFlags.Instance
        //        | BindingFlags.Static
        //        | BindingFlags.Public
        //        | BindingFlags.NonPublic).Where(
        //        e => e.GetCustomAttributes(
        //            typeof(PublicationAttribute), true).Count() > 0).ToList();

        //    foreach (EventInfo info in ev)
        //    {
        //        PublicationAttribute attrib = (PublicationAttribute)info.GetCustomAttributes(
        //            typeof(PublicationAttribute), true)[0];

        //        IComponent publication = null;
        //        if (!this.ComponentDictionary.ContainsKey(attrib.Name))
        //        {
        //            // add a new publication
        //            publication = OnCreatePublication(component, attrib, info);
        //            // register it
        //            this.Add(publication, attrib.Name);
        //        }
        //        publication = this.GetComponent(attrib.Name);
        //        // register the publisher
        //        this.OnRegisterPublisher(component, publication, info);
        //    }
        //}

        //protected virtual void OnGetSubscriptions(IComponent component, string name)
        //{
        //    IList<MethodInfo> ev = component.GetType().GetMethods(
        //        BindingFlags.Instance
        //        | BindingFlags.Static
        //        | BindingFlags.Public
        //        | BindingFlags.NonPublic).Where(
        //        e => e.GetCustomAttributes(
        //            typeof(SubscriptionAttribute), true).Count() > 0).ToList();

        //    foreach (MethodInfo info in ev)
        //    {
        //        SubscriptionAttribute attrib = (SubscriptionAttribute)info.GetCustomAttributes(
        //            typeof(SubscriptionAttribute), true)[0];

        //        IComponent publication = null;
        //        if (!this.ComponentDictionary.ContainsKey(attrib.Name))
        //        {
        //            // add a new publication
        //            publication = OnCreatePublication(attrib, info);
        //            // register it
        //            if (!this.ComponentDictionary.ContainsKey(attrib.Name))
        //                this.Add(publication, attrib.Name);
        //        }
        //        publication = this.GetComponent(attrib.Name);
        //        // register the subscriber
        //        this.OnRegisterSubscriber(component, info, attrib, publication);
        //    }
        //}

        //protected virtual IComponent OnCreatePublication(IComponent publisher, PublicationAttribute attrib, EventInfo eventInfo)
        //{
        //    Type t = eventInfo.EventHandlerType.GetGenericArguments()[0];
        //    Type genericPublication = typeof(Publication<>);
        //    Type specificPublication = genericPublication.MakeGenericType(t);
        //    IComponent publication = (IComponent)Activator.CreateInstance(specificPublication, attrib.Name);

        //    return publication;
        //}

        //protected virtual IComponent OnCreatePublication(SubscriptionAttribute attrib, MethodInfo methodInfo)
        //{
        //    Type genericPublication = typeof(Publication<>);
        //    Type specificPublication = genericPublication.MakeGenericType(
        //        methodInfo.GetParameters()[1].ParameterType.GetGenericArguments()[0]);
        //    IComponent publication = (IComponent)Activator.CreateInstance(specificPublication, attrib.Name);
        //    return publication;
        //}

        //protected virtual void OnRegisterPublisher(IComponent publisher, IComponent publication, EventInfo eventInfo)
        //{
        //    Type t = eventInfo.EventHandlerType.GetGenericArguments()[0];
        //    publication.GetType().GetMethod("RegisterPublisher").Invoke(
        //        publication, new object[] { new Publisher(publisher) });
        //    // attach Listener delegate to source
        //    // create a subscription handler type
        //    Type genericDelegate = typeof(PublicationHandler<>);
        //    Type specificDelegate = genericDelegate.MakeGenericType(t);

        //    // create the delegate and associate with the subscriber
        //    MethodInfo method = publication.GetType().GetMethod("Listener", BindingFlags.NonPublic | BindingFlags.Instance);
        //    Delegate del = (Delegate)Activator.CreateInstance(specificDelegate, publication, method.MethodHandle.GetFunctionPointer());
        //    eventInfo.AddEventHandler(publisher, del);
        //}

        //protected virtual void OnRegisterSubscriber(IComponent subscriberInstance,
        //    MethodInfo info, SubscriptionAttribute attrib, IComponent publication)
        //{
        //    //Subscription<T> ctor:
        //    //  Publication<T>,
        //    //  Subscriber,
        //    //  ISubscriptionInvoke<T>,
        //    //  ITopicFormatter<T>,

        //    //Subscription<T> ctor:
        //    //  string name,
        //    //  object subscriber,
        //    //  ISubscriptionInvoke<T>,
        //    //  ITopicFormatter<T>,



        //    object subscriptionHandler;
        //    object subscription;

        //    // get the topic data type
        //    Type t = info.GetParameters()[1].ParameterType.GetGenericArguments()[0];

        //    // create a Subscriber
        //    Subscriber subscriber = new Subscriber(subscriberInstance);

        //    // create a subscription handler type
        //    Type genericDelegate = typeof(SubscriptionHandler<>);
        //    Type specificDelegate = genericDelegate.MakeGenericType(t);

        //    // create the delegate and associate with the subscriber
        //    subscriptionHandler = Activator.CreateInstance(specificDelegate,
        //        subscriberInstance, info.MethodHandle.GetFunctionPointer());

        //    // create the Subscription instance
        //    Type genericSubscription = typeof(Subscription<>);
        //    Type specificSubscription = genericSubscription.MakeGenericType(t);

        //    // create the ISubscriptionInvoke instance
        //    Type genericInvoke = attrib.SubscriptionInvokerType;
        //    Type specificInvoke = genericInvoke.MakeGenericType(t);
        //    object invokeInstance = Activator.CreateInstance(specificInvoke, subscriptionHandler);

        //    // create the ITopicFormatter instance
        //    Type genericFormatter = attrib.FormatterType;
        //    Type specificFormatter = genericFormatter.MakeGenericType(t);
        //    object formatterInstance = Activator.CreateInstance(specificFormatter);

        //    // create the intraprocess subscription
        //    subscription = Activator.CreateInstance(specificSubscription,
        //        publication, subscriber, invokeInstance, formatterInstance);

        //    // register the subscription with the publisher
        //    MethodInfo method = publication.GetType().GetMethod("RegisterSubscriber");
        //    method.Invoke(publication, new object[] { subscription });

        //}

        protected virtual void OnRemoveComponent(IComponent component)
        {
            lock (_components)
            {
                lock (_resolved)
                {
                    _resolved.Clear();
                }
                try
                {
                    if (component is IDisposable)
                    {
                        ((IDisposable)component).Dispose();
                        Logger.LogInfo("Disposed: " + component.GetType().Name);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An unhandled error occurred disposing component " + component.GetType().FullName);
                }

                Dictionary<string, IComponent>.Enumerator e = _components.GetEnumerator();
                string key = string.Empty;
                while (e.MoveNext())
                {
                    if (e.Current.Equals(component))
                    {
                        key = e.Current.Key;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(key))
                {
                    this._components.Remove(key);
                    this.OnComponentChanged(CompositionContainerComponentChange.Delete, component, key);
                }
            }
        }

        protected virtual void OnRemoveComponent(string name)
        {
            lock (_components)
            {
                name = name.ToLowerInvariant();
                if (_components.ContainsKey(name))
                {
                    IComponent component = _components[name];
                    try
                    {
                        if (component is IDisposable)
                        {
                            ((IDisposable)component).Dispose();
                            Logger.LogInfo("Disposed: " + component.GetType().Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "An unhandled error occurred disposing component " + component.GetType().FullName);
                    }

                    this._components.Remove(name);
                    this.OnComponentChanged(CompositionContainerComponentChange.Delete, component, name);
                }
            }
        }

        protected virtual string OnGetName(IComponent component)
        {
            lock (_components)
            {
                return _components.Where(kvp => kvp.Value.Equals(component)).FirstOrDefault().Key;
            }
        }

        protected virtual T[] OnGetComponents<T>(string name)// where T : IComponent
        {
            lock (_components)
            {
                name = name.ToLowerInvariant();
                List<T> components = new List<T>();
                T[] retList = null;

                T component;
                if (_components.ContainsKey(name))
                {
                    component = (T)_components[name];
                    components.Add(component);
                }
                else
                {
                    if (_resolved.ContainsKey(name))
                        return _resolved[name].OfType<T>().ToArray();

                    Type t = TypeHelper.GetType(name);
                    if (t != null)
                    {
                        foreach (KeyValuePair<string, IComponent> kvp in _components)
                        {
                            Type vType = kvp.Value.GetType();
                            if (vType == t
                                || vType.IsSubclassOf(t)
                                || t.IsInstanceOfType(kvp.Value)
                                || (t.IsInterface && vType.GetInterface(name) != null))
                            {
                                component = (T)kvp.Value;
                                components.Add(component);
                            }
                        }
                    }
                }
                retList = components.ToArray();
                if (components.Count > 0)
                {
                    lock (_resolved)
                    {
                        if (!_resolved.ContainsKey(name))
                            _resolved.Add(name, retList.OfType<IComponent>().ToArray());
                    }
                }
                return retList;
            }
        }

        protected virtual IComponent OnGetComponent(string name)
        {
            lock (_components)
            {
                name = name.ToLowerInvariant();
                IComponent component = null;
                if (_components.ContainsKey(name))
                {
                    component = _components[name];
                }
                else
                {
                    if (_resolved.ContainsKey(name))
                        return _resolved[name][0];

                    List<IComponent> components = new List<IComponent>();
                    Type t = TypeHelper.GetType(name);
                    if (t != null)
                    {
                        foreach (KeyValuePair<string, IComponent> kvp in _components)
                        {
                            Type vType = kvp.Value.GetType();
                            if (vType == t
                                || vType.IsSubclassOf(t)
                                || t.IsInstanceOfType(kvp.Value)
                                || (t.IsInterface && vType.GetInterface(name) != null))
                            {
                                component = kvp.Value;
                                components.Add(component);
                            }
                        }
                    }

                    if (components.Count > 0)
                    {
                        lock (_resolved)
                        {
                            if (!_resolved.ContainsKey(name))
                                _resolved.Add(name, components.ToArray());
                            component = components[0];
                        }
                    }
                }

                return component;
            }
        }

        protected virtual IComponent OnCreateComponent(string name)
        {
            IComponent component = TypeHelper.CreateType(name, new object[] { }) as IComponent;
            if (component != null && component is IInitialize)
            {
                ((IInitialize)component).Initialize(name, StartupArgs);
            }
            return component;
        }
        #endregion Protected
        #endregion Methods

        #region Event Handlers and Callbacks
        #region Public
        #endregion Public

        #region Private
        #endregion Private

        #region Protected
        #endregion Protected
        #endregion Event Handlers and Callbacks



        #region IContainer Members
        /// <summary>
        /// Clients can call this method to register a run-time component with the shell
        /// by name.  With this method, the shell can contain many instances of the
        /// same IComponent type but with different names.  Each named instance
        /// will be treated as a singleton, and any successive attempts to register another
        /// component with the same name will fail.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="name"></param>
        public void Add(IComponent component, string name)
        {
            if (_components.ContainsKey(name))
            {
                throw (new InvalidOperationException("A component with the same name already exists in the collection."));
            }
            OnAddComponent(component, name);
        }
        /// <summary>
        /// Clients can call this method to register a run-time component with the shell
        /// by type.  With this method, the shell can contain only one instance of the
        /// same IComponent type.  Each registered IComponent instance
        /// will be treated as a singleton, and any successive attempts to register another
        /// component with the same name will fail.
        /// </summary>
        /// <param name="component"></param>
        public void Add(IComponent component)
        {
            Add(component, component.GetType().AssemblyQualifiedName);
        }

        /// <summary>
        /// Returns a ComponentCollection of the currently registered components in the shell
        /// </summary>
        public ComponentCollection Components
        {
            get { return new ComponentCollection(_components.Values.ToArray()); }
        }
        /// <summary>
        /// Removes the provided comoponent, if it exists in the registry.
        /// </summary>
        /// <param name="component"></param>
        public void Remove(IComponent component)
        {
            this.OnRemoveComponent(component);
        }
        /// <summary>
        /// Removes a named instance of a component
        /// </summary>
        /// <param name="name"></param>
        public void Remove(string name)
        {
            OnRemoveComponent(name);
        }


        /// <summary>
        /// Removes a an unnamed instance of a component by the component type
        /// </summary>
        /// <param name="componentType"></param>
        public void Remove(Type componentType)
        {
            Remove(componentType.Name);
        }

        #endregion

        
    }
}

