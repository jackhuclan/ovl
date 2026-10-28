using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.TemplateMatching.Core;
using Marvel.Application.Extentions;
using Marvel.DataModels;
using Marvel.DataModels.DeviceParameter;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.DataModels.Recipe40.TestInfo;
using Marvel.DataModels40.DeviceParameter;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using OVL.Algorithm.DoNet;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

using Ovl.UI.ViewModels.Extensions;
namespace Ovl.UI.Recipe.ViewModel
{
    internal partial class Test_Train40ViewModel_DoubleFace : Test_Train40ViewModelBase
    {
        public List<ROIDesc> _cdOvlInnerRoiSnapshotBeforeEdgeOnlyMode;
        public List<ROIDesc> _cdOvlOuterRoiSnapshotBeforeEdgeOnlyMode;

        private readonly IVisionServiceManager _visionServiceManager;
        private readonly IServicePegGrab _servicePegGrab;
        protected override ECamera FirstCamera => InnerCamera;
        public ECamera InnerCamera => _machine40Config.PatternCameraCfg.InnerCamera;
        public ECamera OuterCamera => _machine40Config.PatternCameraCfg.OuterCamera;

        public AxisType InnerAxis => _machine40Config.PatternCameraCfg.InnerCamera == ECamera.Meas ? _machine40Config.PatternCameraCfg.MeasCamAxis : _machine40Config.PatternCameraCfg.MeasCamExtAxis;
        public AxisType OuterAxis => _machine40Config.PatternCameraCfg.OuterCamera == ECamera.Meas ? _machine40Config.PatternCameraCfg.MeasCamAxis : _machine40Config.PatternCameraCfg.MeasCamExtAxis;

        public IHPImageViewer HpImageViewer_Inner { get; set; }
        public IHPImageViewer HpImageViewer_Outer { get; set; }

        public IVisionService VisionMainTrain => _visionService;
        public IVisionService VisionInner => _visionServiceManager.GetVision(InnerCamera);
        public IVisionService VisionOuter => _visionServiceManager.GetVision(OuterCamera);

        public FocusPegResult FineResInner { get; set; }
        public FocusPegResult FineResOuter { get; set; }

        /// <summary>
        /// Inner定位模板数据
        /// </summary>
        protected PositioningTemplateInfo PositioningModel_Inner
        {
            get { return TestInfo.TrainSetting_DoubleFace.PositioningTemplateInfo_Inner; }
        }

        /// <summary>
        /// Outer定位模板数据
        /// </summary>
        protected PositioningTemplateInfo PositioningModel_Outer
        {
            get { return TestInfo.TrainSetting_DoubleFace.PositioningTemplateInfo_Outer; }
        }

        protected override PositioningTemplateInfo PositioningModel
        {
            get { return _isInner ? TestInfo.TrainSetting_DoubleFace.PositioningTemplateInfo_Inner : TestInfo.TrainSetting_DoubleFace.PositioningTemplateInfo_Outer; }
        }

        protected override PositioningTemplateInfo GetPositioningModel(bool isInner)
        {
            return isInner ? PositioningModel_Inner : PositioningModel_Outer;
        }

        /// <summary>
        /// 选择的Tab页索引，0表示Train，1表示Compare
        /// </summary>
        [ObservableProperty]
        private int _selectedTabIndex;
        public System.Windows.Controls.TabControl TabMultiControl;
        private void ShowMeasurementCompareView()
        {
            // Compare 视图默认只显示主相机，进入 Measurement 时需要显式恢复双相机显示。
            alignmentService.ChangeCamera(OuterCamera, false);
            _visionServiceManager.ChangeToBothServiceView();
        }
        private void TabChange(bool isTrain)
        {
            SelectedTabIndex = isTrain ? 0 : 1;
            // 切换tab页后保证tab控件重新刷新一下，保证图片显示完整
            TabMultiControl.UpdateLayout();
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            if (SelectedTabIndex == 1)
            {
                // 如果当前相机即主视窗是OuterCamera（下相机），就转换成上相机
                ShowMeasurementCompareView();
            }
        }

        protected override FocusInfo FocusInfo
        {
            get
            {
                if (TestInfo == null)
                    throw new InvalidOperationException("TestInfo cannot be null");
                var doubleFaceSetting = TestInfo.TrainSetting_DoubleFace;
                if (doubleFaceSetting == null) throw new InvalidCastException("Cannot cast TrainSetting to TrainSettings40_DoubleFace");
                return _isInner ? doubleFaceSetting.FocusModuleInfoInner.FocusInfo : doubleFaceSetting.FocusModuleInfoOuter.FocusInfo;
            }
            set
            {
                var doubleFaceSetting = TestInfo?.TrainSetting_DoubleFace;
                if (doubleFaceSetting == null)
                    throw new InvalidCastException("Cannot cast TrainSetting to TrainSettings40_DoubleFace");
                if (_isInner)
                    doubleFaceSetting.FocusModuleInfoInner.FocusInfo = value;
                else
                    doubleFaceSetting.FocusModuleInfoOuter.FocusInfo = value;
            }
        }

        public Test_Train40ViewModel_DoubleFace(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _visionServiceManager = serviceProvider.GetService<IVisionServiceManager>();
            _servicePegGrab = serviceProvider.GetService<IServicePegGrab>();
        }

        public TrainSettings40_DoubleFace TrainSettingsDoubleFace
        {
            get
            {
                return TestInfo?.TrainSetting as TrainSettings40_DoubleFace;
            }
        }

