using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using Altus.Core;
using Altus.Core.Collections;
using Altus.Core.Component;
using Altus.Core.Licensing;
using Altus.Core.Presentation.ViewModels;
using Altus.GEM.Install;
using Altus.GEM.Selectors;
using Altus.GEM.ViewModels;

[assembly: Component(ComponentType=typeof(Explorer), CtorArgs=new object[]{"Main", "Explorer", "wpf"})]
namespace Altus.GEM.ViewModels
{
    public class Explorer : WPFView
    {
        public Explorer(string windowName, string viewName, string viewType)
            : base(windowName, viewName, viewType, null, Context.CurrentContext.CurrentApp)
        {
            _selectedItem = "Data";
            _itemSize = "Large";
        }

        protected override System.Windows.Controls.DataTemplateSelector OnCreateDataTemplateSelector(System.Windows.DependencyObject container)
        {
            return new EmbeddedDataTemplateSelector();
        }

        protected override bool OnSupportsTopics()
        {
            return false;
        }

        protected override void OnApplyLicensing(ILicense[] licenses, string[] args)
        {
            // default installer behavior is to cycle thru all apps if no app names are given
            Installer installer = new Installer();
            installer.Install();
            
            base.OnApplyLicensing(licenses, args);
        }

        private string _selectedItem;
        public string SelectedItem 
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                this.SelectedItemChanged(value);
                OnPropertyChanged("SelectedItem");
            }
        }

        private void SelectedItemChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                this.Items.Clear();
            }
            else
            {
                EntityType type;
                switch (value)
                {
                    default:
                    case "Data":
                        {
                            type = EntityType.DataEntity;
                            break;
                        }
                    case "Models":
                        {
                            type = EntityType.ModelEntity;
                            break;
                        }
                    case "Reports":
                        {
                            type = EntityType.ReportEntity;
                            break;
                        }
                }

                LoadData(type, this.Observer);
            }
        }
        IObserver<EntityBehavior> _observer = null;
        public IObserver<EntityBehavior> Observer 
        {
            get { return _observer; }
            set
            {
                if (_observer == null)
                {
                    if (_items.Count > 0)
                    {
                        lock (_subscriptions)
                        {
                            foreach (var item in _items)
                            {
                                _subscriptions.Add(item.Subscribe(value));
                            }
                        }
                    }
                    _observer = value;
                }
            }
        }

        SafeObservableCollection<Entity> _items = new SafeObservableCollection<Entity>();
        public ObservableCollection<Entity> Items
        {
            get
            {
                return _items;
            }
        }
        HashSet<IDisposable> _subscriptions = new HashSet<IDisposable>();
        public void LoadData(EntityType type, params IObserver<EntityBehavior>[] observers)
        {
            List<Entity> items = new List<Entity>();
            lock (_subscriptions)
            {
                foreach (var subscription in _subscriptions) subscription.Dispose();
                _subscriptions.Clear();

                foreach (DeclaredApp app in Altus.Core.Component.App.Instance.Apps.Where(a => !a.IsCore))
                {
                    if (app.Name != "GEM")
                    {
                        Context.CurrentContext.CurrentApp = app;
                        foreach (var entity in Entity.Select(type, this.ItemSize ?? "Large"))
                        {
                            foreach (var observer in observers)
                            {
                                _subscriptions.Add(entity.Subscribe(observer));
                            }
                            entity.Explorer = this;
                            items.Add(entity);
                        }
                    }
                }
            }
            lock (_items)
            {
                _items.Clear();
                _items.SuppressNotifications();
                _items.AddRange(items);
                _items.Refresh();
            }
        }


        string _itemSize;
        public string ItemSize
        {
            get { return _itemSize; }
            set { 
                _itemSize = value; 
                OnPropertyChanged("ItemSize");
                foreach (var item in _items) item.CurrentSize = value;
                _items.Refresh(); // causes bindings to update
            }
        }
    }
}
