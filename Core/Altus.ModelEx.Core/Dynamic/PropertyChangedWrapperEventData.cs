using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Altus.Core.Dynamic
{
    internal class PropertyChangedWrapperEventData
    {
        public Dispatcher Dispatcher { get; set; }
        public Action<PropertyChangedEventArgs> Action { get; set; }
        public ISynchronizeInvoke SynchronizeInvoke { get; set; }


        public PropertyChangedWrapperEventData(Dispatcher dispatcher, Action<PropertyChangedEventArgs> action)
	    {
			Dispatcher = dispatcher;
			Action = action;
		}

        public PropertyChangedWrapperEventData(ISynchronizeInvoke syncronizeInvoke, Action<PropertyChangedEventArgs> action)
        {
            SynchronizeInvoke = syncronizeInvoke;
            Action = action;
        }
    }
}
