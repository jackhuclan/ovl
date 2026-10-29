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
using HPImageViewer.Core;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.StageControlModule.Views;
using InteVega.CBB.TemplateMatchingModule.Views;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// Test_Train_DoubleFaceControl.xaml 的交互逻辑
/// </summary>
public partial class Test_Train_DoubleFaceControl : UserControl
{
    protected IServiceProvider _serviceProvider;

    public static IHPImageViewer GetMyProperty(DependencyObject obj)
    {
        return (IHPImageViewer)obj.GetValue(ImageViewer);
    }

    public static void SetMyProperty(DependencyObject obj, int value)
    {
        obj.SetValue(ImageViewer, value);
    }

    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageViewer =
        DependencyProperty.RegisterAttached("ImageViewer", typeof(int), typeof(Test_Train_DoubleFaceControl), new PropertyMetadata(new PropertyChangedCallback(ImageViewerChangeEvent)));

    private static void ImageViewerChangeEvent(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        LogHelper.LogInfo($"Image Viewer Bingindg Success for {nameof(Test_Train_DoubleFaceControl)}");
    }

    public Test_Train_DoubleFaceControl(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
        var alignmentService = _serviceProvider.GetService<IAlignmentService>();
        wmsc.Init(alignmentService, alignmentService.WaferMapInfo);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetImageControl();

    }
    private void SetImageControl()
    {
        if (DataContext is not Test_Train40ViewModel_DoubleFace vm) return;

        var trainViewer = StageControlTrainView.ImageViewer;
        var compareView = StageControlCompareView;

        // 获取测量视图
        var measView = compareView.MainImageViewControl.ImageViewer;
        var measExtView = compareView.SubImageViewControl.ImageViewer;

        // 设置图像查看器
        vm.HpImageViewer = trainViewer;
        vm.HpImageViewer_Inner = vm.InnerCamera == ECamera.Meas ? measView : measExtView;
        vm.HpImageViewer_Outer = vm.OuterCamera == ECamera.MeasExt ? measExtView : measView;

        // 设置定位模板
        vm.PositioningTemplateControl_Inner = positioningModelControl_Inner.TemplateControl;
        vm.PositioningTemplateControl_Inner.ImageViewer = trainViewer;

        vm.PositioningTemplateControl_Outer = positioningModelControl_Outer.TemplateControl;
        vm.PositioningTemplateControl_Outer.ImageViewer = trainViewer;

        // 设置模型模板
        vm.InnerTemplateControl = innerModelTemplateControl;
        vm.OuterTemplateControl = outerModelTemplateControl;
        // 设置影像区Tab页
        vm.TabMultiControl = this.TabMultiControl;

        // 初始化操作
        vm.Loaded();
        vm.StartGrabbing();
    }
}
