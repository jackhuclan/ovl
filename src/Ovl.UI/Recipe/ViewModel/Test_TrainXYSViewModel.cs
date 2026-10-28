using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using HarfBuzzSharp;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.TemplateMatching.Core;
using Marvel.Application.Services;
using Marvel.Application.Services;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.AIM;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using OVL.Algorithm.DoNet;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Point = HPImageViewer.Core.Primitives.Point;

namespace Ovl.UI.Recipe.ViewModel
{
    public partial class Test_TrainXYSViewModel : Test_TrainOverlayViewModel<TestInfo_AIM_XYS>
    {
        public Test_TrainXYSViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            
        }
         

        [ObservableProperty]
        string _tableFirst;

        [ObservableProperty]
        string _tableSecond;

        [ObservableProperty]
        Test_TrainSetRoiViewModel _setRoiVM1;
        [ObservableProperty]
        Test_TrainSetRoiViewModel _setRoiVM2;
        [ObservableProperty]
        Test_TrainSetRoiViewModel _lastRoiVM;
        [ObservableProperty]
        Test_TrainFocusPredictionViewModel _focusPrediction;

        [ObservableProperty]
        string _profileFirst_Inner;

        [ObservableProperty]
        string _profileFirst_Outer;

        [ObservableProperty]
        string _profileSecond_Inner;

        [ObservableProperty]
        string _profileSecond_Outer;

        [ObservableProperty]
        int _selectedProfile;

        TrainSettings_XYS trainSettings_XYS;
        /// <summary>
        /// 第一个方向的训练参数
        /// </summary>
        TestOverlayTrainParam _firstDirectionTrainParam;
        public override void OpenDialogThread(IDialogParameters parameters)
        {
            base.OpenDialogThread(parameters);
            trainSettings_XYS=TrainSetting as TrainSettings_XYS;
            var layoutSetup = TestInfo.PhysicalXYS.LayoutSetup;
          
            //FocusPrediction.Initialize();
            switch (layoutSetup)
            {
                case EnumOptimizeType.X:
                    _firstDirectionTrainParam = trainSettings_XYS.TrainParamX;
                    SetRoiVM1 = new Test_TrainSetRoiViewModel(this, _serviceProvider, TestInfo,
                        _firstDirectionTrainParam, true);
                    break;
                case EnumOptimizeType.Y:
                    _firstDirectionTrainParam = trainSettings_XYS.TrainParamY;
                    SetRoiVM1 = new Test_TrainSetRoiViewModel(this, _serviceProvider, TestInfo, _firstDirectionTrainParam, true);
                    break;
                default:
                    _firstDirectionTrainParam = trainSettings_XYS.TrainParamX;
                    SetRoiVM1 = new Test_TrainSetRoiViewModel(this, _serviceProvider, TestInfo, _firstDirectionTrainParam, true);
                    SetRoiVM2 = new Test_TrainSetRoiViewModel(this, _serviceProvider, TestInfo, trainSettings_XYS.TrainParamY, false);
                    break;
            }
            FocusPrediction = new Test_TrainFocusPredictionViewModel(this, _serviceProvider, TestInfo, _firstDirectionTrainParam , () => HpImageViewer);
            SetRoiVM1.Checked();
            SetRoiVM1.ActionSetFocusRoi = SetVisionParamsEnable;
            SetRoiVM1.ActionConfirmFocusRoi = SetVisionParamsDisable;
            SetRoiVM1.ActionFocusCompleted = OnFocusCompleted;
            if(SetRoiVM2 != null)
            {
                SetRoiVM2.ActionSetFocusRoi = SetVisionParamsEnable;
                SetRoiVM2.ActionConfirmFocusRoi = SetVisionParamsDisable;
            }
            LastRoiVM = SetRoiVM2 ?? SetRoiVM1;
            TableFirst = SetRoiVM1?.TrainParam.DirectionTypeStr;
            TableSecond = SetRoiVM2?.TrainParam.DirectionTypeStr;
            ProfileFirst_Inner = "Inner-" + TableFirst;
            ProfileFirst_Outer = "Outer-" + TableFirst;
            if (!string.IsNullOrWhiteSpace(TableSecond))
            {
                ProfileSecond_Inner = "Inner-" + TableSecond;
                ProfileSecond_Outer = "Outer-" + TableSecond;
            }
            SelectedProfile = -1;
            SelectedProfile = 0;
            TrainSettingVisible=System.Windows.Visibility.Visible;
        }

      

