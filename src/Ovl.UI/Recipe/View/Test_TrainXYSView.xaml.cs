using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using System;
using System.Windows;
using System.Windows.Controls;

using Ovl.UI.Recipe.ViewModel;
namespace Ovl.UI.Recipe.View
{
    /// <summary>
    /// Test_TrainXYSView.xaml 的交互逻辑
    /// </summary>
    public partial class Test_TrainXYSView : UserControl
    {
        public Test_TrainXYSView()
        {
            InitializeComponent();
            SetImageViewer();
            this.DataContextChanged += Test_TrainView_DataContextChanged;
        }

        Test_TrainXYSViewModel vm = null;
        private void SetImageViewer()
        {
            vm = this.DataContext as Test_TrainXYSViewModel;
            if (vm != null)
            {
                vm.SetControl(StageControlImageView?.ImageViewer, templatesView);
                vm.ActionTrainedFocus = () => wmsc.IsEnableMove = false;
            }
            var _serviceProvider = ContainerLocator.Current.Resolve<IServiceProvider>();
            var alignmentService = _serviceProvider.GetService<IAlignmentService>();
            wmsc.Init(alignmentService, alignmentService.WaferMapInfo);
        }

        private void Test_TrainView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetImageViewer();
        }

        private void CheckBox_Click_Inner(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
                vm.CreateMeasureKernelSizeAndTemplate(true, true);
        }

        private void CheckBox_Click_Outer(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
                vm.CreateMeasureKernelSizeAndTemplate(false, true);
        }
    }
}