        public TestOverlayTrainParam TrainParam
        {
            get
            {
                return (TestInfo.TrainSetting as TrainSettings_BiB).TrainParam;
            }
        }
        protected override void SetVisionParams(ECamera camera = ECamera.Meas)
        {
            try
            {
                var trainSetting = ((TrainSettings40_DoubleFace)TestInfo.TrainSetting);
                var cameraParam = camera == ECamera.MeasExt ? TrainSettingsDoubleFace.CameraParamsInner : TrainSettingsDoubleFace.CameraParamsOuter;
                var lightSourceParam = camera == ECamera.MeasExt ? TrainSettingsDoubleFace.LightSourceParamInner : TrainSettingsDoubleFace.LightSourceParamOuter;
                _cameraLightParamService40.SetupCameraAndLightParametersNormal(TestInfo, camera);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.Error(ex.Message);
            }

            VisionMainTrain.NotifyParamsChanged();
        }

        protected override async Task TrainFocus_SingleMethod()
        {
            LogHelper.LogRunMethodStart();
            ChangeCameraInfo = "Focusing …";
            await Task.Run(() =>
            {
                try
                {
                    VisionMainTrain.StartGrabbing();
                    if (_isInner) ProcessInnerFocus(false);
                    else ProcessOuterFocus(false);
                    IsTrained = false;
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
                    ChangeCameraInfo = "";
                    HpImageViewer.UpdateROIs();
                    LogHelper.LogRunMethodEnd("", nameof(TrainFocus_SingleMethod));
                }
            });
        }

        protected override FocusInfo GetTestFocusOrDefault(ECamera camera = ECamera.Meas)
        {
            return FocusInfo ?? _serviceProvider.GetService<ITestService>().GetSystemFocusInfo(camera);
        }

        /// <summary>
        /// 处理InnerRoi聚焦动作
        /// </summary>
        /// <param name="isMeasure"></param>
        private void ProcessInnerFocus(bool isMeasure)
        {
            if (TrainParam.InnerLayer.FocusROI == null)
            {
                _messageBoxService.ErrorDialog("Please draw the inner corresponding ROI！");
                return;
            }
            HpImageViewer.UpdateROIs();
            var visionService = _visionServiceManager.GetVision(InnerCamera);
            var focusInfo = CreateFullTrainInfo(InnerCamera);
            // 用当前位置为聚焦起始点
            focusInfo.StartPosition = _servPlat.GetCurrentZ(InnerAxis);
            LogHelper.LogInfo($"Focus Info_ProcessInnerFocus : CoarseFocus ={focusInfo}");
            var innerResult = ExecuteInnerFocus(focusInfo);
            FocusInnerWithImages(innerResult.ROI, false);
            visionService.StopGrabbing();
        }

        /// <summary>
        /// 执行InnerRoi聚焦动作
        /// </summary>
        /// <param name="waferThickness"></param>
        /// <param name="coarseFocusInfo"></param>
        /// <returns></returns>
        private (ShapeBase ROI, double FocusHeight) ExecuteInnerFocus(AutoRunInfo coarseFocusInfo)
        {
            var ret = ExecuteFocusCommon(InnerCamera, TrainParam.InnerLayer.FocusROI, coarseFocusInfo, InnerColor);
            InnerFocusHeight = ret.FitHeight;
            FineResInner = ret.pegRet;
            return (ret.ROI, InnerFocusHeight);
        }

        /// <summary>
        /// 执行序列图InnerRoi聚焦
        /// </summary>
        /// <param name="innerROI"></param>
        private void FocusInnerWithImages(ShapeBase innerROI, bool isMeasure)
        {
            ProcessInnerFocusCommon(
                innerROI,
                FineResInner,
                TrainSettingsDoubleFace,
                height =>
                {
                    VisionInner.StartGrabbing();
                    _servPlat.MoveZ(InnerAxis, height);
                    VisionInner.StopGrabbing();
                },
                isMeasure);
        }

        /// <summary>
        /// 处理OuterRoi聚焦动作
        /// </summary>
        /// <param name="isMeasure"></param>
        private void ProcessOuterFocus(bool isMeasure)
        {
            if (TrainParam.OuterLayer.FocusROI == null)
            {
                _messageBoxService.ErrorDialog("Please draw the inner corresponding ROI！");
                return;
            }
            var visionService = _visionServiceManager.GetVision(OuterCamera);
            HpImageViewer.UpdateROIs();
            visionService.StartGrabbing();
            var focusInfo = CreateFullTrainInfo(OuterCamera);
            // 开始位置为标定的聚焦位置无需加晶圆厚度
            focusInfo.StartPosition = _servPlat.GetCurrentZ(OuterAxis);
            LogHelper.LogInfo($"Focus Info_ProcessOuterFocus : CoarseFocus ={focusInfo}");
            var outerResult = ExecuteOuterFocus(focusInfo);
            FocusOuterWithImages(outerResult.ROI, false);
            visionService.StopGrabbing();
        }

        /// <summary>
        /// 执行OuterRoi聚焦动作
        /// </summary>
        /// <param name="waferThickness"></param>
        /// <param name="coarseFocusInfo"></param>
        /// <returns></returns>
        private (ShapeBase ROI, double FocusHeight) ExecuteOuterFocus(AutoRunInfo coarseFocusInfo)
        {
            // 外层粗聚焦
            var ret = ExecuteFocusCommon(OuterCamera, TrainParam.OuterLayer.FocusROI, coarseFocusInfo, OuterColor);
            OuterFocusHeight = ret.FitHeight;
            FineResOuter = ret.pegRet;
            return (ret.ROI, OuterFocusHeight);
        }

        /// <summary>
        /// 执行序列图OuterRoi聚焦
        /// </summary>
        /// <param name="outerRoi"></param>
        private void FocusOuterWithImages(ShapeBase outerRoi, bool isMeasure)
        {
            ProcessOuterFocusCommon(
                outerRoi,
                FineResOuter,
                TrainSettingsDoubleFace,
                height =>
                {
                    VisionInner.StartGrabbing();
                    _servPlat.MoveZ(OuterAxis, height);
                    VisionInner.StopGrabbing();
                },
                isMeasure);
        }

