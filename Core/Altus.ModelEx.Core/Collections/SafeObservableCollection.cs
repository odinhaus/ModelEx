using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Altus.Core.Collections
{
    public class SafeObservableCollection<T> : ObservableCollection<T>
    {
        private Dictionary<NotifyCollectionChangedEventHandler, CollectionChangedWrapperEventData> _handlers = new Dictionary<NotifyCollectionChangedEventHandler, CollectionChangedWrapperEventData>();

        public SafeObservableCollection() : base() { }

        public SafeObservableCollection(IEnumerable<T> source) : base(source) { }

        public SafeObservableCollection(List<T> source) : base(source) { }

        bool _suppressCascade = false;
        public void ReplaceRange(IEnumerable<T> items)
        {
            _suppressCascade = true;
            this.Clear();
            this.AddRange(items);
            _suppressCascade = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (T item in items)
                Add(item);
        }
        public void SuppressNotifications()
        {
            _suppressCascade = true;
        }
        public void Refresh()
        {
            _suppressCascade = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_suppressCascade) return;
            lock (_handlers)
            {
                foreach (KeyValuePair<NotifyCollectionChangedEventHandler, CollectionChangedWrapperEventData> kvp in _handlers)
                {
                    if (kvp.Value.Dispatcher == null
                        && kvp.Value.SynchronizeInvoke == null)
                        kvp.Value.Action(e);
                    else if (kvp.Value.Dispatcher == null)
                        kvp.Value.SynchronizeInvoke.Invoke(kvp.Value.Action, new object[] { e });
                    else
                        kvp.Value.Dispatcher.Invoke(kvp.Value.Action, DispatcherPriority.DataBind, e);
                }
            }
        }

        public override event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                CollectionChangedWrapperEventData wrapper;
                if (value.Target is ISynchronizeInvoke)
                {
                    wrapper = new CollectionChangedWrapperEventData((ISynchronizeInvoke)value.Target, new Action<NotifyCollectionChangedEventArgs>(args => value(this, args)));
                }
                else
                {
                    Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread); // experimental (can return null)...
                    wrapper = new CollectionChangedWrapperEventData(dispatcher, new Action<NotifyCollectionChangedEventArgs>(args => value(this, args)));
                }
                lock (_handlers)
                {
                    _handlers.Add(value, wrapper);
                }
            }
            remove
            {
                lock (_handlers)
                {
                    _handlers.Remove(value);
                }
            }
        }

        public static implicit operator SafeObservableCollection<T>(List<T> source)
        {
            return new SafeObservableCollection<T>(source);
        }
    }

    public static class SafeObservableCollectionEx
    {
        public static SafeObservableCollection<T> ToObservable<T>(this IEnumerable<T> source)
        {
            return new SafeObservableCollection<T>(source);
        }
    }

    internal class CollectionChangedWrapperEventData
	{

        public Dispatcher Dispatcher { get; set; }
        public Action<NotifyCollectionChangedEventArgs> Action { get; set; }
        public ISynchronizeInvoke SynchronizeInvoke { get; set; }


		public CollectionChangedWrapperEventData(Dispatcher dispatcher, Action<NotifyCollectionChangedEventArgs> action)
	    {
			Dispatcher = dispatcher;
			Action = action;
		}

        public CollectionChangedWrapperEventData(ISynchronizeInvoke syncronizeInvoke, Action<NotifyCollectionChangedEventArgs> action)
        {
            SynchronizeInvoke = syncronizeInvoke;
            Action = action;
        }

	}
}
