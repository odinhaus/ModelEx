using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Dynamic;
using Altus.Core.Data;
using Altus.Core.PubSub;
using Altus.Core.PubSub.Dynamic;
using System.Reflection;
using Altus.Core.Dynamic;
using System.Windows;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Altus.Core.Diagnostics;
using System.Collections.ObjectModel;
using Altus.Core.Component;
using Altus.Core.Licensing;
using Altus.Core.Realtime;
using Altus.Core.Collections;

namespace Altus.Core.Presentation.ViewModels
{

    public class View : Extendable<View>, IComponent, IInitialize, ILicensedComponent
    {
        public const string WPF = "WPF";
        public const string WinForms = "WinForms";

        public event DynamicSubscriptionHandler TopicUpdated;

        protected View() : base() { this.SupportsTopics = OnSupportsTopics(); }

        
        protected View(string windowName, string instanceName, string viewType, object backingInstance, DeclaredApp app)
            : base(instanceName, backingInstance)
        {
            this.WindowName = windowName;
            this.ViewType = viewType;
            this.MainStatusCallback = OnCreateMainStatusCallback();
            this.SecondaryStatusCallback = OnCreateSecondaryStatusCallback();
            this.ProgressStatusCallback = OnCreateProgressStatusCallback();
            this.IndeterminateProgressStatusCallback = OnCreateIndeterminateProgressStatusCallback();
            this.IsDirty = false;
            this.App = app;
            this.SupportsTopics = OnSupportsTopics();
        }

        protected override string OnGetInstanceType()
        {
            return "View";
        }

        public static View Create(string uiType, string windowName, string instanceName, string viewType, object backingInstance, DeclaredApp app)
        {
            switch (uiType)
            {
                default:
                case WPF:
                    {
                        return new WPFView(windowName, instanceName, viewType, backingInstance, app);
                    }
                case WinForms:
                    {
                        return new WinView(windowName, instanceName, viewType, backingInstance, app);
                    }
            }
        }
        
        protected virtual UpdateTextStatusHandler OnCreateMainStatusCallback()
        {
            return new UpdateTextStatusHandler(delegate(string message) { Console.WriteLine(message); });
        }
        protected virtual UpdateTextStatusHandler OnCreateSecondaryStatusCallback()
        {
            return new UpdateTextStatusHandler(delegate(string message) { Console.WriteLine(message); });
        }
        protected virtual UpdateProgressStatusHandler OnCreateProgressStatusCallback()
        {
            return new UpdateProgressStatusHandler(delegate(int min, int max, int value) { Console.WriteLine(min + ", " + max + ": " + value); });
        }
        protected virtual UpdateIndeterminateProgressStatusHandler OnCreateIndeterminateProgressStatusCallback()
        {
            return new UpdateIndeterminateProgressStatusHandler(delegate(bool isOn) { Console.WriteLine(isOn); });
        }

        protected override IEnumerable<string> OnGetAliases()
        {
            return DataContext.Default.GetViewAliases(this);
        }

        protected override IEnumerable<DynamicFunction<View>> OnGetFunctions()
        {
            return DataContext.Default.GetViewFunctions(this);
        }

        protected override IEnumerable<DynamicProperty<View>> OnGetProperties()
        {
            return DataContext.Default.GetViewProperties(this);
        }

        protected virtual IEnumerable<DynamicTopic> OnGetTopics()
        {
            
            return  DataContext.Default.Select<DynamicTopic>(new { view = this });
        }

        private Dictionary<ulong, DynamicField> _myFieldsIdx = new Dictionary<ulong, DynamicField>();

        protected override void OnExtend()
        {
            if (!_isExtending
                && this.IsExtendable
                && !_extensionsLoaded.Contains(Context.CurrentContext.CurrentApp))
            {
                _isExtending = true;
                this.SetTopics();
                base.OnExtend();
                _isExtending = false;
            }
        }

        private void SetTopics()
        {
            if (SupportsTopics)
                this.OnLoadTopics();
        }

        protected virtual void OnLoadTopics()
        {   
            if (_myFieldsIdx == null)
                _myFieldsIdx = new Dictionary<ulong, DynamicField>();
            
            _topics.SuppressNotifications();
            _topics.AddRange(this.OnGetTopics());
            _topics.Refresh();

            foreach (DynamicTopic topic in _topics)
            {
                foreach (DynamicField df in topic.Fields)
                {
                    if (!_myFieldsIdx.ContainsKey(df.FrameworkField.Id))
                    {
                        _myFieldsIdx.Add(df.FrameworkField.Id, df);
                        _fields.Add(df);
                    }
                }
                topic.Subscribe -= topic_Subscribe;
                topic.Subscribe += topic_Subscribe;
            }
            DynamicTopicManager.RegisterView(this);
        }

        void topic_Subscribe(object sender, DynamicSubscriptionHandlerEventArgs e)
        {
            OnTopicUpdated(sender, e);
        }

        protected virtual void OnTopicUpdated(object topic, DynamicSubscriptionHandlerEventArgs e)
        {
            if (TopicUpdated != null)
            {
                TopicUpdated(topic, e);
            }
            foreach (DynamicField f in e.Fields)
            {
                OnPropertyChanged(f.Name);
                OnPropertyChanged(f.TopicQualifiedName);
                foreach (string alias in f.Aliases)
                {
                    OnPropertyChanged(f.Topic.Name + "." + alias);
                }
            }
        }

