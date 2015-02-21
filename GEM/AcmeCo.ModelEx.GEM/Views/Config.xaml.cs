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
using System.Windows.Shapes;
using Altus.Core.Dynamic;
using Altus.Core.Presentation.ViewModels;
using Altus.GEM.ViewModels;

namespace Altus.GEM.Views
{
    /// <summary>
    /// Interaction logic for Config.xaml
    /// </summary>
    public partial class Config : Window
    {
        public Config(Entity view)
        {
            this.DataContext = view;
            DefaultConfig def = new DefaultConfig(view);
            view.AddProperty(new Core.Dynamic.DynamicProperty<Entity>(view, "Header", (object)"Config"));
            Items = new ObservableCollection<object>();
            Items.Add(def);
            Items.Add(view);
            InitializeComponent();
            Tabs.ItemsSource = Items;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public ObservableCollection<Object> Items { get; private set; }
    }

    

}
