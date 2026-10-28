using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dm.util;
using HalconDotNet;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.StageControlModule.EventAggregators;
using InteVega.CBB.TemplateMatching.Core;
using Marvel.Application40.Services;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.DataModels.Recipe40.Rules;
using Marvel.DataModels.Recipe40.TestInfo;
using Marvel.DataModels40.DeviceParameter;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using NPOI.SS.Formula.Functions;
using OVL.Algorithm.DoNet;
using Prism.Events;
using Prism.Services.Dialogs;
using ScottPlot.MultiplotLayouts;
using SuperSocket.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WinCopies.Collections;
using WinCopies.Linq;
namespace Ovl.UI.Recipe.ViewModel
{
    internal partial class Test_Train40ViewModel_SingleFace : Test_Train40ViewModelBase
    {
        #region Fields
        /// <summary>
        /// 全行程聚焦结果
        /// </summary>
        protected FocusPegResult fine_Res;
        #endregion

        #region Properties
        public FocusPegResult FineResInner { get; set; }
        public FocusPegResult FineResOuter { get;  set; }
        internal ECamera CurCamera => TrainSettingsSingleCamera.MeasCamera;
        protected override ECamera FirstCamera => CurCamera;
        [ObservableProperty]
        bool isFocusSingle;

        [ObservableProperty]
        Test_TrainFocusPredictionViewModel _focusPrediction;
        [ObservableProperty]
        bool _isFocusPredictionTabEnabled;

        [ObservableProperty]
        private int _selectedOpticalModuleIndex = 0; // 0=Up, 1=Down

        [ObservableProperty]
        private string _upCameraMode = "N/A";

        [ObservableProperty]
        private string _downCameraMode = "N/A";

        [ObservableProperty]
        private string _upIntensity = "N/A";

        [ObservableProperty]
        private string _downIntensity = "N/A";

        /// <summary>用户切换 Up/Down 光学模组后，更新硬件光路并刷新界面上的相机模式与光强显示。</summary>
        [RelayCommand]
        private void SelectedOpticalClick()
        {
            UpdateOpticalModuleConfiguration(SelectedOpticalModuleIndex, false);
            UpdateOpticalModuleDisplayValues(); // 新增：切换后刷新显示
        }

        protected TrainSettings40_SingleCamera TrainSettingsSingleCamera =>
            TestInfo?.SingleCameraTrainSettings;
        //??? Inner 、Outer 相关的参数都改成了使用TrainParam.InnerLayer 和 TrainParam.OuterLayer来获取，包括FocusRoi\ FocusKernel\ MeasureRoi\RoiCurve 等
        protected TestOverlayTrainParam TrainParam
        {
            get
            {
                return TestInfo.GetTrainSettings().TrainParam;
            }
        }

        internal IVisionService CurVision => _serviceProvider.GetService<IVisionServiceManager>().GetVision(CurCamera);
        internal AxisType CurAxis => CurCamera == ECamera.Meas ? _machine40Config.PatternCameraCfg.MeasCamAxis : _machine40Config.PatternCameraCfg.MeasCamExtAxis;
        protected override FocusInfo FocusInfo
        {
            get
            {
                if (TestInfo == null)
                    throw new InvalidOperationException("TestInfo cannot be null");
                return TestInfo.TrainSetting_BiB.FocusModuleInfo.FocusInfo;
            }
            set
            {
                if (TestInfo == null)
                    throw new InvalidOperationException("TestInfo cannot be null");
                TestInfo.TrainSetting_BiB.FocusModuleInfo.FocusInfo = value;
            }
        }
        #endregion

        #region Constructor
        private IServiceCamera _serviceCamera;

