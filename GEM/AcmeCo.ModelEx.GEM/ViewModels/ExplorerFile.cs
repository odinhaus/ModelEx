using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WoodMac.ModelEx.Core.Dynamic;
using WoodMac.ModelEx.Core.Licensing;
using WoodMac.ModelEx.Core.Presentation.Commands;
using WoodMac.ModelEx.Core.Presentation.ViewModels;

namespace WoodMac.ModelEx.GEM.ViewModels
{
    public class ExplorerFile : WPFView
    {

        private DeclaredApp declaredApp;
        private string _fullPath;
        private int _level;
        private System.Xml.XmlElement _xNod;

        public ExplorerFile(string windowName, string instanceName, string viewType, DeclaredApp app) 
            : base(windowName, instanceName, viewType, null, app)
        {}

        public ExplorerFile(string windowName, string instanceName, string viewType, DeclaredApp declaredApp, string fullPath, int level, System.Xml.XmlElement xNod)
            : base(windowName, instanceName, viewType, null, declaredApp)
        {
            this.declaredApp = declaredApp;
            this._fullPath = fullPath;
            this._level = level;
            this._xNod = xNod;
        }

        Entity _dataEntity = null;
        public Entity Entity 
        { 
            get { return _dataEntity; } 
            internal set 
            { 
                _dataEntity = value;
                BackingInstance = value;
            } 
        }

        public bool SupportsBehavior(Behavior behavior)
        {
            return this.CommandBag.ContainsKey(behavior.ToString().ToLowerInvariant());
        }
        private void CreateBehaviors()
        {
            foreach (XmlElement xBehavior in _xNod.SelectNodes("m:Entity/m:Behaviors"))
            {
                switch (xBehavior.GetAttribute("xsi:type"))
                {
                    case "DefaultBehavior":
                        {
                            CreateDefaultBehavior(xBehavior);
                            break;
                        }
                    case "CodeBehavior":
                        {
                            CreateCodeBehavior(xBehavior);
                            break;
                        }
                }
            }
        }

        private void CreateCodeBehavior(XmlElement xBehavior)
        {
            
        }

        private void CreateDefaultBehavior(XmlElement xBehavior)
        {
            this.AddCommand(xBehavior.GetAttribute("name"),
                new Core.Presentation.Commands.Command(
                    new Action(HandleDefaultCommandBehavior), true)
                {
                    Tag = (Behavior)Enum.Parse(typeof(Behavior), xBehavior.GetAttribute("name"))
                });
        }

        public event DefaultCommandHandler DefaultCommandExecuted;
        private void HandleDefaultCommandBehavior()
        {
            if (DefaultCommandExecuted != null)
                DefaultCommandExecuted(this, new DefaultCommandArgs((Behavior)Command.Current.Tag));
        }

        #region Framework Overrides
        private static bool MemberResolutionFailed(string memberName, out object resolvedResult)
        {
            // just here to catch it if we want to handle later on
            resolvedResult = null;
            return false;
        }

        protected override IEnumerable<string> OnGetAliases()
        {
            return new string[0];
        }

        protected override IEnumerable<DynamicProperty<View>> OnGetProperties()
        {
            return new DynamicProperty<View>[0];
        }

        protected override IEnumerable<DynamicFunction<View>> OnGetFunctions()
        {
            return new DynamicFunction<View>[0];
        }

        protected override bool OnSupportsTopics()
        {
            return false;
        }

        protected override string OnGetInstanceType()
        {
            return this.GetType().Name;
        }
        #endregion

    }
}
