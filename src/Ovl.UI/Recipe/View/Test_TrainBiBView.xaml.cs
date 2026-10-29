using System.Windows;
using System.Windows.Controls;
using InteVega.CBB.TemplateMatching.Core;
using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// TrainBiBView.xaml 的交互逻辑
/// </summary>
public partial class Test_TrainBiBView : UserControl
{
    public Test_TrainBiBView()
    {
        InitializeComponent();
        SetImageViewer();
        this.DataContextChanged += Test_TrainView_DataContextChanged;

    }





    Test_TrainBiBViewModel vm = null;
    private void SetImageViewer()
    {
        vm = this.DataContext as Test_TrainBiBViewModel;
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
