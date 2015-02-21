using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Security.Principal;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using Altus.Core.Data;
using System.Data.Common;
using Altus.Core.Presentation.ViewModels;


namespace Altus.Core.Data.SQlite
{
    public partial class MetaDataContextSQLite : MetaDataContext
    {
        public MetaDataContextSQLite(string name)
            : base(name)
        {

        }

        /* EXAMPLE of INSERT, UPDATE and DELETE method signatures
         * 
         * For INSERT Operations, your INSERT script should concatenate a select operation after the INSERT that looks like the following:
         * INSERT INTO Foo (Col1, Col2,...) VALUES(@Col1, @Col2,...);$select * from cipfield order by rowid desc limit 1;
         * The $ acts as a separator, causing the data framework to execute both the INSERT and SELECT commands in the same transaction
         * The ScalarWriteStorageCallback handler defined below will be called after the SELECT, so you can build your entity.
         * For multi-table inserts, the same technique can be applied, separating the INSERT commands by $ with a final SELECT that will be used
         * read the inserted data and populate the Primary Key field(s) on the entity.
         * 
        protected void OnInsertTopic(out Func<object, DbParam[], DbParam[]> paramBuilder, out ScalarWriteStorageCallback<Topic> entityBuilder, out string scriptName)
        {
            NodeAddress address = NodeIdentity.NodeAddress;
            scriptName = "InsertTopic";
            entityBuilder = new ScalarWriteStorageCallback<Topic>(delegate(ref Topic entity, DbDataReader reader)
                {
                    this.OnPopulateEntity(entity, reader, StorageMapping.CreateFromType(typeof(Topic)));
                });
            paramBuilder = new Func<object, DbParam[], DbParam[]>(delegate(dynamic filter, DbParam[] parms)
            {
                return new DbParam[]{
                        new DbParam("OrgName", address.Organization), new DbParam("Topic", filter.Name), new DbParam("App", address.Platform)
                    };
            });
        }
        */

        protected void OnGetViewList(string windowName, string viewType, string uiType, string defaultSize, out Func<object, DbParam[], DbParam[]> paramBuilder, out ScalarReadStorageCallback<ViewList> entityBuilder, out string scriptName)
        {
            paramBuilder = new Func<object, DbParam[], DbParam[]>(delegate(dynamic filter, DbParam[] parms)
            {
                return new DbParam[]{
                        new DbParam("WindowName", ((dynamic)filter).windowName),
                        new DbParam("ViewType", ((dynamic)filter).viewType),
                        new DbParam("AppType", ((dynamic)filter).uiType)
                    };
            });
            entityBuilder = new ScalarReadStorageCallback<ViewList>(delegate(ViewList entity, DbDataReader reader)
            {
                List<string[]> dbvals = new List<string[]>();
                while (reader.Read())
                {
                    dbvals.Add(new string[] { reader["CLRType"].ToString(), reader["Name"].ToString(), reader["IconPath"].ToString() });
                }
                reader.Close();

                entity = new ViewList(windowName);
                foreach (string[] dbval in dbvals)
                {
                    Type t = TypeHelper.GetType(dbval[0]);
                    View v;
                    if (t == null || t.Equals(typeof(View)))
                    {
                        v = View.Create(uiType, windowName, dbval[1], viewType, null, Context.CurrentContext.CurrentApp);
                    }
                    else if (t.IsSubclassOf(typeof(View)))
                    {
                        v = (View)Activator.CreateInstance(t, new object[] { windowName, dbval[1], viewType, null, Context.CurrentContext.CurrentApp });
                    }
                    else
                    {
                        v = View.Create(uiType, windowName, dbval[1], viewType, Activator.CreateInstance(t), Context.CurrentContext.CurrentApp);
                    }

                    v.CurrentSize = defaultSize;
                    string iconPath = dbval[2];
                    if (!string.IsNullOrEmpty(iconPath))
                    {
                        try
                        {
                            BitmapImage bi = new BitmapImage();
                            bi.BeginInit();
                            bi.UriSource = new Uri(iconPath);
                            bi.EndInit();
                            v.Icon = bi;
                        }
                        catch { }
                    }
                    entity.Add(v);
                }
                return entity;
            });

            scriptName = "GetViews";
        }

