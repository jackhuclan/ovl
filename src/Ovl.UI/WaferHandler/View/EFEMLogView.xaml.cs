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

using Ovl.UI.WaferHandler.ViewModel;
namespace Ovl.UI.WaferHandler.View
{
    /// <summary>
    /// EFEMLogView.xaml 的交互逻辑
    /// </summary>
    public partial class EFEMLogView : UserControl
    {
        public EFEMLogView()
        {
            InitializeComponent();
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
        }
        private void LogGridOnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var  efemLogViewModel = DataContext as EFEMLogViewModel;
            if (efemLogViewModel == null) return;
            efemLogViewModel.Init();
        }
    }
}
