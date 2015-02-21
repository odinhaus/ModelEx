using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Reflection;
using Altus.Core.Security;
using System.ComponentModel;

namespace Altus.Core.Dynamic
{
    public class DynamicOrgUnit : DynamicObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DynamicOrgUnit(DynamicObject parent)
        {
            Parent = parent;
        }

        private Dictionary<string, DynamicOrgUnit> _children = new Dictionary<string,DynamicOrgUnit>();
       // private Dictionary<string, DynamicOrgUnit> _childrenAliased = new Dictionary<string, DynamicOrgUnit>();
        public string Name { get; set; }
        public IEnumerable<string> Aliases { get; set; }
        public IEnumerable<DynamicOrgUnit> Children { get { return _children.Values.ToArray();}}
        private object _instance = null;
        public object Instance 
        {
            get { return _instance; }
            set
            {
                if (_instance != null && _instance is INotifyPropertyChanged)
                    ((INotifyPropertyChanged)_instance).PropertyChanged -= OnPropertyChanged;
                _instance = value;
                if (_instance != null && _instance is INotifyPropertyChanged)
                    ((INotifyPropertyChanged)_instance).PropertyChanged += OnPropertyChanged;
            }
        }
        public bool IsTerminal { get{ return Children.Count() == 0;}}
        public bool IsRoot { get { return string.IsNullOrEmpty(Name); } }
        public DynamicObject Parent { get; private set; }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this[binder.Name];

            if (result is DynamicOrgUnit 
                && ((DynamicOrgUnit)result).IsTerminal
                && ((DynamicOrgUnit)result).Instance != null)
            {
                result = ((DynamicOrgUnit)result).Instance;
            }
            else if (result == null && Instance != null)
            {
                if (Instance is DynamicObject)
                    return ((DynamicObject)Instance).TryGetMember(binder, out result);
                else
                {
                    MemberInfo mi = Instance.GetType().GetMember(binder.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m is PropertyInfo || m is FieldInfo || (m is MethodInfo && ((MethodInfo)m).GetParameters().Length == 0)).FirstOrDefault();
                    if (mi != null)
                        result = this.Instance;
                }
            }
            
            return result != null;
        }

        public DynamicOrgUnit this[string childName]
        {
            get
            {
                if (this.IsRoot)
                {
                    if (!childName.Contains(':'))
                    {
                        childName = NodeIdentity.Application + ":" + childName;
                    }
                }
                if (_children.ContainsKey(childName))
                {
                    return _children[childName];
                }
                //else if (_childrenAliased.ContainsKey(childName))
                //{
                //    return _childrenAliased[childName];
                //}
                else return null;
            }
        }

        public void AddChild(DynamicOrgUnit node)
        {
            _children.Add(node.Name, node);
            //foreach (string alias in node.Aliases)
            //{
            //    _children.Add(alias, node);
            //}
            //if (node.Instance is IAliased)
            //{
            //    foreach (string alias in ((IAliased)node.Instance).Aliases)
            //    {
            //        _children.Add(alias, node);
            //    }
            //}
        }

        public DynamicOrgUnit FindNode(string dotQualifiedNodeName, bool createDefaultIfMissing)
        {
            string[] nodeNames = dotQualifiedNodeName.Split('.');
            DynamicOrgUnit found = this[nodeNames[0]];
            //if (found == null) return null;

            if (found == null && !createDefaultIfMissing)
            {
                return null;
            }
            else if (found == null)
            {
                SpoofObject so = new SpoofObject(nodeNames[0]);
                found = new DynamicOrgUnit(this) { Name = NodeIdentity.Application + ":" + nodeNames[0], Instance = so };
                AddChild(found);
            }

            if (nodeNames.Length > 1)
            {
                found = found.FindNode(dotQualifiedNodeName.Substring(nodeNames[0].Length + 1), createDefaultIfMissing);
            }
            return found;
        }

        public DynamicOrgUnit CreateNode(string dotQualifiedNodeName)
        {
            string[] nodeNames = dotQualifiedNodeName.Split('.');
            DynamicOrgUnit found = this[nodeNames[0]];
            if (found == null)
            {
                found = new DynamicOrgUnit(this) { Name = nodeNames[0] };
                this.AddChild(found);
            }
            if (nodeNames.Length > 1)
            {
                found = found.CreateNode(dotQualifiedNodeName.Substring(nodeNames[0].Length + 1));
            }
            return found;
        }

        protected void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        protected void OnPropertyChanged(object sender, string name)
        {
            OnPropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class SpoofObject : DynamicObject
    {
        public SpoofObject(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        private bool FieldMemberResolutionFailed(string name, out object result)
        {
            result = "---";
            return true;
        }
    }
}

