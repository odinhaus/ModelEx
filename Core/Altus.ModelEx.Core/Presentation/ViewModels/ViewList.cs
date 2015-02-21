using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Altus.Core.Data;
using System.Collections;
using Altus.Core.Collections;

namespace Altus.Core.Presentation.ViewModels
{
    [StorageMapping("WindowView")]
    public class ViewList : ObservableCollection<View>
    {
        public ViewList() { }
        public ViewList(string windowName) { this.WindowName = windowName; }        
        public string WindowName { get; private set; }

        
    }
}