        BitmapImage _icon;
        public BitmapImage Icon 
        { 
            get { return _icon; } 
            set { _icon = value; OnPropertyChanged("Icon"); OnPropertyChanged("HasIcon"); OnPropertyChanged("IconWidth"); OnPropertyChanged("IconHeight"); } 
        }

        string _currentSize;
        public string CurrentSize { get { return _currentSize; } set { _currentSize = value; OnPropertyChanged("CurrentSize"); } }

        public string WindowName { get; private set; }
        public string ViewType { get; private set; }
        public bool HasIcon { get { return Icon != null; } }
        public int IconWidth { get { return (Icon == null ? 0 : (int)Icon.PixelWidth); } }
        public int IconHeight { get { return (Icon == null ? 0 : (int)Icon.PixelHeight); } }

        public DeclaredApp App { get; private set; }

        private bool _isExtending = false;
        private SafeObservableCollection<DynamicField> _fields = new SafeObservableCollection<DynamicField>();
        public SafeObservableCollection<DynamicField> Fields
        {
            get
            {
                OnExtend();
                return _fields;
            }
        }
        private SafeObservableCollection<DynamicTopic> _topics = new SafeObservableCollection<DynamicTopic>();
        public SafeObservableCollection<DynamicTopic> Topics
        {
            get
            {
                OnExtend();
                return _topics;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            OnExtend();

            if (binder.Name.Equals("Application", StringComparison.InvariantCultureIgnoreCase))
            {
                result = Application.Instance;
                return true;
            }
            else
            {
                DynamicTopic topic;
                if (SupportsTopics && DynamicTopicManager.TryGetTopic(binder.Name, out topic))
                {
                    result = topic;
                    return true;
                }
                else
                {
                    return base.TryGetMember(binder, out result);
                }
            }
        }

        public bool SupportsTopics { get; private set; }
        protected virtual bool OnSupportsTopics()
        {
            return true;
        }

        #region ISupportsStatus
        public void RegisterMainStatus(UpdateTextStatusHandler callback)
        {
            this.MainStatusCallback += callback;
        }

        public void RegisterSecondaryStatus(UpdateTextStatusHandler callback)
        {
            this.SecondaryStatusCallback += callback;
        }

        public void RegisterProgressStatus(UpdateProgressStatusHandler callback)
        {
            this.ProgressStatusCallback += callback;
        }

        public void RegisterIndeterminateProgressStatus(UpdateIndeterminateProgressStatusHandler callback)
        {
            this.IndeterminateProgressStatusCallback += callback;
        }

        public UpdateTextStatusHandler MainStatusCallback { get; private set; }
        public UpdateTextStatusHandler SecondaryStatusCallback { get; private set; }
        public UpdateProgressStatusHandler ProgressStatusCallback { get; private set; }
        public UpdateIndeterminateProgressStatusHandler IndeterminateProgressStatusCallback { get; private set; }
        #endregion

        #region ISupportsNavigate
        Stack<View> _viewBack = new Stack<View>();
        Stack<View> _viewForward = new Stack<View>();
        public void RegisterNavigated(NavigatedHandler callback)
        {
            this.NavigatedHandler = callback;
        }

        public void NavigateTo(string viewName)
        {
            
        }

        public void NavigateBack()
        {
           
            }

        public void NavigateForward()
        {

        }

        protected NavigatedHandler NavigatedHandler { get; private set; }

        protected virtual void OnNavigateTo(View view)
        {

        }
        #endregion

        #region ISupportsDirty
        public bool IsDirty
        {
            get;
            protected set;
        }

        public void HandleDirty(HandleDirtyArgs e)
        {
            this.OnHandleDirty(e);
        }

        protected virtual void OnHandleDirty(HandleDirtyArgs e)
        { }
        #endregion

        #region IComponent
        protected bool _disposed = false;
        public event EventHandler Disposed;

        ~View()
        {
            Dispose(false);
        }

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

        protected void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                this._disposed = true;
                if (disposing)
                {
                    this.OnDispose();
                    if (Disposed != null)
                        Disposed(this, new EventArgs());
                }
            }
        }

        protected virtual void OnDispose()
        {
            if (this.BackingInstance is IDisposable)
            {
                ((IDisposable)this.BackingInstance).Dispose();
            }
            if (this.Topics.Count > 0)
            {
                foreach (DynamicTopic topic in Topics)
                {
                    topic.Subscribe -= topic_Subscribe;
                }
                this.Topics.Clear();
            }
            this._viewBack.Clear();
            this._viewForward.Clear();
        }

        public ISite Site
        {
            get;
            set;
        }

        
        #endregion

        #region IInitialize
        public virtual void Initialize(string name, params string[] args)
        {
            this.IsInitialized = OnInitialize();
        }

        public bool IsInitialized
        {
            get;
            protected set;
        }

        public bool IsEnabled
        {
            get;
            protected set;
        }
        #endregion

        public override bool Equals(object obj)
        {
            View value = obj as View;
            if (value == null) return false;
            return value.WindowName.Equals(this.WindowName, StringComparison.InvariantCultureIgnoreCase)
                && value.ViewType.Equals(this.ViewType, StringComparison.InvariantCultureIgnoreCase)
                && value.Name.Equals(this.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        #region ILicensedComponent
        public void ApplyLicensing(ILicense[] licenses, params string[] args)
        {
            OnApplyLicensing(licenses, args);
        }

        protected virtual void OnApplyLicensing(ILicense[] licenses, string[] args)
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
        #endregion
    }
}


