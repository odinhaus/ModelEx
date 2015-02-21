using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Altus.Core.Component;
using Altus.Core.Dynamic;
using Altus.Core.Presentation.ViewModels;
using Altus.Core.Realtime;

namespace Altus.Core.Data
{
    public static class MetaData_Stub
    {
        public static IEnumerable<string> GetApplicationAliases(this IMetaDataContext ctx, Application app)
        {
            return new string[0];
        }

        public static IEnumerable<DynamicProperty<Application>> GetApplicationProperties(this IMetaDataContext ctx, Application app)
        {
            return new DynamicProperty<Application>[0];
        }

        public static IEnumerable<DynamicFunction<Application>> GetApplicationFunctions(this IMetaDataContext ctx, Application app)
        {
            return new DynamicFunction<Application>[0];
        }

        public static IEnumerable<string> GetViewAliases(this IMetaDataContext ctx, View view)
        {
            return new string[0];
        }

        public static IEnumerable<DynamicProperty<View>> GetViewProperties(this IMetaDataContext ctx, View view)
        {
            return new DynamicProperty<View>[0];
        }

        public static IEnumerable<DynamicFunction<View>> GetViewFunctions(this IMetaDataContext ctx, View view)
        {
            return new DynamicFunction<View>[0];
        }

        public static IPEndPoint GetNodeEndPoint(this IMetaDataContext ctx, string nodeAddress, string protocol)
        {
            IPEndPoint ep;
            TryGetNodeEndPoint(ctx, nodeAddress, protocol, out ep);
            return ep;
        }

        public static bool TryGetNodeEndPoint(this IMetaDataContext ctx, string nodeAddress, string protocol, out IPEndPoint ep)
        {
            ep = null;
            string[] updateGroup = Context.GetEnvironmentVariable<string>("AppUpdateGroup","0.0.0.0:0").Split(':');
            if (nodeAddress.Equals(updateGroup[0]))
            {
                ep = new IPEndPoint(IPAddress.Parse(updateGroup[0]), int.Parse(updateGroup[1]));
                return true;
            }
            //TODO: try to resolve ports from config/db
            return false;
        }

        public static IEnumerable<Field> GetTopicFields(this IMetaDataContext ctx, string topicName)
        {
            return new Field[0];
        }

        public static IEnumerable<string> GetViewTypes(this IMetaDataContext ctx, string windowName, string windowType)
        {
            DbState state = new DbState()
            {
                Callback = delegate(DbState s)
                {
                    List<string> vts = new List<string>();
                    while (s.Reader.Read())
                    {
                        vts.Add(s.Reader["ViewType"].ToString());
                    }
                    s.StateObject = vts.ToArray();
                }
            };

            ctx.ExecuteScript("GetViewTypes", state, 
                new DbParam("WindowName", windowName),
                new DbParam("AppType", windowType));

            return state.StateObject as string[];
        }

        public static string LoadWPFViewTemplate(this IMetaDataContext ctx, string viewName, string viewSize, string windowName)
        {
            DbState state = new DbState()
            {
                Callback = delegate(DbState s)
                {
                    if (s.Reader.HasRows)
                    {
                        s.Reader.Read();
                        string xaml = s.Reader["XAML"].ToString();
                        object template = s.Reader["MasterXAML"];
                        if (!(template is DBNull)
                            && !string.IsNullOrEmpty((string)template))
                        {
                            xaml = ((string)template).Replace("<ContentPresenter/>", xaml);
                        }
                        s.StateObject = xaml;
                    }
                    else
                    {
                        s.StateObject = "";
                    }
                }
            };

            ctx.ExecuteScript("LoadWPFViewTemplate", state,
                new DbParam("Size", viewSize),
                new DbParam("ViewName", viewName),
                new DbParam("Window", windowName));

            return state.StateObject.ToString();
        }

        public static bool CheckIsMulticast(this IMetaDataContext ctx, IPEndPoint ep)
        {
            return false;
        }
    }
}