        protected void OnSelectView(string windowName, string viewType, string uiType, string defaultSize, out Func<object, DbParam[], DbParam[]> paramBuilder, out EnumerableReadStorageCallback<View> listBuilder, out string scriptName)
        {
            paramBuilder = new Func<object, DbParam[], DbParam[]>(delegate(dynamic filter, DbParam[] parms)
            {
                return new DbParam[]{
                        new DbParam("WindowName", ((dynamic)filter).windowName),
                        new DbParam("ViewType", ((dynamic)filter).viewType),
                        new DbParam("AppType", ((dynamic)filter).uiType)
                    };
            });
            listBuilder = new EnumerableReadStorageCallback<View>(delegate(ref IList<View> list, DbDataReader reader)
            {
                System.Collections.IList iList = (System.Collections.IList)list;
                List<string[]> dbvals = new List<string[]>();
                while (reader.Read())
                {
                    dbvals.Add(new string[] { reader["CLRType"].ToString(), reader["Name"].ToString(), reader["IconPath"].ToString() });
                }
                reader.Close();
                foreach (string[] dbval in dbvals)
                {
                    Type t = TypeHelper.GetType(dbval[0]);
                    View v;
                    if (t == null || t.Equals(typeof(View)))
                    {
                        v = View.Create(uiType, windowName, dbval[1], viewType, null, Context.CurrentContext.CurrentApp);
                    }
                    else if (t.IsSubclassOf(typeof(View)))
                    {
                        v = (View)Activator.CreateInstance(t, new object[] { windowName, dbval[1], viewType, null });
                    }
                    else
                    {
                        v = View.Create(uiType, windowName, dbval[1], viewType, Activator.CreateInstance(t), Context.CurrentContext.CurrentApp);
                    }

                    v.CurrentSize = defaultSize;
                    string iconPath = dbval[2];
                    if (!string.IsNullOrEmpty(iconPath))
                    {
                        BitmapImage bi = new BitmapImage();
                        bi.BeginInit();
                        bi.UriSource = new Uri(iconPath);
                        bi.EndInit();
                        v.Icon = bi;
                    }
                    list.Add(v);
                }
            });
            scriptName = "GetViews";
        }

        protected void OnGetView(string windowName, string viewName, string uiType, string defaultSize, out Func<object, DbParam[], DbParam[]> paramBuilder, out ScalarReadStorageCallback<View> entityBuilder, out string scriptName)
        {
            paramBuilder = new Func<object, DbParam[], DbParam[]>(delegate(dynamic filter, DbParam[] parms)
            {
                return new DbParam[]{
                        new DbParam("ViewName", ((dynamic)filter).viewName),
                        new DbParam("AppType", ((dynamic)filter).uiType)
                    };
            });
            entityBuilder = new ScalarReadStorageCallback<View>(delegate(View entity, DbDataReader reader)
            {
                string[] dbvals = null;
                if (reader.Read())
                {
                    dbvals = new string[] { reader["CLRType"].ToString(), reader["Name"].ToString(), reader["IconPath"].ToString(), reader["ViewType"].ToString() };
                }
                reader.Close();

                if (dbvals != null)
                {
                    Type t = TypeHelper.GetType(dbvals[0]);

                    if (t == null || t.Equals(typeof(View)))
                    {
                        entity = View.Create(uiType, windowName, dbvals[1], dbvals[3], null, Context.CurrentContext.CurrentApp);
                    }
                    else if (t.IsSubclassOf(typeof(View)))
                    {
                        entity = (View)Activator.CreateInstance(t, new object[] { windowName, dbvals[1], dbvals[3], null, Context.CurrentContext.CurrentApp });
                    }
                    else
                    {
                        entity = View.Create(uiType, windowName, dbvals[1], dbvals[3], Activator.CreateInstance(t), Context.CurrentContext.CurrentApp);
                    }

                    ((View)entity).CurrentSize = defaultSize;
                    string iconPath = dbvals[2];
                    if (!string.IsNullOrEmpty(iconPath))
                    {
                        BitmapImage bi = new BitmapImage();
                        bi.BeginInit();
                        bi.UriSource = new Uri(iconPath);
                        bi.EndInit();
                        ((View)entity).Icon = bi;
                    }
                }
                return entity;
            });
            scriptName = "GetView";
        }
    }
   
}