        private void UpdateFocusHeightSharpness(ImageData innerImage, ImageData outerImage, double inner_globalHeight, double outer_globalHeight)
        {
            var innerParamFocus = GetCoarseParamFocus(InnerCamera);
            var outerParamFocus = GetCoarseParamFocus(OuterCamera);
            var sharpness_inner = _focusService.GetImageClarity(innerImage, paramFocus: innerParamFocus);
            var sharpness_outer = _focusService.GetImageClarity(outerImage, paramFocus: outerParamFocus);

            var roiSharpness_Inner = _focusService.GetImageClarity(innerImage, TrainParam.InnerLayer.MeasureROI.CreateRoi(), innerParamFocus);
            var roiSharpness_Outer = _focusService.GetImageClarity(innerImage, TrainParam.OuterLayer.MeasureROI.CreateRoi(), outerParamFocus);

            // 根据训练设置类型保存清晰度
            if (TrainSettingsDoubleFace is TrainSettings40_DoubleFace doubleFaceSettings)
            {
                doubleFaceSettings.FocusModuleInfo.Sharpness = sharpness_inner;
                doubleFaceSettings.FocusModuleInfoOuter.Sharpness = sharpness_outer;
                doubleFaceSettings.FocusModuleInfo.RoiSharpness = roiSharpness_Inner;
                doubleFaceSettings.FocusModuleInfoOuter.RoiSharpness = roiSharpness_Outer;
                var offsetZInner = InnerAxis == AxisType.Z ? _alignmentService40.WaferMapInfo.Thickness : 0;
                // 保存清晰度最佳焦面与基准焦面的偏移量
                doubleFaceSettings.FocusModuleInfoInner.BaseFocusHeight = InnerFocusHeight - _alignmentService40.GetMacroFocusZ(_machine40Config.PatternCameraCfg.InnerCamera).z - offsetZInner;
                doubleFaceSettings.FocusModuleInfoInner.GlobalFocusOffset = InnerFocusHeight - inner_globalHeight;
                doubleFaceSettings.InnerHeight = InnerFocusHeight;

                // 保存清晰度最佳焦面与基准焦面的偏移量
                var offsetZOuter = OuterAxis == AxisType.Z ? _alignmentService40.WaferMapInfo.Thickness : 0;
                doubleFaceSettings.FocusModuleInfoOuter.BaseFocusHeight = OuterFocusHeight - _alignmentService40.GetMacroFocusZ(_machine40Config.PatternCameraCfg.OuterCamera).z - offsetZOuter;
                doubleFaceSettings.FocusModuleInfoOuter.GlobalFocusOffset = OuterFocusHeight - outer_globalHeight;
                doubleFaceSettings.OuterHeight = OuterFocusHeight;
                // 记录聚焦结果
                LogHelper.LogInfo($"Focus Result:InnerFocusHeight={InnerFocusHeight:0.######},innerGlobalHeight={inner_globalHeight:0.######}," +
                                               $"OuterFocusHeight={OuterFocusHeight:0.######},outerGlobalHeight={outer_globalHeight:0.######}," +
                                               $"Offset_Inner_base_roi={TrainParam.InnerLayer.SharpnessFocusOffset:0.######}, Offset_Inner_base_global={doubleFaceSettings.FocusModuleInfoInner.GlobalFocusOffset:0.######}," +
                                               $"Offset_Outer_base_roi={TrainParam.OuterLayer.SharpnessFocusOffset:0.######}, Offset_Outer_base_global={doubleFaceSettings.FocusModuleInfoOuter.GlobalFocusOffset:0.######}");
            }
        }

        /// <summary>
        ///  获取晶圆厚度
        /// </summary>
        /// <param name="axisType"></param>
        /// <returns></returns>
        //private double GetWaferThickness(AxisType axisType)
        //{ 
        //    return axisType == AxisType.Z ? _alignmentService40.WaferMapInfo.Thickness : 0;
        //}

        protected override List<CorrelationCurve> GetCorrelationCurve(HObject imageObjectInner, HObject imageObjectOuter, HTuple hTuple)
        {
            var res = OMUtil.GetCorrelationCurve_Double(imageObjectInner, imageObjectOuter, hTuple, out List<CorrelationCurve> curves);
            return curves;
        }

        /// <summary>
        /// 展示Overlay量测结果曲线
        /// </summary>
        /// <param name="ovResult"></param>
        /// <param name="imgObjectInner"></param>
        /// <param name="imgObjectOuter"></param>
        /// <param name="matchItem_inner"></param>
        private void ShowResult(OvResultBase ovResult, HObject imgObjectInner, HObject imgObjectOuter, RectangleDesc roiDescInner, RectangleDesc roiDescOuter, OMROIBase omRoInner, OMROIBase omRoiOuter)
        {
            var oMParam = TestInfo.OMParam_Double;
            oMParam.ConvertToDict(out HTuple hTuple);
            // 底层算法是以外层为中心进行量测的，如果界面上是以内层为中心，此时的ovl值需要取反
            OvResultBase centeringOVL = TestInfo.TargetSetting.TargetCentering == EnumCalOverlayInOrOutEdge.Inner
                ? (OvResultBase)CloneHelper.ReflectionClone(ovResult)
                : ovResult;

            CorrelationCurvesList = GetCorrelationCurve(imgObjectInner, imgObjectOuter, hTuple);
            VisionInner.StopGrabbing();
            VisionOuter.StopGrabbing();
            //TabChange(false);
            // 处理内层和外层显示逻辑
            ProcessImageViewerCommon(HpImageViewer_Inner, imgObjectInner, omRoInner, oMParam.TargetTypeInner, roiDescInner);
            ProcessImageViewerCommon(HpImageViewer_Outer, imgObjectOuter, omRoiOuter, oMParam.TargetTypeOuter, roiDescOuter);
            LoadOvResult(centeringOVL);
            CaptureRawCdOvlResult(centeringOVL);
            AppendCdOvlCrosshairToMeasurementViewers(IsCdOvlCrossEditMode);
            ShowCurveControl();
        }

