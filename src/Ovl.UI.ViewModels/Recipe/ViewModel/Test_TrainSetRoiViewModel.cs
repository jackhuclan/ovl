using DataModels = Marvel.DataModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.ComUI.Helpers;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.SystemConfig.Interface;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using Marvel.DataModels.Core;
using Marvel.DataModels.DeviceParameter;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using OVL.Algorithm.DoNet;
using Prism.Services.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HalconDotNet;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using InteVega.CBB.Prism.UI.Extensions.Dialogs;
using InteVega.CBB.TemplateMatchingModule.Views;
using Wafer.Algorithm.Common.DoNet;
using Marvel.Application.Extentions;
using InteVega.CBB.SectionManager.Interface;
using System.Collections.Generic;
using Marvel.DataModels.Recipe.CD;
using Marvel.Application.Services;
using Megarobo.Ovl.Service.IntfFn;

using Ovl.UI.ViewModels.Controls;
namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
    public partial class Test_TrainSetRoiViewModel : ObservableObject
    {
        public Test_TrainSetRoiViewModel(Test_TrainViewModelBase trainViewMode, IServiceProvider serviceProvider, TestInfo_Overlay testinfo, TestOverlayTrainParam trainParam, bool isFirstDirection)
        {

            _serviceProvider = serviceProvider;
            _isFirstDirection = isFirstDirection;
            TestInfo = testinfo;
            _test_TrainViewModelBase = trainViewMode;

            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
            _alignmentService = serviceProvider.GetRequiredService<IAlignmentService>();
            _visionService = serviceProvider.GetService<IVisionServiceManager>().GetMainVision();
            _focusService = serviceProvider.GetService<IFocusService>();
            _templateMatchingService = serviceProvider.GetService<ITemplateMatchingService>();
            _cameraLightParamService = serviceProvider.GetService<ICameraLightParamService>();
            _wliService = serviceProvider.GetService<IWliService>();
            _pztservice = serviceProvider.GetService<IAsyncAblePztService>();
            _motionControllerService = serviceProvider.GetService<IMotionControllerService>();
            _lightServiceForMeasure = serviceProvider.GetService<ILightServiceForMeasure>();
            _testTrainService = serviceProvider.GetService<ITestTrainService>();
            _setRoiService = serviceProvider.GetService<ISetRoiService>();
            
            TrainParam = trainParam;
            IsFocusSingle = _isSingleGrab;
            _isCPLMode = Physical.TestSettings.GrabMode == EnumGrabMode.CPL;


            if (testinfo.OMParam.AlgorithmParam is BIBAlgorithmParam)
            {
                var a = testinfo.TrainSetting as TrainSettings_Overlay;
                var innerType = a?.InnerType;
                if (innerType != EnumTargetType.Cross)
                {
                    UseCross4rectan = TrainParam.OuterLayer.UseNewCross;
                    CrossUseBib = false;
                }
            }
        }



        public Action<bool, bool> ActionSetFocusRoi;
        public Action ActionConfirmFocusRoi;
        public Action ActionFocusCompleted;
        //protected const string CONST_Average = "Average";
        //protected const string CONST_Inner = "Inner";
        //protected const string CONST_Outer = "Outer";
        const string CONST_PREVIEW = "PreviewMatch";
        const string InnerColor = "#FFFF00";
        const string OutColor = "#00FFFF";
        protected string _testFilesFolder = string.Empty;
        protected bool _isFirstDirection;
        /// <summary>
        /// 白光干涉聚焦训练高度
        /// </summary>
        protected double? wliPztOffset;
        protected IServiceProvider _serviceProvider;
        protected ITemplateMatchingService _templateMatchingService;
        protected IAlignmentService _alignmentService;
        protected IWliService _wliService;
        protected IFocusService _focusService;
        protected IAsyncAblePztService _pztservice;
        protected IMotionControllerService _motionControllerService;
        protected IVisionService _visionService;
        protected ICameraLightParamService _cameraLightParamService;
        protected IMessageBoxService _messageBoxService;
        protected ILightServiceForMeasure _lightServiceForMeasure;
        protected ITestTrainService _testTrainService;
        protected ISetRoiService _setRoiService;
        protected TestInfo_Overlay TestInfo;


        protected Test_TrainViewModelBase _test_TrainViewModelBase;
        public IHPImageViewer HpImageViewer
        {
            get { return _test_TrainViewModelBase.HpImageViewer; }
        }
         
        TrainSettings_Overlay TestTrainSetting
        {
            get { return _testTrainService.TrainSetting as TrainSettings_Overlay; }
        }

        Physical Physical
        {
            get
            {
                return TestInfo.Physical as Physical;
            }
        }

        protected FocusPhysical _focusSetting
        {
            get
            {
                return Physical.FocusSettings as FocusPhysical;
            }
        }

        protected bool _isBaseOnInner
        {
            get
            { return _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner; }
        }



        EnumGrabMode _grabMode
        {
            get
            { return Physical.TestSettings.GrabMode; }
        }

        bool _isSingleGrab
        {
            get
            { return _grabMode == EnumGrabMode.SingleGrab_Average || _grabMode == EnumGrabMode.SingleGrab; }
        }
        /// <summary>
        /// 是否更改过内层的量测roi 
        /// </summary>
        public bool IsUpdateMeasureRoi { get; private set; }

        public TestOverlayTrainParam TrainParam { get; private set; }


        [ObservableProperty]
        private double _innerFocusHeight;
        [ObservableProperty]
        private double _outerFocusHeight;

        [ObservableProperty]
        private double _innerPztOffset;
        [ObservableProperty]
        private double _outerPztOffset;
        [ObservableProperty]
        private double _autoFocusHeight;

        [ObservableProperty]
        private bool _focusROI_SetEnabled;
        [ObservableProperty]
        private bool _focusROI_InnerEnabled;
        [ObservableProperty]
        private bool _focusROI_OuterEnabled;
        [ObservableProperty]
        private bool _measurementROI_SetEnabled;
        [ObservableProperty]
        private bool _measurementROI_InnerEnabled;
        [ObservableProperty]
        private bool _measurementROI_OuterEnabled;
        [ObservableProperty]
        private bool _searchROI_SetEnabled;
        [ObservableProperty]
        private bool _isFocusSingle;
        [ObservableProperty]
        private bool _isShowSearchArea = true;

        [ObservableProperty]
        private bool _isShowTempRoi =true ;

        [ObservableProperty]
        private bool _isShowMeasureRoi = false;


        [ObservableProperty]
        private bool _isComplete;

        [ObservableProperty]
        private bool _useCross4rectan;

        [ObservableProperty]
        private bool _crossUseBib = true;
       /// <summary>
       /// 白光干涉聚焦高度绝对Z轴高度
       /// </summary>
       [ObservableProperty]
        double _absolateWliHeight;

        /// <summary>
        /// 是否显示白光干涉参数
        /// </summary>
        [ObservableProperty]
        Visibility _wliVisible;


        /// <summary>
        /// 全行程聚焦结果
        /// </summary>
        protected FocusPegResult fine_Res;

        protected bool IsEnableWLI
        {
            get
            {
                return _testTrainService.IsEnableWLI;
            }
        }

        partial void OnUseCross4rectanChanged(bool value)
        {
            if (TestInfo?.OMParam?.AlgorithmParam is not BIBAlgorithmParam)
            {
                return;
            }

            TrainParam.OuterLayer.UseNewCross = value;
            var outerRoi = TrainParam.OuterLayer.MeasureROI;
            if (!value && outerRoi is Cross4RectangleDesc)
            {
                TrainParam.OuterLayer.MeasureROI = null;
            }
            else if (value && outerRoi is Cross2dRectangleDesc or QuadRectangleDesc)
            {
                TrainParam.OuterLayer.MeasureROI = null;
            }
        }

        public void Checked()
        {
            InitFocusRoiButton();
            InitMeasurementRoiButton();
            InitPztOffsetCorrections();

            CheckComplete();

            _setRoiService.SetCurretnTrainParam(TrainParam, _isFirstDirection);
            if (TestTrainSetting.FocusModuleInfo.BaseFocusHeight == 0)
            {
                TestTrainSetting.FocusModuleInfo.BaseFocusHeight = _alignmentService.WaferMapInfo.Thickness;
            }
            WliVisible = IsEnableWLI ? Visibility.Visible : Visibility.Collapsed;
            if (IsEnableWLI && TestTrainSetting.FocusModuleInfo.WliFocusOffset != 0)
            {
                AbsolateWliHeight = TestTrainSetting.FocusModuleInfo.BaseFocusHeight + TestTrainSetting.FocusModuleInfo.WliFocusOffset + _alignmentService.MacroFocusZ;

            }
            InnerFocusHeight = CombineZPzt(TestTrainSetting.FocusModuleInfo.BaseFocusHeight, TrainParam.InnerLayer.SharpnessFocusOffset) + _alignmentService.MacroFocusZ;
            OuterFocusHeight = CombineZPzt(TestTrainSetting.FocusModuleInfo.BaseFocusHeight, TrainParam.OuterLayer.SharpnessFocusOffset) + _alignmentService.MacroFocusZ;
            IsShowSearchArea = _isFirstDirection;
            IsShowTempRoi = TrainParam.InnerLayer.FocusROI == null || TrainParam.OuterLayer.FocusROI == null;
          
            InitXYSVisionParams();
        }

        private void CheckComplete()
        {
            IsComplete = TrainParam.InnerLayer.MeasureROI != null && TrainParam.OuterLayer.MeasureROI != null;
        }

        protected double CombineZPzt(double z, double pzt)
        {
            if (_pztservice != null)
            {
              return _pztservice.CombineZPzt(z, pzt);
            }
            return z + pzt  ;
        }
        #region Focus ROI

        /// <summary>
        /// 设置临时聚焦Roi
        /// </summary>
        /// <param name="isInner"></param>
        private void SetTempFocusRoi(bool isInner)
        {
            InitMeasurementRoiButton();
            if (HpImageViewer.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return;
            }
            var roi = TrainParam.GetRoiDesc(isInner, false);
            if (roi != null)
                HpImageViewer.UpdateROIs((RectangleDesc)CloneHelper.ReflectionClone(roi));
            else
                FocusROI(isInner);
            ProgressDialogHelper.ShowDialog("Setting light source param", () =>
            {
                SetCPLParam(isInner);

                SetCPLVisionState(isInner);
                return true;
            });
            FocusROI_SetEnabled = false;
            FocusROI_InnerEnabled = isInner;
            FocusROI_OuterEnabled = !isInner;
            ActionSetFocusRoi?.Invoke(isInner, _isFirstDirection);

        }

        /// <summary>
        /// 
        /// </summary>
        [RelayCommand]
        public void InnerFocusROI_Set()
        {
            SetTempFocusRoi(true);
        }

        [RelayCommand]
        public void OuterFocusROI_Set()
        {
            SetTempFocusRoi(false);
        }

        private void ConfirmTempFocusRoi(bool isInner)
        {
            if (HpImageViewer.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return;
            }
            var roi = HpImageViewer.ImageViewerDesc.ROIDescs?.FirstOrDefault() as RectangleDesc;
            if (roi != null)
            {
                if (isInner)
                    TrainParam.InnerLayer.FocusROI = roi;
                else
                    TrainParam.OuterLayer.FocusROI = roi;
            }
            ConfirmCPLParam(isInner);
            InitFocusRoiButton();
            HpImageViewer.UpdateROIs();
        }

        /// <summary>
        /// 
        /// </summary>
        [RelayCommand]
        public void InnerFocusROI_Confirm()
        {
            ConfirmTempFocusRoi(true);
        }
        [RelayCommand]
        public void OuterFocusROI_Confirm()
        {
            ConfirmTempFocusRoi(false);
        }

        /// <summary>
        /// 关闭修改
        /// </summary>    
        [RelayCommand]
        public void FocusROI_Cancel()
        {
            InitFocusRoiButton();
            HpImageViewer.UpdateROIs();
        }


        public void FocusROI(bool isInner)
        {
            if (HpImageViewer.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return;
            }

            double width = 200;
            double height = 200;
            var top = (((ImageData)HpImageViewer.Image).Height - height) / 2;
            var left = (((ImageData)HpImageViewer.Image).Width - width) / 2;
            var inRoi = GetTemplate(true).ROI;
            var outRoi = GetTemplate(false).ROI;
            if (TestInfo is TestInfo_AIM)
            {
                outRoi = inRoi;
            }
            var roi = isInner ? inRoi : outRoi;
            if (roi != null)
            {
                var roiRect = roi.CreateRect();
                if (roiRect.X > 0 && roiRect.Y > 0)
                {
                    left = roiRect.X;
                    top = roiRect.Y;
                    width = roiRect.Width;
                    height = roiRect.Height;
                }
            }
            ROIDesc rOIDescs = new RectangleDesc() { Left = left, Top = top, Width = width, Height = height };
            HpImageViewer.UpdateROIs(rOIDescs);
        }

        public void InitFocusRoiButton()
        {
            FocusROI_SetEnabled = true;
            FocusROI_InnerEnabled = false;
            FocusROI_OuterEnabled = false;
            HpImageViewer.UpdateROIs();
            ReSetCPLVisionState();
            ActionConfirmFocusRoi?.Invoke();
        }

        #endregion


        #region Train Focus

        [RelayCommand]
        public virtual void FocusSetting()
        {
            Action<FocusInfo> action = new Action<FocusInfo>(SaveActionMethod);
            var parameters = new DialogParameters
            {
                { "FocusInfo", FocusInfo},
                { "SaveAction", action }
            };
            AppHelper.DispatcherInvoke(() =>
            {
                _serviceProvider.GetService<IDialogService>().ShowDialog("Test_FocusTrainSettingDialog", parameters, null);
            });
        }

        protected void SaveActionMethod(FocusInfo focusInfo)
        {
            FocusInfo = focusInfo;
        }

        protected virtual FocusInfo FocusInfo
        {
            get
            {
                return TestTrainSetting.FocusModuleInfo.FocusInfo;
            }
            set
            {
                TestTrainSetting.FocusModuleInfo.FocusInfo = value;
            }
        }


        /// <summary>
        /// Single Focus 
        /// </summary>
        [RelayCommand]
        void TrainFocus_Single()
        {
            if (!_alignmentService.IsFinishAlignment)
            {
                var res = _messageBoxService.WarningAskDialog("An alignment operation must be performed before focusing.Otherwise, the results of the calculation may not be accurate.\r\nAre you sure it's aligned?");
                if (res != ButtonResult.OK)
                    return;
            }
            var targetList = TestInfo.TargetSetting.TargetPoints2;
            if(targetList == null ||  targetList.Count ==0)
            {
                _messageBoxService.ErrorDialog("Please add target first！");
                return;
            }
            if (TrainParam.InnerLayer.FocusROI == null || TrainParam.OuterLayer.FocusROI == null)
            {
                _messageBoxService.ErrorDialog("Please draw the corresponding ROI！");
                return;
            }

            LogHelper.LogRunMethodStart();
            _test_TrainViewModelBase.ChangeCameraInfo = "Focusing …";
            Task.Run(() =>
            {
                try
                {
                    TestTrainSetting.IsTrained = false;
                    LogHelper.LogInfo($"Reset IsTrained,testName={TestInfo?.Name}");
                    HpImageViewer.UpdateROIs();
                    _visionService.StartGrabbing();

                    TrainCoarseFocus();
                    if (_isCPLMode)
                    {
                        TrainTempRoiFineFocus_CPL();
                    }
                    else
                    {
                        TrainTempRoiFineFocus();
                    }
                    _visionService.StopGrabbing();

                    var targetTrainPoint = _alignmentService.GetCurrentAxisPosition();
                    _test_TrainViewModelBase.FocusPosition = targetTrainPoint;

                    //查找与训练位置最近的target 和Field;
                    var fr = _alignmentService.GetFieldR3(targetTrainPoint);
                    double minDistance = 300;
                    TargetLocation targetLocation = null;
                    for (int i = 0; i < targetList.Count; i++)
                    {
                        var targetI = targetList[i];
                        var pointI = _alignmentService.GetAxisPoint(fr.Field, targetI, true);
                        var distance = GeometryHelper.Measure(targetTrainPoint, pointI);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            targetLocation = targetI;
                        }
                    }
                    TestInfo.TargetSetting.TrainingSiteInfo = new DataModels.Recipe.TestInfo.TrainingSiteInfo() { Field = fr.Field, Target = targetLocation };
                    IsShowTempRoi = false;
                    IsShowMeasureRoi = true;

                    ActionFocusCompleted?.Invoke();

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
                    _test_TrainViewModelBase.ChangeCameraInfo = "";
                    HpImageViewer.UpdateROIs();
                    LogHelper.LogRunMethodEnd("", nameof(TrainFocus_Single));
                    _visionService.StartGrabbing();
                }
            });

        }

        /// <summary>
        /// 获取Z轴聚焦的高度
        /// </summary>
        private void GetFocusZHeight()
        {
            InnerFocusHeight = TestTrainSetting.FocusModuleInfo.BaseFocusHeight + _alignmentService.MacroFocusZ;
            OuterFocusHeight = InnerFocusHeight;
        }

        private FocusInfo GetTestFocusOrDefault(ECamera camera = ECamera.Meas)
        {
            return TestTrainSetting.FocusModuleInfo.FocusInfo ?? _serviceProvider.GetService<ITestService>().GetSystemFocusInfo(camera);
        }

        private void TrainCoarseFocus()
        {
            if (!_isFirstDirection)
            {
                GetFocusZHeight();
                _alignmentService.MoveZTo(TestTrainSetting.FocusModuleInfo.BaseFocusHeight, "BaseFocusHeight");
                return;
            }

            var waferThickness = _alignmentService.WaferMapInfo.Thickness;
            var focusInfo = GetTestFocusOrDefault(IServiceCamera.CurCamera);
            var macroFocusZ = _alignmentService.MacroFocusZ;
            //粗聚焦的开始位置为标定的聚焦位置+ 晶圆的厚度
            var coarseFocusInfo = focusInfo.CourseFocusInfo;
            coarseFocusInfo.StartPosition = macroFocusZ + waferThickness;
            LogHelper.LogInfo($"Focus Info:MacroFocus.Z={macroFocusZ:0.######},Wafer Thickness={waferThickness},CoarseFocus ={coarseFocusInfo}");

            //使用内外模板分别获取内外模板的粗聚焦高度 （Z轴高度）
            var innerRoiDesc = TrainParam.InnerLayer.FocusROI;
            innerRoiDesc.Color = InnerColor ;
            var outRoiDesc = TrainParam.OuterLayer.FocusROI;
            outRoiDesc.Color = OutColor;
            HpImageViewer.UpdateROIs(innerRoiDesc, outRoiDesc);
            _pztservice?.AMov(0.0);
            var curveInfo = _alignmentService.CoarseFocusTrain(coarseFocusInfo);
            TrainParam.CoarseFocusCurve = curveInfo;
            var currentz = _motionControllerService.GetCurrentZ();
            TestTrainSetting.FocusModuleInfo.BaseFocusHeight = currentz - _alignmentService.MacroFocusZ; //此行代码必须写在GetFocusZHeight之前，因为GetFocusZHeight方法中会使用到TestTrainSetting.BaseFocusHeight 变量
            GetFocusZHeight();
            // MoveAndWait(null, null);

        }

        /// <summary>
        /// 训练白光干涉聚焦高度
        /// </summary>
        protected void TrainWLI()
        {
            wliPztOffset = _testTrainService.TrainWLI(TrainParam.DirectionFileExt);
            if (wliPztOffset != null)
            {
                //AutoRunInfo interfFocusInfo = _wliService.GetSystemInterfMeter();
                //string wliFile = Marvel.Application.Extentions.Extensions.GetWLIFile(_testFilesFolder, TrainParam.DirectionFileExt);
                //wliPztOffset = _wliService.TrainWliFocus(interfFocusInfo, TestInfo.Physical.FocusSettings.FocusMode, wliFile);
                AbsolateWliHeight = CombineZPzt(_motionControllerService.GetCurrentZ(), wliPztOffset.Value);
            }
        }

        //protected TrainArg CreateTrainArg(EnumGrabMode grabMode, int errorFactor = 2)
        //{
        //    return new TrainArg()
        //    {
        //        ErrorFactor = errorFactor,
        //        GrabMode = grabMode == EnumGrabMode.DoubleGrab ? CurveModel.Double : CurveModel.Single
        //    };
        //}


        ///// <summary>
        ///// 全视野聚焦，并训练聚焦曲线
        ///// </summary>
        ///// <returns></returns>
        //private FocusPegResult TrainGlobal()
        //{
        //    var trainArg = CreateTrainArg(_grabMode);
        //    var allRangeFocusInfo = CreateFineTrainInfo();
        //    var res = _focusService.FineFocusTrain(allRangeFocusInfo, trainArg, out var maxSharpness);
        //    TrainParam.GlobalFineFocusCurve = res.Curve;
        //    return res;
        //}

        /// <summary>
        /// 使用临时roi进行聚焦焦，获取内外层最佳焦面，并采集对应层的图片
        /// </summary>
        private void TrainTempRoiFineFocus()
        {
            // 全视野、pzt全行程、小步进精聚焦训练
            HpImageViewer.UpdateROIs();
            fine_Res = _setRoiService.TrainGlobal(true);

            if (fine_Res == null)
                return;
            TrainWLI();
            //采图前需要开启
            _visionService.StartGrabbing();

            InnerPztOffset = TrainRoiFocus(fine_Res, true, false);
            OuterPztOffset = TrainRoiFocus(fine_Res, false, false);
            InnerCorrectionValue = 0;
            OuterCorrectionValue = 0;
            TrainParam.InnerLayer.FocusCorrection = 0;
            TrainParam.OuterLayer.FocusCorrection = 0;
            SaveAverageImage(true);
            HpImageViewer.Image = LoadFocusImageData(_isBaseOnInner);
            _visionService.StopGrabbing();
        }

        ///// 创建精聚焦训练行程和步进
        ///// 训练时全行程和步进使用系统配置的参数，默认全行程为40，步进用0.1
        ///// </summary>
        ///// <returns></returns>
        //private AutoRunInfo CreateFineTrainInfo()
        //{
        //    var iCfg = _serviceProvider.GetService<ISystemConfigService>();
        //    var lCfg = iCfg.GetConfig<FocusCfg>();
        //    var measCfg = lCfg.MeasPztMeter;
        //    return new AutoRunInfo() { Range = measCfg.AllRange, Step = measCfg.AllRangeStep, SmallRange = 0 };
        //}

        /// <summary>
        /// 训练Roi聚焦，并且保存焦面原图
        /// </summary>
        /// <param name="focusPegResult">全行程聚焦结果，包含聚焦序列图</param>
        /// <param name="isInner">是否是内层</param>
        /// <param name="isMeasure">是否使用量测roi </param>
        /// <returns></returns>
        protected double TrainRoiFocus(FocusPegResult focusPegResult, bool isInner, bool isMeasure, bool isCorrectionGrabed = false)
        {
            var roiDesc = TrainParam.GetRoiDesc(isInner, isMeasure);
            HpImageViewer.UpdateROIs(roiDesc);
            return _setRoiService.TrainRoiFocus(focusPegResult, isInner, isMeasure, isCorrectionGrabed);

            // var trainLayerParam = isInner ? TrainParam.InnerLayer : TrainParam.OuterLayer;
            //var roiDesc = TrainParam.GetRoiDesc(isInner, isMeasure);
            //HpImageViewer.UpdateROIs(roiDesc);
            //return _setRoiService.TrainRoiFocus(focusPegResult, isInner, isMeasure, _isFirstDirection, isCorrectionGrabed);
            //string imageName = isInner ? CONST_Inner : CONST_Outer;
            //var fineFocusInfo = GetTestFocusOrDefault(IServiceCamera.CurCamera).FineFocusInfo;
            //var focusImages = focusPegResult.PegImages;
            //var trainArg = CreateTrainArg(_grabMode);
            //var roi = roiDesc.CreateRoi();
            ////使用全视野的聚焦序列图进行内、外层聚焦，并且获取聚焦曲线
            //var ret = _focusService.TrainCurve(fineFocusInfo, roi, focusImages, trainArg);
            //trainLayerParam.RoiFocusCurve = ret.Curve;
            //var pztOffset = ret.Curve.RefMaximumZ.First();
            //MoveAndWait(null, pztOffset);

            //if (!isCorrectionGrabed)
            //{
            //    // 没有补偿焦面设置时才把聚焦面覆盖为量测建模时使用图片（ex;Inner.bmp 或 Outer.bmp）
            //    SaveFocusImage(imageName, isMeasure ? roi : null);
            //    LogHelper.LogInfo($"Save Focus Image = {isMeasure}, imageName : {imageName}.bmp");
            //}

            //if (isMeasure)
            //{
            //    //在第一个量测方向的第一层的焦面采集预匹配模板原图
            //    if (isInner == _isBaseOnInner && _isFirstDirection)
            //    {
            //        //采集Base层的图片，并且使用该图片创建预匹配模板
            //        LogHelper.LogInfo($"Move to previewmatch layer measure focus:Base Layer={_focusSetting.FocusBasedOn}");
            //        SaveFocusImage(CONST_PREVIEW, null);
            //        CreatePreviewTemplate();
            //    }
            //}
            //return pztOffset;
        }

        /// <summary>
        /// 是否设置过补偿焦面图片
        /// </summary>
        /// <param name="isInner"></param>
        /// <returns></returns>
        private bool IsCorrectionGrabbed(bool isInner)
        {
            if (_focusSetting.IsEnableFocusCorrection)
            {
                if (_isSingleGrab)
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
        private void SetAutoFocusHeight(double midPztHeight)
        {
            try
            {
                //此处SigleGrab模式移动pzt到具体的BaseOn层，其它模式都移动到中间层
                if (_grabMode == EnumGrabMode.SingleGrab)
                {
                    midPztHeight = _isBaseOnInner ? InnerPztOffset : OuterPztOffset;
                }
                AutoFocusHeight = CombineZPzt(_motionControllerService.GetCurrentZ(), midPztHeight);
                _pztservice?.AMov(midPztHeight);
            }
            catch (Exception)
            {
            }
        }

        private void MoveAndWait(double? focusZHeight, double? pztHeight)
        {

            if (focusZHeight != null)
            {
                _alignmentService.MoveZTo(focusZHeight.Value, "", false);
            }
            if (pztHeight != null)
            {
                _pztservice?.AMov(pztHeight.Value);
            }
            //Task.Delay(2000).Wait();
        }

        /// <summary>
        /// 保存最终的量测焦面存图以及各个焦面的偏移量
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> SaveMeasureFocus()
        {
            bool isFoucsByMeasureRoi;
            if (_isCPLMode)
            {
                isFoucsByMeasureRoi = SaveFocusOffsets_CPL();
            }
            else
            {
                isFoucsByMeasureRoi = SaveFocusOffsets();
            }
            SaveFinalCorrectionValue(true);
            SaveFinalCorrectionValue(false);
            return isFoucsByMeasureRoi;
        }

        private void SaveFocusModuleInfo()
        {

            if (_isFirstDirection)
            {
                if (wliPztOffset != null)
                {
                    TestTrainSetting.FocusModuleInfo.WliFocusOffset = wliPztOffset.Value;
                }
                var zFit = GetFocusResult().ZFit;
                TestTrainSetting.FocusModuleInfo.GlobalFocusOffset = zFit;
            }
            TrainParam.InnerLayer.SharpnessFocusOffset = InnerPztOffset;
            TrainParam.OuterLayer.SharpnessFocusOffset = OuterPztOffset;
        }

        /// <summary>
        /// 保存焦面偏移量信息
        /// </summary>
        /// <returns></returns>
        private bool SaveFocusOffsets()
        {
            _visionService.StartGrabbing();//启动采集
            if (fine_Res != null && IsUpdateMeasureRoi)
            {
                LogHelper.LogRunMethodStart();
                //点击量测按钮时，才保存最终的数据
                //TrainWLI(); //这个方法移动到临时roi 聚焦过程
                //InnerPztOffset = TrainRoiFocus(fine_Res, true, true); //这两个方法移动到Confirm Measure Roi 方法执行
                //OuterPztOffset = TrainRoiFocus(fine_Res, false, true);
                SaveAverageImage();

                SaveFocusModuleInfo();



                LogHelper.LogInfo($"Focus Result:InnerFocusHeight={InnerFocusHeight:0.######},OuterFocusHeight={OuterFocusHeight:0.######},InnerPztOffset={InnerPztOffset:0.######},OuterPztOffset={OuterPztOffset:0.######},AutoFocusPostion={AutoFocusHeight:0.######}");

                LogHelper.LogRunMethodEnd();
                return true;
            }
            return false;
        }

        private FocusPegResult GetFocusResult()
        {
            if (_isCPLMode)
            {
                return _isBaseOnInner ? _focusRes_Inner : _focusRes_Outer;
            }
            return fine_Res;
        }
        //private async Task CreatePreviewTemplate()
        //{
        //    if (!_isFirstDirection) return;
        //    //点击Confirm按钮时，重绘量测Roi，避免被其它roi影响
        //    var roiDesc = _isBaseOnInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
        //    if (roiDesc == null)
        //    {
        //        _messageBoxService.InfoDialog("Please draw the corresponding ROI！");
        //        return;
        //    }
        //    //训练预匹配的模板
        //    var image = LoadImageData(CONST_PREVIEW);
        //    //此处需要保存全图的清晰度
        //    var sharpness = _focusService.GetImageClarity(image);
        //    TestTrainSetting.FocusModuleInfo.Sharpness = sharpness;
        //    var roiSharpness = _focusService.GetImageClarity(image, roiDesc.CreateRoi());
        //    TestTrainSetting.FocusModuleInfo.RoiSharpness = roiSharpness;
        //    var previewTemplate = GetTemplate(_isBaseOnInner);

        //    _visionService.StopGrabbing();

        //    //此处先创建模板，后加载图片到图像控件，避免图片被占用
        //    HpImageViewer.Image = image;
        //    HpImageViewer.UpdateROIs(roiDesc);
        //    HpImageViewer.Image = image;
        //    HpImageViewer.UpdateROIs(roiDesc);
        //    previewTemplate = (SingleTemplateMatchingContext)CloneHelper.ReflectionClone(previewTemplate);
        //    previewTemplate.Name = "PreviewModel";
        //    TrainModelViewModel trainModelVM = new TrainModelViewModel(_serviceProvider, previewTemplate, false, () => HpImageViewer, false);
        //    await trainModelVM.TrainModelAsync(previewTemplate.SelectedMatchModelGroup.MatchModel.ParamMatch.MinScore);
        //    TestTrainSetting.PreviewTemplate = previewTemplate;
        //}

        public void TestSharpness()
        {
            //点击Confirm按钮时，重绘量测Roi，避免被其它roi影响
            var roiDesc = _isBaseOnInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
            if (roiDesc == null)
            {
                _messageBoxService.InfoDialog("Please draw the corresponding ROI！");
                return;
            }
            try
            {
                //训练预匹配的模板
                var image = _setRoiService.LoadImageData(CONST_PREVIEW);
                var roi = roiDesc.CreateRoi();

                string path = @"E:\工作\开发\调试数据\OM100\OM100-2\图片";
                if (File.Exists(path))
                {
                    image = ImageData.ReadImage(path);
                }
                //此处需要保存全图的清晰度
                var paramFocus = GetTestFocusOrDefault(IServiceCamera.CurCamera).CloneFineParamFocus();
                var sharpness = _focusService.GetImageClarity(image, paramFocus: paramFocus);
                var roiSharpness = _focusService.GetImageClarity(image, roi, paramFocus);
                HpImageViewer.UpdateROIs(roiDesc);
            }
            catch (Exception ex)
            {
                _messageBoxService.ErrorDialog(ex.Message);
            }
        }

        #endregion Train Focus

        #region MeasureRoi

        private ImageData ReadImageFile(string filePath)
        {
            try
            {
                return ImageData.ReadImage(filePath);
            }
            catch (Exception e)
            {
                _messageBoxService.ErrorDialog(e.Message);
                return null;
            }
        }

        /// <summary>
        /// 量测添加ROI前 加载聚焦生成的原图
        /// <paramref name="isInner"> 是否是取内层的图片，
        /// 该参数只有在Double 或者CPL模式下才生效，
        /// SingleGrab模式下只会取BaseOn层的图片
        /// SingleGrab_Average模式下只会取Average层的图片</paramref>
        /// </summary>
        public ImageData LoadFocusImageData(bool isInner)
        {
            return _setRoiService.LoadFocusImageData(isInner);
        }
            //if (Physical == null || Physical.TestSettings == null)
            //{
            //    throw new Exception("Physical data is incomplete!");// ("Physical 数据不完整！");
            //}

            ////SingleGrab-Averagae 两层都取 Average的图
            ////SingleGrab 两层都取BaseOn的图
            ////DoubleGrab 取内、外层不同的图
            //string imgName = CONST_Average;
            //if (_isSingleGrab)
            //{
            //    if (_grabMode == EnumGrabMode.SingleGrab)
            //    {
            //        imgName = _isBaseOnInner ? CONST_Inner : CONST_Outer;
            //    }
            //}
            //else
            //{
            //    imgName = isInner ? CONST_Inner : CONST_Outer;
            //}

            //return LoadImageData(imgName);
        //}

        //private ImageData LoadImageData(string imgName)
        //{

        //    string imgPath = GetImageAbsolutePath(imgName);
        //    if (!File.Exists(imgPath))
        //    {
        //        throw new Exception($"Please complete the {imgName}  focusing operation!");
        //    }
        //    return ReadImageFile(imgPath);
        //}

        ///// <summary>
        ///// 获取图片绝对路径
        ///// </summary>
        ///// <param name="imageName">图片名称</param>
        ///// <returns></returns>
        //private string GetImageAbsolutePath(string imageName)
        //{
        //    var filePath = Path.Combine(_testFilesFolder, imageName + TrainParam.DirectionFileExt + ".bmp");
        //    return filePath;
        //}
        //private void SaveFocusImage(string imageName, ShapeBase   roi)
        //{
        //    var filePath = GetImageAbsolutePath(imageName);
        //    FileHelper.DeleteFile(filePath);
        //    ImageData imageData = null;
        //    Task.Run(()=> imageData = _alignmentService.MoveAndGrab()).Wait();
        //    imageData.SaveBmp(filePath);
        //    if (roi != null)
        //    {
        //        var kernel = _focusService.GetImageKernel(imageData, roi);
        //        bool isInner = imageName.StartsWith(CONST_Inner);

        //        if (isInner)
        //        {
        //            TrainParam.InnerLayer.FocusKernel = kernel;
        //        }
        //        else
        //        {
        //            TrainParam.OuterLayer.FocusKernel = kernel;
        //        }
        //    }
        //}

        private void SaveAverageImage(bool isAlways = false)
        {
            var midPztHeight = (InnerPztOffset + OuterPztOffset) / 2;
            SetAutoFocusHeight(midPztHeight);
            if (IsUpdateMeasureRoi || isAlways)
            {
                _setRoiService.SaveAverageImge(midPztHeight);
                //MoveAndWait(null, midPztHeight);
                //SaveFocusImage(CONST_Average, null);
            }
        }
        void CreateMeasureRoi(bool isInner)
        {

            if (HpImageViewer.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return;
            }

            void JudgeAutoGenResult(ExResult autoGenResult)
            {
                if (!autoGenResult.IsOK)
                {
                    LogHelper.LogError($"{autoGenResult.ErrorMsg}");
                    _messageBoxService.ErrorDialog("Failed to automatically generate the ROI. Please drawing a rectangle close to the vivinity of the outer pattern");
                    //throw new Exception("Failed to automatically generate the ROI. Please drawing a rectangle close to the vivinity of the outer pattern", new Exception(autoGenResult.ErrorMsg));
                }
            }
            var imageData = (ImageData)HpImageViewer.Image;
            double width = imageData.Width / 2;
            double height = imageData.Height / 2;
            var top = (imageData.Height - height) / 2;
            var left = (imageData.Width - width) / 2;

            ROIDesc rOIDescs = null;
            var type = isInner ? TestTrainSetting.InnerType : TestTrainSetting.OuterType;
            ExResult autoGenResult = default;

            try
            {
                switch (type)
                {
                    case EnumTargetType.Circle:
                    case EnumTargetType.Box:
                    case EnumTargetType.Bar:
                    case EnumTargetType.Frame:
                        if (type != EnumTargetType.Circle)
                        {
                            OMTargetType omType = OMTargetType.Bar;
                            if (type == EnumTargetType.Frame)
                                omType = OMTargetType.Frame;
                            else if (type == EnumTargetType.Box)
                                omType = OMTargetType.Box;
                            var roi = GetRangeRoi(imageData);
                            using var hImage = imageData.ToHObject();
                            autoGenResult = OMUtil.AutoGenMeasureRoiOfBIB(hImage, roi, omType, OMTargetLayer.InnerAndOuter, out Bib2d innerRoi, out Bib2d outerRoi);
                            JudgeAutoGenResult(autoGenResult);
                            var bib2d = isInner ? innerRoi : outerRoi;
                            var bibAxis = bib2d?.Box?.Axis;
                            if (bibAxis != null && bibAxis.Width != 0 && bibAxis.Height != 0)
                            {
                                rOIDescs = new QuadRectangleDesc()
                                {
                                    Top = bibAxis.Y,
                                    Left = bibAxis.X,
                                    Width = bibAxis.Width,
                                    Height = bibAxis.Height,
                                    BandWidth = bib2d.Box.Width, //BandLength = bib2d.BandLength,
                                    BandPercent = bib2d.BandPercent
                                };
                            }
                        }
                        if (rOIDescs == null)
                            rOIDescs = new QuadRectangleDesc()
                            {
                                Top = top,
                                Left = left,
                                Width = width,
                                Height = height,
                                BandWidth = width * 0.3,
                                Color = "#FFFF00", //BandLength = width * 0.5,
                                BandPercent = 30
                            };
                        break;
                    case EnumTargetType.Cross:
                        var roiCross = GetRangeRoi(imageData);
                        using (var hImageCross = imageData.ToHObject())
                        {
                            autoGenResult = OMUtil.AutoGenMeasureRoiOfCross(hImageCross, roiCross, OMTargetType.Cross, out Cross2d innerRoiCross, out Cross2d outerRoiCross);
                            JudgeAutoGenResult(autoGenResult);
                            var cross2d = isInner ? innerRoiCross : outerRoiCross;
                            if (cross2d != null && cross2d.RectAxis?.Axis?.Count() != 0)
                            {
                                rOIDescs = new Cross2dRectangleDesc()
                                {
                                    Left = cross2d.Rect.X,
                                    Top = cross2d.Rect.Y,
                                    Width = cross2d.Rect.Width,
                                    Height = cross2d.Rect.Height,
                                    RactPointList = cross2d.RectAxis.Axis.Select<Point2d, (double, double)>(_ => new(_.X, _.Y)).ToList(),
                                    BandPercent = cross2d.BandPercent,
                                    ColumnWidth = cross2d.Width,
                                    IntervalLength_X = cross2d.IntervalLength_X,
                                    IntervalLength_Y = cross2d.IntervalLength_Y,
                                };
                            }
                            if (rOIDescs == null)
                            {
                                rOIDescs = new Cross2dRectangleDesc() { Top = top, Left = left, Width = width, Height = height, BandWidth = 0, ColumnWidth = width * 0.1, Color = "#FFFF00", BandPercent = 30, IntervalLength_X = width * 0.2, IntervalLength_Y = height * 0.2 };
                            }
                        }
                        break;
                    case EnumTargetType.AIM:
                    case EnumTargetType.AIM_XYS:
                        var roiAim = GetRangeRoi(imageData);
                        AimOrientation aimOrientation = TestInfo.TargetSetting.OrientationType == EnumOrientationType.Counterclockwise ? AimOrientation.CounterClockWise : AimOrientation.ClockWise;
                        using (var hImageAim = imageData.ToHObject())
                        {
                            autoGenResult = OMUtil.AutoGenMeasureRoiOfAIM(hImageAim, roiAim, OMTargetType.AIM, aimOrientation, out Aim2d innerAimRoi, out Aim2d outerAimRoi);
                            JudgeAutoGenResult(autoGenResult);
                            var aim2d = isInner ? innerAimRoi : outerAimRoi;
                            aim2d.Rect.X = aim2d.Rect.X == 0 ? left : aim2d.Rect.X;
                            aim2d.Rect.Y = aim2d.Rect.Y == 0 ? top : aim2d.Rect.Y;
                            aim2d.Rect.Width = aim2d.Rect.Width == 0 ? width : aim2d.Rect.Width;
                            aim2d.Rect.Height = aim2d.Rect.Height == 0 ? height : aim2d.Rect.Height;
                            var aimRectangleDesc = aim2d.CreateRoiDesc() as AimRectangleDesc;

                            if (TrainParam.DirectionType == EnumOptimizeType.X)
                            {
                                rOIDescs = aimRectangleDesc.CreatXYSDesc(true, aimOrientation);
                            }
                            else if (TrainParam.DirectionType == EnumOptimizeType.Y)
                            {
                                rOIDescs = aimRectangleDesc.CreatXYSDesc(false, aimOrientation);
                            }
                            else
                            {
                                rOIDescs = aimRectangleDesc;
                            }
                        }
                        break;
                    default:
                        break;
                }
                var searchRoi = TestTrainSetting.AutoRoiRange;
                if (searchRoi != null)
                {
                    var _autoRangeRoi = searchRoi.CreateRoiDesc();
                    _autoRangeRoi.Color = "#FFFF00";
                    HpImageViewer.UpdateROIs(rOIDescs, _autoRangeRoi);
                }
                else
                {
                    HpImageViewer.UpdateROIs(rOIDescs);
                }
            }
            catch (Exception ex)
            {
                _messageBoxService.ErrorDialog(ex.Message);
                LogHelper.LogError(ex, "Auto create roi errored!");
            }
        }

        void Create4MeasureRoi()
        {

            var imageData = (ImageData)HpImageViewer.Image;
            double width = imageData.Width / 3;
            double height = imageData.Height / 3;
            var top = (imageData.Height - height) / 2;
            var left = (imageData.Width - width) / 2;

            ROIDesc rOIDescs = null;
            var type = TestTrainSetting.OuterType;
            ExResult autoGenResult = default;

            try
            {
                switch (type)
                {
                    case EnumTargetType.Circle:
                    case EnumTargetType.Box:
                    case EnumTargetType.Bar:
                    case EnumTargetType.Frame:
                        if (rOIDescs == null)
                            rOIDescs = new Cross4RectangleDesc()
                            {
                                Top = top,
                                Left = left,
                                Width = width,
                                Height = height,
                                BandWidth = 200 * 0.3,
                                Color = "#FFFF00", //BandLength = width * 0.5,
                                BandPercent = 60
                            };
                        break;
                    default:
                        break;
                }
                var searchRoi = TestTrainSetting.AutoRoiRange;
                if (searchRoi != null)
                {
                    var _autoRangeRoi = searchRoi.CreateRoiDesc();
                    _autoRangeRoi.Color = "#FFFF00";
                    HpImageViewer.UpdateROIs(rOIDescs, _autoRangeRoi);
                }
                else
                {
                    HpImageViewer.UpdateROIs(rOIDescs);
                }
            }
            catch (Exception ex)
            {
                _messageBoxService.ErrorDialog(ex.Message);
                LogHelper.LogError(ex, "Auto create roi errored!");
            }
        }
        private Rect2d GetRangeRoi(ImageData imageData)
        {
            var roi = HpImageViewer.ImageViewerDesc.ROIDescs?.FirstOrDefault()?.CreateRoi() as Rect2d;
            //如果不是矩形roi，则忽略其范围，如果是矩形roi，则在指定的矩形roi内自动查找内外侧
            if (roi != null)
            {
                TestTrainSetting.AutoRoiRange = roi;
            }
            else
            {
                roi = TestTrainSetting.AutoRoiRange;
            }
            //  var roi = GetSearchingRect();
            if (roi == null)
            {
                roi = new Rect2d(0, 0, imageData.Width, imageData.Height);
            }

            return roi;
        }

        void SetMeasureRoi(bool isInner)
        {
            try
            {
                if (_test_TrainViewModelBase.FocusPosition == null)
                {
                    _messageBoxService.ErrorDialog($"Please complete the focusing operation!");
                    return;
                }
                InitFocusRoiButton();
                var roi = TrainParam.GetRoiDesc(isInner, true);
                
                if (roi != null)
                {
                    roi = (RectangleDesc)CloneHelper.ReflectionClone(roi);
                }
                if (!isInner && UseCross4rectan && TrainParam.OuterLayer.UseNewCross == false)
                {
                    roi = null;
                }
                ProgressDialogHelper.ShowDialog("Setting light source param", () =>
                {
                    SetCPLParam(isInner);
                    SetCPLVisionState(isInner);
                    return true;
                });
                bool isTest = false;
                if (isTest)
                {
                    if (roi != null)
                        HpImageViewer.UpdateROIs(roi);
                }
                else
                {
                    ImageData imageData = (ImageData)HpImageViewer.Image;
                    //ImageData imageData = LoadFocusImageData(isInner);
                    if (imageData == null)
                    {
                        return;
                    }
                    _visionService.StopGrabbing();
                    HpImageViewer.Image = imageData;
                    var template = GetTemplate(isInner);
                    //如果roi 不等于null ,并且已创建过模板，则使用已创建的模板和roi 匹配
                    if (roi != null && template?.SelectedMatchModelGroup?.MatchModel?.ModelInfo != null)
                    {
                        //此处为测试代码，在调试过程中会跳过LoadFocusImageData方法加载图片
                        if (imageData ==null  && HpImageViewer.Image !=null )
                        {
                            imageData =(ImageData)HpImageViewer.Image;
                        }
                        var mr = _alignmentService.FindTemplateForRun(imageData, template, false);
                        //执行一次模板匹配，如果未匹配到模板，说明视野内或者搜索区域内没有需要量测的标记，
                        //提示用户移动到视野中心后重新聚焦

                        if (!mr.IsSuccess)
                        {
                            if (UseCross4rectan)
                            {
                                Create4MeasureRoi();
                            }
                            else
                            {
                                CreateMeasureRoi(isInner);
                            }

                            //var searchArea = _templateMatchingService.CreateSearchArea(imageData, template.SelectedMatchModelGroup.MatchModel);
                            //ISectionImageStorage sectionImageStorage = _serviceProvider.GetRequiredService<ISectionImageStorage>();
                            //string testTrainImage = "TestTrainImage";
                            //string ext = isInner ? "Inner" : "Outer";
                            //sectionImageStorage.SaveSpecImage(imageData, testTrainImage, TestInfo.Name + "_" + ext);
                            //var imagePath = Path.Combine(_testFilesFolder, $"{template.Name}.bmp");
                            //var modelImage = ReadImageFile(imagePath);
                            //if (modelImage != null)
                            //{
                            //    sectionImageStorage.SaveSpecImage(modelImage, testTrainImage, TestInfo.Name + "_Model_" + ext);
                            //}
                            ////当前搜索区域未匹配到量测标记，请将标记移动到视野中心，重新聚焦后再重试
                            //string errmsg = "The current search area does not match the measurement target point. Please move it to the center of vision, refocus, and try again";
                            //LogHelper.LogError(errmsg);
                            //_messageBoxService.ErrorDialog(errmsg);
                            ////如果移动了视野，必须要重新聚焦，所以此处将FocusPosition置为null
                            //_test_TrainViewModelBase.FocusPosition = null;
                            //return;
                        }
                        else
                        { //上次创建模板时匹配到的模板中心位置
                            var lastTrainMatchItem = isInner ? TestTrainSetting.InnerTrainMatchItem : TestTrainSetting.OuterTrainMatchItem;
                            var offset = mr.BestMatchItem.ToPoint() - lastTrainMatchItem.ToPoint();
                            //根据模板中心的偏移量，更新roi的偏移
                            roi = roi.CreateOffsetRoi(offset) as RectangleDesc;
                            HpImageViewer.UpdateROIs(roi);
                            var kernel = _focusService.GetImageKernel(imageData, roi.CreateRoi());
                        }
                    }
                    else
                    {
                        if (!isInner && UseCross4rectan)
                        {
                            Create4MeasureRoi();

                        }
                        else
                        {
                            CreateMeasureRoi(isInner);
                        }
                    }
                                                   
                    MeasurementROI_SetEnabled = false;
                    MeasurementROI_InnerEnabled = isInner;
                    MeasurementROI_OuterEnabled = !isInner;
                }
            }
            catch (Exception ex)
            {
                _messageBoxService.ErrorDialog(ex.Message);
            }
        }

        /// <summary>
        ///设置或显示内层量测Roi
        /// </summary>
        [RelayCommand]
        void InnerMeasureRoi_Set()
        {
            SetMeasureRoi(true);

            //测试代码，测试roi的偏移
            //List<ROIDesc> roiDescs = new List<ROIDesc>(){
            //    new EllipseDesc() { R = 10 },
            //    new EllipseDoubleDesc() { R = 15 , BandLength =3},
            //    new RectangleDesc() { Width = 100, Height = 150 } ,
            //    new BoxDesc() { Width = 60, Height = 80, BandWidth = 5 },
            //   new Cross2dRectangleDesc(){ Width=100,Height =100, BandWidth =2,BandPercent=30,ColumnWidth=4 },
            //new CrossRectangleDesc(){ Width =100, Height =125, BandWidth =3, ColumnWidth=6 },
            //new QuadRectangleDesc (){Width =140,Height =140, BandWidth =5, BandPercent =40 } ,
            //new AimXYSDesc() { Width = 140, Height = 140, BandWidth = 5, OffsetX = 5, OffsetY = 10, RegionWidth = 100, RegionHeight = 50 },
            //new AimRectangleDesc(){  Width = 140, Height = 140, BandWidth = 5, OffsetX = 5, OffsetY = 10, RegionWidth = 100, RegionHeight = 50 },
            //new CrosshairsDesc(){ ActualX =100, ActualY =100 ,Angle =20, Size =3, },
            //new PolygonDesc() { Vertices =new List<HPImageViewer.Core.Primitives.Point> (){ new HPImageViewer.Core.Primitives.Point (0,0),new HPImageViewer.Core.Primitives.Point(50,50) ,new HPImageViewer.Core.Primitives.Point(60,30)},IsClosed =true },
            //new RotatedPickersDesc (){ EdgePoints=new List<HPImageViewer.Core.Primitives.Point> (){ new HPImageViewer.Core.Primitives.Point (0,0),new HPImageViewer.Core.Primitives.Point(50,50) ,new HPImageViewer.Core.Primitives.Point(60,30)},StartAngle =45,PickerCount =5},
            //new RotatedRectDesc(){ Angle =5, Width =30,Height =40}

            //};
            //HpImageViewer.UpdateROIs(roiDescs.ToArray());
            //var listOffsetRoi = new List<ROIDesc>();
            //foreach (var roi in roiDescs)
            //{
            //    listOffsetRoi.Add(roi.CreateOffsetRoi(new Point2d(500, 500)));
            //}
            //HpImageViewer.UpdateROIs(listOffsetRoi.ToArray());

        }

        /// <summary>
        /// 设置或显示外层量测Roi
        /// </summary>
        [RelayCommand]
        void OuterMeasureRoi_Set()
        {
            SetMeasureRoi(false);
        }

        void ResetMeasureRoi(bool isInner)
        {
            try
            {
                ProgressDialogHelper.ShowDialog("Setting light source param", () =>
                {
                    SetCPLParam(isInner);
                    SetCPLVisionState(isInner);
                    return true;
                });
                ImageData imageData = LoadFocusImageData(isInner);
                if (imageData == null)
                {
                    return;
                }
                _visionService.StopGrabbing();
                HpImageViewer.Image = imageData;
                if (!isInner && UseCross4rectan)
                {
                    Create4MeasureRoi();

                }
                else
                {
                    CreateMeasureRoi(isInner);
                }
            }
            catch (Exception ex)
            {
                _messageBoxService.ErrorDialog(ex.Message);
            }
        }
        /// <summary>
        /// 重置内层量测Roi
        /// </summary>
        [RelayCommand]
        void InnerMeasureRoi_ReSet()
        {
            ResetMeasureRoi(true);
        }

        /// <summary>
        /// 重置外层量测Roi
        /// </summary>
        [RelayCommand]
        void OuterMeasureRoi_ReSet()
        {
            ResetMeasureRoi(false);
        }


        /// <summary>
        /// 创建bib内框 kernel Size调整框
        /// </summary>
        [RelayCommand]
        private void InnerMeasureRoi_Confirm()
        {
            ConfirmMeasureRoi(true);
        }

        [RelayCommand]
        private void OuterMeasureRoi_Confirm()
        {
            ConfirmMeasureRoi(false);
        }

        private void ConfirmMeasureRoi(bool isInner)
        {

            if (HpImageViewer.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return;
            }
            var rOIDescs = HpImageViewer.ImageViewerDesc.ROIDescs.FirstOrDefault();
            if (rOIDescs == null)
            {
                _messageBoxService.InfoDialog("Please draw the corresponding ROI！");
                return;
            }
            if (!isInner && UseCross4rectan && rOIDescs is not Cross4RectangleDesc)
            {
                _messageBoxService.ErrorDialog("Use NewCross is enabled. Please adjust the outer ROI with the Cross4 editor.");
                return;
            }
            if (!isInner && !UseCross4rectan && rOIDescs is Cross4RectangleDesc)
            {
                _messageBoxService.ErrorDialog("Use NewCross is disabled. Please use the standard outer ROI type.");
                return;
            }
            if (_test_TrainViewModelBase.FocusPosition == null)
            {
                _messageBoxService.ErrorDialog($"Please complete the focusing operation!");
                return;
            }

            var currentPosition = _alignmentService.GetCurrentAxisPosition();
            if (!currentPosition.Equals(_test_TrainViewModelBase.FocusPosition))
            {
                _alignmentService.MoveTo(_test_TrainViewModelBase.FocusPosition);
            }

            if (isInner)
            {
                rOIDescs.Color = InnerColor;
                TrainParam.InnerLayer.MeasureROI = rOIDescs as RectangleDesc;
            }
            else
            {
                rOIDescs.Color = OutColor;
                TrainParam.OuterLayer.MeasureROI = rOIDescs as RectangleDesc;
                TrainParam.OuterLayer.UseNewCross = UseCross4rectan;
            }
            var roiCenter = rOIDescs.CreateRoi().GetCenter();
            var imagedata = HpImageViewer.Image as ImageData;
            //创建量测模板后，记录模板中心所在的机台坐标，当定位模板与量测模板不一致时，用该值来计算量测模板与定位模板的偏移量
            _test_TrainViewModelBase.TargetPoint = _alignmentService.ConvertImageToAxis(_alignmentService.GetCurrentAxisPosition(), new MatchingItem() { Column = roiCenter.X, Row = roiCenter.Y }, imagedata.Width, imagedata.Height);
            SaveVisionParams(isInner);
            //TargetPoint = _alignmentService.ConvertImageToAxis(_alignmentService.GetCurrentAxisPosition(), new MatchingItem() { Column = roiCenter.X, Row = roiCenter.Y }, imagedata.Width, imagedata.Height);

            IsUpdateMeasureRoi = true;
            if (isInner)
            {
                IsInnerCorrectionEnabled = true;
            }
            else
            {
                IsOuterCorrectionEnabled = true;
            }
            ProgressDialogHelper.ShowDialog("Setting ROI please later...", () =>
            {
                FocusByMeasureRoi(isInner);
                SaveVisionParams(isInner);
                InitMeasurementRoiButton();
                CheckComplete();
                // 因为最后一步会开启实时影像，所以有补偿焦面时则移动到补偿焦面
                // 补偿焦面的图已经在FocusByMeasureRoi步骤存在本地供后续“Run”按钮建模时使用
                MoveToCorrectionedHeight(isInner);
                _visionService.StartGrabbing();
                return true;
            });

        }

        private void MoveToCorrectionedHeight(bool isInner)
        {
            if (!IsCorrectionGrabbed(isInner)) return;
            double finalePztHeight = 0d;
            if (_isSingleGrab)
            {
                finalePztHeight = _isBaseOnInner ? InnerPztOffset + InnerCorrectionValue : OuterPztOffset + OuterCorrectionValue;
            }
            else
            {
                finalePztHeight = isInner ? InnerPztOffset + InnerCorrectionValue : OuterPztOffset + OuterCorrectionValue;
            }
            MoveAndWait(null, finalePztHeight);
        }


        private void FocusByMeasureRoi(bool isInner)
        {
            _visionService.StartGrabbing();//启动采集
            var focusRes = fine_Res;
            if (_isCPLMode)
            {
                //使用量测Roi以及聚焦序列图更新聚焦位置及聚焦图片
                //因为此处需要采图，需要提前设置相应的光源参数
                SetCPLParam(isInner);
                focusRes = isInner ? _focusRes_Inner : _focusRes_Outer;
            }
            var pztOffset = TrainRoiFocus(focusRes, isInner, true, IsCorrectionGrabbed(isInner));
            if (isInner)
            {
                InnerPztOffset = pztOffset;
            }
            else
            {
                OuterPztOffset = pztOffset;
            }
        }

        private void SaveVisionParams(bool isInner)
        {
            //将第一层的参数设置为视觉默认参数
            //如果是inner层，并且也是基于inner层量测，则说明此时为第一层
            //如果不是inner层（也就是outer层），并且也不是基于inner层量测（也就是基于Outer层），则说明此时为第一层
            bool isFirstLayer = isInner == _isBaseOnInner;
            if (!isFirstLayer) return;
            var lightParam = _cameraLightParamService.GetLightSourceParam(IServiceCamera.CurCamera) as MeasureLightSourceParam;
            //此处主要区分XY方向的视觉参数设置，如果是第一个方向，则设置TrainSettings里面的视觉参数，
            //如果是第二个方向，则设置该方向的视觉参数
            ITrainSettingsVisionParam visionSettings = _isFirstDirection ? TestTrainSetting : _visionParams_Y;
            if (visionSettings != null)
            {
                visionSettings.CameraParams = _cameraLightParamService.GetCamearaParam(IServiceCamera.CurCamera);
                visionSettings.LightSourceParam = lightParam;
            }
            SaveXYSVisionParams();
            if (_isCPLMode)
            {
                //CPL模式下，将第一层的CPL参数设置为默认的光源参数，在量测时设置第一个量测点的光源参数时会用到
                var cplParam = _isBaseOnInner ? TrainParam.InnerLayer.CplParam : TrainParam.OuterLayer.CplParam;
                if (lightParam != null)
                {
                    lightParam.Filter = cplParam.Filter;
                    lightParam.Wave = cplParam.Wave;
                    lightParam.BandwidthIndex = cplParam.BandwidthIndex;
                    lightParam.AttenIndex = cplParam.Intensity;
                }
                if (visionSettings != null)
                {
                    if (visionSettings.CameraParams is AlignmentCameraParam alignmentCameraParam)
                    {
                        alignmentCameraParam.ExploreTime = (float)cplParam.ExposureTime;
                    }
                    else if (visionSettings.CameraParams is MeasureCameraParam measureCameraParam)
                    {
                        measureCameraParam.ExploreTime = (int)cplParam.ExposureTime;
                    }
                }
            }

            if (visionSettings != null)
                SaveLightAgeInitialInfo(IServiceCamera.CurCamera, visionSettings.CameraParams, visionSettings.LightSourceParam);
        }

        /// <summary>
        /// XYS分方向训练保存视觉参数时记录Recipe初始LightAge系数。
        /// </summary>
        private void SaveLightAgeInitialInfo(ECamera camera, CameraParamBase cameraParam, LightSourceParamBase lightParam)
        {
            if (cameraParam is not MeasureCameraParam measureCameraParam)
                return;

            var age = _serviceProvider.GetService<IServiceCalData>()?.GetLightAge(camera, measureCameraParam.CameraAttr) ?? 1;
            measureCameraParam.AttenuationCoef = age;

            TestInfo.IsLightPmExecuted = true;
            LogHelper.LogDebug($"Set Age:{age}, Camera: {camera}");
        }

        [RelayCommand]
        void MeasureRoi_Cancel()
        {
            InitMeasurementRoiButton();
            _visionService.StartGrabbing();
        }

        public void InitMeasurementRoiButton(bool isClearRoi = true)
        {
            MeasurementROI_SetEnabled = true;
            MeasurementROI_InnerEnabled = false;
            MeasurementROI_OuterEnabled = false;
            SearchROI_SetEnabled = true;
            if (isClearRoi)
                HpImageViewer.UpdateROIs();
        }

        /// <summary>
        ///设置或显示SearchRoi
        /// </summary>
        [RelayCommand]
        void SearchRoi_Set()
        {
            var searchRoi = GetSearchingRect();
            if (searchRoi == null && HpImageViewer.Image is ImageData img)
            {
                double size = Math.Min(img.Width, img.Height) / 2;
                double left = img.Width / 2 - size / 2;
                double top = img.Height / 2 - size / 2;
                searchRoi = new Rect2d(left, top, size, size);
            }
            HpImageViewer.UpdateROIs(searchRoi?.CreateRoiDesc());
            SearchROI_SetEnabled = false;
            MeasurementROI_SetEnabled = false;
            MeasurementROI_InnerEnabled = false;
            MeasurementROI_OuterEnabled = false;
        }

        /// <summary>
        /// 创建SearchRoi
        /// </summary>
        [RelayCommand]
        void SearchRoi_Confirm()
        {
            var rOIDescs = HpImageViewer.ImageViewerDesc.ROIDescs.FirstOrDefault();
            if (rOIDescs == null || rOIDescs is not RectangleDesc desc)
            {
                _messageBoxService.InfoDialog("Please draw the Rect ROI！");
                return;
            }
            SetSearchingRect(desc.CreateRoi() as Rect2d);
            InitMeasurementRoiButton();
        }

        private SingleTemplateMatchingContext GetTemplate(bool isInner)
        {
            return isInner ? TestTrainSetting.OverlayMark_InnerModels[0] : TestTrainSetting.OverlayMark_OuterModels[0];
        }
        private void SetSearchingRect(Rect2d rect)
        {
            GetTemplate(true)?.UpdateSearchArea(rect);
            GetTemplate(false)?.UpdateSearchArea(rect);
        }

       

        private Rect2d GetSearchingRect()
        {
            return GetTemplate(true).SelectedMatchModelGroup.MatchModel.SearchingRect;
        }


        /// <summary>
        /// 清除SearchRoi
        /// </summary>
        [RelayCommand]
        void SearchRoi_Clear()
        {
            SetSearchingRect(null);
            HpImageViewer.UpdateROIs();
            InitMeasurementRoiButton();
        }

        /// <summary>
        /// 取消设置SearchRoi
        /// </summary>
        [RelayCommand]
        void SearchRoi_Cancel()
        {
            InitMeasurementRoiButton();
        }

        #endregion MeasureRoi
    }
}
