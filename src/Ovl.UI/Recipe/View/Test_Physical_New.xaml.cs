using System;
using System.Collections.Generic;
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

using Ovl.UI.Recipe.ViewModel;
namespace Ovl.UI.Recipe.View
{
    /// <summary>
    /// Test_Physical_New.xaml 的交互逻辑
    /// </summary>
    public partial class Test_Physical_New : UserControl
    {
        public Test_Physical_New()
        {
            InitializeComponent();
            this.Loaded += Test_DataCulling_Loaded;
            this.Unloaded += Test_DataCulling_Unloaded;
        }

        Test_SettingsViewModelBase vm;
        private void Test_DataCulling_Loaded(object sender, RoutedEventArgs e)
        {
            vm = (Test_SettingsViewModelBase)DataContext;
            if (vm != null)
                vm.Subscribe();
        }

        private void Test_DataCulling_Unloaded(object sender, RoutedEventArgs e)
        {
            vm.UnSubscribe();
        }
    }
}