        /// <summary>
        /// DoubleFace 的 Measurement 页需要同时展示内层补偿十字，
        /// 不再沿用单面“仅当前层显示”的逻辑。
        /// </summary>
        protected override bool RequiresInnerCdOvlEdgeForDisplay()
        {
            return true;
        }

        /// <summary>
        /// DoubleFace 的 Measurement 页需要同时展示外层补偿十字，
        /// 不再沿用单面“仅当前层显示”的逻辑。
        /// </summary>
        protected override bool RequiresOuterCdOvlEdgeForDisplay()
        {
            return true;
        }

        /// <summary>
        /// 双面模式分别维护 inner/outer 两份 ROI 快照。
        /// </summary>
        protected override void ClearCdOvlRoiSnapshot()
        {
            _cdOvlInnerRoiSnapshotBeforeEdgeOnlyMode = null;
            _cdOvlOuterRoiSnapshotBeforeEdgeOnlyMode = null;
        }

        /// <summary>
        /// 进入 EdgeSelect 编辑态前，分别保存 inner/outer compare viewer 当前 ROI。
        /// </summary>
        protected override void CaptureCdOvlRoiSnapshotForRestore()
        {
            _cdOvlInnerRoiSnapshotBeforeEdgeOnlyMode = HpImageViewer_Inner?.ImageViewerDesc?.ROIDescs?.ToList() ?? new List<ROIDesc>();
            _cdOvlOuterRoiSnapshotBeforeEdgeOnlyMode = HpImageViewer_Outer?.ImageViewerDesc?.ROIDescs?.ToList() ?? new List<ROIDesc>();
        }

        /// <summary>
        /// 恢复双面 compare viewer 在进入 EdgeSelect 前的 ROI 快照。
        /// </summary>
        protected override bool TryRestoreCdOvlSnapshotBeforeEdgeOnlyMode()
        {
            bool restored = false;
            if (_cdOvlInnerRoiSnapshotBeforeEdgeOnlyMode?.Count > 0)
            {
                HpImageViewer_Inner?.UpdateROIs(_cdOvlInnerRoiSnapshotBeforeEdgeOnlyMode.ToArray());
                restored = true;
            }

            if (_cdOvlOuterRoiSnapshotBeforeEdgeOnlyMode?.Count > 0)
            {
                HpImageViewer_Outer?.UpdateROIs(_cdOvlOuterRoiSnapshotBeforeEdgeOnlyMode.ToArray());
                restored = true;
            }

            ClearCdOvlRoiSnapshot();
            return restored;
        }

        /// <summary>
        /// 双面模式下分别恢复 inner/outer viewer，并各自叠加对应层的补偿十字。
        /// </summary>
        protected override void RestoreCdOvlFullOverlayAndCrosses(bool crossesMovable)
        {
            RestoreViewerSnapshotWithCross(HpImageViewer_Inner, _cdOvlInnerRoiSnapshotBeforeEdgeOnlyMode, appendInner: true, appendOuter: false, crossesMovable);
            RestoreViewerSnapshotWithCross(HpImageViewer_Outer, _cdOvlOuterRoiSnapshotBeforeEdgeOnlyMode, appendInner: false, appendOuter: true, crossesMovable);
            ClearCdOvlRoiSnapshot();
        }

        /// <summary>
        /// 清空边点后，只在两个 compare viewer 中分别显示各自的量测 ROI。
        /// </summary>
        protected override void RebuildCdOvlViewerMeasureRoisOnly()
        {
            if (TrainParam.InnerLayer.MeasureROI != null)
            {
                HpImageViewer_Inner?.UpdateROIs(TrainParam.InnerLayer.MeasureROI);
            }
            else
            {
                HpImageViewer_Inner?.UpdateROIs();
            }

            if (TrainParam.OuterLayer.MeasureROI != null)
            {
                HpImageViewer_Outer?.UpdateROIs(TrainParam.OuterLayer.MeasureROI);
            }
            else
            {
                HpImageViewer_Outer?.UpdateROIs();
            }
        }

        /// <summary>
        /// 双面 EdgeSelect 编辑态：分别在 inner/outer compare viewer 中显示各自可拖动十字。
        /// </summary>
        protected override void RefreshCdOvlCrosshair(bool isMovable)
        {
            UpdateViewerWithCrossesOnly(HpImageViewer_Inner, appendInner: true, appendOuter: false, isMovable);
            UpdateViewerWithCrossesOnly(HpImageViewer_Outer, appendInner: false, appendOuter: true, isMovable);
        }

        /// <summary>
        /// 从 inner compare viewer 读取确认后的内层补偿十字位置。
        /// </summary>
        protected override List<Point2d> GetConfirmedInnerCdOvlCompensationPoints()
        {
            return GetConfirmedCdOvlCompensationPoints(HpImageViewer_Inner, InnerCompCrossColor, "Inner");
        }

        /// <summary>
        /// 从 outer compare viewer 读取确认后的外层补偿十字位置。
        /// </summary>
        protected override List<Point2d> GetConfirmedOuterCdOvlCompensationPoints()
        {
            return GetConfirmedCdOvlCompensationPoints(HpImageViewer_Outer, OuterCompCrossColor, "Outer");
        }