        /// <summary>单面 OM40 Overlay 训练 ViewModel；解析当前量测相机属性服务。</summary>
        public Test_Train40ViewModel_SingleFace(IServiceProvider serviceProvider) : base(serviceProvider)
        {

            _serviceCamera = _serviceProvider.GetService<IServiceCamera>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// 更新光学模块配置并刷新光参
        /// </summary>
        private void UpdateOpticalModuleConfiguration(int index, bool isForLoad)
        {
            try
            {
                bool isUpLight = index == 0;
                ECamera lightModuleId = isUpLight ? ECamera.Meas : ECamera.MeasExt;

                // 更新视觉服务的光源模块ID
                var visionManager = _serviceProvider.GetRequiredService<IVisionServiceManager>();
                visionManager.SetLightModuleId(CurCamera, lightModuleId);

                if (isForLoad)
                {
                    // 重新设置光参以刷新硬件状态
                    _cameraLightParamService40.SetLightSourceParamMeasureSingleFace(
                        TrainSettingsSingleCamera.LightSourceParam);
                }

                // 通知参数已变更
                _visionService.NotifyParamsChanged();

                LogHelper.LogInfo($"Optical module switched to {(isUpLight ? "Up" : "Down")}");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"UpdateOpticalModuleConfiguration error: {ex.Message}");
                _messageBoxService.Error($"光学模块切换失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新光学模组显示值（相机模式和光强）
        /// </summary>
        private void UpdateOpticalModuleDisplayValues()
        {
            try
            {
                // 获取 Up (Meas) 相机模式和光强
                var upCameraAttr = _serviceCamera.GetCameraAttr(ECamera.Meas);
                UpCameraMode = upCameraAttr.SpecBand.ToString();

                var upLightParam = _cameraLightParamService40.GetLightSourceParam(ECamera.Meas);
                if (upLightParam is MeasureLightSourceParam40 upParam)
                {
                    UpIntensity = upParam.Intensity.ToString("F0");
                }

                // 获取 Down (MeasExt) 相机模式和光强
                var downCameraAttr = _serviceCamera.GetCameraAttr(ECamera.MeasExt);
                DownCameraMode = downCameraAttr.SpecBand.ToString();

                var downLightParam = _cameraLightParamService40.GetLightSourceParam(ECamera.MeasExt);
                if (downLightParam is MeasureLightSourceParam40 downParam)
                {
                    DownIntensity = downParam.Intensity.ToString("F0");
                }

                LogHelper.LogInfo($"Updated optical module display: Up={UpCameraMode}/{UpIntensity}, Down={DownCameraMode}/{DownIntensity}");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"UpdateOpticalModuleDisplayValues error: {ex.Message}");
                UpCameraMode = "Error";
                DownCameraMode = "Error";
                UpIntensity = "N/A";
                DownIntensity = "N/A";
            }
        }

        /// <summary>按当前单面量测轴将 Z 移动到指定高度（毫米）；<paramref name="focusZHeight"/> 为 null 时不移动。</summary>
        protected void MoveAndWait(double? focusZHeight)
        {
            if (focusZHeight != null)
            {
                _servPlat.MoveZ(false, CurAxis, focusZHeight.Value);
            }
        }
        /// <summary>单次「内外一起」精聚焦：全视野训练得到 <see cref="fine_Res"/>，再对内外层量测 ROI 依次做 ROI 曲线聚焦并采平均图。</summary>
        private async Task TrainFineFocus()
        {
            // 全视野、pzt全行程、小步进精聚焦训练
            HpImageViewer.UpdateROIs();
            SetRoiColors();
            await Task.Run(() => fine_Res = TrainGlobal());
            await FocusInnerOuterSingleGrab(false, true);
        }
        /// <summary>同时为内外层聚焦 ROI 上色并刷新到图像控件。</summary>
        private void SetRoiColors()
        {
            var innerRoiDesc = TrainParam.InnerLayer.FocusROI;
            innerRoiDesc.Color = InnerColor;

            var outRoiDesc = TrainParam.OuterLayer.FocusROI;
            outRoiDesc.Color = OuterColor;
            HpImageViewer.UpdateROIs(innerRoiDesc, outRoiDesc);
        }
        /// <summary>仅针对内层或外层做单侧精聚焦（全视野 + 对应侧 ROI 曲线）。</summary>
        /// <param name="isInner">true 聚焦内层，false 聚焦外层。</param>
        private async Task TrainFineFocus(bool isInner)
        {
            // 全视野、pzt全行程、小步进精聚焦训练
            HpImageViewer.UpdateROIs();
            SetRoiColors(isInner);
            if (isInner)
                await FocusInner(false);
            else
                await FocusOuter(false);
        }
        /// <summary>仅为内层或外层聚焦 ROI 上色并刷新。</summary>
        private void SetRoiColors(bool isInner)
        {
            if (isInner)
            {
                var innerRoiDesc = TrainParam.InnerLayer.FocusROI;
                innerRoiDesc.Color = InnerColor;
                HpImageViewer.UpdateROIs(innerRoiDesc);
            }
            else
            {
                var outRoiDesc = TrainParam.OuterLayer.FocusROI;
                outRoiDesc.Color = OuterColor;
                HpImageViewer.UpdateROIs(outRoiDesc);
            }
        }

        /// <summary>双抓模式下，在对应层的焦面高度上执行量测侧聚焦（内或外）。</summary>
        private async Task FocusInnerOuterDoubleGrab(bool isMeasure, bool isInner)
        {
            if (isMeasure)
            {
                if (isInner)
                {
                    //inner focus
                    MoveAndWait(InnerFocusHeight);
                    await FocusInner(isMeasure);
                }
                else
                {
                    //outer focus
                    MoveAndWait(OuterFocusHeight);
                    await FocusOuter(isMeasure);
                }

                //// 如果当前面是基准面，则采集当前面的图片作为预匹配模板
                //if (isMeasure && (isInner && _isBaseOnInner || !isInner && !_isBaseOnInner))
                //{
                //    var image = CurVision.GrabOneImage();
                //    SavePreviewImage(image);
                //    
                //}
            }
        }

        /// <summary>
        /// 量测聚焦完成后更新配方中的清晰度、内外层相对全局焦面的偏移，以及单相机运行参数中的基准高度与全局偏移。
        /// </summary>
        /// <param name="image">用于计算清晰度的当前图像。</param>
        /// <param name="globalHeight">全视野聚焦得到的 Z 拟合高度。</param>
        /// <param name="isInner">当前操作侧（用于与基准面对齐时的清晰度 ROI 选择）。</param>
        protected void UpdateFoucsHeightSharpness(ImageData image, double globalHeight, bool isInner)
        {
            if (image == null)
            {
                string errmsg = "Image is null ,update foucs height sharpness failed.";
                AppHelper.DispatcherInvoke(() =>
                {
                    _messageBoxService.ErrorDialog(errmsg);
                });
                return;
            }
            //double midZHeight = (InnerFocusHeight + OuterFocusHeight) / 2;
            if (IsInBaseOn(isInner))
            {
                var paramFocus = GetCoarseParamFocus(IServiceCamera.CurCamera);
                var sharpness = _focusService.GetImageClarity(image, paramFocus: paramFocus);
                TestInfo.TrainSetting.FocusModuleInfo.Sharpness = sharpness;
                var roiDesc = _isBaseOnInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
                var sharpnessRoi = _focusService.GetImageClarity(image, roiDesc.CreateRoi(), paramFocus);
                //此处需要保存全图的清晰度
                TestInfo.TrainSetting.FocusModuleInfo.RoiSharpness = sharpness;
            }
            TrainParam.InnerLayer.SharpnessFocusOffset = InnerFocusHeight - globalHeight;
            TrainParam.OuterLayer.SharpnessFocusOffset = OuterFocusHeight - globalHeight;
            var zCal = _alignmentService40.GetMacroFocusZ(CurCamera).z;
            var baseOnHeight = _isBaseOnInner ? InnerFocusHeight : OuterFocusHeight;
            TrainSettingsSingleCamera.FocusModuleInfo.BaseFocusHeight = globalHeight - zCal - _alignmentService40.WaferMapInfo.Thickness;
            TrainSettingsSingleCamera.FocusModuleInfo.GlobalFocusOffset = baseOnHeight - globalHeight;
            LogHelper.LogInfo($"Focus Result:globalHeight={globalHeight:0.######},InnerFocusHeight={InnerFocusHeight:0.######}, OuterFocusHeight={OuterFocusHeight:0.######}");
        }

        /// <summary>
        /// 单抓/平均模式下：基于已有全视野结果 <see cref="fine_Res"/> 对一层或内外两层做 ROI 聚焦，移到中间高度并可选采图更新清晰度。
        /// </summary>
        /// <param name="isMeasure">是否写入量测相关清晰度等数据。</param>
        /// <param name="isBoth">是否连续做内外两层 ROI 聚焦。</param>
        /// <param name="isInner">当 <paramref name="isBoth"/> 为 false 时，指定只做内层或外层。</param>
        private async Task FocusInnerOuterSingleGrab(bool isMeasure, bool isBoth, bool isInner = false)
        {
            if (fine_Res == null)
                return;
            //采图前需要开启
            _visionService.StartGrabbing();
            if (isBoth)
            {
                await TrainRoiFocus(fine_Res, true, isMeasure);
                await TrainRoiFocus(fine_Res, false, isMeasure);
            }
            else if (isInner)
            {
                await TrainRoiFocus(fine_Res, true, isMeasure);
            }
            else
            {
                await TrainRoiFocus(fine_Res, false, isMeasure);
            }

            JudgeFocusHeightException();
            var midZHeight = (InnerFocusHeight + OuterFocusHeight) / 2;
            MoveAndWait(midZHeight);
            var img = await SaveFocusImage_ByGrab(CONST_Average, CurVision, false, null, isMeasure);
            //SavePreviewImage(img);
            //点击量测按钮时，才保存最终的数据
            if (isMeasure)
            {
                var image = await CurVision.GrabOneImageAsync();

                //此处需要保存全图的清晰度
                UpdateFoucsHeightSharpness(image, fine_Res.ZFit, isInner);
            }
            _visionService.StopGrabbing();
        }

        /// <summary>内层：必要时先全视野训练，再对内侧量测 ROI 做曲线聚焦并移到内层焦高。</summary>
        protected async Task FocusInner(bool isMeasure)
        {
            if (!isMeasure)
            {
                await Task.Run(() => FineResInner = TrainGlobal());
            }
            await TrainRoiFocus(FineResInner, true, isMeasure);
            MoveAndWait(InnerFocusHeight);
        }

        /// <summary>外层：必要时先全视野训练，再对外侧量测 ROI 做曲线聚焦并移到外层焦高。</summary>
        protected async Task FocusOuter(bool isMeasure)
        {
            if (!isMeasure)
            {
                await Task.Run(() => FineResOuter = TrainGlobal());
            }
            await TrainRoiFocus(FineResOuter, false, isMeasure);
            MoveAndWait(OuterFocusHeight);
        }
        /// <summary>双抓且 PEG 模式下，若内外焦面几乎重合则抛异常，避免无效工艺配置。</summary>
        protected void JudgeFocusHeightException()
        {
            if (_grabMode == EnumGrabMode.DoubleGrab && Physical.AnraSettings.DoubleGrabMode == EnumDoubleGrabMode.PEG && Math.Abs(InnerFocusHeight - OuterFocusHeight) < 0.0001)
            {
                throw new Exception("Focus offset less than 0.1um, not adjust to 'Double Grab Mode: PEG'");
            }
        }
        private bool _isBaseOnInner
        {
            get
            { return _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner; }
        }

        /// <summary>
        /// 使用全视野序列图在指定层的量测/聚焦 ROI 上训练锐度曲线，得到最佳 Z；按补偿状态决定是否覆盖保存 Inner/Outer 焦面图与预览模板。
        /// </summary>
        /// <returns>该层 ROI 聚焦得到的 Z 高度。</returns>
        protected async Task<double> TrainRoiFocus(FocusPegResult focusPegResult, bool isInner, bool isMeasure)
        {
            var trainLayerParam = isInner ? TrainParam.InnerLayer : TrainParam.OuterLayer;
            var roiDesc = TrainParam.GetRoiDesc(isInner, isMeasure);
            string imageName = isInner ? CONST_Inner : CONST_Outer;
            var coarseFocusInfo = GetTestFocusOrDefault(IServiceCamera.CurCamera).CourseFocusInfo;
            var focusImages = focusPegResult.PegImages;
            var trainArg = CreateTrainArg(_grabMode);
            var roi = roiDesc.CreateRoi();
            //使用全视野的聚焦序列图进行内、外层聚焦，并且获取聚焦曲线
            var ret = _focusService.TrainCurve(coarseFocusInfo, roi, focusImages, trainArg);
            trainLayerParam.RoiFocusCurve = ret.Curve;
            var finalRoiHeight = ret.Curve.RefMaximumZ.First();
            MoveAndWait(finalRoiHeight);
            if (isMeasure)
            {
                LogRoiForAlgoTest($"************TrainRoiFocus isInner ={isInner}", roi);
            }

            if (!IsCorrectionGrabbed(isInner))
            {
                // 没有补偿焦面设置时才把聚焦面覆盖为量测建模时使用图片（ex;Inner.bmp 或 Outer.bmp）
                var img = await SaveFocusImage_ByGrab(imageName, CurVision, isInner, roi, isMeasure);
                // 如果当前面是基准面，则采集当前面的图片作为预匹配模板，并且训练全视野全形成聚焦曲线
                if (IsInBaseOn(isInner))
                {
                    TrainGlobalFocusCurve(coarseFocusInfo, GetFocusPegResult(isInner));
                    SavePreviewImage(img);
                }
                if (isInner) InnerFocusHeight = finalRoiHeight;
                else OuterFocusHeight = finalRoiHeight;
                if (isMeasure)
                {
                    UpdateFoucsHeightSharpness(img, GetFocusPegResult(_isBaseOnInner).ZFit, isInner);
                }


                LogHelper.LogInfo($"Save Focus Image = {isMeasure}, imageName : {imageName}.bmp");
            }
            return finalRoiHeight;
        }

        /// <summary>
        /// 当前面是否为基准面
        /// </summary>
        /// <param name="isInner"></param>
        /// <returns></returns>
        private bool IsInBaseOn(bool isInner) => isInner && _isBaseOnInner || !isInner && !_isBaseOnInner;

        /// <summary>双抓返回对应侧的全视野结果；单抓返回共用的 <see cref="fine_Res"/>。</summary>
        protected virtual FocusPegResult GetFocusPegResult(bool isInner)
        {
            if (_grabMode == EnumGrabMode.DoubleGrab)
            {
                return isInner ? FineResInner : FineResOuter;
            }
            else
            {
                return fine_Res;
            }
        }

        /// <summary>在基准焦面将当前图像落盘为预匹配用图（CONST_PREVIEW），不更新 Kernel。</summary>
        /// <param name="img">已对准焦面的图像。</param>
        protected void SavePreviewImage(ImageData img)
        {
            //采集Base层的图片，并且使用该图片创建预匹配模板
            LogHelper.LogInfo($"Move to previewmatch layer measure focus:Base Layer={_focusSetting.FocusBasedOn}");
            //传null是为了不设置sharpness和kernel
            SaveFocusImageCore(CONST_PREVIEW, null, img);
        }

        /// <summary>
        /// 全视野聚焦，并训练聚焦曲线
        /// </summary>
        /// <returns></returns>
        protected FocusPegResult TrainGlobal()
        {
            var allRangeFocusInfo = CreateFullTrainInfo(CurCamera);
            // 调用示例
            return ExecuteGlobalFineFocus(allRangeFocusInfo);
        }
        /// <summary>
        /// 执行全视野精细聚焦
        /// </summary>
        /// <returns>聚焦结果</returns>
        private FocusPegResult ExecuteGlobalFineFocus(AutoRunInfo allRangeFocusInfo)
        {
            var trainArg = CreateTrainArg(_grabMode);
            allRangeFocusInfo.StartPosition = _servPlat.GetCurrentZ(CurAxis);
            var res = _focusService40.FineFocusSingleCamTrain(CurCamera, allRangeFocusInfo, null);
            //TrainSettingsSingleCamera.FocusModuleInfo.BaseFocusHeight = res.ZFit - _alignmentService40.GetMacroZ(CurCamera);
            MoveAndWait(res.ZFit);
            //SetFocusZHeight();
            return res;
        }
        /// <summary>
        /// 训练全视野聚焦曲线
        /// </summary>
        /// <param name="focusInfo">聚焦信息</param>
        /// <param name="focusResult">聚焦结果</param>
        private FocusPegResult TrainGlobalFocusCurve(AutoRunInfo focusInfo, FocusPegResult focusResult)
        {
            var trainArg = CreateTrainArg(_grabMode);
            // 全视野聚焦训练
            var ret2 = _focusService.TrainCurve(focusInfo, null, focusResult.PegImages, trainArg);
            TrainParam.GlobalFineFocusCurve = ret2.Curve;
            return ret2;
        }

        /// <summary>量测前刷新 OM 参数中的像素当量与目标类型（与当前单相机运行参数一致）。</summary>
        protected void UpdateOMPara(ECamera camera, OMParam oMParam)
        {
            var (px, py) = _servCalDat.GetUmPixel(CurCamera, _serviceProvider);
            oMParam.PixelSizeX = px;
            oMParam.PixelSizeY = py;
            var targetType = _focusSetting.FocusBasedOn == EnumFocusBasedOn.Outer
                ? TrainSettingsSingleCamera.OuterType
                : TrainSettingsSingleCamera.InnerType;
            oMParam.TargetType = targetType.ToOMTargetType();
        }

        /// <summary>按补偿后的内外焦高差调用 PEG 单次抓取，分别保存内外量测用图。</summary>
        private void SaveCorrectImageByPeg()
        {
            var heightPair = CalculateFocusHeightsByCorrection();

            var images = _serviceProvider.GetService<IServicePegGrab>().SingleGrab_ByPosition(CurCamera, heightPair.actualInnerHeight, heightPair.heightDifference);
            SaveFocusImage_ByExistImage(CONST_Inner, images.ElementAt(0).Image, true, TrainParam.InnerLayer.MeasureROI.CreateRoi());
            SaveFocusImage_ByExistImage(CONST_Outer, images.ElementAt(1).Image, false, TrainParam.OuterLayer.MeasureROI.CreateRoi());
        }

        /// <summary>
        /// 计算内外层的实际高度和高度差值(包括手动高度补偿)
        /// </summary>
        /// <returns>innerheight和outer与inner的差值</returns>
        private (double actualInnerHeight, double actualOuterHeight, double heightDifference) CalculateFocusHeightsByCorrection()
        {
            // 计算内外层偏移量
            var innerFinalHeight = _focusSetting.IsEnableFocusCorrection ? InnerFocusHeight + TrainParam.InnerLayer.FocusCorrection : InnerFocusHeight;
            var outerFinalHeight = _focusSetting.IsEnableFocusCorrection ? OuterFocusHeight + TrainParam.OuterLayer.FocusCorrection : OuterFocusHeight;

            // 计算实际高度和差值
            var heightDifference = outerFinalHeight - innerFinalHeight;

            return (innerFinalHeight, outerFinalHeight, heightDifference);
        }

        /// <summary>单面量测内外共用同一物理 Z 轴（当前相机对应的平台轴）。</summary>
        protected override AxisType GetZAxis(bool isInner) => CurAxis;
        /// <summary>单面训练使用主路视觉采流服务（与内/外层标志无关）。</summary>
        protected override IVisionService GetVisionService(bool isInner) => _visionService;
        #endregion

        #region override methods
        /// <summary>仅外层「单面分步聚焦」流程：后台线程执行外层 <see cref="TrainFineFocus(bool)"/>，异常弹窗并在 finally 中恢复采图提示与 ROI 显示。</summary>
        protected override async Task TrainFocus_OuterSingleMethod()
        {
            if (TrainParam.OuterLayer.FocusROI == null)
            {
                _messageBoxService.ErrorDialog("Please draw the corresponding outer ROI！");
                return;
            }

            LogHelper.LogRunMethodStart();
            ChangeCameraInfo = "Focusing …";
            try
            {
                _visionService.StartGrabbing();
                await Task.Run(() => TrainFineFocus(false));

            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                string errmsg = "Outer: Focusing failed. Please modify the focusing parameters and try again.\n" + $"Detail:{ex.Message}";
                AppHelper.DispatcherInvoke(() =>
                {
                    _messageBoxService.ErrorDialog(errmsg);
                });
            }
            finally
            {
                DoubleGrabUpdateCamerainfo();
                HpImageViewer.UpdateROIs();
                LogHelper.LogRunMethodEnd("", nameof(TrainFocus_OuterSingleMethod));
            }
        }
        /// <summary>聚焦流程结束后清空界面上的相机状态提示文案。</summary>
        private void DoubleGrabUpdateCamerainfo()
        {
            ChangeCameraInfo = "";
        }
        /// <summary>按单相机运行参数将相机与光源参数下发到 OM40 服务，并同步 Meas 相机属性（ShiftTo）。</summary>
        protected override void SetVisionParams(ECamera camera = ECamera.Meas)
        {
            try
            {
                //Todo double face cameraparams & lightSourceParam
                var cameraParam = TrainSettingsSingleCamera.CameraParams;
                var lightSourceParam = TrainSettingsSingleCamera.LightSourceParam;
                _cameraLightParamService40.SetupCameraAndLightParametersNormal(TestInfo, camera);
                var oldAttr = _serviceCamera.GetCameraAttr(ECamera.Meas);
                var curAttr = _serviceCamera.GetCameraAttr(ECamera.Meas);
                _serviceCamera.ShiftTo(ECamera.Meas, ECamera.Meas, oldAttr, curAttr);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.Error(ex.Message);
            }

            _visionService.NotifyParamsChanged();
        }
        /// <summary>仅内层「单面分步聚焦」流程：执行内层 <see cref="TrainFineFocus(bool)"/>；失败时恢复内层聚焦 Set 按钮可用性。</summary>
        protected override async Task TrainFocus_InnerSingleMethod()
        {
            if (TrainParam.InnerLayer.FocusROI == null)
            {
                _messageBoxService.ErrorDialog("Please draw the corresponding inner ROI！");
                return;
            }

            LogHelper.LogRunMethodStart();
            ChangeCameraInfo = "Focusing …";
            try
            {
                _visionService.StartGrabbing();
                await TrainFineFocus(true);
                FocusInnerROI_SetAfterLightControlEnabled = false;
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                string errmsg = "Inner: Focusing failed. Please modify the focusing parameters and try again.\n" + $"Detail:{ex.Message}";
                AppHelper.DispatcherInvoke(() =>
                {
                    _messageBoxService.ErrorDialog(errmsg);
                });
                FocusInnerROI_SetAfterLightControlEnabled = true;
            }
            finally
            {
                isCreatedModel = false;
                DoubleGrabUpdateCamerainfo();
                HpImageViewer.UpdateROIs();
                LogHelper.LogRunMethodEnd("", nameof(TrainFocus_InnerSingleMethod));
            }
        }
        /// <summary>将当前相机参数、光源参数（含双抓标志）及衰减系数写回单相机运行参数。</summary>
        protected override void SaveVisionParams()
        {
            try
            {
                TrainSettingsSingleCamera.CameraParams = _cameraLightParamService.GetCamearaParam(CurCamera);

                var _visionServiceManager = _serviceProvider.GetRequiredService<IVisionServiceManager>();
                var lightModuleCamera = _visionServiceManager.GetLightModuleId(CurCamera);
                TrainSettingsSingleCamera.LightSourceParam = _cameraLightParamService.GetLightSourceParam(lightModuleCamera);
                if (TrainSettingsSingleCamera.LightSourceParam is MeasureLightSourceParam40 measureLight)
                {
                    measureLight.IsDoubleGrab = _grabMode == EnumGrabMode.DoubleGrab;
                }

                SaveAttenuationCoef(CurCamera, TrainSettingsSingleCamera.CameraParams);
            }
            catch (Exception e)
            {
                LogHelper.LogError(e);
            }
        }
        /// <summary>由内外 Halcon 图像与 OM 参数字典生成 Overlay 相关性曲线列表，供曲线弹窗使用。</summary>
        protected override List<CorrelationCurve> GetCorrelationCurve(HObject imageObjectInner, HObject imageObjectOuter, HTuple hTuple)
        {
            OMUtil.GetCorrelationCurve(imageObjectInner, imageObjectOuter, hTuple, out List<CorrelationCurve> curves);
            return curves;
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
                if (_grabMode != EnumGrabMode.DoubleGrab)
                {
                    InnerFocusHeight = TrainSettingsSingleCamera.FocusModuleInfo.BaseFocusHeight + _alignmentService40.GetMacroFocusZ(CurCamera).z;
                    OuterFocusHeight = InnerFocusHeight;
                }
                else
                {
                    InnerFocusHeight = TrainSettingsSingleCamera.FocusModuleInfo.BaseFocusHeight + _alignmentService40.GetMacroFocusZ(CurCamera).z;
                    var offset = TrainSettingsSingleCamera.GetBibRelativeOffset(EnumFocusBasedOn.Inner.ToOffsetType(), EnumOvlFocusOffsetType.Outer);
                    OuterFocusHeight = InnerFocusHeight + offset;
                }
            }
        }
        /// <summary>
        /// 量测完成后：停采流、按抓取模式叠加轮廓与量测 ROI、切换显示图像、加载数值结果；
        /// CD OVL 时叠加可拖十字并缓存原始中心/Misreg 供界面补偿。
        /// </summary>
        protected override void ShowResult(OvResultBase ovResult, HObject imgObjectInner, HObject imgObjectOuter)
        {
            _visionService.StopGrabbing();
            var oMParam = TestInfo.OMParam;
            oMParam.ConvertToDict(out HTuple hTuple);
            OvResultBase centeringOVL = ovResult;
            //底层算法是以外层为中心进行量测的，如果界面上是以内层为中心，此时的ovl值需要取反
            if (TestInfo.TargetSetting.TargetCentering == EnumCalOverlayInOrOutEdge.Inner)
            {
                centeringOVL = (OvResultBase)CloneHelper.ReflectionClone(ovResult);
            }
            CorrelationCurvesList = GetCorrelationCurve(imgObjectInner, imgObjectOuter, hTuple);
            List<ROIDesc> rOIDescs = new List<ROIDesc>();
            if (_grabMode != EnumGrabMode.DoubleGrab || FocusBasedOn == EnumFocusBasedOn.Inner)
            {
                rOIDescs.AddRange(GetProfileForImageView(imgObjectInner, oMParam.OmROIInner, oMParam.TargetType));
                rOIDescs.AddRange(TrainParam.InnerLayer.MeasureROI);
            }
            if (_grabMode != EnumGrabMode.DoubleGrab || FocusBasedOn == EnumFocusBasedOn.Outer)
            {
                rOIDescs.AddRange(GetProfileForImageView(imgObjectOuter, oMParam.OmROIOuter, oMParam.TargetType));
                rOIDescs.AddRange(TrainParam.OuterLayer.MeasureROI);
            }

            HpImageViewer.FitImageToArea();
            if (_grabMode == EnumGrabMode.DoubleGrab && FocusBasedOn == EnumFocusBasedOn.Inner)
                HpImageViewer.Image = imgObjectInner.ToImageData();
            else if (_grabMode == EnumGrabMode.DoubleGrab && FocusBasedOn == EnumFocusBasedOn.Outer)
            {
                HpImageViewer.Image = imgObjectOuter.ToImageData();
            }
            else
                HpImageViewer.Image = imgObjectInner.ToImageData();//inner 和outer相同

            // TestInfo.OvResultBase = centeringOVL;
            //TrainSetting.IsTrained = true;
            SetFocusPredictionTabEnabled();
            LoadOvResult(centeringOVL);
            CaptureRawCdOvlResult(centeringOVL);
            AppendCdOvlCrosshair(rOIDescs, IsCdOvlCrossEditMode);
            HpImageViewer.UpdateROIs(rOIDescs.ToArray());
            ShowCurveControl();
        }

        /// <summary>单面「内外同一 Run」聚焦：要求内外聚焦 ROI 均已设置，依次全视野 + 双层 ROI 聚焦；成功后暂将 IsTrained 置 false 直至量测完成链路。</summary>
        protected override async Task TrainFocus_SingleMethod()
        {
            if (TrainParam.InnerLayer.FocusROI == null || TrainParam.OuterLayer.FocusROI == null)
            {
                _messageBoxService.ErrorDialog("Please draw the corresponding ROI！");
                return;
            }

            LogHelper.LogRunMethodStart();
            ChangeCameraInfo = "Focusing …";
            IsTrained = false;

            try
            {
                await TrainFineFocus();
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                string errmsg = "Focusing failed. Please modify the focusing parameters and try again.\n" + $"Detail:{ex.Message}";
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

        }

        /// <summary>
        /// 用户确认量测 ROI 后：在后台线程根据单抓/双抓模式，使用量测 ROI 重新聚焦并更新对应层的焦面与落盘图像。
        /// </summary>
        /// <param name="isInner">当前确认的是内层还是外层量测 ROI。</param>
        protected override async Task FindMeasureRoiFocusLayer(bool isInner)
        {
            bool focusSuccess = false;


            // 第1步：聚焦处理和图像抓取
            await Task.Run(async () =>
            {
                if (_grabMode == EnumGrabMode.DoubleGrab)
                {
                    if (FineResInner == null || FineResOuter == null)
                    {
                        throw new Exception("Please run inner & outer focus operation completely!");
                    }
                }
                else
                {
                    if (fine_Res == null)
                    {
                        throw new Exception("Please complete the focusing operation!");
                    }
                }
                //使用量测Roi更新聚焦位置及聚焦图片
                //采图前需要开启
                _visionService.StartGrabbing();
                // 如果是doublegrab模式，则需要在清洗位置peg采图并且保存到本地
                if (_grabMode == EnumGrabMode.DoubleGrab)
                {
                    await FocusInnerOuterDoubleGrab(true, isInner);
                }
                else
                {
                    await FocusInnerOuterSingleGrab(true, false, isInner);
                }
                _visionService.StopGrabbing();
                focusSuccess = true;
            });
        }

        /// <summary>
        /// 单面 Run 量测：加载内外焦面图，必要时重建 Kernel/模板与 R3；调用 <see cref="IMeasurementService.MeasureOverlay"/>，
        /// 再 <see cref="ShowResult"/> 刷新界面，最后 <see cref="Test_Train40ViewModelBase.Reset"/> 收尾。
        /// </summary>
        protected async override Task RunMeasureMethod()
        {
            try
            {
                ImageData img_inner = LoadFocusImageData(true);
                ImageData img_outer = LoadFocusImageData(false);

                if (IsUpdateMeasureRoi || IsInnerCorrectioned || IsOuterCorrectioned)
                {
                    JudgeFocusHeightException();

                    await CreateMeasureKernelSizeAndTemplate(true, true, img_inner);
                    await CreateMeasureKernelSizeAndTemplate(false, true, img_outer);
                    //await Task.Run((async () => await CreatePreviewTemplate(_isBaseOnInner, CONST_PREVIEW)));
                    await CreatePreviewTemplate(_isBaseOnInner, CONST_PREVIEW);
                    SaveCorrectHeight(true);
                    SaveCorrectHeight(false);
                    // 设置清晰度放到量测roi聚焦逻辑里面，因为此时的图片有可能offset焦面的图
                    //UpdateFoucsHeightSharpness(_isBaseOnInner ? img_inner : img_outer, GetFocusPegResult(FocusBasedOn == EnumFocusBasedOn.Inner).ZFit);

                    await AutoCorrectR3();
                    IsTrained = true;
                }

                // 第3步：准备测量图像
                using var imgObjectInner = img_inner?.ToHObject();
                if (imgObjectInner == null) return;

                using var imgObjectOuter = img_outer?.ToHObject();

                // 验证KernelModel
                if (TestInfo.OMParam?.KernelModelInner == null || TestInfo.OMParam?.KernelModelOuter == null || TrainParam?.InnerLayer?.MeasureROI == null || TrainParam?.OuterLayer?.MeasureROI == null)
                {
                    _messageBoxService.ErrorDialog("Please set measurement roi first!");
                    return;
                }

                // 第4步：执行测量

                HpImageViewer.UpdateROIs();//清空Roi
                OMParam oMParam = TestInfo.OMParam;
                UpdateOMPara(CurCamera, oMParam);
                SaveOMParamToFile(oMParam);
                var trainSettings = TestInfo.GetTrainSettings();
                var transMatInner = GetTransmat(trainSettings.InnerTrainMatchItem, trainSettings.InnerTrainMatchItem);
                var transMatOuter = FocusBasedOn == EnumFocusBasedOn.Inner ? transMatInner : GetTransmat(trainSettings.OuterTrainMatchItem, trainSettings.OuterTrainMatchItem);
                var (offsetX, offsetY) = Physical?.GetOvlOffset() ?? (0, 0);
                var ovResult = _measService.MeasureOverlay(imgObjectInner, imgObjectOuter, TestInfo.OMParam, transMatInner, transMatOuter, offsetX, offsetY);
                ShowResult(ovResult, imgObjectInner, imgObjectOuter);
                Reset();
            }
            catch (Exception ex)
            {
                _messageBoxService.ErrorDialog($"{ex.Message}");
            }
        }

        /// <summary>
        /// SingleGrab-Averagae 两层都取 Average的图
        /// SingleGrab 两层都取BaseOn的图
        /// DoubleGrab 取内、外层不同的图
        /// </summary>
        /// <param name="isInner">是否为inner</param>
        /// <returns></returns>
        protected override string GetImageName(bool isInner)
        {
            if (_grabMode == EnumGrabMode.SingleGrab)
            {
                return _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner ? CONST_Inner : CONST_Outer;
            }
            else if (_grabMode == EnumGrabMode.DoubleGrab)
            {
                return isInner ? CONST_Inner : CONST_Outer;
            }
            else
            {
                return CONST_Average;
            }
        }
        /// <summary>对话框打开时：注入 TestInfo、区分单抓/单抓平均、初始化光学模块与聚焦预测 Tab 的 ViewModel。</summary>
        public override void OpenDialogThread(IDialogParameters parameters)
        {
            _testTrainService.TestInfo = TestInfo;

            base.OpenDialogThread(parameters);

            IsFocusSingle = _grabMode == EnumGrabMode.SingleGrab_Average || _grabMode == EnumGrabMode.SingleGrab;

            // 初始化光学模块选择器
            InitializeOpticalModuleSelector();
            // 初始化聚焦预测 ViewModel
            FocusPrediction = new Test_TrainFocusPredictionViewModel(this, _serviceProvider,
                TestInfo, TrainParam, () => HpImageViewer);

        }

        /// <summary>用户切换到「Focus Prediction」Tab 时，初始化预测子 ViewModel 的数据与订阅。</summary>
        [RelayCommand]
        private void FocusPredictionChecked()
        {
            FocusPrediction?.Initialize(TestInfo.TrainSetting,TrainParam );
        }

        /// <summary>根据视觉服务当前光源模块（Meas / MeasExt）同步单选索引，失败时默认 Up。</summary>
        private void InitializeOpticalModuleSelector()
        {
            try
            {
                var visionManager = _serviceProvider.GetRequiredService<IVisionServiceManager>();
                var currentLightModule = visionManager.GetLightModuleId(CurCamera);
                SelectedOpticalModuleIndex = currentLightModule == ECamera.Meas ? 0 : 1;

                LogHelper.LogInfo($"Initialized optical module: {SelectedOpticalModuleIndex}");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"InitializeOpticalModuleSelector error: {ex.Message}");
                SelectedOpticalModuleIndex = 0; // 默认 Up
            }
        }

        /// <summary>对话框关闭时广播事件，允许界面上的光源选择等控件恢复可用。</summary>
        public override void OnDialogClosed()
        {
            // 通知光源控件：训练对话框已关闭，启用光源选择
            _eventAggregator?.GetEvent<TrainDialogStateEvent>().Publish(false);

            base.OnDialogClosed();
        }

        /// <summary>将算法生成的 OM ROI 与 Kernel 模型写入当前 Test 的 <see cref="OMParam"/>（内/外分别字段）。</summary>
        protected override void SaveKernelModel(bool isInner, OMROIBase oMROIBase, OMKernelModel oMKernelModel)
        {
            if (isInner)
            {
                TestInfo.OMParam.OmROIInner = oMROIBase;
                TestInfo.OMParam.KernelModelInner = oMKernelModel;
            }
            else
            {
                TestInfo.OMParam.OmROIOuter = oMROIBase;
                TestInfo.OMParam.KernelModelOuter = oMKernelModel;
            }
        }

        /// <summary>根据抓取模式与是否单面，初始化焦面补偿区可见性及内外层补偿高度/数值显示绑定源。</summary>
        protected override void InitCorrections()
        {
            CorrectionVisibility = _grabMode != EnumGrabMode.SingleGrab_Average && _focusSetting.IsEnableFocusCorrection ? Visibility.Visible : Visibility.Collapsed;
            InnerCorrectionVisibility = !IsFocusSingle || _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner ? Visibility.Visible : Visibility.Collapsed;
            OuterCorrectionVisibility = !IsFocusSingle || _focusSetting.FocusBasedOn == EnumFocusBasedOn.Outer ? Visibility.Visible : Visibility.Collapsed;
            InnerCorrectionValue = TrainParam.InnerLayer.FocusCorrection;
            OuterCorrectionValue = TrainParam.OuterLayer.FocusCorrection;
            InnerCorrectionHeight = InnerFocusHeight + TrainParam.InnerLayer.FocusCorrection;
            OuterCorrectionHeight
                = OuterFocusHeight + TrainParam.OuterLayer.FocusCorrection;
        }
        /// <summary>页面加载：在基类 Loaded 之后广播训练窗打开、绑定定位模板控件、按配方恢复光学模块并决定是否启用聚焦预测 Tab。</summary>
        public override async Task Loaded()
        {
            await base.Loaded();
            // 通知光源控件：训练对话框已打开，禁用光源选择
            _eventAggregator?.GetEvent<TrainDialogStateEvent>().Publish(true);

            // CrossFov 由 Test_Train40ViewModel_CrossFov.BindCrossFovPositioningTemplates 单独绑定内外定位控件
            if (PositioningTemplateControl != null && TestInfo?.CrossFov != true)
            {
                PositioningTemplateControl.TemplateContext = PositioningModel.Template;
                PositioningTemplateControl.ActionUserModified = PositionTemplateUserModified;
                PositioningTemplateControl.FuncBeforeCreateModel = () => BeforeCreatePostionModel(_isBaseOnInner);
                if (TestInfo.TargetSetting.EnabledTargetPoints.Count() > 1)
                {
                    CheckPositionModelForeachVisible = Visibility.Visible;
                }
                else
                {
                    CheckPositionModelForeachVisible = Visibility.Collapsed;
                    TestInfo.GetTrainSettings().PositioningTemplateInfo.IsForEachTarget = true;
                }
            }
            LoadSelectedOpticalModule();
            SetFocusPredictionTabEnabled();
        }

        private void SetFocusPredictionTabEnabled()
        {
            IsFocusPredictionTabEnabled = FocusPredictionTabPolicy.CanOpen(
                TrainSettingsSingleCamera.InnerType,
                TrainSetting?.IsTrained ?? false);
        }

        /// <summary>从配方中的光源参数恢复上次选择的 Up/Down 模组，并应用硬件配置与界面显示。</summary>
        private void LoadSelectedOpticalModule()
        {
            if (TrainSettingsSingleCamera.LightSourceParam is MeasureLightSourceParam40 measureLight40)
            {
                SelectedOpticalModuleIndex = measureLight40.Camera == ECamera.Meas ? 0 : 1;
            }
            UpdateOpticalModuleConfiguration(SelectedOpticalModuleIndex, true);
            UpdateOpticalModuleDisplayValues(); // 新增：配置完成后更新显示值
        }

        /// <summary>
        /// 创建定位模板的通用方法
        /// TODO 待整合
        /// </summary>
        /// <param name="isInner">是否为内层相机</param>
        /// <returns>操作是否成功</returns>
        protected virtual bool BeforeCreatePostionModel(bool isInner)
        {
            return BeforeCreatePositionModel_Common(isInner,
                                        () => (CurAxis, CurCamera, PositioningModel));
        }

        /// <summary>焦面补偿确认后按模式重新抓取量测用图（不带 ROI，避免用补偿图重算 Kernel）。</summary>
        protected override async void SaveFocusImage_ByCorrectionMove(bool isInner)
        {
            //此处的Roi 需要置为空，不能使用补偿后的焦面获取FocusKernel
            if (_grabMode == EnumGrabMode.DoubleGrab)
            {
                var imgName = isInner ? CONST_Inner : CONST_Outer;
                await SaveFocusImage_ByGrab(imgName, _visionService, isInner, null, isMeasure: true);
            }
            else
            {
                //单面拍照模式下，一次性保存Inner和Outer的图片
                await SaveFocusImage_ByGrab(CONST_Inner, _visionService, true, null, isMeasure: true);
                await SaveFocusImage_ByGrab(CONST_Outer, _visionService, false, null, isMeasure: true);
            }
        }

        /// <summary>单面 Overlay 内外层共用当前量测相机。</summary>
        protected override ECamera GetECamera(bool isInner)
        {
            return CurCamera;
        }

        /// <summary>是否已在对应层完成过焦面补偿采图（单面时由基准层决定一侧即可）。</summary>
        protected override bool IsCorrectionGrabbed(bool isInner)
        {
            if (_focusSetting.IsEnableFocusCorrection)
            {
                if (IsFocusSingle)
                {
                    return _isBaseOnInner ? IsInnerCorrectioned : IsOuterCorrectioned;
                }
                else
                {
                    return isInner ? IsInnerCorrectioned : IsOuterCorrectioned;
                }
            }
            return false;
        }
        #endregion


        #region 定位模板逻辑

        /// <summary>将示教点经当前基准层的量测模板匹配，得到机台坐标系下的目标点。</summary>
        protected override Point2d GetTargetAxisPoint(Point2d teachingPoint)
        {
            var measureModels = _isBaseOnInner ? TrainSettingsSingleCamera.OverlayMark_InnerModels[0] : TrainSettingsSingleCamera.OverlayMark_OuterModels[0];
            return _alignmentService.FindSingleTemplate(teachingPoint, measureModels, true, false, out _);
        }

        //protected override Point2d GetTarget0Point(ImageData imagedata, Point2d teachingPoint)
        //{
        //    var res = _alignmentService.FindTemplateForRun(imagedata, GetModel(_isBaseOnInner), false);
        //    if (res.IsSuccess)
        //    {
        //        TargetPoint = _alignmentService.ConvertImageToAxis(teachingPoint, res.BestMatchItem, imagedata.Width, imagedata
        //            .Height);
        //        return TargetPoint;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}        //protected override Point2d GetTarget0Point(ImageData imagedata, Point2d teachingPoint)
        //{
        //    var res = _alignmentService.FindTemplateForRun(imagedata, GetModel(_isBaseOnInner), false);
        //    if (res.IsSuccess)
        //    {
        //        TargetPoint = _alignmentService.ConvertImageToAxis(teachingPoint, res.BestMatchItem, imagedata.Width, imagedata
        //            .Height);
        //        return TargetPoint;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        /// <summary>单面将预匹配模板上下文写入配方（不按内外拆分）。</summary>
        protected override void SetPreviewTemplate(bool isInner, SingleTemplateMatchingContext template)
        {
            // 单面不需要isInner判断
            TrainSettingsSingleCamera.PreviewTemplate = template;
        }
        #endregion

    }
}
