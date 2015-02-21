using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Altus.Core;
using Altus.Core.Presentation.ViewModels;
using Altus.Core.Presentation.Wpf.Views;
using Altus.UI.ViewModels;
using Altus.Core.Presentation.Wpf;
using Altus.Core.Presentation.Commands;
using Altus.Core.Licensing;

namespace Altus.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ShellWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Altus.Core.Component.App.Instance.Exit();
        }

        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        bool _isLicensed = false;
        protected override void OnApplyLicensing(ILicense[] licenses, params string[] args)
        {
            foreach (ILicense license in licenses)
            {
                string outNode;
                if (license.TryGetToken<string>("//Product[text()='ModelEx']", out outNode))
                {
                    _isLicensed = true;
                    break;
                }
            }
            if (_isLicensed)
            {
                //Altus.Core.Data.DataContext.Default.CreateWPFNode(this.WindowName);
            }
        }

        protected override bool OnIsLicensed(object component)
        {
            if (component == this) return _isLicensed;
            else return true;
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount >= 2)
                {
                    this.WindowState = System.Windows.WindowState.Maximized;
                }
                else
                {
                    if (this.WindowState == System.Windows.WindowState.Maximized)
                    {

                        System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(
                               new System.Windows.Interop.WindowInteropHelper(this).Handle);
                        this.Width = screen.WorkingArea.Width;
                        this.Height = screen.WorkingArea.Height;
                        this.Top = 0;
                        this.Left = 0;
                        this.WindowState = System.Windows.WindowState.Normal;
                    }
                    this.DragMove();
                }
            }
        }

        private void ShellWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Altus.Core.Component.App.Instance.Shell.ComponentLoader.LoadComplete += ComponentLoader_LoadComplete;
            Altus.Core.Component.App.Instance.Shell.ComponentLoader.LoadStatus += ComponentLoader_LoadStatus;
        }

        void ComponentLoader_LoadStatus(Core.Component.ComponentLoadStatusEventArgs e)
        {
            Dispatcher.Invoke(new Action<int, int>(UpdateLoadStatus), e.Index, e.Count);
        }

        void UpdateLoadStatus(int value, int max)
        {
            this.ProgressLoading.Visibility = System.Windows.Visibility.Visible;
            this.ProgressLoading.Maximum = max;
            this.ProgressLoading.Value = value;
        }

        void ComponentLoader_LoadComplete()
        {
            Dispatcher.Invoke(new Action(LoadViews));
        }

        private void LoadViews()
        {
            //this.BlurContainer.Visibility = System.Windows.Visibility.Visible;
            this.ProgressLoading.Visibility = System.Windows.Visibility.Collapsed;
            this.Loading.Visibility = System.Windows.Visibility.Collapsed;
            ObservableCollection<NavModule> modules = (ObservableCollection<NavModule>)this.Modules.ItemsSource;
            if (modules == null)
            {
                modules = new ObservableCollection<NavModule>();
                this.Modules.ItemContainerGenerator.ItemsChanged += ItemContainerGenerator_ItemsChanged;
                this.Modules.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
                this.Modules.ItemsSource = modules;
            }

            foreach (string viewType in base.GetViewTypes())
            {
                NavModule root = modules.Where(nm => nm.Caption.Equals(viewType)).FirstOrDefault();
                if (root == null)
                {
                    root = new NavModule(this.WindowName) { Caption = viewType, ViewSize = "tool" };
                }
                foreach (View view in base.GetViews(viewType, root.ViewSize))
                {
                    if (!root.Views.Contains(view))
                    {
                        AddView(view, root);
                    }
                }
                if (root.Views.Count() > 0)
                    modules.Add(root);
            }
            if (modules.Count > 0)
            {

                if (modules.Count == 1 && modules[0].Views.Count() == 1)
                {
                    this.Navigation.Visibility = System.Windows.Visibility.Collapsed;
                    OnViewSelected(modules[0].Views.First());
                }
                else if (modules.Count >= 1 || modules[0].Views.Count() > 1)
                {
                    this.Navigation.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                this.Navigation.Visibility = System.Windows.Visibility.Collapsed;
                this.NoApps.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void AddView(View view, NavModule root)
        {
            Command cmd = new Command(new Action<object>(OnViewSelected), true);
            cmd.Tag = view;
            cmd.Executing += Command_Executing;
            view.AddCommand("Select", cmd);
            root.AddView(view);
        }

        void Command_Executing(object sender, ExecutingCommandEventArgs e)
        {
            e.Parameter = e.Command.Tag;
        }

        void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (((ItemContainerGenerator)sender).Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {

            }
        }

        void ItemContainerGenerator_ItemsChanged(object sender, System.Windows.Controls.Primitives.ItemsChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {

            }
        }

        void OnViewSelected(object view)
        {
            if (view != this.CurrentView)
            {
                Context.CurrentContext.CurrentApp = ((WPFView)view).App;
                DependencyObject dp = ((WPFView)view).GetDependencyObject("tool");
                if (dp != null)
                {
                    ListBoxItem lbi = dp.FindParent<ListBoxItem>() as ListBoxItem;
                    if (lbi != null)
                    {
                        lbi.IsSelected = true;
                    }
                }

                WPFView viewBody = (WPFView)base.GetView(((View)view).Name, "body");
                this.Body.ItemTemplate = viewBody.SelectTemplate(this.Body);
                this.Body.ItemsSource = new View[] { (View)viewBody };

                if (viewBody is ISupportsStatus)
                {
                    ISupportsStatus iss = viewBody as ISupportsStatus;
                    iss.RegisterMainStatus(new UpdateTextStatusHandler(delegate(string message)
                    {
                        Dispatcher.Invoke(new UpdateTextStatusHandler(delegate(string msg) { this.sbMessage.Text = msg; }), message);
                    }));
                    iss.RegisterSecondaryStatus(new UpdateTextStatusHandler(delegate(string message)
                    {
                        Dispatcher.Invoke(new UpdateTextStatusHandler(delegate(string msg) { this.tbStats.Text = msg; }), message);
                    }));
                    iss.RegisterIndeterminateProgressStatus(new UpdateIndeterminateProgressStatusHandler(delegate(bool isOn)
                    {
                        Dispatcher.Invoke(new UpdateIndeterminateProgressStatusHandler(delegate(bool on) { this.pbActivity.IsIndeterminate = on; }), isOn);
                    }));
                    iss.RegisterProgressStatus(new UpdateProgressStatusHandler(delegate(int min, int max, int value)
                    {
                        Dispatcher.Invoke(new UpdateProgressStatusHandler(delegate(int m, int x, int v) { this.pbActivity.Minimum = m; this.pbActivity.Maximum = x; this.pbActivity.Value = v; }), min, max, value);
                    }));
                }

                this.CurrentView = (View)view;
                Dispatcher.Invoke(new Action(delegate() { this.Navigation.IsExpanded = false; }));
            }
        }

        public View CurrentView { get; private set; }
    }
}
