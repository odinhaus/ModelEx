using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;
using Altus.Core;
using System.Reflection;

namespace Altus.Core.Dynamic.Components
{
    public class DynamicShell : App
    {
        protected DynamicShell() : base() { }

        public static new App Instance
        {
            get;
            protected set;
        }

        protected override CompositionContainer OnCreateShell(Core.Context context, params string[] args)
        {
            Context.GlobalContext = context;

            SetEnvironmentVariables(args);

            Assembly shellAssembly = Assembly.GetExecutingAssembly();

            object[] attribs = shellAssembly.GetCustomAttributes(typeof(CompositionContainerAttribute), true);

            CompositionContainerAttribute sa = null;
            // get the shell atributes, there should only be one
            if (attribs.Length == 1)
            {
                sa = attribs[0] as CompositionContainerAttribute;
            }
            else
            {
                throw (new InvalidOperationException("A Shell requires one declared ShellAttribute."));
            }

            CompositionContainer shell = (CompositionContainer)TypeHelper.CreateType(
                ((CompositionContainerAttribute)attribs[0]).ShellType,
                null);
            Shell = shell;
            Shell.Initialize((CompositionContainerAttribute)attribs[0], args);
            Shell.Load();
            App.Instance = this;
            return Shell;
        }
    }
}