        [RelayCommand]

        private void FirstTableChecked()
        {
            SetRoiVM1?.Checked();
            ChangeDirectionVisionParams(true);
        }

        [RelayCommand]

        private void SecondTableChecked()
        {
            SetRoiVM2?.Checked();
            ChangeDirectionVisionParams(false);
        }
        [RelayCommand]

        private void FocusPredictionChecked()
        {
            FocusPrediction.Initialize(TrainSetting_Overlay, _firstDirectionTrainParam);
        }

        private async void ChangeDirectionVisionParams(bool isFirstDirection)
        {
            ChangeCameraInfo = "Changing vision params...";
            await Task.Run(() =>
            {
                try
                {
                    ITrainSettingsVisionParam visionParam = isFirstDirection ? trainSettings_XYS : trainSettings_XYS.VisionParams_Y;
                    if (visionParam != null)
                    {
                        if (!isFirstDirection)
                        {
                            var visionParamY = visionParam.LightSourceParam as MeasureLightSourceParam;
                            var visionParamX = trainSettings_XYS.LightSourceParam as MeasureLightSourceParam;
                            if (SetRoiVM2.IsSameNA)
                            {
                                visionParamY.Na = visionParamX.Na;
                            }
                            if (SetRoiVM2.IsSamePolarizer)
                            {
                                visionParamY.PoliarizerIndex = visionParamX.PoliarizerIndex;
                            }
                            if (SetRoiVM2.IsSameFilter)
                            {
                                visionParamY.Filter = visionParamX.Filter;
                                visionParamY.Wave = visionParamX.Wave;
                            }
                        }
                        _cameraLightParamService.SetTrainSettinsParam(IServiceCamera.CurCamera, visionParam);
                        _visionService.NotifyParamsChanged();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError(ex);
                    _messageBoxService.ErrorDialog(ex.Message);
                }
                finally
                {
                    ChangeCameraInfo = "";
                }
            });
        }

        private ImageData LoadLayerImage(bool isInner, EnumOptimizeType type)
        {
            var vm = SetRoiVM1.TrainParam.DirectionType == type ? SetRoiVM1 : SetRoiVM2;
            if (vm == null) vm = SetRoiVM1;
            return vm.LoadFocusImageData(isInner);
        }


        protected override ImageData LoadTemplateModeImage(bool isInner)
        {
            try
            {
                return SetRoiVM1.LoadFocusImageData(isInner);
            }
            catch
            { return null; }
        }

        protected override ROIDesc GetMeasureRoiDesc(bool isInner)
        {
            return SetRoiVM1.TrainParam.GetRoiDesc(isInner, true);
        }




        /// <summary>
        ///  创建AIM外框 kernel Size调整框
        /// </summary>
        private void GenKernelRoiOfAIMXYS(bool isInner)
        {
            var rectRoiX = trainSettings_XYS.TrainParamX.GetRoiDesc(isInner, true) as AimXYSDesc;
            var rectRoiY = trainSettings_XYS.TrainParamY.GetRoiDesc(isInner, true) as AimXYSDesc;
            var xysAim2dX = rectRoiX?.CreateAimRoi<XYSAim2d>();
            var xysAim2dY = rectRoiY?.CreateAimRoi<XYSAim2d>();
            var layout = TestInfo.PhysicalXYS.LayoutSetup;
            if (layout == EnumOptimizeType.X)
            {
                xysAim2dY = xysAim2dX;
            }
            else if (layout == EnumOptimizeType.Y)
            {
                xysAim2dX = xysAim2dY;
            }
            var exResult = OMUtil.GenKernelRoiOfXYSAIM(xysAim2dX, xysAim2dY, out XYSAIMROI kernelRoi);
            using var imageX = LoadLayerImage(isInner, EnumOptimizeType.X)?.ToHObject();
            using var imageY = LoadLayerImage(isInner, EnumOptimizeType.Y)?.ToHObject();
            OMUtil.GenKernelModelOfXYS(imageX, imageY, kernelRoi, OMTargetType.XYSAIM, out OMKernelModel kernelModel);

            SaveKernelModel(isInner, kernelRoi, kernelModel);

        }


        protected override void AutoCreateKernelModel(bool isInner)
        {
            switch (TrainSetting_Overlay.InnerType)
            {

                case EnumTargetType.AIM_XYS:
                    GenKernelRoiOfAIMXYS(isInner);
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        async void Run()
        {
            try
            {
                ChangeCameraInfo = "Running...";
                var _measService = _serviceProvider.GetService<IMeasurementService>();
                //此处只是用来调试时验证数据使用的，需要加断点动态修改isTest的值
                bool isTest = true;//此处为了便于在调试中拖动代码
                isTest = false;
                if (isTest)
                {
                    DebuggerService.DebugXYS(TestInfo, _measService, _alignmentService, false);
                    return;
                }
                if (FocusPosition != null)
                {
                    _alignmentService.MoveTo(FocusPosition);
                    await Task.Run(async () =>
                    {
                        LogHelper.LogRunMethodStart();
                        await SetRoiVM1.SaveMeasureFocus();
                        if (SetRoiVM2 != null)
                        {
                            await SetRoiVM2?.SaveMeasureFocus();
                        }

                    });
                    if (SetRoiVM1.IsUpdateMeasureRoi || SetRoiVM1.IsInnerCorrectioned)
                    {
                        if (!InnerTemplateBeforeCreateModel())
                            return;
                        //自动创建定位模板和 kernel Model
                        await CreateMeasureKernelSizeAndTemplate(true, false);
                    }
                    if (SetRoiVM1.IsUpdateMeasureRoi || SetRoiVM1.IsOuterCorrectioned)
                    {
                        if (!OuterTemplateBeforeCreateModel())
                            return;
                        await CreateMeasureKernelSizeAndTemplate(false, false);
                    }
                    if (SetRoiVM1.IsUpdateMeasureRoi)
                    {
                        await AutoCorrectR3();
                    }
                    SetRoiVM1.Reset();
                    if (SetRoiVM2 != null)
                    {
                        SetRoiVM2.Reset();
                    }
                    SaveOMParamDict();
                    GetTrainSettings().IsTrained = true;
                    LogHelper.LogInfo($"Train test xys success, testName={TestInfo?.Name} ");
                }

                if (_omParam?.KernelModelInner == null || _omParam?.KernelModelOuter == null || SetRoiVM1.TrainParam?.InnerLayer?.MeasureROI == null || SetRoiVM1.TrainParam?.OuterLayer?.MeasureROI == null)
                {
                    throw new Exception("Please set measurement roi of X first!");// ("X方向的Roi 未设置完整！");

                }
                if (SetRoiVM2 != null && (SetRoiVM2?.TrainParam?.InnerLayer?.MeasureROI == null || SetRoiVM2.TrainParam?.OuterLayer?.MeasureROI == null))
                {
                    throw new Exception("Please set measurement roi of Y first!");// ("Y方向的Roi 未设置完整！");
                }
                _visionService.StopGrabbing();
                //清空Roi
                HpImageViewer.UpdateROIs();

                int layout = (int)TestInfo.PhysicalXYS.LayoutSetup;
                var x = 0;// TestInfo.TrainSetting_BiB.InnerRectangle.CenterX;
                var y = 0;// TestInfo.TrainSetting_BiB.InnerRectangle.CenterY;
                var transMat = _measService.GetTransMat(y, x, new MatchingItem() { Column = x, Row = y }, 0);
                using var imgObjectInner_X = LoadLayerImage(true, EnumOptimizeType.X)?.ToHObject();
                using var imgObjectOuter_X = LoadLayerImage(false, EnumOptimizeType.X)?.ToHObject();
                using var imgObjectInner_Y = LoadLayerImage(true, EnumOptimizeType.Y)?.ToHObject();
                using var imgObjectOuter_Y = LoadLayerImage(false, EnumOptimizeType.Y)?.ToHObject();
                var ovResult = _measService.MeasureAimXYS(imgObjectInner_X, imgObjectInner_Y, imgObjectOuter_X, imgObjectOuter_Y, _omParam, transMat, transMat, layout);
                if (ovResult.MisregX == 0 && ovResult.MisregY == 0)
                {
                    //量测结果异常，请重新聚焦、画量测roi再重试
                    throw new Exception("The measurement result is abnormal. Please refocus, draw a measurement ROI, and try again!");
                }
                // ovResult = _measService.MeasureOverlay(imgObjectInner_X, imgObjectInner_Y, _omParam, transMat, transMat);
                ShowResult(ovResult, imgObjectInner_X, imgObjectOuter_X, imgObjectInner_Y, imgObjectOuter_Y);

                Submit(true);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.ErrorDialog(ex.Message);
            }
            finally
            {
                ChangeCameraInfo = "";
                LogHelper.LogRunMethodEnd();
            }

        }

        protected void ShowResult(OvResultBase ovResult, HObject imgObjectInner_X, HObject imgObjectOuter_X, HObject imgObjectInner_Y, HObject imgObjectOuter_Y)
        {

            OvResultBase centeringOVL = ovResult;
            //底层算法是以外层为中心进行量测的，如果界面上是以内层为中心，此时的ovl值需要取反
            if (TestInfo.TargetSetting.TargetCentering == EnumCalOverlayInOrOutEdge.Inner)
            {
                centeringOVL = (OvResultBase)CloneHelper.ReflectionClone(ovResult);
            }
            LoadOvResult(centeringOVL);
            var oMParam = _omParam;
            oMParam.ConvertToDict(out HTuple hTuple);
            OMUtil.GetCorrelationCurveOfXYS(imgObjectInner_X, imgObjectInner_Y, imgObjectOuter_X, imgObjectOuter_Y, hTuple, out List<CorrelationCurve> curves);
            CorrelationCurvesList = curves;
            LoadProfile();
            ShowCurveControl();

        }



        [RelayCommand]
        private void LoadProfile()
        {
            bool isInner = true;
            EnumOptimizeType type;
            if (IsFocusSingle)
            {
                if (SelectedProfile == 1) type = EnumOptimizeType.Y;
                else type = SetRoiVM1.TrainParam.DirectionType;
            }
            else
            {
                isInner = SelectedProfile % 2 == 0;
                if (SelectedProfile >= 2) type = EnumOptimizeType.Y;
                else type = SetRoiVM1.TrainParam.DirectionType;
            }


            //List<ROIDesc> rOIDescs = new List<ROIDesc>();
            //if (_isSingleGrab || FocusBasedOn == EnumFocusBasedOn.Inner)
            //{
            //    rOIDescs.AddRange(GetProfileForImageView(imgObjectInner, oMParam.OmROIInner, oMParam.TargetType));
            //    //rOIDescs.Add(TestTrainSetting.InnerRectangle);
            //}
            //if (_isSingleGrab || FocusBasedOn == EnumFocusBasedOn.Outer)
            //{
            //    rOIDescs.AddRange(GetProfileForImageView(imgObjectOuter, oMParam.OmROIOuter, oMParam.TargetType));
            //    //  rOIDescs.Add(TestTrainSetting.OuterRectangle);
            //}

            HpImageViewer.FitImageToArea();
            using var imgHobj = LoadLayerImage(isInner, type)?.ToHObject();
            HpImageViewer.Image = imgHobj.ToImageData();
            var omRoi = isInner ? _omParam.OmROIInner : _omParam.OmROIOuter;
            var rOIDescs = GetProfileForImageView(imgHobj, imgHobj, omRoi, type);
            if (IsFocusSingle)
            {
                omRoi = isInner ? _omParam.OmROIOuter : _omParam.OmROIInner;
                rOIDescs.AddRange(GetProfileForImageView(imgHobj, imgHobj, omRoi, type));
            }
            HpImageViewer.UpdateROIs(rOIDescs.ToArray());
        }

        protected List<ROIDesc> GetProfileForImageView(HObject imageObjectX, HObject imageObjectY, OMROIBase omRoi, EnumOptimizeType type)
        {
            List<ROIDesc> rOIDescs = new List<ROIDesc>();
            OMUtil.GetProfileForImageViewOfXYS(imageObjectX, imageObjectY, omRoi, _omParam.TargetType, out List<ProjectionProfile> profileListX, out List<ProjectionProfile> profileListY);
            if (type != EnumOptimizeType.Y)
            {
                foreach (var item in profileListX)
                {
                    List<Point> pointListInner = new List<Point>();
                    for (int i = 0; i < item.Xs.Count(); i++)
                    {
                        pointListInner.Add(new Point(item.Xs[i], item.Ys[i]));
                    }
                    rOIDescs.Add(new PolygonDesc() { Vertices = pointListInner, IsClosed = false });
                }
            }
            if (type != EnumOptimizeType.X)
            {
                foreach (var item in profileListY)
                {
                    List<Point> pointListInner = new List<Point>();
                    for (int i = 0; i < item.Xs.Count(); i++)
                    {
                        pointListInner.Add(new Point(item.Xs[i], item.Ys[i]));
                    }
                    rOIDescs.Add(new PolygonDesc() { Vertices = pointListInner, IsClosed = false });
                }
            }
            return rOIDescs;
        }
    }
}
