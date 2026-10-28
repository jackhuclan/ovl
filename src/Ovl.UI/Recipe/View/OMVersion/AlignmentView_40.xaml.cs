using InteVega.CBB.CommonLib.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Ovl.UI.Recipe.ViewModel.OMVersion;
namespace Ovl.UI.Recipe.View.OMVersion
{
    /// <summary>
    /// AlignmentView_40.xaml 的交互逻辑
    /// </summary>
    public partial class AlignmentView_40 : UserControl
    {
        ~AlignmentView_40() 
        { 
        }
        public AlignmentView_40()
        {
            InitializeComponent();
            SetImageViewer();
            this.DataContextChanged += AlignmentView_DataContextChanged;
            this.Loaded += AlignmentView_Loaded;
        }

        AlignmentVm_40 vm;
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        private void edgeSel_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Grid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(sender as FrameworkElement);
            vm.MoveTo(p);
        }

        private void alignmentControl_Unloaded(object sender, RoutedEventArgs e)
        {

            timer.Elapsed -= Timer_Elapsed;
            timer.Stop();
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
        private void SetImageViewer()
        {
            if (this.DataContext is AlignmentVmBase vm)
            {
                vm.HpImageViewer = StageControlImageView?.ImageViewer;
            }
        }
        private void AlignmentView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetImageViewer();
        }
        private void AlignmentView_Loaded(object sender, RoutedEventArgs e)
        {
            AlignmentVmBase vm = this.DataContext as AlignmentVmBase;
            wmsc.Init(vm.AlignmentService, vm.RecipeElementsInfo.WaferMap);
            vm.WaferMapStageControl = wmsc;
        }

    }
}
