using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// Test_TrainView.xaml 的交互逻辑
/// </summary>
public partial class Test_TrainView : UserControl
{
    public Test_TrainView()
    {
        InitializeComponent();
        SetImageViewer();
        this.DataContextChanged += Test_TrainView_DataContextChanged;
    }

    Test_TrainViewModel vm = null;
    private void SetImageViewer()
    {
        vm = this.DataContext as Test_TrainViewModel;
        if (vm != null)
        {
            vm.HpImageViewer = StageControlImageView?.ImageViewer;
            vm.StageFocusControlView = focusControlView;
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
            vm.CreateMeasureKernelSizeAndTemplate(true,true );
    }

    private void CheckBox_Click_Outer(object sender, RoutedEventArgs e)
    {
        if ((sender as CheckBox).IsChecked == true)
            vm.CreateMeasureKernelSizeAndTemplate(false,true );
    }
}
