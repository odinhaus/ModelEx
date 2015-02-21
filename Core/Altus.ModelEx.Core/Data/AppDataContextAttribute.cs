using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core.Data
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
    public class AppDataContextAttribute : Attribute
    {
        /// <summary>
        /// Describes the data types to use for a given app
        /// </summary>
        /// <param name="contextType">type that implements IMetaDataContext</param>
        /// <param name="connectionType">type that implements IDbConnection</param>
        /// <param name="connectionManagerType">type that implements IDbConnectionManager</param>
        public AppDataContextAttribute(Type contextType, Type connectionType, Type connectionManagerType)
        {
            ContextType = contextType;
            ConnectionManagerType = connectionManagerType;
            ConnectionType = connectionType;
        }

        public Type ContextType { get; private set; }
        public Type ConnectionType { get; private set; }
        public Type ConnectionManagerType { get; private set; }
    }
}
