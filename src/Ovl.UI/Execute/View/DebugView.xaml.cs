using InteVega.CBB.Common;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.TemplateMatching.Core;
using Megarobo.Ovl.NumericWheel;
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
using Prism.Ioc;
using Microsoft.Extensions.DependencyInjection;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using InteVega.CBB.DomainService.Api.Interfaces;
using Megarobo.Ovl.Service.IntfFn.fn;

namespace Ovl.UI.Execute.View
{
    /// <summary>
    /// DebugView.xaml 的交互逻辑
    /// </summary>
    public partial class DebugView : UserControl
    {
        public DebugView()
        {
            InitializeComponent();

            stageControlImageView.SetProductMode(true);
            // 辅助弹窗关闭时不要 StopGrabbing，否则 Retry 取图会卡死
            stageControlImageView.KeepGrabbingOnUnload = true;
            stageMotionView.HideStations();
            stageFocus.Visibility = Visibility.Collapsed;
            tmc.Visibility = Visibility.Collapsed;
            serviceProvider = ContainerLocator.Current.Resolve<IServiceProvider>();
            _visionService = serviceProvider.GetService<IVisionServiceManager>().GetMainVision();
            _visionService.StartGrabbing();

        }
        public Action CloseBuzzer;
        IServiceProvider serviceProvider;
        IVisionService _visionService;
        /// <summary>
        /// 设置聚焦参数，并且显示聚焦信息
        /// </summary>
        /// <param name="focusInfo">聚焦信息</param>
        /// <param name="isPZT">是否是pzt聚焦</param>
        public void SetFocusInfo(AutoRunInfo focusInfo, bool isPZT)
        {
            if (focusInfo == null)
            {
                focusInfo = _visionService.GetSystemFocusInfo(IServiceCamera.CurCamera, isPZT);
            }
            if (focusInfo.ParamFocus == null)
                focusInfo.ParamFocus = new Wafer.Algorithm.Common.DoNet.ParamFocus();
            stageFocus.Visibility = Visibility.Visible;
            stageFocus.Init(focusInfo, isPZT);
        }

        /// <summary>
        /// 设置模板匹配列表
        /// </summary>
        /// <param name="temps"></param>
        public void SetTemplates(List<SingleTemplateMatchingContext> temps, bool isEnableMove = true, bool isRotate180 = false)
        {
            if (temps == null || temps.Count == 0) { return; }
            tmc.Visibility = Visibility.Visible;
            if (!isEnableMove)
                stageMotionView.Visibility = Visibility.Collapsed;
            MultiTemplateMatchingContext context = new MultiTemplateMatchingContext();
            context.TemplateContexts = temps;
            var tempViewModel = serviceProvider.CreateTemplateViewModel(context, stageControlImageView.ImageViewer, isRotate180) as MultiTemplateMatchingViewModel;
            tempViewModel.IsShowCreateModel = false;
            tmc.DataContext = tempViewModel;
        }



        private void NumericUpDown_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            NumericWheel.MouseWheel(sender, e);
        }

       
    }
}
