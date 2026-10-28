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
    /// Test_TisOptimize.xaml 的交互逻辑
    /// </summary>
    public partial class Test_TisOptimize : UserControl
    {
        public Test_TisOptimize()
        {
            InitializeComponent();
            this.Loaded += Test_Tis_Loaded;
            this.Unloaded += Test_Tis_Unloaded;
            this.IsVisibleChanged += Test_TisOptimize_IsVisibleChanged;
        }

        private void Test_TisOptimize_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                vm.SetXYSOptimizeBy();
            }
        }

        Test_TisOptViewModel vm;
        private void Test_Tis_Loaded(object sender, RoutedEventArgs e)
        {
            vm = (Test_TisOptViewModel)DataContext;
            vm.Subscribe();
        }

        private void Test_Tis_Unloaded(object sender, RoutedEventArgs e)
        {
            vm.UnSubscribe();
        }
    }
}
