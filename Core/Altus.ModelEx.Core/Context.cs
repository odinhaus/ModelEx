using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using System.ComponentModel;
using Altus.Core.Caching;
using System.Reflection;
using System.Threading;
using Altus.Core.Exceptions;
using Altus.Core.Serialization;
using Altus.Core.Licensing;
using Altus.Core.Component;

namespace Altus.Core
{
    public delegate void EnvironmentVariableChangedHandler(object sender, PropertyChangedEventArgs e);
    public delegate void PrincipalChangedHandler(object sender, PrincipalChangedEventArgs e);
    public class PrincipalChangedEventArgs : EventArgs
    {
        public PrincipalChangedEventArgs(IPrincipal old, IPrincipal newP) { PreviousPrincipal = old; NewPrincipal = newP; }
        public IPrincipal PreviousPrincipal { get; private set; }
        public IPrincipal NewPrincipal { get; private set; }
    }

    [System.Serializable()]
    public partial class Context
    {
        #region Fields
        #region Static Fields
        [NonSerialized()]
        static Dictionary<string, object> _env = new Dictionary<string, object>();
        #endregion Static Fields

        #region Instance Fields
        protected IPrincipal _principal;
        protected string _client;
        protected string _domain;

        [NonSerialized()]
        protected static ICache _cache;
        protected Exception _lastError;
        protected string _lastServiceCall = "";
        protected byte[] _lastServiceBytes = null;
        protected string _clientKey;
        private string _productKey;

        [NonSerialized()]
        private string _instanceId = Guid.NewGuid().ToString();


        #endregion Instance Fields
        #endregion Fields

        #region Event Declarations
        public static event EnvironmentVariableChangedHandler EnvironmentVariableChanged;
        public static event PrincipalChangedHandler PrincipalChanged;
        #endregion Event Declarations

        #region Properties


        #region Public

        public bool HasCustomConfig { get; set; }

        string _config;
        public string ConfigurationFile
        {
            get
            {
                if (!HasCustomConfig)
                    return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                else
                    return _config;
            }
            set
            {
                _config = value;
                HasCustomConfig = true;
            }
        }

        //========================================================================================================//
        /// <summary>
        /// Gets a unique instance identifier for this context copy
        /// </summary>
        public string InstanceId
        {
            get { return _instanceId; }
        }
        //========================================================================================================//


        [ThreadStatic]
        static Context _current = null;
        //========================================================================================================//
        /// <summary>
        /// Gets/sets the current context for the process.  This operation has affinity to the specific
        /// thread which is making the Get/Set call.  It does not set the context globally throughout
        /// the executing windows process.
        /// </summary>
        public static Context CurrentContext
        {
            get
            {
                return _current;
            }
            set
            {
                _current = value;
                if (GlobalContext == null)
                {
                    GlobalContext = value.Copy();
                }
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Gets/sets the current IIdentity of the process
        /// </summary>
        public IIdentity User
        {
            get
            {
                if (this._principal != null)
                    return this._principal.Identity;
                return null;
            }
            set
            {
            }
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Gets/sets the IPrincipal of the process
        /// </summary>
        public IPrincipal Principal
        {
            get { return this._principal; }
            set
            {
                PrincipalChangedEventArgs e = new PrincipalChangedEventArgs(this._principal, value);
                this._principal = value;
                if (PrincipalChanged != null)
                {
                    PrincipalChanged(this, e);
                }
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Gets the caching strategy that can be used to cache objects within this process, or across processes
        /// </summary>
        public static ICache Cache
        {
            get
            {
                if (_cache == null)
                {
                    _cache = new WinCache();//WebCache(this);            
                }
                return _cache;
            }
        }
        //========================================================================================================//
        
       

        //========================================================================================================//
        /// <summary>
        /// Gets the last exception set for the current context
        /// </summary>
        public Exception LastException
        {
            get { return this._lastError; }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Gets the binary representation (if any) of the last contextual service call
        /// </summary>
        public byte[] LastServiceBytes
        {
            get { return this._lastServiceBytes; }
            set { this._lastServiceBytes = value; }
        }
        //========================================================================================================//


        #endregion Public

        #region Private
        #endregion Private

        #region Protected
        #endregion Protected
        #endregion Properties

        #region Methods
        #region Public
        //========================================================================================================//
        /// <summary>
        /// Use this method to query an environment variable value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object GetEnvironmentVariable(string key)
        {
            if (_env.ContainsKey(key))
                return _env[key];
            else
                return null;
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        ///  Use this method to query an environment variable value and return a default value if the desired value
        ///  is not defined.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetEnvironmentVariable<T>(string key, T defaultValue)
        {
            try
            {
                if (_env.ContainsKey(key))
                    return (T)_env[key];
                else
                    return defaultValue;
            }
            catch (InvalidCastException)
            {
                try
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                    return (T)converter.ConvertFrom(_env[key]);
                }
                catch { return defaultValue; }
            }
            catch
            {
                return defaultValue;
            }
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Use this query to set an environment variable value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetEnvironmentVariable(string key, object value)
        {
            if (_env.ContainsKey(key))
                _env[key] = value;
            else
                _env.Add(key, value);

            if (EnvironmentVariableChanged != null)
            {
                EnvironmentVariableChanged(typeof(Context), new PropertyChangedEventArgs(key));
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Sets the last known exception for the current context
        /// </summary>
        /// <param name="code"></param>
        public void SetLastException(ExceptionCode code)
        {
            this._lastError = ExceptionHelper.CreateException(code);
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Sets the last known exception for the current context
        /// </summary>
        /// <param name="exception"></param>
        public void SetLastException(Exception exception)
        {
            this._lastError = exception;

        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Copies the current instance and optionally copies the principal as well
        /// </summary>
        /// <param name="includePrincipal"></param>
        /// <returns></returns>
        public Context Copy(bool includePrincipal)
        {
            Context copy = this.Copy() as Context;
            if (!includePrincipal)
            {
                copy.Principal = null;
            }
            return copy;
        }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Makes a binary copy of this context instance
        /// </summary>
        /// <returns></returns>
        public Context Copy()
        {
            return SerializationHelper.FromBinary<Context>(SerializationHelper.ToBinary(this, false), false);
        }
        //========================================================================================================//

        #endregion Public

        #region Private
        #endregion Private

        #region Protected
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
    }
}
