using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Configuration
{
    public class DBConnectionInfo
    {
        /// <summary>
        /// Our server name
        /// </summary>
        private string _serverName = String.Empty;

        /// <summary>
        /// Our server name
        /// </summary>
        public string ServerName
        {
            get { return _serverName; }
            set { _serverName = value; }
        }

        /// <summary>
        /// Our Object name
        /// </summary>
        private string _ObjectName = String.Empty;

        /// <summary>
        /// Our Object name
        /// </summary>
        public string ObjectName
        {
            get { return _ObjectName; }
            set { _ObjectName = value; }
        }

        /// <summary>
        /// Do we use integrated security?
        /// </summary>
        private bool _integratedSecurity;

        /// <summary>
        /// Do we use integrated security?
        /// </summary>
        public bool IntegratedSecurity
        {
            get { return _integratedSecurity; }
            set { _integratedSecurity = value; }
        }

        /// <summary>
        /// Object user id
        /// </summary>
        private string _userID = String.Empty;

        /// <summary>
        /// Object user id
        /// </summary>
        public string UserID
        {
            get { return _userID; }
            set { _userID = value; }
        }

        /// <summary>
        /// Object password
        /// </summary>
        private string _password = String.Empty;

        /// <summary>
        /// Object password
        /// </summary>
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        /// <summary>
        /// default public constructor needed for serializable objects
        /// </summary>
        public DBConnectionInfo()
        {
        }

        public DBConnectionInfo(string connectionString)
            : this()
        {
            Dictionary<string, string> connSettings = CrackConnectionString(connectionString);

            this.ServerName = GetOneOption(connSettings, true, "Server", "Data Source", "Address", "Addr", "Network Address");
            this.ObjectName = GetOneOption(connSettings, true, "Object", "Initial Catalog");

            string securityModel = GetOneOption(connSettings, false, "Trusted_Connection", "Integrated Security");
            if (StringComparer.OrdinalIgnoreCase.Equals(securityModel, "true") ||
                StringComparer.OrdinalIgnoreCase.Equals(securityModel, "sspi"))
            {
                this.IntegratedSecurity = true;
                this.UserID = this.Password = null;
            }
            else
            {
                this.IntegratedSecurity = false;
                this.UserID = GetOneOption(connSettings, true, "User ID");
                this.Password = GetOneOption(connSettings, true, "Password", "Pwd");
            }
        }

        //========================================================================================================//

        private static Dictionary<string, string> CrackConnectionString(string input)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string substr in input.Trim(' ', ';').Split(';'))
            {
                string[] subvals = substr.Split(new char[] { '=' }, 2);
                if (subvals.Length == 1)
                    subvals = new string[] { subvals[0], String.Empty };
                values[subvals[0].Trim()] = subvals[1].Trim();
            }
            return values;
        }

        //========================================================================================================//

        private static string GetOneOption(Dictionary<string, string> options, bool throwIfNotFound, params string[] possibles)
        {
            string result = null;
            foreach (string possible in possibles)
            {
                if (options.TryGetValue(possible, out result))
                    return result;
            }
            if (!throwIfNotFound)
                return null;
            throw new ApplicationException(String.Format("Unable to locate the sql connection argument: {0}", possibles[0]));
        }

        #region overrides
        /// <summary>
        /// Returns the hash of the this objects instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode() ^ _serverName.GetHashCode() ^ _ObjectName.GetHashCode() ^
                _integratedSecurity.GetHashCode() ^ _userID.GetHashCode() ^ _password.GetHashCode();
        }

        /// <summary>
        /// Provides equals for this objects instance
        /// </summary>
        /// <param name="obj">An object instance to compare this one to</param>
        /// <returns>true if the value is the same else false</returns>
        public override bool Equals(object obj)
        {
            bool retVal = false;
            DBConnectionInfo dbConNfo = obj as DBConnectionInfo;
            if (dbConNfo == null) return false;
            if (this == dbConNfo) return true;

            retVal =
                this._serverName == dbConNfo.ServerName &&
                this._ObjectName == dbConNfo.ObjectName &&
                this._integratedSecurity == dbConNfo.IntegratedSecurity &&
                this._userID == dbConNfo.UserID &&
                this._password == dbConNfo.Password;

            return retVal;
        }

        /// <summary>
        /// String representation of this objects instance
        /// </summary>
        /// <returns>String representation of this objects instance</returns>
        public override string ToString()
        {
            if (IntegratedSecurity)
                return String.Format("Server={0};Object={1};Integrated Security={2}", this.ServerName, this.ObjectName,
                    this.IntegratedSecurity);

            return String.Format("Server={0};Object={1};User ID={2};Password={3}", this.ServerName, this.ObjectName,
                this.UserID, this.Password);
        }
        #endregion
    }
}
