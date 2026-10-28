using Marvel.Interfaces.Application;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
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
    /// Test_Train_SingleFaceControl.xaml 的交互逻辑
    /// </summary>
    public partial class Test_Train_SingleFaceControl : UserControl
    {
        protected IServiceProvider _serviceProvider;
        public Test_Train_SingleFaceControl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
        }

        private void SetImageControl()
        {
            var alignmentService = _serviceProvider.GetService<IAlignmentService>();
            wmsc.Init(alignmentService, alignmentService.WaferMapInfo);
            if (this.DataContext is Test_Train40ViewModelBase vm)
            {
                vm.HpImageViewer = StageControlTrainView.ImageViewer;
                vm.PositioningTemplateControl = positioningModelControl.TemplateControl;
                vm.PositioningTemplateControl.ImageViewer = StageControlTrainView?.ImageViewer;
                vm.InnerTemplateControl = innerModelTemplateControl;
                vm.OuterTemplateControl = outerModelTemplateControl;
                vm.Loaded();
            }
        }

        private void CheckBox_Click_Inner(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
                ((Test_Train40ViewModel_SingleFace)DataContext).CreateMeasureKernelSizeAndTemplate(true, true, ((Test_Train40ViewModel_SingleFace)DataContext).LoadFocusImageData(true));
        }

        private void CheckBox_Click_Outer(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
                ((Test_Train40ViewModel_SingleFace)DataContext).CreateMeasureKernelSizeAndTemplate(false, true, ((Test_Train40ViewModel_SingleFace)DataContext).LoadFocusImageData(false));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetImageControl();
        }
    }
}
