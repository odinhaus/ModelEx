using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using System.Activities.XamlIntegration;
using System.IO;
using AcmeCo.Workflow.SpreadsheetGearActivities;
using System.Xaml;
using System.Xml;

namespace Altus.GEM.Workflow
{

    public static class WorkflowExtensions
    {
        //private static ActivityXamlServicesSettings _settings = new ActivityXamlServicesSettings() 
        //{ 
        //    CompileExpressions = true 
        //};

        public static IDictionary<string, object> InvokeActivity(string activityXamlFile, IDictionary<string, object> inputs)
        {
            //DynamicActivity activity = ActivityXamlServices.Load(activityXamlFile, _settings) as DynamicActivity;
            DynamicActivity activity = ActivityXamlServices.Load(activityXamlFile) as DynamicActivity;
            return activity.InvokeActivity(inputs);
        }

        public static bool TryInvokeActivity(string activityXamlFile, IDictionary<string, object> inputs, out IDictionary<string, object> outputs)
        {
            //DynamicActivity activity = ActivityXamlServices.Load(activityXamlFile, _settings) as DynamicActivity;
            DynamicActivity activity = ActivityXamlServices.Load(activityXamlFile) as DynamicActivity;

            return activity.TryInvokeActivity(inputs, out outputs);
        }

        public static IDictionary<string, object> InvokeActivity(Stream stream, IDictionary<string, object> inputs)
        {
            //DynamicActivity activity = ActivityXamlServices.Load(activityXamlFile, _settings) as DynamicActivity;
            DynamicActivity activity = ActivityXamlServices.Load(stream) as DynamicActivity;

            return activity.InvokeActivity(inputs);
        }

        public static bool TryInvokeActivity(Stream stream, IDictionary<string, object> inputs, out IDictionary<string, object> outputs)
        {
            //DynamicActivity activity = ActivityXamlServices.Load(activityXamlFile, _settings) as DynamicActivity;
            DynamicActivity activity = ActivityXamlServices.Load(stream) as DynamicActivity;

            return activity.TryInvokeActivity(inputs, out outputs);
        }

        public static IDictionary<string, object> InvokeActivity(XamlReader xamlReader, IDictionary<string, object> inputs)
        {
            //DynamicActivity activity = ActivityXamlServices.Load(activityXamlFile, _settings) as DynamicActivity;
            DynamicActivity activity = ActivityXamlServices.Load(xamlReader) as DynamicActivity;

            return activity.InvokeActivity(inputs);
        }

        public static bool TryInvokeActivity(XamlReader xamlReader, IDictionary<string, object> inputs, out IDictionary<string, object> outputs)
        {
            //DynamicActivity activity = ActivityXamlServices.Load(activityXamlFile, _settings) as DynamicActivity;
            DynamicActivity activity = ActivityXamlServices.Load(xamlReader) as DynamicActivity;

            return activity.TryInvokeActivity(inputs, out outputs);
        }

        public static IDictionary<string, object> InvokeActivity(XmlReader xmlReader, IDictionary<string, object> inputs)
        {
            //DynamicActivity activity = ActivityXamlServices.Load(activityXamlFile, _settings) as DynamicActivity;
            DynamicActivity activity = ActivityXamlServices.Load(xmlReader) as DynamicActivity;

            return activity.InvokeActivity(inputs);
        }

        public static bool TryInvokeActivity(XmlReader xmlReader, IDictionary<string, object> inputs, out IDictionary<string, object> outputs)
        {
            //DynamicActivity activity = ActivityXamlServices.Load(activityXamlFile, _settings) as DynamicActivity;
            DynamicActivity activity = ActivityXamlServices.Load(xmlReader) as DynamicActivity;

            return activity.TryInvokeActivity(inputs, out outputs);
        }

        public static IDictionary<string, object> InvokeActivity(this DynamicActivity activity, IDictionary<string, object> inputs)
        {
            IDictionary<string, object> outputs = new Dictionary<string, object>();

            if (activity.TryInvokeActivity(inputs, out outputs))
            {
                return outputs;
            }
            else
            {
                return null;
            }
        }

        public static bool TryInvokeActivity(this DynamicActivity activity, IDictionary<string, object> inputs, out IDictionary<string, object> outputs)
        {
            IDictionary<string, object> arguments = new Dictionary<string, object>();

            foreach (var property in activity.Properties)
            {
                if (inputs.ContainsKey(property.Name))
                {
                    arguments.Add(property.Name, inputs[property.Name]);
                }
            }

            try
            {
                outputs = WorkflowInvoker.Invoke(activity, arguments);
                return true;
            }
            catch
            {
                outputs = null;
                return false;
            }
        }
    }
}