        /// <summary>
        /// Run 完成后，把内外层补偿十字分别叠加回两个 compare viewer，
        /// 保证 Measurement 页里看到的画面就是后续 EdgeSelect 操作的对象。
        /// </summary>
        private void AppendCdOvlCrosshairToMeasurementViewers(bool isMovable)
        {
            UpdateViewerWithCrosses(HpImageViewer_Inner, appendInner: true, appendOuter: false, isMovable);
            UpdateViewerWithCrosses(HpImageViewer_Outer, appendInner: false, appendOuter: true, isMovable);
        }

        /// <summary>
        /// 在指定 viewer 上仅显示当前层需要编辑的补偿十字，仿照 singleface 的 EdgeSelect 编辑态。
        /// </summary>
        private void UpdateViewerWithCrossesOnly(IHPImageViewer viewer, bool appendInner, bool appendOuter, bool isMovable)
        {
            if (viewer == null)
            {
                return;
            }

            var rois = new List<ROIDesc>();
            if (appendInner)
            {
                AppendInnerCdOvlCrosshair(rois, isMovable);
            }

            if (appendOuter)
            {
                AppendOuterCdOvlCrosshair(rois, isMovable);
            }

            viewer.UpdateROIs(rois.ToArray());
        }

        /// <summary>
        /// 在指定 viewer 上保留非十字 ROI，再叠加当前层需要显示的补偿十字。
        /// </summary>
        private void UpdateViewerWithCrosses(IHPImageViewer viewer, bool appendInner, bool appendOuter, bool isMovable)
        {
            if (viewer == null)
            {
                return;
            }

            var rois = viewer.ImageViewerDesc?.ROIDescs?.Where(r => r is not CrosshairsDesc).ToList() ?? new List<ROIDesc>();
            if (appendInner)
            {
                AppendInnerCdOvlCrosshair(rois, isMovable);
            }

            if (appendOuter)
            {
                AppendOuterCdOvlCrosshair(rois, isMovable);
            }

            viewer.UpdateROIs(rois.ToArray());
        }

        /// <summary>
        /// 从进入编辑态前的快照恢复指定 viewer，再叠加锁定/可拖动的补偿十字。
        /// </summary>
        private void RestoreViewerSnapshotWithCross(IHPImageViewer viewer, List<ROIDesc> snapshot, bool appendInner, bool appendOuter, bool crossesMovable)
        {
            if (viewer == null)
            {
                return;
            }

            var rois = snapshot?.Where(r => r is not CrosshairsDesc).ToList() ?? new List<ROIDesc>();
            if (appendInner)
            {
                AppendInnerCdOvlCrosshair(rois, crossesMovable);
            }

            if (appendOuter)
            {
                AppendOuterCdOvlCrosshair(rois, crossesMovable);
            }

            viewer.UpdateROIs(rois.ToArray());
        }

        /// <summary>
        /// 从指定 compare viewer 读取某一层补偿十字的位置，
        /// 并转换为要写回算法的两个边点。
        /// </summary>
        protected List<Point2d> GetConfirmedCdOvlCompensationPoints(IHPImageViewer viewer, string color, string layerName)
        {
            var crosshairs = viewer?.ImageViewerDesc?.ROIDescs?.OfType<CrosshairsDesc>().ToList();
            if (crosshairs == null || crosshairs.Count == 0)
            {
                _messageBoxService.InfoDialog($"Please set {layerName.ToLower()} compensation crosshair first.");
                return null;
            }

            var p0 = crosshairs.FirstOrDefault(c => c.Color == color && Math.Abs(c.Angle) < 0.01);
            var p1 = crosshairs.FirstOrDefault(c => c.Color == color && Math.Abs(c.Angle - 180) < 0.01);
            if (p0 == null || p1 == null)
            {
                _messageBoxService.InfoDialog($"{layerName} layer: both edge crosshairs (horizontal and vertical leg) are required.");
                return null;
            }

            return new List<Point2d>
            {
                new Point2d(p0.CenterX, p0.CenterY),
                new Point2d(p1.CenterX, p1.CenterY),
            };
        }

        protected override async Task FindMeasureRoiFocusLayer(bool isInner)
        {
            if (isInner)
            {
                if (FineResInner == null)
                {
                    AppHelper.DispatcherInvoke(() =>
                    {
                        throw new Exception("Please run inner focus operation completely!");
                    });
                }

                FocusInnerWithImages(TrainParam.InnerLayer.MeasureROI.CreateRoi(), true);
            }
            else
            {
                if (FineResOuter == null)
                {
                    throw new Exception("Please run outer focus operation completely!");
                }
                FocusOuterWithImages(TrainParam.OuterLayer.MeasureROI.CreateRoi(), true);
            }
            await Task.CompletedTask;
        }

        protected override AxisType GetZAxis(bool isInner) => isInner ? InnerAxis : OuterAxis;

