using HPImageViewer.Core.Persistence;
using ImTools;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe.CD;
using Megarobo.Ovl.Type;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinCopies.Util;

using Ovl.UI.Extensions;
namespace Ovl.UI.Recipe.ViewModel
{

    /// <summary>
    /// CD Train 参数页 ViewModel。
    /// 负责普通 CD 训练中的聚焦流程、聚焦图/量测图保存，以及量测 ROI 与定位模板训练的基础交互。
    /// 多 ROI 的设置、保存和重置逻辑由基类的 partial 文件扩展实现。
    /// </summary>
    public partial class Test_TrainCDParamViewModel : Test_TrainCDParamViewModelBase
    {
        public Test_TrainCDParamViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }
      
        public override void OpenDialogThread(IDialogParameters parameters)
        {
            base.OpenDialogThread(parameters);
            if (TestInfo != null)
            {
                if (IsEnableWLI && TestInfo.TrainSetting.FocusModuleInfo.WliFocusOffset != 0)
                {
                    AbsolateWliHeight = TestInfo.TrainSetting_CD.GetFocusHeight(EnumCDFocusOffsetType.Wli) + _alignmentService.MacroFocusZ;
                }
            }
        }
        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="hPImageViewer"></param>
        /// <param name="visionService"></param>
        /// <param name="testInfo"></param>
        /// <param name="visionNumber"></param>

        #region 事件

        #region Measurement Code



        /// <summary>
        /// 大Z建立聚焦曲线时Z轴步进调整
        /// Align相机用Z轴建立曲线的时候，step太小了导致z轴速度不稳定会超过align相机帧率导致丢图
        /// step太大了有可能会超出一个Pzt的行程，会对后续建立pzt曲线或者pzt聚焦有影响
        /// </summary>
        private void BoundaryTreatmentSmallStep(ref AutoRunInfo courseFocusInfo)
        {
            //if (_workCfg.ZStepMin > courseFocusInfo.SmallStep) courseFocusInfo.SmallStep = _workCfg.ZStepMin;
            //if (_workCfg.ZStepMax < courseFocusInfo.SmallStep) courseFocusInfo.SmallStep = _workCfg.ZStepMax;
        }
        #endregion
        #endregion
        #region focus 粗聚焦和精聚焦   #########
        /// <summary>
        /// 聚焦
        /// </summary>
        /// <param name="coarseFocusAction">如果是量测相机返回不带roi的粗聚焦训练+粗聚焦小步进方法  如果是Align相机返回带roi的粗聚焦方法+粗聚焦小步进方法</param>
        /// <param name="fineAutoFocusAction"> 如果是量测相机返回pzt精聚焦方法 如果是对准相机返回空，对准相机CD量测不需要精聚焦建模动作</param>
        protected override void TrainFocusAction()
        {
            if (!CheckFocusParam()) return;
            LogHelper.LogRunMethodStart();
            _changeCameraInfoAction?.Invoke("Focusing …");
            // 聚焦包含运动控制和采图耗时操作，放到后台线程执行，避免阻塞 Train 界面。
            Task.Run(() =>
            {
                try
                {
                    HpImageViewer.UpdateROIs();
                    _StageController.StartGrabbing();
                    var waferThickness = _alignmentService.WaferMapInfo.Thickness;
                    var focusInfo = GetTestFocusOrDefault_new(GetTestCamera());
                    var macroFocusZ = _alignmentService.MacroFocusZ;
                    //粗聚焦的开始位置为标定的聚焦位置+ 晶圆的厚度
                    var coarseFocusInfo = focusInfo.CourseFocusInfo;
                    coarseFocusInfo.StartPosition = macroFocusZ + waferThickness;

                    // CD 训练先以 Focus ROI 建立粗聚焦范围，后续再根据相机类型决定是否训练 PZT 曲线。
                    ShapeBase shapeBaseRoi = TrainSetting_CD.FocusRoiDesc.GetEnumerableValue(0).CreateRoi();
                    // 刷新界面的ROI颜色
                    TrainSetting_CD.FocusRoiDesc.ToList().ForEach(_ => _.Color = "#FFFF00");
                    HpImageViewer.UpdateROIs(TrainSetting_CD.FocusRoiDesc.ToArray());

                    if (_cameraType == CDCameraType.Align)
                    {
                        BoundaryTreatmentSmallStep(ref coarseFocusInfo);
                    }
                    var coarseFocus = _focusService.CoarseFocusWithImages(coarseFocusInfo, shapeBaseRoi, true);
                    FocusZHeight = coarseFocus.ZFit;
                    //将Z轴移动到粗聚焦位置
                    _alignmentService.MoveZTo(FocusZHeight, "TrainedFocusHeight", false);
                    MoveAndWait(null, null);


                    if (_cameraType == CDCameraType.Align)
                    {
                        // Align 相机不做 PZT 精聚焦；直接用粗聚焦序列训练曲线并保存同一张图作为聚焦图和量测图。
                        //根据量测Roi，训练两点曲线
                        var trainArg = CreateTrainArg(EnumGrabMode.SingleGrab);
                        var ret = _focusService.TrainCurve(coarseFocusInfo, shapeBaseRoi, coarseFocus.PegImages, trainArg);
                        TrainSetting_CD.GlobalCurve = ret.Curve;

                        var image = _alignmentService.MoveAndGrab(null);
                        string filePath = Path.Combine(TestFilesFolder, FocusImageName + ".bmp");
                        FileHelper.DeleteFile(filePath);
                        image.SaveBmp(filePath);

                        string fileMesaurePath = Path.Combine(TestFilesFolder, MeasureImageName + ".bmp");
                        FileHelper.DeleteFile(fileMesaurePath);
                        image.SaveBmp(fileMesaurePath);
                    }
                    else
                    {
                        // 量测相机需要先训练全视野曲线，再对 ROI 精聚焦；WLI 打开时额外记录白光焦面偏移。
                        //自动精聚焦
                        GetFineFocus(TrainSetting_CD.FocusRoiDesc);
                        TrainWLI("");
                        if (wliPztOffset != null)
                        {
                            AbsolateWliHeight = _pztservice.CombineZPzt(_motionControllerService.GetCurrentZ(), wliPztOffset.Value);
                            TrainSetting_CD.FocusModuleInfo.WliFocusOffset = wliPztOffset.Value - FocusPztOffset;
                        }
                        LogHelper.LogInfo($"Global pztOffset={fine_Res.ZFit},Sharpness pztOffset={FocusPztOffset},wliPztOffset={wliPztOffset}");
                    }
              
                    // BaseFocusHeight 存相对 MacroFocusZ 的高度，正式量测时再叠加机台当前宏观焦面。
                    AutoFocusHeight =_pztservice.CombineZPzt ( FocusZHeight , FocusPztOffset);
                    TrainSetting_CD.FocusModuleInfo.BaseFocusHeight = AutoFocusHeight - _alignmentService.MacroFocusZ;

                    LogHelper.LogInfo($"Focus Result:FocusFinalHeight={TrainSetting_CD.FocusModuleInfo.BaseFocusHeight:0.######}, AutoFocusHeight={AutoFocusHeight:0.######}");
                    isTrainedFocus = true;
                    _StageController.StopGrabbing();
                }
                catch (Exception ex)
                {
                    LogHelper.LogError(ex);
                    string errmsg = "Focusing failed. Please modify the focusing parameters and try again.";
                    AppHelper.DispatcherInvoke(() =>
                    {
                        _messageBoxService.ErrorDialog(errmsg);
                    });
                }
                finally
                {

                    _changeCameraInfoAction?.Invoke("");
                    HpImageViewer.UpdateROIs();
                    LogHelper.LogRunMethodEnd("", nameof(TrainFocusAction));
                }
            });

        }

