using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Altus.Core;
using Altus.Core.Licensing;
using Altus.Core.Component;

namespace Altus.Core
{
    public enum InstanceType : int
    {
        WindowsService = 1,
        WindowsFormsClient = 2,
        WebClient = 4,
        MobileClient = 8,
        WebServer = 16,
        WPFClient = 32,
        ASPNetHost = 64,
        Other = 100
    }

    public enum IdentityType : int
    {
        Unknown = 0,
        ServiceProcess = 1,
        UserProcess = 2
    }

    public partial class Context
    {
        #region Fields
        protected InstanceType _ctx;
        protected IdentityType _idt;
        protected string _path;
        protected string _codeBase;
        private double _timeZoneOffset = 0d;
        #endregion

        #region Constructors
        #region Public
        [Obsolete("Specify the ExecutionContext desired, this ctor is used for serialization only.", true)]
        public Context() : this(InstanceType.WindowsFormsClient, IdentityType.ServiceProcess) { }
        public Context(InstanceType executionContext, IdentityType identityType)
        {
            this._ctx = executionContext;
            this._idt = identityType;

            System.TimeZone tz = System.TimeZone.CurrentTimeZone;
            _timeZoneOffset = tz.GetUtcOffset(CurrentTime.Now).TotalMinutes;

            
        }
        #endregion Public
        #endregion  Constructors

        private static Context _gCtx = null;
        //========================================================================================================//
        /// <summary>
        /// Gets/sets the context that can be used in a thread-neutral fashion
        /// </summary>
        public static Context GlobalContext 
        { 
            get
            {
                if (_gCtx == null && Context.CurrentContext != null)
                    _gCtx = Context.CurrentContext;
                return _gCtx;
            }
            set
            {
                _gCtx = value;
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Gets the context under which all code accessing this class instance will run (Desktop, Server, etc)
        /// </summary>
        public InstanceType InstanceType
        {
            get { return this._ctx; }
            set { this._ctx = value; }
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Gets the context under which all code accessing this class instance will run (Desktop, Server, etc)
        /// </summary>
        public IdentityType IdentityType
        {
            get { return this._idt; }
            set { this._idt = value; }
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Returns the executing path of the current app
        /// </summary>
        public string Location
        {
            get
            {
                return CurrentApp.Location;
            }
        }
        //========================================================================================================//


        [ThreadStatic]
        private DeclaredApp _currentApp = null;
        //========================================================================================================//
        /// <summary>
        /// Gets/sets the current app for the current context.  If no explicit app has been set, the default Core 
        /// app will be set by default
        /// </summary>
        public DeclaredApp CurrentApp
        {
            get
            {
                if (_currentApp == null && App.Instance != null)
                    return App.Instance["Core"];
                else return _currentApp;
            }
            set
            {
                _currentApp = value;
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Returns the path of the loaded entry assembly in the current application domain.  This may or may not
        /// equal the CurrentPath value.
        /// </summary>
        public string CodeBase
        {
            get
            {
                if (CurrentApp == null || CurrentApp.CodeBase == null)
                {
                    string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Replace(@"file:///", ""));
                    string codeBase = AppDomain.CurrentDomain.GetData("CodeBase") as string;
                    if (string.IsNullOrEmpty(codeBase))
                    {
                        codeBase = path;
                    }
                    return codeBase;
                }
                else return CurrentApp.CodeBase;
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Gets the timezone of the current caller's context in number of hours offset from GMT
        /// </summary>
        public double UtcOffsetMinutes
        {
            get
            {

                System.TimeZone tz = System.TimeZone.CurrentTimeZone;
                _timeZoneOffset = tz.GetUtcOffset(CurrentTime.Now).TotalMinutes;

                return this._timeZoneOffset;
            }
            set { this._timeZoneOffset = value; }
        }
        //========================================================================================================//
    }
}
