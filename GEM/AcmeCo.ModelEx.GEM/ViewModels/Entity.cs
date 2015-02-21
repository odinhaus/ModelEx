using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Altus.Core;
using Altus.Core.Data;
using Altus.Core.Dynamic;
using Altus.Core.Presentation.ViewModels;
using Altus.GEM.Selectors;

namespace Altus.GEM.ViewModels
{
    public enum EntityType
    {
        DataEntity = 1,
        ModelEntity = 2,
        ReportEntity = 3
    }


    public class Entity : Extendable<Entity>, IObservable<EntityBehavior>
    {
        protected Entity(string name, EntityType type)
            : base(name, null, true, new MemberResolutionHandler(ResolveMember))
        {
            this.Parent = null;
            this.EntityType = type;
        }

        public Entity(string name, EntityType type, Entity parent)
            : base(name, null, true, new MemberResolutionHandler(ResolveMember))
        {
            this.Parent = parent;
            this.EntityType = type;
        }

        protected Entity(DbDataReader rdr)
            : this(rdr["Name"].ToString(), (EntityType)(int)rdr["Type"], null)
        {
            _instanceType = rdr["Extension"].ToString();
            Id = (int)(long)rdr["Id"];
            ParentId = rdr["ParentId"] == DBNull.Value ? 0 : (int)(long)rdr["ParentId"];
        }

        private static bool ResolveMember(string memberName, out object resolvedResult)
        {
            resolvedResult = null;
            return false;
        }

        protected override IEnumerable<DynamicProperty<Entity>> OnGetProperties()
        {
            return new DynamicProperty<Entity>[0];
        }

        protected override IEnumerable<DynamicFunction<Entity>> OnGetFunctions()
        {
            return new DynamicFunction<Entity>[0];
        }

        protected override void OnInvokeByName(string methodName, params object[] args)
        {
            // do nothing
        }

        public Explorer Explorer { get; set; }

        private string _instanceType;
        protected override string OnGetInstanceType()
        {
            return _instanceType;
        }

        protected override IEnumerable<string> OnGetAliases()
        {
            return new string[0];
        }

        string _currentSize;
        public string CurrentSize 
        { 
            get { return _currentSize; } 
            set { _currentSize = value; OnPropertyChanged("CurrentSize"); } 
        }


        public DataTemplate SelectTemplate(DependencyObject container)
        {
            if (Dispatcher == null)
                Dispatcher = OnGetDispatcher();
            return OnSelectTemplate(container);
        }

        public DataTemplate SelectTemplate(DependencyObject container, string viewSize)
        {
            string size = _currentSize;
            try 
            {
                _currentSize = viewSize;
                return OnSelectTemplate(container);
            }
            finally 
            {
                _currentSize = size;
            }
        }

        public bool TrySelectTemplate(DependencyObject container, out DataTemplate template)
        {
            template = SelectTemplate(container);
            return template != null;
        }

        public bool TrySelectTemplate(DependencyObject container, string viewSize, out DataTemplate template)
        {
            template = SelectTemplate(container, viewSize);
            return template != null;
        }

        protected virtual DataTemplate OnSelectTemplate(DependencyObject container)
        {
            DataTemplateSelector vts = OnCreateDataTemplateSelector(container);
            ((FrameworkElement)container).DataContext = this;

            SetDependencyObject(container);

            return vts.SelectTemplate(this, container);
        }

        protected virtual DataTemplateSelector OnCreateDataTemplateSelector(DependencyObject container)
        {
            if (_currentSize == "Config")
            {
                return new ConfigViewContentTemplateSelector();
            }
            return new EmbeddedDataTemplateSelector();
        }

        protected Dispatcher OnGetDispatcher()
        {
            Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            return dispatcher;
        }

        public Dispatcher Dispatcher { get; protected set; }

        DependencyObject _dependencyObject = null;
        public DependencyObject GetDependencyObject()
        {
            return _dependencyObject;
        }

        public void SetDependencyObject(DependencyObject container)
        {
            _dependencyObject = container;
        }

        public int Id { get; set; }

        public int ParentId { get; set; }
        public Entity Parent { get; set; }
        public EntityType EntityType { get; private set; }

        public bool CanExecute { get { return OnCanExecute(); } }

        protected virtual bool OnCanExecute()
        {
            return true;
        }

