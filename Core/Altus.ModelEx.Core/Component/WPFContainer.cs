using Altus.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace Altus.Core.Component
{
    public abstract class WPFContainer : CompositionContainer
    {
        public Application Application { get; private set; }
        public Window MainWindow { get; private set; }
        public Window SplashWindow { get; private set; }
        public Window StartWindow { get; private set; }
        public string MainWindowName { get; private set; }
        //private Window BackgroundWindow { get; set; }
        protected override void OnLoad()
        {
            this.Application = OnCreateApplication();
            this.SplashWindow = OnCreateSplashWindow();

            if (this.SplashWindow != null)
            {
                //this.BackgroundWindow = new Window()
                //{
                //    Background = System.Windows.Media.Brushes.Black,
                //    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                //    WindowStyle = System.Windows.WindowStyle.None,
                //    WindowState = System.Windows.WindowState.Maximized,
                //    ShowInTaskbar = false,
                //    Topmost = true
                //};

                //this.BackgroundWindow.Show();

                this.SplashWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                this.SplashWindow.WindowStyle = WindowStyle.None;
                this.SplashWindow.ShowInTaskbar = false;
                this.SplashWindow.ShowActivated = true;
                this.SplashWindow.Topmost = true;
                this.StartWindow = this.SplashWindow;
                this.MainWindow = OnCreateMainWidow();
            }
            else
            {
                this.MainWindow = OnCreateMainWidow();
                this.StartWindow = this.MainWindow;
            }
            this.MainWindowName = this.MainWindow.Name;
            this.StartWindow.Show();
            
            this.MainWindow.Loaded += MainWindow_Loaded;

            Thread loader = new Thread(new ThreadStart(OnBackgroundLoad));
            loader.IsBackground = true;
            loader.Name = "Service Component Background Loader";
            loader.Start();
        }

        protected virtual void OnBackgroundLoad()
        {
            Context.CurrentContext = Context.GlobalContext;
            Logger.Log("Loading Background Components");
            this.ComponentLoader.LoadComponents();
        }

        protected abstract Application OnCreateApplication();
        protected abstract Window OnCreateMainWidow();
        protected virtual Window OnCreateSplashWindow() { return null; }

        protected override void OnLoadMainForm() { }

        protected override void OnLoadComplete()
        {
            if (this.SplashWindow != null)
            {
                this.SplashWindow.Dispatcher.Invoke(new Action(delegate()
                {
                    this.MainWindow.Show();
                }));
            }
            base.OnLoadComplete();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.SplashWindow != null)
            {
                this.SplashWindow.Dispatcher.Invoke(new Action(delegate()
                {
                    this.SplashWindow.Hide();
                    //this.BackgroundWindow.Close();
                }));
            }
        }

        protected override bool OnExit(bool forced)
        {
            bool ret = this.OnUnload(forced);
            if (MainWindow != null)
            {
                MainWindow.Close();
            }
            if (this.SplashWindow != null)
            {
                SplashWindow.Close();
            }
            //this.Application.Shutdown(0);
            return ret;
        }
    }
}
