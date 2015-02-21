using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Altus.Core.Presentation.ViewModels;


namespace Altus.UI.ViewModels
{
    public class NavModule : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public NavModule(string windowName)
        {
            this.WindowName = windowName;
            this.Children = new List<NavModule>();
            this.Views = new List<View>();
            this.ViewSize = "tool";
        }

        public NavModule(string windowName, NavModule parent)
            : this(windowName)
        {
            this.Parent = parent;
            this.Parent.AddChild(this);
        }

        public NavModule(string windowName, dynamic backingInstance)
            : this(windowName)
        {
            this.BackingInstance = backingInstance;
        }

        public NavModule(string windowName, dynamic backingInstance, NavModule parent)
            : this(windowName, parent)
        {
            this.BackingInstance = backingInstance;
        }

        protected void AddChild(NavModule child)
        {
            ((List<NavModule>)Children).Add(child);
        }

        protected void RemoveChild(NavModule child)
        {
            ((List<NavModule>)Children).Remove(child);
        }

        public void AddView(View child)
        {
            ((List<View>)Views).Add(child);
        }

        public void RemoveView(View child)
        {
            ((List<View>)Views).Remove(child);
        }


        public NavModule Parent { get; private set; }
        public IEnumerable<View> Views { get; private set; }
        public IEnumerable<NavModule> Children { get; private set; }
        public dynamic BackingInstance { get; private set; }
        public string ViewSize { get; set; }
        public string WindowName { get; private set; }

        private string _caption;
        public string Caption 
        {
            get { return _caption; }
            set
            {
                _caption = value;
                OnPropertyChanged("Caption");
            }
        }

        private BitmapImage _image;
        public BitmapImage Image 
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged("Image");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return this.Caption;
        }
    }
}