        protected override IVisionService GetVisionService(bool isInner) => isInner ? VisionInner : VisionOuter;
        protected async override Task RunMeasureMethod()
        {
            LogHelper.LogRunMethodStart();
            var img_inner = LoadFocusImageData(true);
            var img_outer = LoadFocusImageData(false);
            if (IsUpdateMeasureRoi || IsInnerCorrectioned || IsOuterCorrectioned)
            {
                // 在最佳清晰面的图片上自动创建定位模板和 kernel Model
                await CreateMeasureKernelSizeAndTemplate(true, false, img_inner);
                await CreatePreviewTemplate(true, CONST_PREVIEW_INNER);
                SaveCorrectHeight(true);

                await CreateMeasureKernelSizeAndTemplate(false, false, img_outer);
                await CreatePreviewTemplate(false, CONST_PREVIEW_OUTER);
                SaveCorrectHeight(false);

                // 更新Recipe内聚焦高度和Sharpness
                UpdateFocusHeightSharpness(img_inner, img_outer, InnerGlobalHeight, OuterGlobalHeight);

                await AutoCorrectR3();
                IsTrained = true;
                // 保存OMParam
                UpdateOMPara(InnerCamera, TestInfo.OMParam_Double);
                UpdateOMPara(OuterCamera, TestInfo.OMParam_Double);
                SaveOMParamToFile(TestInfo.OMParam_Double);
            }

            // 以下是对当前拍照的图片进行量测
            //var ret = _servicePegGrab.DoubleGrab_IO();

            using var imgObjectInner = img_inner.ToHObject();
            if (imgObjectInner == null) return;
            using var imgObjectOuter = img_outer.ToHObject();
            if (imgObjectOuter == null) return;

            if (imgObjectInner == null || imgObjectOuter == null) return;
            if (TestInfo.OMParam_Double.KernelModelInner == null || TestInfo.OMParam_Double.KernelModelOuter == null)
            {
                _messageBoxService.ErrorDialog("Please set measurement roi first!");// ("Physical 数据不完整！");
                return;
            }

            // 运行量测逻辑
            // 在同时拍照的图片上重新做roi的匹配，因为Inner图层在Outer层建模的时双击移动导致，同时拍照后的图和聚焦序列图里的图清晰面的图不一致
            var roiPairInner = CreateOmROIandRoi(img_inner, TrainParam.InnerLayer.MeasureROI, TestInfo.OMParam_Double.OmROIInner, true, out var matchItemInner);
            var transMatInner = GetTransmat(matchItemInner, matchItemInner);// 因为双面时的建模roi的中心可能有移动不能用建模时的匹配结果生成TransMat,直接用run时候的匹配结果生成没有旋转移动缩放的TransMat
            var roiPairOuter = CreateOmROIandRoi(img_outer, TrainParam.OuterLayer.MeasureROI, TestInfo.OMParam_Double.OmROIOuter, false, out var matchItemOuter);
            var transMatOuter = GetTransmat(matchItemOuter, matchItemOuter);// 同Inner,直接用run时候的匹配结果生成没有旋转移动缩放的TransMat
            var (offsetX, offsetY) = Physical?.GetOvlOffset() ?? (0, 0);
            var ovResult = _measService.MeasureOverlay_Double(imgObjectInner, imgObjectOuter, TestInfo.OMParam_Double, transMatInner, transMatOuter, _machineDomainService.GetBaseCamera(), offsetX, offsetY);
            ShowResult(ovResult, imgObjectInner, imgObjectOuter, roiPairInner.roiDesc, roiPairOuter.roiDesc, roiPairInner.omRoi, roiPairOuter.omRoi);
            LogHelper.LogRunMethodEnd();
        }

        private SingleTemplateMatchingContext GetSingleTemplate(bool isInner)
        {
            return isInner ?
                PositioningModel_Inner.Template.SelectedMatchModelGroup.MatchModel.ModelInfo == null ? InnerTemplateControl.TemplateContext :
                PositioningModel_Inner.Template :
                PositioningModel_Outer.Template.SelectedMatchModelGroup.MatchModel.ModelInfo == null ? OuterTemplateControl.TemplateContext : PositioningModel_Outer.Template;
        }
        /// <summary>
        /// 创建平移后的roi和omroi
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private (RectangleDesc roiDesc, OMROIBase omRoi) CreateOmROIandRoi(ImageData img, RectangleDesc measureRoi, OMROIBase oMROI, bool isInner, out MatchingItem matchItem)
        {
            var template = GetSingleTemplate(isInner);
            // 获取模板中心点和量测roi中心点之间的差，最后补到roi移动上，因为模板有可能是定位模板
            var templateCenter = template.ROI.GetCenter();
            //var deltaToMeasureTmp = new Point2d(templateCenter.X - measureRoi.CenterX, templateCenter.Y - measureRoi.CenterY);
            //var deltaToMeasureTmp = new Point2d(measureRoi.CenterX - templateCenter.X, measureRoi.CenterY - templateCenter.Y);
            var deltaToMeasureTmp = new Point2d(0, 0);

            // 根据匹配结果获取offset移动的roi
            matchItem = _alignmentService40.FindTemplateForRun(img, template, false)?.MatchItems[0];
            // 注意不要创建Kernelmodel也不要把roi和omroi保存进recipe里，流程还是需要用清晰面图片建模的kernelmodel，roi，omroi
            Point2d roiOffset = new Point2d(matchItem.Column - templateCenter.X, matchItem.Row - templateCenter.Y) + deltaToMeasureTmp;
            var offsetedRoiDesc = measureRoi.CreateOffsetRoi(roiOffset) as RectangleDesc;
            var offsetOMRoi = oMROI.Move(roiOffset.X, roiOffset.Y);
            return (offsetedRoiDesc, offsetOMRoi);
        }

        protected override string GetImageName(bool isInner)
        {
            return isInner ? CONST_Inner : CONST_Outer;
        }

        protected void UpdateOMPara(ECamera camera, OMParam_Double oMParam)
        {
            var (px, py) = _servCalDat.GetUmPixel(camera, _serviceProvider);
            if (camera == InnerCamera)
            {
                oMParam.PixelSizeXInner = px;
                oMParam.PixelSizeYInner = py;
            }
            else if (camera == OuterCamera)
            {
                oMParam.PixelSizeXOuter = px;
                oMParam.PixelSizeYOuter = py;
            }
        }

        public override void OpenDialogThread(IDialogParameters parameters)
        {
            _testTrainService.TestInfo = TestInfo;

            base.OpenDialogThread(parameters);
        }

        public void StartGrabbing()
        {
            if (HpImageViewer_Inner != null) VisionInner.StartGrabbing();
            if (HpImageViewer_Outer != null) VisionOuter.StartGrabbing();
        }

        private void RestoreTrainViewerByLayer(bool isInner)
        {
            if (TryRestoreTrainViewerFocusImage(isInner))
            {
                return;
            }

            TryRestoreTrainViewerTemplate(isInner);
        }

