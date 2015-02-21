using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WoodMac.ModelEx.Core;
using WoodMac.ModelEx.Core.Dynamic;
using WoodMac.ModelEx.Core.Licensing;
using WoodMac.ModelEx.Core.Presentation.Commands;
using WoodMac.ModelEx.Core.Presentation.ViewModels;

namespace WoodMac.ModelEx.GEM.ViewModels
{
    [FlagsAttribute]
    public enum ChildrenType : int
    {
        None = 0,
        Folders = 1,
        NonFolders = 2
    }

    public class GetChildrenEventArgs : EventArgs
    {
        public GetChildrenEventArgs(ChildrenType type)
        {
            ChildrenType = type;
        }
        public ChildrenType ChildrenType { get; private set; }
    }

    public enum Behavior
    {
        OnOpen,
        OnCreate,
        OnRead,
        OnUpdate,
        OnDelete,
        OnClose,
        OnSelect,
        OnDeselect,
        OnDragStart,
        OnDragComplete,
        OnPrint,
        OnPropertiesShow,
        OnPropertiesHide,
        OnRename
    }

    public delegate void DefaultCommandHandler(object sender, DefaultCommandArgs e);

    public class DefaultCommandArgs : EventArgs
    {
        public DefaultCommandArgs(Behavior behavior)
        {
            this.DefaultBehavior = behavior;
        }

        public Behavior DefaultBehavior { get; private set; }
    }

    public class ExplorerFolder : WPFView
    {
        static object _lock = new object();

        XmlDocument _schema;
        string _root;
        XmlElement _xRoot;
        //bool _isInit = false;
        XmlNamespaceManager _xnsm;
        private void Initialize()
        {
            lock (_lock)
            {
                //if (!_isInit)
                //{
                // TODO:  this really only needs to be done once per app - but doing this lazy for the demo
                    _schema = new XmlDocument();
                    string appSchema = Path.Combine(Context.CurrentContext.CurrentApp.CodeBase,
                        "Schema",
                        Context.CurrentContext.CurrentApp.Name + ".xml");
                    _schema.Load(appSchema);

                    _xnsm = new XmlNamespaceManager(_schema.NameTable);
                    _xnsm.AddNamespace("m", "modelex");
                    Entity.SetNamespaceManager(_xnsm);
                    _xRoot = ((XmlElement)_schema.SelectSingleNode("//m:ModelEx", _xnsm));
                    _root = _xRoot.GetAttribute("name");
                    //_isInit = true;
                //}
            }
        }

        public ExplorerFolder(string windowName, string instanceName, string viewType, DeclaredApp app) 
            : base(windowName, instanceName, viewType, null, app)
        {
            Initialize();
            _fullPath = "WoodMac";
            _xNode = (XmlElement)_schema.SelectSingleNode("//m:ModelEx", _xnsm);
            _dataEntity = Entity.CreateRoot(_root);
        }

        internal int _level;
        internal string _fullPath;
        public ExplorerFolder(string windowName, string instanceName, string viewType, DeclaredApp app, string parentPath, int lvl, XmlElement xNode)
            : base(windowName, instanceName, viewType, null, app)
        {
            Initialize();
            _fullPath = Path.Combine(parentPath, instanceName);
            _level = lvl;
            _xNode = xNode;
            
            Debug.WriteLine("ctor: Name: " + instanceName + " XNode: " + xNode.GetAttribute("name"));
        }

