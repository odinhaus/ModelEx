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
using Altus.Core.Dynamic;
using Altus.Core.Licensing;
using Altus.GEM.ViewModels;
using Altus.UI.Controls;

namespace Altus.GEM.Views
{
    /// <summary>
    /// Interaction logic for Explorer_body.xaml
    /// </summary>
    public partial class Explorer_body : UserControl, IObserver<EntityBehavior>
    {
        public Explorer_body()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ((Explorer)this.DataContext).Observer = this;
        }

        private void BodyContent_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >=2
                && BodyContent.SelectedItem != null)
            {
                ((Entity)BodyContent.SelectedItem).HandleExecute();
            }
        }

        private void MenuExecute_Click(object sender, RoutedEventArgs e)
        {
            ((Entity)BodyContent.SelectedItem).HandleExecute();
        }

        private void MenuProperties_Click(object sender, RoutedEventArgs e)
        {
            this.ConfigBg.Visibility = Visibility.Visible;
            //Config config = new Config((Entity)BodyContent.SelectedItem);
            //config.Owner = Window.GetWindow(this);
            //config.ShowDialog();
            ShowConfigPad((Entity)BodyContent.SelectedItem);
            this.ConfigBg.Visibility = Visibility.Collapsed;
        }

        private void BodyContent_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1
                && BodyContent.SelectedItem != null)
            {
                Entity item = (Entity)BodyContent.SelectedItem;
                DataTemplate dt;
                ((MenuItem)BodyContent.ContextMenu.Items[0]).IsEnabled = item.CanExecute;
                ((MenuItem)BodyContent.ContextMenu.Items[2]).IsEnabled = item.TrySelectTemplate(new Grid(), "Config", out dt);

                this.BodyContent.ContextMenu.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void KeyPadOk_Click_1(object sender, RoutedEventArgs e)
        {
            HideConfigPad();
        }

        private void KeyPadCancel_Click_1(object sender, RoutedEventArgs e)
        {
            HideConfigPad();
        }

        private void HideConfigPad()
        {
            if (this.ConfigContainer.Visibility == System.Windows.Visibility.Visible)
            {
                this.ConfigContainer.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void ShowConfigPad(Entity view)
        {
            DefaultConfig def = new DefaultConfig(view);
            view.AddProperty(new Core.Dynamic.DynamicProperty<Entity>(view, "Header", (object)"Config"));
            this.ConfigItems = new ObservableCollection<object>();
            this.ConfigItems.Add(def);
            this.ConfigItems.Add(view);
            this.Tabs.ItemsSource = ConfigItems;
            this.ConfigContainer.Visibility = System.Windows.Visibility.Visible;
        }

        public ObservableCollection<Object> ConfigItems { get; private set; }

        private void SmallGripper_Moving_1(object sender, MovingEventArgs e)
        {
            TransformGroup transforms = (TransformGroup)this.ConfigContainer.RenderTransform;
            TranslateTransform translate = transforms.Children.OfType<TranslateTransform>().FirstOrDefault();
            if (translate != null)
            {
                double x = translate.X + e.Offset.X;
                double y = translate.Y + e.Offset.Y;
                translate.X = x;
                translate.Y = y;
            }
        }



        public void OnCompleted()
        {
            
        }

        public void OnError(Exception error)
        {
            MessageBox.Show(error.ToString());
        }

        public void OnNext(EntityBehavior value)
        {
            
        }

       
    }

    public class DefaultConfig : Extendable<DefaultConfig>
    {
        public DefaultConfig(object backingInstance) : base("Config", backingInstance) { }
        protected override IEnumerable<string> OnGetAliases()
        {
            return new string[0];
        }

        protected override IEnumerable<DynamicProperty<DefaultConfig>> OnGetProperties()
        {
            return new DynamicProperty<DefaultConfig>[]
            { 
                new DynamicProperty<DefaultConfig>(this, "Header", (object)"Default")
            };
        }

        protected override IEnumerable<DynamicFunction<DefaultConfig>> OnGetFunctions()
        {
            return new DynamicFunction<DefaultConfig>[0];
        }

        protected override string OnGetInstanceType()
        {
            return "Config";
        }
    }
}