        private bool TryRestoreTrainViewerFocusImage(bool isInner)
        {
            try
            {
                var image = LoadFocusImageData(isInner);
                if (image == null)
                {
                    return false;
                }

                HpImageViewer.Image = image;
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogDebug($"Restore train viewer focus image failed, isInner={isInner}, error={ex.Message}");
                return false;
            }
        }

        private bool TryRestoreTrainViewerTemplate(bool isInner)
        {
            try
            {
                var template = GetSingleTemplate(isInner);
                if (template == null)
                {
                    return false;
                }

                HpImageViewer.SetTemplateImage(template, TestFilesFolder);
                return HpImageViewer.Image != null;
            }
            catch (Exception ex)
            {
                LogHelper.LogDebug($"Restore train viewer template failed, isInner={isInner}, error={ex.Message}");
                return false;
            }
        }

        private bool _isInner = true;
        [RelayCommand]
        internal void PatternTabChanged(object tabIndex)
        {
            int index = Convert.ToInt32(tabIndex);
            _isInner = index == 0 || index == 2;
            var mainVision = _visionServiceManager.GetMainVision();
            HpImageViewer.UpdateROIs();// 每次切换roitab页就把主显示控件的ROI清空掉，否则点击Stagecontrol的聚焦会使用残留roi有几率报错
            switch (index)
            {
                case 0:
                    // Inner
                    TabChange(true);
                    alignmentService.ChangeCamera(InnerCamera, false);
                    _servPlat.MoveZ(InnerAxis, GetFocusHeightByLayer(true));
                    RestoreTrainViewerByLayer(true);
                    mainVision.StartGrabbing();
                    break;
                case 1:
                    // Outer
                    TabChange(true);
                    alignmentService.ChangeCamera(OuterCamera, false);
                    _servPlat.MoveZ(OuterAxis, GetFocusHeightByLayer(false));
                    RestoreTrainViewerByLayer(false);
                    mainVision.StartGrabbing();
                    break;
                case 2:
                    // Measurement
                    TabChange(false);
                    ShowMeasurementCompareView();
                    break;
                default:
                    break;
            }
            //await Task.Run(() =>
            //{

            //});
        }


        protected override void SaveVisionParams()
        {
            try
            {
                if (_isInner)
                {
                    TrainSettingsDoubleFace.CameraParams = _cameraLightParamService.GetCamearaParam(InnerCamera);
                    TrainSettingsDoubleFace.LightSourceParam = _cameraLightParamService.GetLightSourceParam(InnerCamera);
                    SaveAttenuationCoef(InnerCamera, TrainSettingsDoubleFace.CameraParams);
                    SaveLightAgeInitialInfo(InnerCamera, TrainSettingsDoubleFace.CameraParams, TrainSettingsDoubleFace.LightSourceParam);
                }
                else
                {
                    TrainSettingsDoubleFace.CameraParamsOuter = _cameraLightParamService.GetCamearaParam(OuterCamera);
                    TrainSettingsDoubleFace.LightSourceParamOuter = _cameraLightParamService.GetLightSourceParam(OuterCamera);
                    SaveAttenuationCoef(OuterCamera, TrainSettingsDoubleFace.CameraParamsOuter);
                    SaveLightAgeInitialInfo(OuterCamera, TrainSettingsDoubleFace.CameraParamsOuter, TrainSettingsDoubleFace.LightSourceParamOuter);
                }
            }
            catch (Exception e)
            {
                LogHelper.LogError(e);
            }
        }


        protected override void SaveKernelModel(bool isInner, OMROIBase oMROIBase, OMKernelModel oMKernelModel)
        {
            if (isInner)
            {
                TestInfo.OMParam_Double.OmROIInner = oMROIBase;
                TestInfo.OMParam_Double.KernelModelInner = oMKernelModel;
            }
            else
            {
                TestInfo.OMParam_Double.OmROIOuter = oMROIBase;
                TestInfo.OMParam_Double.KernelModelOuter = oMKernelModel;
            }
        }

        /// <summary>
        /// 获取Z轴聚焦的高度
        /// </summary>
        protected override void SetFocusZHeight()
        {
            if (!TrainSetting.IsTrained)
            {
                InnerFocusHeight = 0.0d;
                OuterFocusHeight = 0.0d;
            }
            else
            {

                InnerFocusHeight = GetFocusHeightByLayer(true);
                OuterFocusHeight = GetFocusHeightByLayer(false);
            }
        }

        private double GetFocusHeightByLayer(bool isInner)
        {
            if (TrainSettingsDoubleFace.FocusModuleInfo.BaseFocusHeight == 0)
            {
                TrainSettingsDoubleFace.FocusModuleInfo.BaseFocusHeight = _alignmentService.WaferMapInfo.Thickness;
            }
            return isInner ? TrainSettingsDoubleFace.FocusModuleInfoInner.BaseFocusHeight + _alignmentService40.GetMacroFocusZ(InnerCamera).z
                           : TrainSettingsDoubleFace.FocusModuleInfoOuter.BaseFocusHeight + _alignmentService40.GetMacroFocusZ(OuterCamera).z + _alignmentService.WaferMapInfo.Thickness;
        }

        #region abstract overrides
        protected override void InitCorrections()
        {
            CorrectionVisibility = _grabMode != EnumGrabMode.SingleGrab_Average && _focusSetting.IsEnableFocusCorrection ? Visibility.Visible : Visibility.Collapsed;
            InnerCorrectionValue = TrainParam.InnerLayer.FocusCorrection;
            OuterCorrectionValue = TrainParam.OuterLayer.FocusCorrection;
            InnerCorrectionHeight = InnerFocusHeight + TrainParam.InnerLayer.FocusCorrection;
            OuterCorrectionHeight = OuterFocusHeight + TrainParam.OuterLayer.FocusCorrection;
        }

