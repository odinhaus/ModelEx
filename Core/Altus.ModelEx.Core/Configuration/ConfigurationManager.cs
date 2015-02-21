using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Configuration;

namespace Altus.Core.Configuration
{
    public static class ConfigurationManager
    {
        #region Fields
        #region Static Fields
        static string _currentConfig = string.Empty;
        static string _root = string.Empty;
        #endregion Static Fields

        #region Instance Fields
        #endregion Instance Fields
        #endregion Fields

        #region Event Declarations
        #endregion Event Declarations

        #region Constructors
        #region Public
        #endregion Public

        #region Private
        static ConfigurationManager()
        {
            switch (Context.GlobalContext.InstanceType)
            {
                case InstanceType.WebServer:
                    {
                        if (System.Web.HttpContext.Current == null)
                        {
                            _root = Context.GlobalContext.CodeBase;
                        }
                        else
                        {
                            _root = System.Web.HttpContext.Current.Server.MapPath("~");
                        }
                        break;
                    }
                case InstanceType.ASPNetHost:
                case InstanceType.WindowsService:
                case InstanceType.WindowsFormsClient:
                case InstanceType.WPFClient:
                    {
                        _root = Context.GlobalContext.CodeBase;
                        break;
                    }
                default:
                    {
                        throw (new InvalidOperationException("Configuration manager only supports web server, windows server/service, and windows desktop applications"));
                    }
            }
        }
        #endregion Private

        #region Protected
        #endregion Protected
        #endregion  Constructors

        #region Properties
        #region Public

        /// <summary>
        /// Gets the root literal path where the configuration file exists.
        /// </summary>
        public static string ConfigurationRootPath
        {
            get { return _root; }
        }

        [ThreadStatic]
        static string _current;
        /// <summary>
        /// Gets the path to the Current Config file that will be used to service the current
        /// execution context.  This value may change on a call-by-call basis, depending
        /// upon the combination of the CustomConfig value and the Context.GlobalContext.InstanceType
        /// value.
        /// </summary>
        public static string CurrentConfig
        {
            get
            {
                return _current;
            }
            set
            {
                _current = value;
            }
        }
        #endregion Public
        #endregion Properties

        #region Methods
        #region Public
        //========================================================================================================//
        /// <summary>
        /// Gets a section of the application configuration file
        /// </summary>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public static ConfigurationSection GetSection(string sectionName, Context context)
        {
            Context prev = Context.GlobalContext;
            try
            {
                Context.GlobalContext = context;
                CurrentConfig = context.ConfigurationFile;
                System.Configuration.Configuration config = null;
                config = Context.Cache["Configuration" + sectionName] as System.Configuration.Configuration;
                if (config == null)
                {
                    switch (context.InstanceType)
                    {
                        case InstanceType.ASPNetHost:
                            {
                                config = System.Configuration.ConfigurationManager.OpenExeConfiguration(Path.GetFileNameWithoutExtension(CurrentConfig));
                                break;
                            }
                        case InstanceType.WindowsFormsClient:
                        case InstanceType.WPFClient:
                        case InstanceType.WindowsService:
                            config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                            break;
                        case InstanceType.WebServer:
                            if (Altus.Core.Component.App.Instance != null
                                && Altus.Core.Component.App.Instance.API != null
                                && Altus.Core.Component.App.Instance.API.IsChild
                                && Altus.Core.Component.App.Instance.API.IsHosted)
                            {
                                config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                            }
                            else
                            {
                                config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(@"\");
                            }
                            break;
                        default:
                            throw (new InvalidOperationException("Configuration manager only supports web server, windows server/service, and windows desktop applications"));
                    }
                    Context.Cache.Insert("Configuration" + sectionName, config);
                }

                return config.GetSection(sectionName);
            }
            finally
            {
                Context.GlobalContext = prev;
            }

        }
     

        //========================================================================================================//
        /// <summary>
        /// Gets a setting value from the "appSettings" section of the application's config file
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public static string GetAppSetting(string keyName, Context context)
        {
            return ReadAppSetting(keyName, context);
        }

        private static string ReadAppSetting(string keyName, Context context)
        {
            Context prev = Context.GlobalContext;
            try
            {
                Context.GlobalContext = context;
                string appSetting = Context.Cache.ContainsKey("ConfigurationAppSetting" + keyName) ?
                    Context.Cache["ConfigurationAppSetting" + keyName] as string
                    : null;

                if (appSetting == null)
                {
                    AppSettingsSection section = (AppSettingsSection)GetSection("appSettings", context);
                    Context.Cache.Insert("ConfigurationAppSetting" + keyName, section);
                    return section.Settings[keyName].Value;
                }
                else
                {
                    return appSetting;
                }
            }
            finally
            {
                Context.GlobalContext = prev;
            }
        }
        //========================================================================================================//


        //========================================================================================================//
        /// <summary>
        /// Gets a setting value from the "appSettings" section of the application's config file
        /// using the Context.GlobalContext for the current thread.
        /// 
        /// Use this method with great caution, as cross-thread context synchronization issues
        /// may cause unpredictable results.  This method is best used from the desktop environment only.
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public static string GetAppSetting(string keyName)
        {
            return GetAppSetting(keyName, Context.GlobalContext);
        }
        //========================================================================================================//
        /// <summary>
        /// Creates a parsed version of the Object connection string for providing detailed
        /// information to third party components.
        /// </summary>
        /// <param name="connectionStringKey">usually "ProductionLive"</param>
        /// <param name="context">The current user's context</param>
        /// <returns>A DBConnectionInfo structure containing the discrete connection properties.</returns>
        public static DBConnectionInfo GetDBConnectionInfo(string connectionStringKey, Context context)
        {
            string connectionString = ConfigurationManager.GetConnectionString(
                connectionStringKey, Context.GlobalContext);
            return new DBConnectionInfo(connectionString);
        }

        //========================================================================================================//
        /// <summary>
        /// Gets a connection string from the "connectionStrings" section of the application's config file
        /// </summary>
        /// <param name="connectionStringKey"></param>
        /// <returns></returns>
        public static string GetConnectionString(string connectionStringKey, Context context)
        {
            Context prev = Context.GlobalContext;
            try
            {
                Context.GlobalContext = context;
                string connection = Context.Cache["ConfigurationConnectionString" + connectionStringKey] as string;
                if (connection == null)
                {
                    ConnectionStringsSection section = (ConnectionStringsSection)GetSection("connectionStrings", context);
                    connection = section.ConnectionStrings[connectionStringKey].ConnectionString;

                    Context.Cache.Insert("ConfigurationConnectionString" + connectionStringKey, connection);

                    return connection;
                }
                else
                {
                    return connection;
                }
            }
            finally
            {
                Context.GlobalContext = prev;
            }
        }
        //========================================================================================================//

        #endregion Public

        #endregion Methods

        /// <summary>
        /// Grab a connection key and decrypt if necessary
        /// </summary>
        /// <param name="connectionStringKey">connection string</param>
        /// <returns></returns>
        public static string GetConnectionString(string connectionStringKey)
        {
            string connection = Context.Cache["ConfigurationConnectionString" + connectionStringKey] as string;
            if (String.IsNullOrEmpty(connection) == false) return connection;

            ConnectionStringsSection section = (ConnectionStringsSection)GetSection("connectionStrings", Context.GlobalContext);
            connection = section.ConnectionStrings[connectionStringKey].ConnectionString;

            Context.Cache.Insert("ConfigurationConnectionString" + connectionStringKey, connection);

            return connection;
        }
    }
}