        public static IEnumerable<Entity> Select(EntityType type, string size, params string[] tags)
        {
            DbState state = new DbState()
            {
                Callback = new DbCallback(delegate(DbState s)
                {
                    List<Entity> entities = new List<Entity>();
                    while(s.Reader.Read())
                    {
                        entities.Add(
                            new Entity(s.Reader) { _currentSize = size });
                    }
                    s.StateObject = entities;
                })
            };

            DataContext.Default.ExecuteScript("SelectEntities",
                state,
                new DbParam("Type", (int)type),
                new DbParam("TagsIn", tags.Length == 0 ? null : tags) { Type = DbParamType.InSetOf },
                new DbParam("Tags", tags.Length == 0 ? null : tags));

            List<Entity> results = new List<Entity>();
            foreach(var group in ((IEnumerable<Entity>)state.StateObject).GroupBy(e => e.InstanceType))
            {
                List<int> ids = new List<int>();
                foreach(Entity e in group)
                {
                    ids.Add(e.Id);
                }

                DbState state2 = new DbState()
                {
                    Callback = new DbCallback(delegate(DbState s)
                    {
                        List<Entity> entities = new List<Entity>();
                        string typeName = string.Format("{0}{1}, {2}",
                            typeof(Entity).FullName.Replace("Entity", ""),
                            group.Key,
                            Context.CurrentContext.CurrentApp.PrimaryAssembly.GetName().Name);
                        Type entityType = TypeHelper.GetType(typeName);
                        while (s.Reader.Read())
                        {
                            Entity e = (Entity)Activator.CreateInstance(entityType, s.Reader);
                            e._currentSize = size;
                            entities.Add(e);
                        }
                        s.StateObject = entities;
                    })
                };

                DataContext.Default.ExecuteScript("SelectEntitiesExtended",
                state2,
                new DbParam("Extension", group.Key),
                new DbParam("Ids", ids.ToArray()));

                results.AddRange(state2.StateObject as IEnumerable<Entity>);
            }

            return results;
        }

        

        public void HandleExecute()
        {
            OnHandleExecute();
        }

        protected virtual void OnHandleExecute()
        {
            EntityBehavior behavior = null;

            switch(this.EntityType)
            {
                default:
                case ViewModels.EntityType.DataEntity:
                    {
                        behavior = OnHandleDataEntityExecute();
                        break;
                    }
                case ViewModels.EntityType.ModelEntity:
                    {
                        behavior = OnHandleModelEntityExecute();
                        break;
                    }
                case ViewModels.EntityType.ReportEntity:
                    {
                        behavior = OnHandleReportEntityExecute();
                        break;
                    }
            }

            lock (_observers)
            {
                foreach (IObserver<EntityBehavior> observer in _observers)
                {
                    observer.OnNext(behavior);
                }
            }
        }

        protected virtual EntityBehavior OnHandleDataEntityExecute()
        {
            return new EntityBehavior(this,
                BehaviorType.Execute,
                new EntityBehaviorResult(ResultType.Success),
                new EntityBehaviorResult(ResultType.Information, "DataEntity Execute"));
        }

        protected virtual EntityBehavior OnHandleModelEntityExecute()
        {
            return new EntityBehavior(this,
                BehaviorType.Execute,
                new EntityBehaviorResult(ResultType.Success),
                new EntityBehaviorResult(ResultType.Information, "ModelEntity Execute"));
        }

        protected virtual EntityBehavior OnHandleReportEntityExecute()
        {
            // create workflow instance
            // hydrate input data
            // invoke workflow
            // display results

            return new EntityBehavior(this,
                BehaviorType.Execute,
                new EntityBehaviorResult(ResultType.Success),
                new EntityBehaviorResult(ResultType.Information, "ReportEntity Execute"));
        }

        HashSet<IObserver<EntityBehavior>> _observers = new HashSet<IObserver<EntityBehavior>>();
        public IDisposable Subscribe(IObserver<EntityBehavior> observer)
        {
            return new EntityObserverSubscription(_observers, observer);
        }

        private class EntityObserverSubscription : IDisposable
        {
            public EntityObserverSubscription(HashSet<IObserver<EntityBehavior>> observers, IObserver<EntityBehavior> observer)
            {
                this.Observers = observers;
                this.Observer = observer;
                if (observer == null) return;
                lock (this.Observers)
                {
                    if (!this.Observers.Contains(this.Observer))
                        this.Observers.Add(this.Observer);
                }
            }

            public void Dispose()
            {
                lock(this.Observers)
                {
                    if (this.Observer != null
                        && this.Observers.Contains(this.Observer))
                    {
                        this.Observers.Remove(this.Observer);
                    }
                }
            }

            public HashSet<IObserver<EntityBehavior>> Observers { get; private set; }

            public IObserver<EntityBehavior> Observer { get; private set; }
        }
    }
}
