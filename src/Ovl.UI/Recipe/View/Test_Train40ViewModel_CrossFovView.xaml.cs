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
    /// LAngle OVL 跨视野 Train 视图：绑定主图、内外量测模板与内外定位模板控件。
    /// </summary>
    public partial class Test_Train40ViewModel_CrossFovView : UserControl
    {
        public Test_Train40ViewModel_CrossFovView()
        {
            InitializeComponent();
            Loaded += OnCrossFovViewLoaded;
        }

        private void OnCrossFovViewLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnCrossFovViewLoaded;
            BindImageAndTemplateControls();
            var serviceProvider = ContainerLocator.Current.Resolve<IServiceProvider>();
            var alignmentService = serviceProvider.GetService<IAlignmentService>();
            wmsc.Init(alignmentService, alignmentService.WaferMapInfo);
        }

        private void BindImageAndTemplateControls()
        {
            if (DataContext is not Test_Train40ViewModel_CrossFov vm)
                return;

            var imageViewer = StageControlImageView?.ImageViewer;
            vm.HpImageViewer = imageViewer;
            vm.InnerTemplateControl = innerModelTemplateControl;
            vm.OuterTemplateControl = outerModelTemplateControl;

            if (positioningModelControl?.TemplateControl != null)
            {
                vm.PositioningTemplateControl_Inner = positioningModelControl.TemplateControl;
                vm.PositioningTemplateControl_Inner.ImageViewer = imageViewer;
                vm.PositioningTemplateControl = positioningModelControl.TemplateControl;
                vm.PositioningTemplateControl.ImageViewer = imageViewer;
            }

            if (outerpositioningModelControl?.TemplateControl != null)
            {
                vm.PositioningTemplateControl_Outer = outerpositioningModelControl.TemplateControl;
                vm.PositioningTemplateControl_Outer.ImageViewer = imageViewer;
            }

            if (vm.TestInfo?.CrossFov == true)
            {
                vm.TestInfo.EnsureCrossFovTrainSettings();
                vm.BindCrossFovPositioningTemplates();
            }
        }
    }
}