        private IEnumerable<DynamicFunction<View>> CreateBehaviors()
        {
            foreach(XmlElement xBehavior in _xNode.SelectNodes("m:Entity/m:Behaviors/m:Behavior", _xnsm))
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
                        yield return CreateCodeBehavior(xBehavior);
                        break;
                    }
                }
            }
        }

        public bool SupportsBehavior(Behavior behavior)
        {
            return this.CommandBag.ContainsKey(behavior.ToString().ToLowerInvariant());
        }

        private DynamicFunction<View> CreateCodeBehavior(XmlElement xBehavior)
        {
            string bodyCS = "public void {0}(object {1}){{ {2} }}";
            bodyCS = string.Format(bodyCS,
                xBehavior.GetAttribute("a:Action"),
                xBehavior.GetAttribute("inputParameterName"),
                xBehavior.SelectSingleNode("m:Code", _xnsm).InnerText);
            DynamicFunction<View> function = new DynamicFunction<View>(
                this,
                this.Name,
                xBehavior.GetAttribute("a:Action"),
                bodyCS,
                null
                );
            return function;
        }

        private void CreateDefaultBehavior(XmlElement xBehavior)
        {
            this.AddCommand(xBehavior.GetAttribute("a:Action"),
                new Core.Presentation.Commands.Command(
                    new Action(HandleDefaultCommandBehavior), true)
                    {
                        Tag = (Behavior)Enum.Parse(typeof(Behavior), xBehavior.GetAttribute("a:Action"))
                    });
        }

        public event DefaultCommandHandler DefaultCommandExecuted;
        private void HandleDefaultCommandBehavior()
        {
            if (DefaultCommandExecuted != null)
                DefaultCommandExecuted(this, new DefaultCommandArgs((Behavior)Command.Current.Tag));
        }

        public XmlElement XRoot { get { return _xRoot; } }

        internal XmlElement _xNode;
        /// <summary>
        /// Gets the corresponding XmlNode definition that was used to create the 
        /// schema for the current ShellFolder
        /// </summary>
        public XmlElement XNode { get { return _xNode; } }
        Entity _dataEntity = null;
        public Entity Entity 
        { 
            get { return _dataEntity; } 
            private set 
            { 
                _dataEntity = value;
                BackingInstance = value;
            } 
        }

        // Every folder class should override the GetChildren method and return its children.
        // Children should first be collected in an array/collection/arraylist or any 
        // object which implements IEnumerable and then returned as the return value
        public System.Collections.IEnumerable GetChildren(GetChildrenEventArgs e)
        {
            lock (_lock)
            {
                // TODO : Substitute method body with code specific to your folder

                ArrayList ret = new ArrayList();
                Debug.WriteLine("Children: " + this.Entity.Name);
                if ((e.ChildrenType & ChildrenType.Folders) != 0)
                {
                    foreach (XmlElement xNod in _xNode.SelectNodes("m:Folders/m:Folder", _xnsm))
                    {
                        ret.AddRange(LoadShellFolders(xNod));
                    }
                }

                if ((e.ChildrenType & ChildrenType.NonFolders) != 0)
                {
                    foreach (XmlElement xNod in _xNode.SelectNodes("m:Files/m:File", _xnsm))
                    {
                        ret.AddRange(LoadShellFiles(xNod));
                    }
                }

                return ret;
            }
        }

        private ICollection LoadShellFiles(XmlElement xNod)
        {
            List<ExplorerFile> children = new List<ExplorerFile>();
            Entity.ExplorerFolderReader rdr;
            Entity.CreateChildReader(this, xNod, out rdr);

            foreach (Entity e in rdr.CreateChildren())
            {
                children.Add(new ExplorerFile(
                    this.WindowName,
                    e.Name,
                    this.ViewType,
                    this.App,
                    this._fullPath,
                    this._level + 1,
                    xNod) { Entity = e });
            }

            return children;
        }


        private ICollection LoadShellFolders(XmlElement xNod)
        {
            List<ExplorerFolder> children = new List<ExplorerFolder>();
            Entity.ExplorerFolderReader rdr;
            Entity.CreateChildReader(this, xNod, out rdr);

            foreach (Entity e in rdr.CreateChildren())
            {
                children.Add(new ExplorerFolder(
                    this.WindowName, 
                    e.Name, 
                    this.ViewType, 
                    this.App,
                    this._fullPath, 
                    this._level + 1, 
                    xNod) { Entity = e });
            }

            return children;
        }

        public override string ToString()
        {
            return this.Name;
        }

        #region Framework Overrides
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
            return CreateBehaviors();
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