        public override async Task Loaded()
        {
            await base.Loaded();
            string recipeFolder = _serviceProvider.GetService<IRecipeManager>().GetRecipePath(_alignmentService.RecipeElementsInfo.Id);
            TestFilesFolder = Path.Combine(recipeFolder, TestInfo.Name);
            _templateMatchingService.TemplatesFolder = TestFilesFolder;
            if (PositioningTemplateControl_Inner != null)
            {
                PositioningTemplateControl_Inner.TemplateContext = PositioningModel_Inner.Template;
                PositioningTemplateControl_Inner.TemplateContext.Name = "Positioning Model Inner";
                PositioningTemplateControl_Inner.ActionUserModified = PositionTemplateUserModified;
                PositioningTemplateControl_Inner.FuncBeforeCreateModel = () => BeforeCreatePostionModel(true);
                if (TestInfo.TargetSetting.EnabledTargetPoints.Count() > 1)
                {
                    CheckPositionModelForeachVisible = Visibility.Visible;
                }
                else
                {
                    CheckPositionModelForeachVisible = Visibility.Collapsed;
                    TestInfo.TrainSetting.PositioningTemplateInfo.IsForEachTarget = true;
                }
            }
            if (PositioningTemplateControl_Outer != null)
            {
                PositioningTemplateControl_Outer.TemplateContext = PositioningModel_Outer.Template;
                PositioningTemplateControl_Outer.TemplateContext.Name = "Positioning Model Outer";
                PositioningTemplateControl_Outer.ActionUserModified = PositionTemplateUserModified;
                PositioningTemplateControl_Outer.FuncBeforeCreateModel = () => BeforeCreatePostionModel(false);
                if (TestInfo.TargetSetting.EnabledTargetPoints.Count() > 1)
                {
                    CheckPositionModelForeachVisible = Visibility.Visible;
                }
                else
                {
                    CheckPositionModelForeachVisible = Visibility.Collapsed;
                    TestInfo.TrainSetting.PositioningTemplateInfo.IsForEachTarget = true;
                }
            }
        }

        /// <summary>
        /// 创建定位模板的通用方法
        /// </summary>
        /// <param name="isInner">是否为内层相机</param>
        /// <returns>操作是否成功</returns>
        private bool BeforeCreatePostionModel(bool isInner)
        {
            return BeforeCreatePositionModel_Common(isInner,
                                                    () =>
                                                    {
                                                        // 选择相应的相机和坐标轴
                                                        var camera = isInner ? InnerCamera : OuterCamera;
                                                        var axis = isInner ? InnerAxis : OuterAxis;
                                                        var positioningModel = isInner ? PositioningModel_Inner : PositioningModel_Outer;
                                                        return (axis, camera, positioningModel);
                                                    });

        }

        private void SavePositioningImageOuter()
        {
            var filePath = Path.Combine(TestFilesFolder, "Positioning_Outer.bmp");
            FileHelper.DeleteFile(filePath);

            var imageData = _alignmentService.MoveAndGrab();
            imageData.SaveBmp(filePath);

        }

        protected override async void SaveFocusImage_ByCorrectionMove(bool isInner)
        {
            var imgName = isInner ? CONST_Inner : CONST_Outer;
            await SaveFocusImage_ByGrab(imgName, _visionService, isInner, null, isMeasure: true);
        }

        protected override ECamera GetECamera(bool isInner)
        {
            return isInner ? InnerCamera : OuterCamera;
        }

        protected override bool IsCorrectionGrabbed(bool isInner)
        {
            if (_focusSetting.IsEnableFocusCorrection)
            {
                if (TestInfo.IsDoubleFace)
                {
                    return isInner ? IsInnerCorrectioned : IsOuterCorrectioned;
                }
            }
            return false;
        }
        #endregion


        #region 定位模板逻辑

        private Point2d _targetPoint_Inner;

        private Point2d _targetPoint_Outer;

        public override Point2d TargetPoint
        {
            get
            {
                return _isInner ? _targetPoint_Inner : _targetPoint_Outer;
            }
            set
            {
                if (_isInner)
                {
                    _targetPoint_Inner = value;
                }
                else
                {
                    _targetPoint_Outer = value;
                }
            }
        }

        protected override bool ShouldUpdateTargetPointAfterConfirmMeasureRoi(bool isInner)
        {
            return true;
        }

        protected override void UpdateTargetPointAfterConfirmMeasureRoi(bool isInner, Point2d targetPoint)
        {
            if (isInner)
            {
                _targetPoint_Inner = targetPoint;
            }
            else
            {
                _targetPoint_Outer = targetPoint;
            }
        }

        protected override Point2d GetTargetAxisPoint(Point2d teachingPoint)
        {
            // TODO DoubleFace AutoCorrectR3需要考虑以哪个定位模板定位&判断是否两个模板都定位
            if (_isInner)
            {
                alignmentService.ChangeCamera(InnerCamera, false);
                return _alignmentService.FindSingleTemplate(teachingPoint, TrainSettingsDoubleFace.OverlayMark_InnerModels[0], true, false, out _);
            }
            else
            {

                alignmentService.ChangeCamera(OuterCamera, false);
                return _alignmentService.FindSingleTemplate(teachingPoint, TrainSettingsDoubleFace.OverlayMark_OuterModels[0], true, false, out _);
            }
        }

        protected override void SetPreviewTemplate(bool isInner, SingleTemplateMatchingContext template)
        {
            if (isInner)
            {
                TrainSettingsDoubleFace.PreviewTemplateInner = template;
            }
            else
            {
                TrainSettingsDoubleFace.PreviewTemplateOuter = template;
            }
        }
        #endregion
    }
}