        /// <summary>
        /// 如果是量测相机返回pzt精聚焦方法
        /// 如果是对准相机返回空，对准相机CD量测不需要精聚焦建模动作
        /// </summary>
        /// <returns></returns>
        private void GetFineFocus(IEnumerable<ROIDesc> roiDescs)
        {
            var cam = GetTestCamera();
            if (cam == ECamera.Meas)
            {
                // 精聚焦前回到 PZT 零位，避免上一次训练或人工调整带来的残余偏移。
                _pztservice?.AMov(0.0);
                HpImageViewer.UpdateROIs(roiDescs.ToArray());
                //全视野聚焦
                //全视野聚焦
                fine_Res = TrainGlobal();
                //采图前需要开启
                _visionService.StartGrabbing();
                // 使用全视野聚焦序列在 ROI 上重新找最佳清晰度，并保存聚焦/量测图。
                FocusPztOffset = TrainRoiFocus(fine_Res, false);

                _visionService.StopGrabbing();
            }
        }




        protected override void MoveAndWait(double? focusZHeight, double? pztHeight)
        {
            // 参数为 null 表示本次调用只等待或只移动另一轴，方便粗聚焦和精聚焦流程共用。
            if (focusZHeight != null)
            {
                _alignmentService.MoveZTo(focusZHeight.Value, "", false);
            }
            if (pztHeight != null)
            {
                _pztservice?.AMov(pztHeight.Value);
            }
        }



        #endregion
    }
}
