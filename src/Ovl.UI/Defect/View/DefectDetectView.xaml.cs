using InteVega.CBB.CommonLib.Helpers;
using Marvel.DataModels.Defect;
using Marvel.DataModels.Recipe;
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

using Ovl.UI.Defect.ViewModel;
using Ovl.UI.Recipe.ViewModel;
namespace Ovl.UI.Defect.View
{
    /// <summary>
    /// DefectDetectView.xaml 的交互逻辑
    /// </summary>
    public partial class DefectDetectView : UserControl
    {
        public DefectDetectView()
        {
            InitializeComponent();
            StageControlImageView.SetProductMode(false);
            SetViewModel();
            DataContextChanged += DefectDetectView_DataContextChanged;
            Loaded += DefectDetectView_Loaded;
            Unloaded += DefectDetectView_Unloaded;
        }
        DefectDetectViewModel vm;
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        private void DefectDetectView_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Elapsed -= Timer_Elapsed;
            timer.Stop();
        }

        private void DefectDetectView_Loaded(object sender, RoutedEventArgs e)
        {
            //timer.Elapsed -= Timer_Elapsed;
            //timer.Elapsed += Timer_Elapsed;
            //timer .Start ();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (vm == null) return;
            AppHelper.DispatcherInvoke(() =>
            {
                var point = vm.GetAxisPoint();
                Canvas.SetLeft(pathCross, point.X);
                Canvas.SetTop(pathCross, point.Y);
            });
        }

        private void DefectDetectView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetViewModel();
        }

        private void SetViewModel()
        {
            vm = DataContext as DefectDetectViewModel;
            if (vm != null)
            {
                vm.WaferStageControl = wmsc;
                vm.ImageViewer = hpImage;
            }
        }

        private void btnGetR3_Click(object sender, RoutedEventArgs e)
        {
            vm?.GetR3();
        }

        private void btnGoto_Click(object sender, RoutedEventArgs e)
        {
            vm?.Goto();
        }

        private void targetinfos_LoadingRow(object sender, DataGridRowEventArgs e)
        {

        }

        private void targetinfos_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            vm.SelectedDefect = (e.OriginalSource as FrameworkElement).DataContext as DefectInfo;
        }

        private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void Grid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(sender as FrameworkElement);
            vm.MoveTo(p);
        }
    }
}
