using System;
using System.Collections.Generic;
using System.Data.Common;
using Altus.Core.Data;
using System.ComponentModel;
using Altus.Core.Component;

namespace Altus.Core.Data
{
    public delegate T ScalarReadStorageCallback<T>(T entity, DbDataReader reader);
    public delegate void EnumerableReadStorageCallback<T>(ref IList<T> entityList, DbDataReader reader);
    public delegate void ScalarWriteStorageCallback<T>(ref T entity, DbState state);

    public interface IMetaDataContext : IInitialize, IComponent
    {
        //DataContext.DbLock Lock { get; }
        string Name { get; }

        void Insert<T>(ref T entity);
        void Insert<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler);
        void Insert<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, ScalarWriteStorageCallback<T> executeCallback);
        void Update<T>(ref T entity);
        void Update<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler);
        void Update<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, ScalarWriteStorageCallback<T> executeCallback);
        T Get<T>(object entity);
        T Get<T>(object entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler);
        T Get<T>(object entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, ScalarReadStorageCallback<T> executeCallback);
        void Delete<T>(ref T entity);
        void Delete<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler);
        void Delete<T>(ref T entity, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, ScalarWriteStorageCallback<T> executeCallback);
        IEnumerable<T> Select<T>(object filter = null);
        IEnumerable<T> Select<T>(object filter, Func<object, DbParam[], DbParam[]> parameterBuilderHandler);
        IEnumerable<T> Select<T>(object filter, Func<object, DbParam[], DbParam[]> parameterBuilderHandler, EnumerableReadStorageCallback<T> executeCallback);

        //global::System.Data.Common.DbConnection CurrentConnection { get; }
        IDbConnection Connection { get; }
        void ExecuteScript(string scriptName, params DbParam[] dbParams);
        void ExecuteScript(string scriptName, DbState state, params DbParam[] dbParams);
        void ExecuteQuery(string query, params DbParam[] dbParams);
        void ExecuteQuery(string query, DbState state, params DbParam[] dbParams);
    }

}
