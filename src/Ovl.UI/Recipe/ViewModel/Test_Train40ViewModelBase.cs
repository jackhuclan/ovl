using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;
using HPImageViewer.Core.Primitives;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainLib.Services;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.MotionController;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.SectionManager.Interface;
using InteVega.CBB.StageControlModule.EventAggregators;
using InteVega.CBB.StageControlModule.Views.CameraView;
using InteVega.CBB.SystemConfig.Interface;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using InteVega.CBB.TemplateMatchingModule.Views;
using Marvel.Application40.Services;
using Marvel.DataModels;
using Marvel.DataModels.DeviceParameter;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.AIM;
using Marvel.DataModels.Recipe.CD;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.DataModels.Recipe40;
using Marvel.DataModels.Recipe40.TestInfo;
using Marvel.DataModels40.DeviceParameter;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.Interface;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using OVL.Algorithm.DoNet;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InteVega.CBB.MotionProof.Abstraction;
using Wafer.Algorithm.Common.DoNet;

using Ovl.UI.Recipe.View;
using Ovl.UI.ViewModels.Controls;
using Ovl.UI.ViewModels.Models;
using Ovl.UI.ViewModels.Extensions;
namespace Ovl.UI.Recipe.ViewModel
{
    internal partial class Test_Train40ViewModelBase : Test_TrainViewModelBase<TestInfo40>
    {
        #region CONST String

        private const string TRAIN = "Train";
        private const string _messageBoxToken = "TestTrainMessage";
        protected const string CONST_Average = "Average";
        protected const string CONST_Inner = "Inner";
        protected const string CONST_Outer = "Outer";
        protected const string CONST_PREVIEW = "PreviewMatch";
        protected const string CONST_PREVIEW_INNER = "PreviewMatch_Inner";
        protected const string CONST_PREVIEW_OUTER = "PreviewMatch_Outer";

        #endregion

        protected readonly IFocusService_SpecCam _focusService40;
        protected readonly IMeasurementService _measService;
        protected readonly IEventAggregator _eventAggregator;
        protected readonly IFocusConfigService _focusMeterService;

        /// <summary>CD OVL L 型方向枚举的全部取值，避免每次 Loaded 反射分配。</summary>
        private static readonly List<LShapeDirection> AllLShapeDirections = Enum.GetValues(typeof(LShapeDirection))
                                                                               .Cast<LShapeDirection>()
                                                                               .ToList();
        /// <summary>
        /// 为 true 时表示正在从配方同步 <see cref="CurrentDirection"/>，此时忽略 <see cref="OnCurrentDirectionChanged"/>，
        /// 避免 Loaded 赋值误记为“用户修改方向”。
        /// </summary>
        private bool _isApplyingDirectionFromModel;
        /// <summary>
        /// 用户是否在界面上修改过 CD OVL 的 L 型方向（相对配方中已存值）；为 true 时设置量测 ROI 会强制重绘默认 ROI。
        /// </summary>
        protected bool _isDirectionChanged;

        [ObservableProperty]
        // 绑定数据源
        public List<LShapeDirection> _directionList;

        [ObservableProperty]
        // 当前选中的枚举
        private LShapeDirection _currentDirection;

        /// <summary>
        /// CD OVL 方向下拉变更：写回配方中的 CDOVLDirection，并标记需刷新量测 ROI。
        /// </summary>
        partial void OnCurrentDirectionChanged(LShapeDirection old, LShapeDirection newLShape)
        {
            if (_isApplyingDirectionFromModel || TestInfo == null)
            {
                return;
            }

            if (TestInfo.CDOVLDirection == newLShape)
            {
                return;
            }

            TestInfo.CDOVLDirection = newLShape;
            _isDirectionChanged = true;
        }

        protected IAlignmentService40 _alignmentService40 => _alignmentService as IAlignmentService40;
        // 内层全局高度
        protected double InnerGlobalHeight { get; set; }
        // 外层全局高度
        protected double OuterGlobalHeight { get; set; }

        protected readonly IAlignmentService alignmentService;
        protected readonly IServiceCalData _servCalDat;
        private readonly IVisionServiceManager _visionServiceManager;
        //private readonly ITrainTestSetParamterNotifyManager _trainTestSetParamterNotifyManager;
        protected readonly IMachineDomainService _machineDomainService;
        protected readonly MachineDomainConfig _machine40Config;
        protected readonly IServicePlat _servPlat;

        protected List<CorrelationCurve> CorrelationCurvesList;
        [ObservableProperty]
        private EnumFocusBasedOn _focusBasedOn = EnumFocusBasedOn.Inner;

        /// <summary>Measurement 页 Inner/Outer 单选变更；子类可覆写 <see cref="OnFocusBasedOnChangedCore"/>。</summary>
        partial void OnFocusBasedOnChanged(EnumFocusBasedOn value) => OnFocusBasedOnChangedCore(value);

        protected virtual void OnFocusBasedOnChangedCore(EnumFocusBasedOn value) { }

        /// <summary>
        /// StageControlImageView
        /// </summary>
        [ObservableProperty]
        IStageControlImageView _stageControlImageView;
        [ObservableProperty]
        private string _selectedImageViewNaviItem = TRAIN;
        [ObservableProperty]
        BIBAlgorithmParam _bibAlgorithmParam;

        [ObservableProperty]
        CDOvlAlgorithmParam _CDOvlAlgorithmParam;

        [ObservableProperty]
        CDOvlOfOverFovAlgorithmParam _CDOvlOfOverFovAlgorithmParam;

        [ObservableProperty]
        private ObservableCollection<BIBAlgorithmType> _enumBibAlgorithmType;

        [ObservableProperty]
        private double _innerFocusHeight;
        [ObservableProperty]
        private double _outerFocusHeight;
        [ObservableProperty]
        private double _zInnerOffset;
        [ObservableProperty]
        private double _zOuterOffset;

        [ObservableProperty]
        private int _roiExtendSize_Inner = 40;
        [ObservableProperty]
        private int _roiExtendSize_Outer = 40;

        [ObservableProperty]
        private bool _useCross4rectan;

        [ObservableProperty]
        private bool _crossUseBib = true;

        partial void OnUseCross4rectanChanged(bool value)
        {
            if (TestInfo?.OMParam?.AlgorithmParam is not BIBAlgorithmParam || TrainParam == null)
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

        /// <summary>
        /// 当前测试是否使用 CD OVL 算法（用于显示 L 型方向等 CD OVL 专用 UI）。
        /// </summary>
        [ObservableProperty]
        private bool _isCdOvlAlgorithm;

        protected ICameraLightParamService40 _cameraLightParamService40 => _cameraLightParamService as ICameraLightParamService40;
        public SingleTemplateControl InnerTemplateControl { get; set; }
        public SingleTemplateControl OuterTemplateControl { get; set; }
        public SingleTemplateControl PositioningTemplateControl_Inner
        {
            get
            {
                return PositioningTemplateControl;
            }
            set
            {
                PositioningTemplateControl = value;
            }
        }
        public SingleTemplateControl PositioningTemplateControl_Outer { get; set; }

        [ObservableProperty]
        private ObservableCollection<CorrelationType> _enumCorrelationType;
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
        private bool _isShowSearchArea = true;
        [ObservableProperty]
        private ObservableCollection<MeasurementResultData> _measurementResultDataList;
        [ObservableProperty]
        private bool _focusInnerROI_SetAfterLightControlEnabled;

        MultiTemplateMatchingContext _templateContext2_inner;
        MultiTemplateMatchingContext _templateContext2_outer;

        Point2d _innerMainPoint;
        Point2d _outerMainPoint;
        protected Physical Physical
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

        protected EnumGrabMode _grabMode
        {
            get
            { return Physical.TestSettings.GrabMode; }
        }

        protected TrainSettings_BiB TestTrainSetting
        {
            get
            {
                return TestInfo?.GetTrainSettings();
            }
        }

        TestOverlayTrainParam TrainParam
        {
            get
            {
                return TestInfo?.GetTrainSettings()?.TrainParam;
            }
        }

        LightServiceForMeasure40 LightServiceForMeasure
        {
            get
            {
                return _lightServiceForMeasure as LightServiceForMeasure40;
            }
        }

        /// <summary>
        /// 是否更改过内层的量测roi 
        /// </summary>
        public bool IsUpdateMeasureRoi { get; protected set; }

        protected const string OuterColor = "#00FFFF";
        protected const string InnerColor = "#FFFF00";
        protected const string PositioningColor = "#FF7F00";
        public Test_Train40ViewModelBase(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _focusMeterService = serviceProvider.GetService<IFocusConfigService>();
            alignmentService = _serviceProvider.GetRequiredService<IAlignmentService>();
            _servCalDat = _serviceProvider.GetService<IServiceCalData>();
            _visionServiceManager = _serviceProvider.GetService<IVisionServiceManager>();
            _visionService = _visionServiceManager.GetMainVision();
            //_trainTestSetParamterNotifyManager = _serviceProvider.GetService<ITrainTestSetParamterNotifyManager>();
            _measService = _serviceProvider.GetRequiredService<IMeasurementService>();
            _machineDomainService = serviceProvider.GetService<IMachineDomainService>();
            _machine40Config = _machineDomainService.MachineDomainConfig;
            _focusService40 = serviceProvider.GetService<IFocusService_SpecCam>();
            _servPlat = serviceProvider.GetService<IServicePlatNoProof>() as IServicePlat;
            _measService = serviceProvider.GetService<IMeasurementService>();
            _lightServiceForMeasure40 = _serviceProvider.GetService<ILightServiceForMeasure>() as LightServiceForMeasure40;
            _eventAggregator = _serviceProvider.GetService<IEventAggregator>();
        }

        protected ParamFocus GetCoarseParamFocus(ECamera camera = ECamera.Meas)
        {
            var paramFocus = GetTestFocusOrDefault(camera).CourseFocusInfo?.ParamFocus;
            return paramFocus == null ? null : CloneHelper.ReflectionClone<ParamFocus>(paramFocus);
        }

        /// <summary>
        /// 初始进入界面时需要显示的相机
        /// </summary>
        protected virtual ECamera FirstCamera { get; }

        protected override async Task ChangeMeasureCamera(ECamera camera = ECamera.Meas, bool isShiftCamera = true)
        {
            ChangeCameraInfo = "Camera switching …";
            await Task.Run(() =>
            {
                try
                {
                    _alignmentService.ChangeCamera(camera, isShiftCamera);
                }
                catch (ErrorCodeException e)
                {
                    var alarmService = _serviceProvider.GetService<IAxisSoftLimitAlarmService>();
                    if (alarmService?.IsSoftLimitError(e.ErrorCode) == true)
                    {
                        LogHelper.LogInfo($"switch camera error,{e}");
                        alarmService.SetAlarm(e);
                        try
                        {
                            _messageBoxService.Warning("switch camera out of softlimt,please move axis to appropriate position and try again");
                        }
                        finally
                        {
                            alarmService.ClearAlarm(e);
                        }
                    }
                    else
                    {
                        LogHelper.LogError($"switch camera error,{e}");
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogError(e.Message);
                }
                finally
                {
                    //_visionServiceManager.NotifySetCameraLightParameter(true);

                    SetVisionParams(camera);
                    if (TestInfo.IsDoubleFace)
                    {
                        SetVisionParams(camera == ECamera.MeasExt ? ECamera.Meas : ECamera.MeasExt);
                    }
                    ChangeCameraInfo = "";
                    //_visionServiceManager.IsSetCamreaLightPara.Set();
                    //_visionServiceManager.NotifySetCameraLightParameter(false);
                }
            });
        }

        /// <summary>
        /// 设置量测相机参数
        /// </summary>
        protected virtual void SetVisionParams(ECamera camera = ECamera.Meas)
        {
        }
        [RelayCommand]
        public virtual async Task Loaded()
        {
            await ChangeMeasureCamera(FirstCamera);
            BindCheckBox();
            // LoadOvResult();
            LogHelper.LogInfo($"Test_TrainViewModel Loaded:MacroFocusZ={alignmentService.MacroFocusZ},BaseFocusHeight={TestTrainSetting.FocusModuleInfo.BaseFocusHeight}");
            SetFocusZHeight();
            RoiExtendSize_Inner = TestTrainSetting.RoiExtendSize_Inner;
            RoiExtendSize_Outer = TestTrainSetting.RoiExtendSize_Outer;
            // 先关闭 CD OVL 专用 UI，再根据当前配方算法类型打开（避免切换配方后状态残留）。
            IsCdOvlAlgorithm = false;
            if (TestInfo.OMParam.AlgorithmParam is BIBAlgorithmParam)
            {
                var a = TestInfo.TrainSetting as TrainSettings_Overlay;
                var innerType = a?.InnerType;
                if (innerType != EnumTargetType.Cross)
                {
                    UseCross4rectan = TrainParam.OuterLayer.UseNewCross;
                    CrossUseBib = false;
                }
            }
            if ((TestInfo.OMParam.AlgorithmParam is CDOvlAlgorithmParam))
            {
                IsCdOvlAlgorithm = true;
                DirectionList = new List<LShapeDirection>(AllLShapeDirections);
                EnumCDOVLPolarity = new ObservableCollection<EdgePolarity>(AllEdgePolarityDirections);
                EnumCDOVLEdgeSelect = new ObservableCollection<EdgeSelect>(AllEdgeSelectDirections);
                // 从配方同步方向到 VM，期间不触发“方向已变更”逻辑。
                _isApplyingDirectionFromModel = true;
                try
                {
                    CurrentDirection = TestInfo.CDOVLDirection;
                    _isDirectionChanged = false;
                }
                finally
                {
                    _isApplyingDirectionFromModel = false;
                }
            }
            if (TestTrainSetting.RoiExtendSize_Inner == 0 && TestTrainSetting.RoiExtendSize_Outer == 0)
            {
                if (TestTrainSetting.InnerType == EnumTargetType.AIM)
                {
                    RoiExtendSize_Inner = 40;
                    RoiExtendSize_Outer = 40;
                }
                else
                {
                    RoiExtendSize_Inner = 0;
                    RoiExtendSize_Outer = 0;
                }
            }

            switch (TestTrainSetting.InnerType)
            {
                case EnumTargetType.Bar:
                case EnumTargetType.Box:
                case EnumTargetType.Circle:
                case EnumTargetType.Cross:
                case EnumTargetType.Frame:
                    BibAlgorithmParam = (BIBAlgorithmParam)TestInfo.OMParam.AlgorithmParam;
                    break;
                case EnumTargetType.AIM:
                    break;
                case EnumTargetType.AIM_XYS:
                    break;
                case EnumTargetType.LS:
                    break;
                case EnumTargetType.Aperture:
                    break;
                case EnumTargetType.LAngle:
                    CDOvlAlgorithmParam = (CDOvlAlgorithmParam)TestInfo.OMParam.AlgorithmParam;
                    SelectEdgePolarity = CDOvlAlgorithmParam.EdgePolarity;
                    SelectEdgeSelect = CDOvlAlgorithmParam.EdgeSelect;
                    EdgeFilterLevel = CDOvlAlgorithmParam.EdgeFilterLevel;
                    MinEdgeContrast = CDOvlAlgorithmParam.MinEdgeContrast;
                    break;
                case EnumTargetType.LCV:
                    CDOvlOfOverFovAlgorithmParam = (CDOvlOfOverFovAlgorithmParam)TestInfo.OMParam.AlgorithmParam;
                    SelectEdgePolarity = CDOvlOfOverFovAlgorithmParam.EdgePolarity;
                    SelectEdgeSelect = CDOvlOfOverFovAlgorithmParam.EdgeSelect;
                    EdgeFilterLevel = CDOvlOfOverFovAlgorithmParam.EdgeFilterLevel;
                    MinEdgeContrast = CDOvlOfOverFovAlgorithmParam.MinEdgeContrast;
                    break;
                default:
                    break;
            }

            if (InnerTemplateControl != null)
            {
                if (!TestTrainSetting.OverlayMark_InnerModels.Any())
                {
                    TestTrainSetting.OverlayMark_InnerModels = new List<SingleTemplateMatchingContext>() { new() };
                }
                TestTrainSetting.OverlayMark_InnerModels[0].Name = "Inner Model";
                LogHelper.LogDebug($"{InnerTemplateControl.GetHashCode()}");
                InnerTemplateControl.TemplateContext = TestTrainSetting.OverlayMark_InnerModels[0];
                InnerTemplateControl.ActionUserModified = TemplateUserModified;
                InnerTemplateControl.FuncBeforeCreateModel = InnerTemplateBeforeCreateModel;
                if (_focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner)
                {
                    HpImageViewer.SetTemplateImage(InnerTemplateControl.TemplateContext, TestFilesFolder);
                }
            }
            if (OuterTemplateControl != null)
            {
                if (!TestTrainSetting.OverlayMark_OuterModels.Any())
                {
                    TestTrainSetting.OverlayMark_OuterModels = new List<SingleTemplateMatchingContext>() { new() };
                }
                TestTrainSetting.OverlayMark_OuterModels[0].Name = "Outer Model";
                OuterTemplateControl.TemplateContext = TestTrainSetting.OverlayMark_OuterModels[0];
                OuterTemplateControl.ActionUserModified = TemplateUserModified;
                OuterTemplateControl.FuncBeforeCreateModel = OuterTemplateBeforeCreateModel;
                if (_focusSetting.FocusBasedOn == EnumFocusBasedOn.Outer)
                {
                    HpImageViewer.SetTemplateImage(OuterTemplateControl.TemplateContext, TestFilesFolder);
                }
            }
            InitFocusRoiButton();
            InitCorrections();
            InitMeasurementRoiButton();
            if (TestTrainSetting.FocusModuleInfo.BaseFocusHeight == 0)
            {
                TestTrainSetting.FocusModuleInfo.BaseFocusHeight = _alignmentService.WaferMapInfo.Thickness;
            }
            IsTrained = TrainSetting.IsTrained;
            if (!IsTrained)
            {

                _lightServiceForMeasure40.Init();
                if (HpImageViewer?.Image == null)
                    _visionService.StartGrabbing();
            }
            SetRecipeWaferHeightToFocusControl();
        }

        /// <summary>
        /// 将Recipe中的晶圆厚度设置到聚焦控件，用于计算焦面高度，可以使Sharpest按钮使用Recipe中的厚度移动到焦面
        /// </summary>
        private void SetRecipeWaferHeightToFocusControl()
        {
            _eventAggregator?.GetEvent<SetSpecWaferHeightEvent>().Publish(_alignmentService40.WaferMapInfo.Thickness);
        }

        protected void SaveOMParamToFile(object omParam)
        {
            string dirPath = Path.Combine(_serviceProvider.GetService<IRecipeManager>().GetRecipePath(_alignmentService40.RecipeElementsInfo.Id), TestInfo.Name);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            var fileName = Path.Combine(dirPath, "omParamTest.hdict");
            if (omParam is OMParam omPara) omPara.SerializeToDictFile(fileName);
            else if (omParam is OMParam_Double omPara_Double) omPara_Double.SerializeToDictFile(fileName);
            else throw new Exception("Not Support OmParam Type");
        }

        internal bool InnerTemplateBeforeCreateModel()
        {
            return BeforeCreateLayerModel(true);
        }
        internal void TemplateUserModified(SingleTemplateControl control, ITemplateMatchingView.UserModifyType type, SingleTemplateMatchingContext context, bool isSuccess)
        {
            if (!isSuccess) return;
            bool isInner = control == InnerTemplateControl;
            TemplateUserModified(isInner, type, context);
        }

        private void TemplateUserModified(bool isInner, ITemplateMatchingView.UserModifyType type, SingleTemplateMatchingContext context)
        {

            if (type == ITemplateMatchingView.UserModifyType.ModelCreated)
            {
                if (isInner)
                {
                    TestTrainSetting.InnerTrainMatchItem = context.MatchingItems[0];
                }
                else
                {
                    TestTrainSetting.OuterTrainMatchItem = context.MatchingItems[0];
                }
            }
            if (type == ITemplateMatchingView.UserModifyType.ModelCleanUp)
            {
                HpImageViewer.Image = LoadFocusImageData(isInner);
            }

        }

        internal bool OuterTemplateBeforeCreateModel()
        {
            return BeforeCreateLayerModel(false);

        }




        private bool BeforeCreateLayerModel(bool isInner)
        {
            var imageData = LoadFocusImageData(isInner);
            HpImageViewer.Image = imageData;
            if (HpImageViewer.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return false;
            }

            //点击Confirm按钮时，重绘量测Roi，避免被其它roi影响
            var roiDesc = GetRoiDesc(isInner, isMeasure: true);// isInner ? TestTrainSetting.InnerRectangle : TestTrainSetting.OuterRectangle;
            if (roiDesc == null)
            {
                _messageBoxService.InfoDialog("Please draw the corresponding ROI！");
                return false;
            }
            if (TargetPoint == null)
            {
                _messageBoxService.InfoDialog("Please redraw the measure ROI！");
                return false;
            }
            HpImageViewer.UpdateROIs(roiDesc);

            _visionService.StopGrabbing();
            SetRoiExtendSize(isInner);
            IsUpdateMeasureRoi = true;
            return true;
        }

        private ROIDesc GetRoiDesc(bool isInner, bool isMeasure)
        {
            if (isMeasure)
                return isInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
            else
                return isInner ? TrainParam.InnerLayer.FocusROI : TrainParam.OuterLayer.FocusROI;
        }

        protected void SetRoiExtendSize(bool isInner)
        {
            var template = isInner ? InnerTemplateControl.TemplateContext : OuterTemplateControl.TemplateContext;
            if (template.SelectedMatchModelGroup != null)
            {
                template.SelectedMatchModelGroup.RoiExtendSize = isInner ? RoiExtendSize_Inner : RoiExtendSize_Outer;
            }
        }

        protected virtual void SetFocusZHeight()
        {
            throw new NotImplementedException();
        }

        public void InitFocusRoiButton()
        {
            FocusInnerROI_SetAfterLightControlEnabled = true;
            FocusROI_SetEnabled = true;
            FocusROI_InnerEnabled = false;
            FocusROI_OuterEnabled = false;
            HpImageViewer.UpdateROIs();
        }

        /// <summary>
        /// 是否已完成创建model
        /// </summary>
        protected bool isCreatedModel = false;

        /// <summary>
        /// 量测添加ROI前 加载聚焦生成的原图
        /// </summary>
        public ImageData LoadFocusImageData(bool isInner)
        {

            if (Physical == null || Physical.TestSettings == null)
            {
                _messageBoxService.ErrorDialog("Physical data is incomplete!");
                return null;
            }
            return LoadImageData(GetImageName(isInner));
        }

        private ImageData LoadImageData(string imgName)
        {
            string imgPath = GetImageAbsolutePath(imgName);
            if (!File.Exists(imgPath))
            {
                throw new Exception($"Please complete the {imgName}  focusing operation!");
            }
            return ReadImageFile(imgPath);
        }

        /// <summary>
        /// 获取Image名称
        /// </summary>
        /// <param name="isInner">是否为inner层</param>
        /// <returns></returns>
        protected virtual string GetImageName(bool isInner)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取图片绝对路径
        /// </summary>
        /// <param name="imageName">图片名称</param>
        /// <returns></returns>
        private string GetImageAbsolutePath(string imageName)
        {
            var filePath = Path.Combine(TestFilesFolder, imageName + ".bmp");
            return filePath;
        }

        void BindCheckBox()
        {
            EnumBibAlgorithmType = new ObservableCollection<BIBAlgorithmType>();
            foreach (BIBAlgorithmType value in Enum.GetValues<BIBAlgorithmType>())
            {
                EnumBibAlgorithmType.Add(value);
            }
            EnumCorrelationType = new ObservableCollection<CorrelationType>();
            foreach (CorrelationType value in Enum.GetValues<CorrelationType>())
            {
                EnumCorrelationType.Add(value);
            }
        }
        private bool _isTrained;
        protected bool IsTrained
        {
            get => _isTrained;
            set
            {
                _isTrained = value;
                TestInfo.GetTrainSettings().IsTrained = value;
                if (!value)
                    SetSearchingRect(null);
            }
        }

        protected void Submit(bool isCheckData)
        {
            try
            {
                LogHelper.LogInfo($"Save train result,isCheckData={isCheckData},test={TestInfo .Name},IsTrained={TestTrainSetting.IsTrained}");
                TestTrainSetting.RoiExtendSize_Inner = RoiExtendSize_Inner;
                TestTrainSetting.RoiExtendSize_Outer = RoiExtendSize_Outer;
                UpdateTestInfoData(isCheckData);
                if (isCheckData)
                {
                    ExecuteSomeMethod(_messageBoxToken);
                }
                //_messageBoxService.Success("Data saved successfully！", "TestTrainMessage");
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.Error($"Failed to save data!{ex.Message}", _messageBoxToken);
            }
        }
        private void ExecuteSomeMethod(string messageToken)
        {
            NotifySaveAction?.Invoke(TestInfo, messageToken);
        }
        protected void UpdateTestInfoData(bool isCheckData)
        {
            if (isCheckData)
            {
                UpdateTemplateModelScore();
                CheckTemplateData(InnerTemplateControl.TemplateContext, CONST_Inner);
                CheckTemplateData(OuterTemplateControl.TemplateContext, CONST_Outer);
            }
        }

        void CheckTemplateData(SingleTemplateMatchingContext template, string key)
        {
            _templateMatchingService.ReadTemplatesModelInfo(template, TestFilesFolder);
            if (template.SelectedMatchModelGroup == null || template.SelectedMatchModelGroup.MatchModel.ModelInfo == null)
            {
                _messageBoxService.Error($"The {key} template is empty!", _messageBoxToken);
            }
        }

        /// <summary>
        /// Inner聚焦ROI设置
        /// </summary>
        [RelayCommand]
        public void InnerFocusROI_Set()
        {
            SetTempFocusRoi(true);
        }

        /// <summary>
        /// Outer聚焦Roi设置
        /// </summary>
        [RelayCommand]
        public void OuterFocusROI_Set()
        {
            SetTempFocusRoi(false);
        }
        /// <summary>
        /// 设置临时聚焦Roi
        /// </summary>
        /// <param name="isInner"></param>
        private void SetTempFocusRoi(bool isInner)
        {
            FocusInnerROI_SetAfterLightControlEnabled = true;
            if (HpImageViewer?.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return;
            }
            var roi = isInner ? TrainParam.InnerLayer.FocusROI : TrainParam.OuterLayer.FocusROI;
            if (roi != null)
                HpImageViewer.UpdateROIs((RectangleDesc)CloneHelper.ReflectionClone(roi));
            else
                FocusROI(isInner);

            FocusROI_SetEnabled = false;
            FocusROI_InnerEnabled = isInner;
            FocusROI_OuterEnabled = !isInner;
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
            var inRoi = TestTrainSetting.OverlayMark_InnerModels[0].ROI;
            var outRoi = TestTrainSetting.OverlayMark_OuterModels[0].ROI;
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

        /// <summary>
        /// Single Focus 
        /// </summary>
        [RelayCommand]
        async Task TrainFocus_Single()
        {
            await TrainFocus_SingleMethod();
        }

        /// <summary>
        /// Single Focus 
        /// </summary>
        [RelayCommand]
        async Task TrainFocus_InnerSingle()
        {
            await TrainFocus_InnerSingleMethod();
        }

        /// <summary>
        /// Single Focus 
        /// </summary>
        [RelayCommand]
        async Task TrainFocus_OuterSingle()
        {
            await TrainFocus_OuterSingleMethod();
        }

        protected virtual Task TrainFocus_SingleMethod()
        {
            throw new NotImplementedException();
        }

        protected virtual Task TrainFocus_InnerSingleMethod()
        {
            throw new NotImplementedException();
        }
        protected virtual Task TrainFocus_OuterSingleMethod()
        {
            throw new NotImplementedException();
        }

        private ImageData SelectImageData(FocusPegResult innerRes, double focusHeight)
        {
            return innerRes.PegImages.Where(_ => _.Dot.Z.Equals(focusHeight)).Select(_ => _.Image).FirstOrDefault();
        }

        /// <summary>
        /// 在第一层的焦面采集预匹配模板原图
        /// </summary>
        /// <param name="isInner"></param>
        protected void SavePreviewImageSingleLayer(ImageData img, string name)
        {
            //采集Base层的图片，并且使用该图片创建预匹配模板
            LogHelper.LogInfo($"Move to previewmatch layer measure focus:Base Layer={_focusSetting.FocusBasedOn}");
            //传null是为了不设置sharpness和kernel
            SaveFocusImageCore(name, null, img);
        }

        // 处理内层聚焦的通用逻辑
        protected virtual void ProcessInnerFocusCommon(ShapeBase innerROI, FocusPegResult innerRes,
            TrainSettings_BiB trainSettings, Action<double> moveToHeight, bool isTrainCurve = false)
        {
            try
            {
                var coarseFocusInfo = GetTestFocusOrDefault().CourseFocusInfo;
                var focusImages = innerRes.PegImages;
                var trainArg = CreateTrainArg(_grabMode);


                // ROI聚焦训练
                var ret = _focusService.TrainCurve(coarseFocusInfo, innerROI, focusImages, trainArg);
                // 全视野聚焦训练
                var ret2 = _focusService.TrainCurve(coarseFocusInfo, null, focusImages, trainArg);
                if (isTrainCurve)
                {
                    // 保存聚焦曲线
                    TrainParam.InnerLayer.RoiFocusCurve = ret.Curve;

                    // 保存全局聚焦曲线（根据训练设置类型）
                    if (trainSettings is TrainSettings40_DoubleFace doubleFaceSettings)
                    {
                        doubleFaceSettings.GlobalFocusCurveInner = ret2.Curve;
                    }
                }

                // 获取聚焦高度
                InnerFocusHeight = ret.ZFit;
                InnerGlobalHeight = ret2.ZFit;

                // 移动到聚焦高度
                moveToHeight(InnerFocusHeight);
                //await CreateMeasureKernelSizeAndTemplate(false, false, LoadFocusImageData(false));

                // 保存聚焦图像
                var img = SaveFocusImage_ByExistFocusMaxSharpness(CONST_Inner, true, innerROI, innerRes, ret.ZFit, isTrainCurve, IsCorrectionGrabbed(true));

                // 保存预匹配图像
                SavePreviewImageSingleLayer(img, CONST_PREVIEW_INNER);
            }
            catch (Exception ex)
            {
                AppHelper.DispatcherInvoke(() =>
                {
                    _messageBoxService.ErrorDialog($"Inner focus train exception:{ex}");
                });
            }
        }

        // 处理外层聚焦的通用逻辑
        protected virtual void ProcessOuterFocusCommon(ShapeBase outerROI, FocusPegResult outerRes,
            TrainSettings_BiB trainSettings, Action<double> moveToHeight, bool isTrainCurve = false)
        {
            try
            {
                var coarseFocusInfo = GetTestFocusOrDefault().CourseFocusInfo;
                var focusImages = outerRes.PegImages;
                var trainArg = CreateTrainArg(_grabMode);

                // ROI聚焦训练
                var ret = _focusService.TrainCurve(coarseFocusInfo, outerROI, focusImages, trainArg);
                // 全视野聚焦训练
                var ret2 = _focusService.TrainCurve(coarseFocusInfo, null, focusImages, trainArg);

                // 保存聚焦曲线
                TrainParam.OuterLayer.RoiFocusCurve = ret.Curve;

                // 保存全局聚焦曲线（根据训练设置类型）
                if (trainSettings is TrainSettings40_DoubleFace doubleFaceSettings)
                {
                    doubleFaceSettings.GlobalFocusCurveOuter = ret2.Curve;
                }

                // 获取聚焦高度
                OuterFocusHeight = ret.ZFit;
                OuterGlobalHeight = ret2.ZFit;

                // 移动到聚焦高度
                moveToHeight(OuterFocusHeight);

                // 保存聚焦图像
                var img = SaveFocusImage_ByExistFocusMaxSharpness(CONST_Outer, false, outerROI, outerRes, ret.ZFit, isTrainCurve, IsCorrectionGrabbed(false));

                // 保存预匹配图像
                SavePreviewImageSingleLayer(img, CONST_PREVIEW_OUTER);
            }
            catch (Exception ex)
            {
                AppHelper.DispatcherInvoke(() =>
                {
                    _messageBoxService.ErrorDialog($"Outer focus train exception:{ex}");
                });
            }
        }

        // 处理图像查看器的通用逻辑
        protected virtual void ProcessImageViewerCommon(IHPImageViewer viewer, HObject image,
            OMROIBase roiDesc, OMTargetType targetType, ROIDesc measureROI)
        {
            var roiDescs = new List<ROIDesc>();
            roiDescs.AddRange(GetProfileForImageView(image, roiDesc, targetType));
            roiDescs.Add(measureROI);
            viewer.Image = image.ToImageData();
            viewer.UpdateROIs(roiDescs.ToArray());
        }

        /// <summary>
        /// 创建聚焦训练行程和步进
        /// 行程用全行程设置为1，步进用0.1
        /// </summary>
        /// <returns></returns>
        protected AutoRunInfo CreateFullTrainInfo(ECamera cam)
        {
            var measCfg = _focusMeterService.GetCurrentFocusMeter();//cam == ECamera.Meas ? lCfg.MeasZMeter : lCfg.MeasZExtMeter;
            // var coarseFocusInfo = GetTestFocusOrDefault(cam).CourseFocusInfo;
            int unit = 1000;
            // 此处大行程步进必须大于小形成步进，否则执行精聚焦参数校验时会报错
            return new AutoRunInfo() { Range = measCfg.AllRange / unit, Step = measCfg.AllRangeStep * 2 / unit, SmallRange = measCfg.AllRange / unit, SmallStep = measCfg.AllRangeStep / unit };
        }

        protected async Task<ImageData> SaveFocusImage_ByGrab(string imageName, IVisionService visionService, bool isInner, ShapeBase roi, bool isMeasure)
        {
            ImageData imageData = null;
            visionService.StartGrabbing();
            // 使用异步方式获取图像数据，避免界面阻塞
            imageData = await visionService.GrabOneImageAsync();
            //var imageData = visionService.GrabOneImage();
            SaveFocusImageCore(imageName, roi, imageData);
            if (isMeasure)
            {
                SetKernelAndGray(imageName, isInner, roi, imageData);
            }
            return imageData;
        }

        protected ImageData SaveFocusImage_ByExistFocusMaxSharpness(string imageName,
            bool isInner,
            ShapeBase roi,
            FocusPegResult innerRes,
            double focusHeight,
            bool isMeasure,
            bool isCorrectionGrabed = false)
        {
            var img = SelectImageData(innerRes, focusHeight);
            HpImageViewer.Image = img;
            HpImageViewer.UpdateROIs();
            if (!isCorrectionGrabed)
            {
                SaveFocusImageCore(imageName, roi, img);
            }
            if (!isCorrectionGrabed && isMeasure)
            {
                SetKernelAndGray(imageName, isInner, roi, img);
            }
            return img;
        }

        protected void SaveFocusImage_ByExistImage(string imageName, ImageData imageData, bool isInner, ShapeBase roi)
        {
            SaveFocusImageCore(imageName, roi, imageData);
            SetKernelAndGray(imageName, isInner, roi, imageData);
        }

        protected void SaveFocusImageCore(string imageName, ShapeBase roi, ImageData imageData)
        {
            var filePath = GetImageAbsolutePath(imageName);
            FileHelper.DeleteFile(filePath);
            imageData.SaveBmp(filePath);
        }

        protected void SetKernelAndGray(string imageName, bool isInner, ShapeBase roi, ImageData imageData)
        {
            SetGray(imageData, isInner, roi);
            SetKernel(roi, isInner, imageData);
        }

        /// <summary>
        /// 40Train界面执行聚焦通用方法
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="roi"></param>
        /// <param name="coarseFocusInfo"></param>
        /// <param name="roiColor"></param>
        /// <returns></returns>
        protected (ShapeBase ROI, double FitHeight, FocusPegResult pegRet) ExecuteFocusCommon(ECamera camera, ROIDesc roi, AutoRunInfo coarseFocusInfo, string roiColor)
        {
            // 处理ROI
            var roiDesc = roi;
            var finalRoi = roi.CreateRoi();
            roiDesc.Color = roiColor;
            HpImageViewer.UpdateROIs(roiDesc);
            if (finalRoi is not Rect2d) throw new Exception("Please draw roi type of Reactangle!");
            var pegRes = _focusService40.FineFocusSingleCamTrain(camera, coarseFocusInfo, shape: finalRoi);
            return (finalRoi, pegRes.ZFit, pegRes);
        }

        private void SetGray(ImageData imageData, bool isInner, ShapeBase roi)
        {
            if (roi != null)
            {
                var gray = LightServiceForMeasure.GetGrayOfImage(imageData, roi);
                if (isInner)
                {
                    TrainParam.InnerLayerGray = gray;
                }
                else
                {
                    TrainParam.OuterLayerGray = gray;
                }
            }
        }

        protected void SetKernel(ShapeBase roi, bool isInner, ImageData imageData)
        {
            if (roi != null)
            {
                var kernel = _focusService.GetImageKernel(imageData, roi);
                if (isInner)
                {
                    TrainParam.InnerLayer.FocusKernel = kernel;
                }
                else
                {
                    TrainParam.OuterLayer.FocusKernel = kernel;
                }
                LogHelper.LogInfo($"Save FocusKernel,isInner={isInner}");
            }
        }

        [RelayCommand]
        async Task Run()
        {
            await RunMeasureMethod();
        }

        protected async virtual Task RunMeasureMethod()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 创建量测的KernelSize和定位模板
        /// </summary>
        /// <param name="isInner"></param>
        /// <param name="isShowTestResult">是否显示模板匹配结果</param>
        public async Task CreateMeasureKernelSizeAndTemplate(bool isInner, bool isShowTestResult, ImageData imageData)
        {
            _visionServiceManager.GetMainVision().StopGrabbing();
            //var imageData = LoadFocusImageData(isInner);
            HpImageViewer.Image = imageData;
            //HpImageViewer.FitImageToArea();
            if (HpImageViewer.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return;
            }
            //点击Confirm按钮时，重绘量测Roi，避免被其它roi影响
            var roiDesc = isInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
            if (roiDesc == null)
            {
                _messageBoxService.InfoDialog("Please draw the corresponding ROI！");
                return;
            }
            HpImageViewer.UpdateROIs(roiDesc);

            try
            {
                var exResult = await GenKernelModelRoi(isInner, isShowTestResult);
                if (exResult != null)
                {
                    if (exResult.IsOK)
                    {
                        InitMeasurementRoiButton(!isShowTestResult);
                    }
                    else
                    {
                        _messageBoxService.ErrorDialog(exResult.ErrorCode + exResult.ErrorMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                _messageBoxService.ErrorDialog("Failed to generate measurement template data！");
                LogHelper.LogError(ex, "CreateMeasureKernelSizeAndTemplate failed");
            }

        }

        /// <summary>
        /// 创建 gray Size调整框和Kernelmodel
        /// </summary>
        private async Task<ExResult> GenKernelModelRoi(bool isInner, bool isTest)
        {
            ExResult exResult = null;
            await AutoCreateModel(isInner, isTest);
            return CreateKernelModel(isInner, rectRoi => GenKernelRoi(rectRoi, isInner, out exResult));
        }

        protected OMROIBase GenKernelRoi(RectangleDesc roi, bool isInner, out ExResult exResult)
        {
            exResult = default;
            if (!isInner && (UseCross4rectan || roi is Cross4RectangleDesc))
            {
                return GenKernelRoiOfBIB(roi, out exResult);
            }

            var type = isInner ? TestTrainSetting.InnerType : TestTrainSetting.OuterType;
            switch (type)
            {
                case EnumTargetType.Bar:
                case EnumTargetType.Box:
                case EnumTargetType.Frame:
                case EnumTargetType.Circle:
                    return GenKernelRoiOfBIB(roi, out exResult);
                case EnumTargetType.Cross:
                    return GenKernelRoiOfCross(roi, out exResult);
                default:
                    throw new ArgumentException($"GenKernelRoi not support type: {type}");
            }
        }

        private ExResult CreateKernelModel(bool isInner, Func<RectangleDesc, OMROIBase> funcGenKernelRoi)
        {
            //var rectRoi = isInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
            //OMROIBase oMROIBase = funcGenKernelRoi(rectRoi);
            //var imageData = HpImageViewer.Image as ImageData;
            //using var imageInner = imageData.ToHObject();
            //var targetType = isInner ? TestTrainSetting.InnerType : TestTrainSetting.OuterType;
            //OMTargetType oMTargetType = targetType.ToOMTargetType();
            //var result = OMUtil.GenKernelModel(imageInner, oMROIBase, oMTargetType, out OMKernelModel oMKernelModel);
            //SaveKernelModel(isInner, oMROIBase, oMKernelModel);
            //SaveGoldenModel(isInner, imageData, oMROIBase, oMTargetType);
            //return result;

            var type = isInner ? TestTrainSetting.InnerType : TestTrainSetting.OuterType;
            if (type != EnumTargetType.LAngle && type != EnumTargetType.LCV)
            {
                var rectRoi = isInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
                OMROIBase oMROIBase = funcGenKernelRoi(rectRoi);
                var imageData = HpImageViewer.Image as ImageData;
                using var imageInner = imageData.ToHObject();
                var targetType = isInner ? TestTrainSetting.InnerType : TestTrainSetting.OuterType;
                OMTargetType oMTargetType = targetType.ToOMTargetType();
                var result = OMUtil.GenKernelModel(imageInner, oMROIBase, oMTargetType, out OMKernelModel oMKernelModel);
                SaveKernelModel(isInner, oMROIBase, oMKernelModel);
                SaveGoldenModel(isInner, imageData, oMROIBase, oMTargetType);
                return result;
            }
            else
            {
                var imageData = HpImageViewer.Image as ImageData;
                using var imageInner = imageData.ToHObject();
                var rectRoi = isInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
                if (rectRoi is not LRectangleDesc lRect)
                {
                    _messageBoxService.ErrorDialog("CD OVL measure ROI must be L-shape (LRectangleDesc).");
                    return default;
                }

                CDOvlROI cDOvl = new CDOvlROI();
                if (isInner)
                {
                    cDOvl.ROI = ROIRenderExtensions.CreateDimensionROI(lRect, NormalizeCdOvlEdgeEnumerableForKernel(InneredgeConfirmRois));
                }
                else
                {
                    cDOvl.ROI = ROIRenderExtensions.CreateDimensionROI(lRect, NormalizeCdOvlEdgeEnumerableForKernel(OuteredgeConfirmRois));
                }
                //AlgorithmParamBase ParamBase = new AlgorithmParamBase();
                var result = OMUtil.GenKernelModel(imageInner, cDOvl, TestInfo.OMParam.AlgorithmParam, OMTargetType.CDOvl, out OMKernelModel kernelRoi);
                SaveKernelModel(isInner, cDOvl, kernelRoi);

                return result;
            }

        }
        protected virtual void SaveKernelModel(bool isInner, OMROIBase oMROIBase, OMKernelModel oMKernelModel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 保存每一层的黄金模板
        /// </summary>
        /// <param name="isInner">是否是内层</param>
        /// <param name="imageData">量测焦面图像</param>
        /// <param name="oMROIBase">roi </param>
        /// <param name="oMTargetType">标记类型</param>
        protected void SaveGoldenModel(bool isInner, ImageData imageData, OMROIBase oMROIBase, OMTargetType oMTargetType)
        {
            if (oMTargetType != OMTargetType.Bar && oMTargetType != OMTargetType.Frame)
                return;
            try
            {
                var layer = isInner ? TrainParam.InnerLayer : TrainParam.OuterLayer;
                var goldenModel = _focusService.GenDefectInspectModel(imageData, oMROIBase, oMTargetType,layer.PolarityType );
                layer.GoldenModel = goldenModel;
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
            }
        }
        protected void LogRoiForAlgoTest(string logHeader, ShapeBase roi)
        {
            LogHelper.LogDebug($"{logHeader}: roi2d = {roi.ToJson()}");
            _serviceProvider.GetService<ISectionImageStorage>().SaveRoiRegion(roi, Path.Combine(TestFilesFolder, $"{DateTime.Now.ToString("HHmmss_fff")}.hobj"), true);
        }

        private BIBROI GenKernelRoiOfBIB(RectangleDesc roi, out ExResult exResult)
        {
            var bib2d = roi.CreateRoi() as Bib2d;
            if (bib2d == null)
            {
                throw new ArgumentException($"Cannot convert {roi?.GetType().Name} to Bib2d for kernel generation.");
            }
            exResult = OMUtil.GenKernelRoiOfBIB(bib2d, out BIBROI kernelRoi);
            LogRoiForAlgoTest($"***************************GenKernelRoiOfBIB :", bib2d);
            LogHelper.LogDebug($"RectangleDesc = {roi.ToJson()}");
            return kernelRoi;
        }

        private CrossROI GenKernelRoiOfCross(RectangleDesc roi, out ExResult exResult)
        {
            var rectRoi = roi as Cross2dRectangleDesc;
            Cross2d cross2D = new Cross2d();
            cross2D.Width = rectRoi.ColumnWidth;
            cross2D.Rect = new Rect2d();
            cross2D.Rect.X = roi.Left;
            cross2D.Rect.Y = roi.Top;
            cross2D.Rect.Width = roi.Width;
            cross2D.Rect.Height = roi.Height;
            cross2D.BandPercent = rectRoi.BandPercent;
            cross2D.IntervalLength_X = rectRoi.IntervalLength_X;
            cross2D.IntervalLength_Y = rectRoi.IntervalLength_Y;

            foreach (var item in rectRoi.RactPointList)
            {
                cross2D.RectAxis.Axis.Add(new Point2d(item.Item1, item.Item2));
            }
            var length = Convert.ToInt32(rectRoi.BandLength);//与算法确认过，直接取整即可
            exResult = OMUtil.GenKernelRoiOfCross(cross2D, length, out CrossROI kernelRoi);
            LogRoiForAlgoTest($"***************************GenKernelRoiOfCross :", cross2D);
            return kernelRoi;
        }

        /// <summary>
        /// 自动创建模板
        /// </summary>
        /// <param name="isInner"></param>
        protected virtual async Task AutoCreateModel(bool isInner, bool isShowTestResult)
        {
            var modelControl = isInner ? InnerTemplateControl : OuterTemplateControl;
            var positioningModel = GetPositioningModel(isInner);
            //如果启用了定位模板，但是未设置搜索区域的话，则自动添加搜索区域
            if (positioningModel.IsAvailable)
            {
                var shapeBase = TestTrainSetting.TrainParam.GetRoiDesc(isInner, true).CreateTemplateRoi();
                var roiRect = CreateRectByShapeBase(shapeBase);
                var searchRect = modelControl.TemplateContext.SelectedMatchModelGroup.MatchModel.SearchingRect;
                if (searchRect == null || searchRect.Width < roiRect.Width || searchRect.Height < roiRect.Height)
                {
                    modelControl.TemplateContext?.UpdateSearchArea(roiRect);
                }
            }
            LogHelper.LogDebug($"AutoCreateModel isInner = {isInner},searchRect = {modelControl.TemplateContext.SelectedMatchModelGroup.MatchModel.SearchingRect}, searchRect180 = {modelControl.TemplateContext.SelectedMatchModelGroup.MatchModel180.SearchingRect}");
            await modelControl.CreateModelAsync(isShowTestResult);
        }

        protected virtual PositioningTemplateInfo GetPositioningModel(bool isInner)
        {
            return PositioningModel;
        }

        protected virtual void ShowResult(OvResultBase ovResult, HObject imgObjectInner, HObject imgObjectOuter)
        {
            throw new NotImplementedException();
        }

        public ObservableCollection<KeyValuePair<string, List<CDChartCurve>>> Datas = default;
        public LightServiceForMeasure40 _lightServiceForMeasure40;

        [RelayCommand]
        void Correlation()
        {
            if (CorrelationCurvesList == null)
                return;

            CreateCorrelation();

            var dialogParameters = new DialogParameters
            {
                { "Datas", Datas }
            };

            _dialogService.ShowDialog(nameof(ChartResult_ovl), dialogParameters, _ => { });
        }
        /// <summary>
        /// 创建相关性曲线数据
        /// </summary>
        protected void CreateCorrelation()
        {

            Datas = new ObservableCollection<KeyValuePair<string, List<CDChartCurve>>>();

            // 校验曲线数量是否符合要求
            if (CorrelationCurvesList.Count % 4 != 0)
            {
                _messageBoxService.Error($"CorrelationCurvesList Count Error!, Count = {CorrelationCurvesList?.Count}", _messageBoxToken);
                return;
            }

            // 定义方向和层级常量
            const string RIGHT_TO_LEFT = "Right To Left";
            const string BOTTOM_TO_TOP = "Bottom To Top";
            const string INNER = "inner";
            const string OUTER = "outer";

            // 获取目标类型
            var trainSettings = TestInfo.GetTrainSettings();
            var innerType = trainSettings.InnerType;
            var outerType = trainSettings.OuterType;
            bool isInnerCross = innerType == EnumTargetType.Cross;
            bool isOuterCross = outerType == EnumTargetType.Cross;

            // 根据目标类型确定索引映射
            var curveMap = GetCurveIndexMap(isInnerCross, isOuterCross);

            // 构建相关性曲线数据结构
            var correlationCurves = new Dictionary<string, Dictionary<string, List<CorrelationCurve>>>
            {
                [INNER] = new Dictionary<string, List<CorrelationCurve>>
                {
                    [RIGHT_TO_LEFT] = GetCurvesFromIndices(curveMap.InnerRightToLeft),
                    [BOTTOM_TO_TOP] = GetCurvesFromIndices(curveMap.InnerBottomToTop)
                },
                [OUTER] = new Dictionary<string, List<CorrelationCurve>>
                {
                    [RIGHT_TO_LEFT] = GetCurvesFromIndices(curveMap.OuterRightToLeft),
                    [BOTTOM_TO_TOP] = GetCurvesFromIndices(curveMap.OuterBottomToTop)
                }
            };

            // 构建图表数据
            BuildChartData(correlationCurves);
        }

        /// <summary>
        /// 根据相关性曲线数据构建图表数据
        /// </summary>
        /// <param name="correlationCurves">相关性曲线数据结构</param>
        private void BuildChartData(Dictionary<string, Dictionary<string, List<CorrelationCurve>>> correlationCurves)
        {
            foreach (var (layerName, directionDict) in correlationCurves)
            {
                foreach (var (directionName, curveList) in directionDict)
                {
                    if (curveList.Count == 0)
                        continue;

                    // 创建图表曲线
                    var chartCurves = curveList.Select(curve => new CDChartCurve
                    {
                        Name = layerName,
                        Xs = curve.Xs,
                        Ys = curve.Ys,
                        IsSelected = false
                    }).ToList();

                    // 添加到结果集
                    Datas.Add(new KeyValuePair<string, List<CDChartCurve>>(directionName, chartCurves));
                }
            }
        }

        /// <summary>
        /// 根据索引获取曲线列表
        /// </summary>
        private List<CorrelationCurve> GetCurvesFromIndices(int[] indices)
        {
            return indices.Select(i => CorrelationCurvesList[i]).ToList();
        }

        /// <summary>
        /// 根据内外层目标类型获取曲线索引映射
        /// </summary>
        private (int[] InnerBottomToTop, int[] InnerRightToLeft, int[] OuterBottomToTop, int[] OuterRightToLeft)
            GetCurveIndexMap(bool isInnerCross, bool isOuterCross)
        {
            if (isInnerCross)
            {
                if (isOuterCross)
                {
                    // 内层Cross，外层Cross
                    return (
                        InnerBottomToTop: new[] { 0, 1 },
                        InnerRightToLeft: new[] { 2, 3 },
                        OuterBottomToTop: new[] { 4, 5 },
                        OuterRightToLeft: new[] { 6, 7 }
                    );
                }
                else
                {
                    // 内层Cross，外层非Cross
                    return (
                        InnerBottomToTop: new[] { 0, 1 },
                        InnerRightToLeft: new[] { 2, 3 },
                        OuterBottomToTop: new[] { 5 },
                        OuterRightToLeft: new[] { 4 }
                    );
                }
            }
            else
            {
                if (isOuterCross)
                {
                    // 内层非Cross，外层Cross
                    return (
                        InnerBottomToTop: new[] { 1 },
                        InnerRightToLeft: new[] { 0 },
                        OuterBottomToTop: new[] { 2, 3 },
                        OuterRightToLeft: new[] { 4, 5 }
                    );
                }
                else
                {
                    // 内层非Cross，外层非Cross
                    return (
                        InnerBottomToTop: new[] { 1 },
                        InnerRightToLeft: new[] { 0 },
                        OuterBottomToTop: new[] { 3 },
                        OuterRightToLeft: new[] { 2 }
                    );
                }
            }
        }

        protected virtual void LoadOvResult(OvResultBase result)
        {
            if (result != null && result.CenterInner != null)
            {
                MeasurementResultDataList = new ObservableCollection<MeasurementResultData>
                {
                    new MeasurementResultData
                    {
                        Axit = "X",
                        CenterInner = result.CenterInner.X,
                        QMeritInner = result.QMerit.InnerX,
                        CenterOuter = result.CenterOuter.X,
                        OVLMisreg = result.MisregX,
                        QMeritOuter = result.QMerit.OuterX,
                        QMeritTarget = result.QMerit.X
                    },
                    new MeasurementResultData
                    {
                        Axit = "Y",
                        CenterInner = result.CenterInner.Y,
                        QMeritInner = result.QMerit.InnerY,
                        CenterOuter = result.CenterOuter.Y,
                        OVLMisreg = result.MisregY,
                        QMeritOuter = result.QMerit.OuterY,
                        QMeritTarget = result.QMerit.Y
                    }
                };
            }
        }
        protected virtual List<CorrelationCurve> GetCorrelationCurve(HObject imageObjectInner, HObject imageObjectOuter, HTuple hTuple)
        {
            OMUtil.GetCorrelationCurve_Double(imageObjectInner, imageObjectOuter, hTuple, out List<CorrelationCurve> curves);
            return curves;
        }
        protected List<ROIDesc> GetProfileForImageView(HObject imageObject, OMROIBase omROI, OMTargetType oMTargetType)
        {
            List<ROIDesc> rOIDescs = new List<ROIDesc>();
            OMUtil.GetProfileForImageView(imageObject, omROI, oMTargetType, out List<ProjectionProfile> profileList);
            foreach (var item in profileList)
            {
                List<Point> pointListInner = new List<Point>();
                for (int i = 0; i < item.Xs.Count(); i++)
                {
                    pointListInner.Add(new Point(item.Xs[i], item.Ys[i]));
                }
                rOIDescs.Add(new PolygonDesc() { Vertices = pointListInner, IsClosed = false });
            }
            return rOIDescs;
        }

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
        protected void CreateMeasureRoi(bool isInner)
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
                    case EnumTargetType.LAngle:
                    case EnumTargetType.LCV:
                        rOIDescs = new LRectangleDesc()
                        {
                            Width = width,
                            Height = height,
                            Left = left,
                            Top = top,
                            BandWidthRatio = 60,
                            BandLengthRatio = 60,
                            BandWidth = 30,
                            Direction = TestInfo.CDOVLDirection,
                            IsSpin = !isInner
                        };

                        break;
                    default:
                        break;
                }

                if (TestTrainSetting.AutoRoiRange != null)
                {
                    var _autoRangeRoi = TestTrainSetting.AutoRoiRange.CreateRoiDesc();
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

        /// <summary>
        /// 根据类型创建不同的量测ROI
        /// </summary>
        /// <param name="enumROIType"></param>
        /// <returns></returns>
        private ROIDesc CreateMeasureROI(CD_Orientention cD_Orientention, bool Inner, EnumROIType enumROIType = EnumROIType.Rectangle)
        {

            double startAngle = 0;
            int pickerCount = 2;
            //跨视野只显示一个
            //if (IsAcrossTheCameraVision)
            pickerCount = 1;
            switch (cD_Orientention)
            {
                case CD_Orientention.Vertical:
                    startAngle = 90;
                    break;
                default:
                    startAngle = 180;
                    break;
            }
            //跟vision1方向相对
            if (!Inner)
            {
                //var angle = (TestInfo.TrainSetting_CD.MultiRoiList[0].MeasureROI as RotatedPickersDesc).StartAngle;
                startAngle = startAngle + 180;
            }
            return CreateDefaultDescRois(pickerCount, startAngle, cD_Orientention).GetEnumerableValue(0);

        }
        private IEnumerable<ROIDesc> CreateDefaultDescRois(int pickerCount, double startAngle, CD_Orientention cD_Orientention = 0)
        {
            var imageData = HpImageViewer.Image as ImageData;

            return ROIRenderExtensions.CreateDescROI(RoiDefaultParam_CD_Factory.Build(cD_Orientention, pickerCount, startAngle),
                                          imageData.Width / 2,
                                          imageData.Height / 2,
                                          imageData.Width / 5,
                                          imageData.Height / 5) as List<ROIDesc>;
        }
        protected void Create4MeasureRoi()
        {
            if (HpImageViewer.Image is not ImageData imageData)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return;
            }

            void JudgeAutoGenResult(ExResult autoGenResult)
            {
                if (autoGenResult != null && !autoGenResult.IsOK)
                {
                    LogHelper.LogError($"{autoGenResult.ErrorMsg}");
                    _messageBoxService.ErrorDialog("Failed to automatically generate the ROI. Please drawing a rectangle close to the vivinity of the outer pattern");
                }
            }

            ROIDesc rOIDescs = null;
            var outerType = TestTrainSetting.OuterType;
            var searchRoi = GetRangeRoi(imageData);

            try
            {
                switch (outerType)
                {
                    case EnumTargetType.Circle:
                    case EnumTargetType.Box:
                    case EnumTargetType.Bar:
                    case EnumTargetType.Frame:
                        if (outerType != EnumTargetType.Circle)
                        {
                            var omType = outerType switch
                            {
                                EnumTargetType.Frame => OMTargetType.Frame,
                                EnumTargetType.Box => OMTargetType.Box,
                                _ => OMTargetType.Bar
                            };
                            var autoGenResult = OMUtil.AutoGenMeasureRoiOfBIB(
                                imageData.ToHObject(), searchRoi, omType,
                                OMTargetLayer.InnerAndOuter, out _, out Bib2d outerRoi);
                            JudgeAutoGenResult(autoGenResult);
                            rOIDescs = Bib2dToCross4RectangleDesc(outerRoi);
                        }
                        break;
                    case EnumTargetType.Cross:
                    {
                        var autoGenResult = OMUtil.AutoGenMeasureRoiOfCross(
                            imageData.ToHObject(), searchRoi, OMTargetType.Cross,
                            out _, out Cross2d outerCross);
                        JudgeAutoGenResult(autoGenResult);
                        rOIDescs = Cross2dToCross4RectangleDesc(outerCross);
                        break;
                    }
                }

                if (rOIDescs == null)
                {
                    rOIDescs = CreateDefaultCross4RectangleDesc(imageData);
                }

                UpdateMeasureRoisOnViewer(rOIDescs);
            }
            catch (Exception ex)
            {
                _messageBoxService.ErrorDialog(ex.Message);
                LogHelper.LogError(ex, "Auto create roi errored!");
            }
        }

        static Cross4RectangleDesc Bib2dToCross4RectangleDesc(Bib2d bib2d)
        {
            var axis = bib2d?.Box?.Axis;
            if (axis == null || axis.Width == 0 || axis.Height == 0)
            {
                return null;
            }

            return new Cross4RectangleDesc
            {
                Top = axis.Y,
                Left = axis.X,
                Width = axis.Width,
                Height = axis.Height,
                BandWidth = bib2d.Box.Width,
                BandPercent = bib2d.BandPercent,
                Color = "#FFFF00"
            };
        }

        static Cross4RectangleDesc Cross2dToCross4RectangleDesc(Cross2d cross2d)
        {
            if (cross2d?.Rect == null || cross2d.Rect.Width == 0 || cross2d.Rect.Height == 0)
            {
                return null;
            }

            return new Cross4RectangleDesc
            {
                Top = cross2d.Rect.Y,
                Left = cross2d.Rect.X,
                Width = cross2d.Rect.Width,
                Height = cross2d.Rect.Height,
                BandWidth = cross2d.Width > 0 ? cross2d.Width : cross2d.Rect.Width * 0.1,
                BandPercent = cross2d.BandPercent > 0 ? cross2d.BandPercent : 60,
                Color = "#FFFF00"
            };
        }

        static Cross4RectangleDesc CreateDefaultCross4RectangleDesc(ImageData imageData)
        {
            double width = imageData.Width / 2;
            double height = imageData.Height / 2;
            return new Cross4RectangleDesc
            {
                Top = (imageData.Height - height) / 2,
                Left = (imageData.Width - width) / 2,
                Width = width,
                Height = height,
                BandWidth = width * 0.3,
                Color = "#FFFF00",
                BandPercent = 60
            };
        }

        void UpdateMeasureRoisOnViewer(ROIDesc measureRoi)
        {
            if (measureRoi == null)
            {
                return;
            }

            if (TestTrainSetting.AutoRoiRange != null)
            {
                var autoRangeRoi = TestTrainSetting.AutoRoiRange.CreateRoiDesc();
                autoRangeRoi.Color = "#FFFF00";
                HpImageViewer.UpdateROIs(measureRoi, autoRangeRoi);
            }
            else
            {
                HpImageViewer.UpdateROIs(measureRoi);
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
            if (roi == null)
            {
                roi = new Rect2d(0, 0, imageData.Width, imageData.Height);
            }

            return roi;
        }

        void SetMeasureRoi(bool isInner)
        {
            SetMeasureRoiMethod(isInner);
            MeasurementROI_SetEnabled = false;
            MeasurementROI_InnerEnabled = isInner;
            MeasurementROI_OuterEnabled = !isInner;
        }

        private void SetMeasureRoiMethod(bool isInner)
        {
            var roi = TestTrainSetting.TrainParam.GetRoiDesc(isInner, true);
            ImageData imageData = LoadFocusImageData(isInner);
            if (imageData == null)
            {
                return;
            }
            // CD OVL 方向被用户改过、或外层 Cross4 相关状态变化时，不使用已存 ROI 克隆，走重新生成默认量测 ROI。
            if (_isDirectionChanged || !isInner && UseCross4rectan && TrainParam.OuterLayer.UseNewCross == false)
            {
                roi = null;
            }
            _visionService.StopGrabbing();
            HpImageViewer.Image = imageData;
               
            if (roi != null)
            {
                HpImageViewer.UpdateROIs(roi);
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

            if (!isInner)
            {
                // 外层量测 ROI 流程结束后再清除方向变更标记，与原先仅在外层分支清理行为一致。
                _isDirectionChanged = false;
            }
            MeasurementROI_SetEnabled = false;
            MeasurementROI_InnerEnabled = isInner;
            MeasurementROI_OuterEnabled = !isInner;
        }

        /// <summary>
        ///设置或显示内层量测Roi
        /// </summary>
        [RelayCommand]
        void InnerMeasureRoi_Set()
        {
            SetMeasureRoi(true);
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
        /// 创建bib内框 gray Size调整框
        /// </summary>
        [RelayCommand]
        private async Task InnerMeasureRoi_Confirm()
        {
            await ConfirmMeasureRoi(true);
        }

        [RelayCommand]
        private async Task OuterMeasureRoi_Confirm()
        {
            await ConfirmMeasureRoi(false);
        }

        private async Task ConfirmMeasureRoi(bool isInner)
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
            if (isInner)
            {
                TestTrainSetting.TrainParam.InnerLayer.MeasureROI = rOIDescs as RectangleDesc;
                // 量测 ROI 已变更，原 CD OVL 边点不再对应新几何
                InneredgeConfirmRois = null;
            }
            else
            {
                rOIDescs.Color = OuterColor;
                // 量测 ROI 已变更，原 CD OVL 边点不再对应新几何
                OuteredgeConfirmRois = null;
                TestTrainSetting.TrainParam.OuterLayer.MeasureROI = rOIDescs as RectangleDesc;
                TrainParam.OuterLayer.UseNewCross = UseCross4rectan;
            }
            var roiCenter = rOIDescs.CreateRoi().GetCenter();
            var imagedata = HpImageViewer.Image as ImageData;
            var cam = GetECamera(isInner);
            if (ShouldUpdateTargetPointAfterConfirmMeasureRoi(isInner))
            {
                //创建量测模板后，记录模板中心所在的机台坐标，当定位模板与量测模板不一致时，用该值来计算量测模板与定位模板的偏移量
                var targetPoint = _alignmentService40.ConvertImageToAxisBySpecCam(_alignmentService.GetCurrentAxisPosition(), new MatchingItem() { Column = roiCenter.X, Row = roiCenter.Y }, imagedata.Width, imagedata.Height, cam);
                UpdateTargetPointAfterConfirmMeasureRoi(isInner, targetPoint);
                LogHelper.LogDebug($"ROI Center = {roiCenter} ");
            }


            IsUpdateMeasureRoi = true;
            if (isInner)
            {
                IsInnerCorrectionEnabled = true;
            }
            else
            {
                IsOuterCorrectionEnabled = true;
            }
            ChangeCameraInfo = "Measure Focusing...";
            try
            {
                SaveVisionParams();
                InitMeasurementRoiButton();
                await FindMeasureRoiFocusLayer(isInner);
                // 最后读取量测图显示到控件上
                HpImageViewer.Image = LoadFocusImageData(isInner);
            }
            catch (Exception ex)
            {
                _messageBoxService.ErrorDialog($"{ex.Message}");
            }
            finally
            {
                ChangeCameraInfo = "";
            }
        }

        protected virtual bool ShouldUpdateTargetPointAfterConfirmMeasureRoi(bool isInner)
        {
            return (_focusBasedOn == EnumFocusBasedOn.Inner && isInner)
                || (_focusBasedOn == EnumFocusBasedOn.Outer && !isInner);
        }

        protected virtual void UpdateTargetPointAfterConfirmMeasureRoi(bool isInner, Point2d targetPoint)
        {
            TargetPoint = targetPoint;
        }

        protected abstract Task FindMeasureRoiFocusLayer(bool isInner);

        [RelayCommand]
        void MeasureRoi_Cancel()
        {
            InitMeasurementRoiButton();
        }



        protected async Task CreatePreviewTemplate(bool isInner, string name, ImageData image = null)
        {

            image ??= LoadImageData(name);
            //点击Confirm按钮时，重绘量测Roi，避免被其它roi影响
            var roiDesc = isInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
            if (roiDesc == null)
            {
                _messageBoxService.InfoDialog("Please draw the corresponding ROI！");
                return;
            }
            //此处需要保存全图的清晰度
            var paramFocus = GetCoarseParamFocus(IServiceCamera.CurCamera);
            var sharpness = _focusService.GetImageClarity(image, paramFocus: paramFocus);
            TestTrainSetting.FocusModuleInfo.Sharpness = sharpness;
            var roiSharpness = _focusService.GetImageClarity(image, roiDesc.CreateRoi(), paramFocus);
            TestTrainSetting.FocusModuleInfo.RoiSharpness = roiSharpness;
            var measureTemplate = isInner ? TestTrainSetting.OverlayMark_InnerModels[0] : TestTrainSetting.OverlayMark_OuterModels[0];
            _visionService.StopGrabbing();
            //训练预匹配的模板
            SingleTemplateMatchingContext previewTemplate = CloneHelper.ReflectionClone<SingleTemplateMatchingContext>(measureTemplate);
            await _testTrainService.CreateRoiTemplate(previewTemplate, image, roiDesc, name);
            LogHelper.LogDebug($"CreatePreviewTemplate isInner = {isInner},searchRect = {measureTemplate.SelectedMatchModelGroup.MatchModel.SearchingRect}, searchRect180 = {measureTemplate.SelectedMatchModelGroup.MatchModel180.SearchingRect}");

            SetPreviewTemplate(isInner, previewTemplate);
            //此处先创建模板，后加载图片到图像控件，避免图片被占用
            HpImageViewer.Image = image;
            HpImageViewer.UpdateROIs(roiDesc);
            //previewTemplate = (SingleTemplateMatchingContext)CloneHelper.ReflectionClone(previewTemplate);
            //previewTemplate.MainTemplateName = "";
            //previewTemplate.Name = name;
            //TrainModelViewModel trainModelVM = new TrainModelViewModel(_serviceProvider, previewTemplate, false, () => HpImageViewer, false);

            //await _templateMatchingService.CreateTemplateModelAsync(previewTemplate, image, roiDesc.CreateTemplateRoi(), previewTemplate.SelectedMatchModelGroup.MatchModel.ParamMatch.MinScore, false);
            //SetPreviewTemplate(isInner, previewTemplate);
            //TestTrainSetting.PreviewTemplate = previewTemplate;
        }

        /// <summary>
        /// 设置PreviewTemplate时需要从外部传入isInner
        /// 因为在双面模式时此时在RunMeasureMethod界面，_isInner变量无法正常使用
        /// </summary>
        /// <param name="isInner"></param>
        /// <param name="template"></param>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual void SetPreviewTemplate(bool isInner, SingleTemplateMatchingContext template)
        {
            throw new NotImplementedException();
        }

        //protected HTuple GetTransmat(RectangleDesc roi)
        //{
        //    var x = roi.CenterX;
        //    var y = roi.CenterY;
        //    return _measService.GetTransMat(y, x, new MatchingItem() { Column = x, Row = y }, 0);
        //}

        protected HTuple GetTransmat(MatchingItem modelMatchItem, MatchingItem resultMatchItem)
        {
            var x = modelMatchItem.Column;
            var y = modelMatchItem.Row;
            return _measService.GetTransMat(y, x, resultMatchItem, 0);
        }

        private SingleTemplateMatchingContext GetTemplate(bool isInner)
        {
            var modelControl = isInner ? InnerTemplateControl : OuterTemplateControl;
            return modelControl?.TemplateContext;
        }

        private void SetSearchingRect(Rect2d rect)
        {
            //设置或者清除搜索区域时，需要同时处理0°和180°方向
            var inner = GetTemplate(true);
            inner?.UpdateSearchArea(rect);

            var outer = GetTemplate(false);
            outer?.UpdateSearchArea(rect);
        }

        private Rect2d GetSearchingRect()
        {
            var inner = GetTemplate(true);
            return inner?.SelectedMatchModelGroup?.MatchModel.SearchingRect;
        }

        /// <summary>
        /// 设置或显示 SearchRoi
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
        /// 确认 SearchRoi
        /// </summary>
        [RelayCommand]
        void SearchRoi_Confirm()
        {
            var rOIDescs = HpImageViewer.ImageViewerDesc.ROIDescs?.FirstOrDefault();
            if (rOIDescs == null || rOIDescs is not RectangleDesc desc)
            {
                _messageBoxService.InfoDialog("Please draw the Rect ROI！");
                return;
            }
            SetSearchingRect(desc.CreateRoi() as Rect2d);
            InitMeasurementRoiButton();
        }

        /// <summary>
        /// 取消设置 SearchRoi
        /// </summary>
        [RelayCommand]
        void SearchRoi_Cancel()
        {
            InitMeasurementRoiButton();
        }

        /// <summary>
        /// 清除 SearchRoi
        /// </summary>
        [RelayCommand]
        void SearchRoi_Clear()
        {
            SetSearchingRect(null);
            HpImageViewer.UpdateROIs();
        }

        public void InitMeasurementRoiButton(bool isClearRoi = true)
        {
            MeasurementROI_SetEnabled = true;
            MeasurementROI_InnerEnabled = false;
            MeasurementROI_OuterEnabled = false;
            SearchROI_SetEnabled = true;
            if (isClearRoi)
                HpImageViewer?.UpdateROIs();
        }

        /// <summary>
        /// 创建定位模板的通用方法（整合单面和双面版本）
        /// </summary>
        /// <param name="isInner">是否为内层相机</param>
        /// <param name="isDoubleFace">是否为双面模式</param>
        /// <returns>操作是否成功</returns>
        protected bool BeforeCreatePositionModel_Common(bool isInner, Func<(AxisType axisType, ECamera cam, PositioningTemplateInfo positioningTemplateInfo)> getTemplateParam)
        {
            if (TargetPoint == null)
            {
                _messageBoxService.ErrorDialog("Please set/reset the measurement ROI first!");
                return false;
            }

            if (HpImageViewer.Image is not ImageData imageData || imageData.IsEmpty || imageData.Width <= 0 || imageData.Height <= 0)
            {
                throw new ErrorCodeException((int)EnumTemplateError.ImageError);
            }

            var roiDesc = HpImageViewer.ImageViewerDesc?.ROIDescs?.FirstOrDefault();
            if (roiDesc == null)
            {
                throw new ErrorCodeException((int)EnumTemplateError.RoiError);
            }

            ChangeCameraInfo = "Focusing ...";
            try
            {
                // 选择相应的层级参数
                var layerFocusROI = isInner ? TrainParam.InnerLayer.FocusROI : TrainParam.OuterLayer.FocusROI;
                if (layerFocusROI == null)
                {
                    _messageBoxService.ErrorDialog($"Please draw the {(isInner ? "inner" : "outer")} corresponding ROI!");
                    return false;
                }

                // 更新ROI显示
                HpImageViewer.UpdateROIs();
                var para = getTemplateParam.Invoke();
                // 选择相应的相机、坐标轴和定位模型
                ECamera camera = para.cam;
                AxisType axis = para.axisType;
                PositioningTemplateInfo positioningModel = para.positioningTemplateInfo;
                _visionService.StartGrabbing();

                // 创建聚焦信息
                var focusInfo = CreateFullTrainInfo(camera);
                focusInfo.StartPosition = _servPlat.GetCurrentZ(axis);
                LogHelper.LogInfo($"Focus Info_Process{(isInner ? "Inner" : "Outer")}Focus : CoarseFocus ={focusInfo}");

                // 执行聚焦
                var focusResult = ExecuteFocusCommon(camera, roiDesc, focusInfo, PositioningColor);
                var coarseFocusInfo = GetTestFocusOrDefault().CourseFocusInfo;
                var focusImages = focusResult.pegRet.PegImages;
                var trainArg = CreateTrainArg(_grabMode);

                double waferThickness = default;
                if (camera == ECamera.Meas)
                {
                    waferThickness = _alignmentService40.WaferMapInfo.Thickness;
                }

                // ROI聚焦训练
                var ret = _focusService.TrainCurve(coarseFocusInfo, focusResult.ROI, focusImages, trainArg);
                positioningModel.RoiCurve = ret.Curve;
                positioningModel.RoiFocusOffset = ret.ZFit - _alignmentService40.GetMacroFocusZ(camera).z - waferThickness;

                // 全视野聚焦训练
                var ret2 = _focusService.TrainCurve(coarseFocusInfo, null, focusImages, trainArg);
                positioningModel.GlobalCurve = ret2.Curve;
                positioningModel.GlobalFocusOffset = ret2.ZFit - _alignmentService40.GetMacroFocusZ(camera).z - waferThickness;

                LogHelper.LogInfo($"Camera: {camera}, " +
                                  $"GlobalFocusOffset: {positioningModel.GlobalFocusOffset}, " +
                                  $"Global ZFit: {ret2.ZFit}, " +
                                  $"RoiFocusOffset: {positioningModel.RoiFocusOffset}, " +
                                  $"Roi ZFit: {ret.ZFit}");

                // 移动到定位模板的聚焦清晰面
                _servPlat.MoveZ(axis, ret.ZFit);

                // 等待Z轴到位后拍照
                HpImageViewer.Image = _visionService.GrabOneImageAsync().GetAwaiter().GetResult();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.ErrorDialog(ex.Message);
                return false;
            }
            finally
            {
                ChangeCameraInfo = "";
            }
        }

        #endregion MeasureRoi


        protected SingleTemplateMatchingContext GetModel(bool isInner)
        {
            var model = isInner ? TestTrainSetting.OverlayMark_InnerModels[0] : TestTrainSetting.OverlayMark_OuterModels[0];
            if (model.SelectedMatchModelGroup.MatchModel.ModelInfo == null) throw new Exception("Please create the positioning & measure template firstly!");
            return model;
        }

        [RelayCommand]
        void CloseView()
        {
            UpdateTestInfoData(false);
            var result = new DialogResult(ButtonResult.OK, new DialogParameters() { { "modifiedTestInfo", TestInfo }, { "isClose", true } });
            RaiseRequestClose(result);
            NotifySaveAction = null;
        }

        [RelayCommand]
        void Submit()
        {
            Submit(true);
            var result = new DialogResult(ButtonResult.OK, new DialogParameters() { { "modifiedTestInfo", TestInfo } });
            RaiseRequestClose(result);
            NotifySaveAction = null;
        }

        [RelayCommand]
        void Save()
        {
            Submit(true);
        }

        [ObservableProperty]
        private ChartResultViewModel_Ovl _curveResultViewModel;
        protected void ShowCurveControl()
        {
            if (CurveResultViewModel == null)
            {
                if (CorrelationCurvesList == null || CorrelationCurvesList.Count == 0)
                    return;

                CreateCorrelation();

                CurveResultViewModel = new ChartResultViewModel_Ovl();
                CurveResultViewModel.SetViewModelDatas(Datas);
            }
        }

        protected override void FocusSettingMethod()
        {
            Action<FocusInfo> action = new Action<FocusInfo>(SaveActionMethod);
            var parameters = new DialogParameters
            {
                { "FocusInfo", FocusInfo},
                { "SaveAction", action }
            };

            AppHelper.DispatcherInvoke(() =>
            {
                _dialogService.ShowDialog("Test_FocusTrainSettingDialog_OM40", parameters, null);
            });
        }

        protected void SaveAttenuationCoef(ECamera camera, CameraParamBase cameraParam)
        {
            var camAttr = _serviceProvider.GetService<IServiceCamera>().GetCameraAttr(camera);
            var age = _serviceProvider.GetService<IServiceCalData>().GetLightAge(camera, camAttr);
            if(cameraParam is MeasureCameraParam measureCameraParam)
            {
                TestInfo.IsLightPmExecuted = true;
                measureCameraParam.AttenuationCoef = age;
                LogHelper.LogDebug($"Set Age:{age}, Camera: {camera}");
            }
        }

        #region 新增重新训练MeasureKernel 流程

        /// <summary>
        /// 异步重建 CD OVL 的量测 Kernel。
        /// 可等待的调用方直接 await；同步属性变更链路请走 <see cref="QueueCreateMeasureKernelAsync"/>。
        /// </summary>
        protected async Task CreatMeasureKernelAsync()
        {
            if (!SupportsCdOvlEdgeCompensation())
            {
                return;
            }
            if (_grabMode != EnumGrabMode.DoubleGrab || FocusBasedOn == EnumFocusBasedOn.Inner)
            {
                ImageData img_outer = LoadFocusImageData(false);
                await CreateMeasureKernelSizeAndTemplate(false, true, img_outer);
                ImageData img_inner = LoadFocusImageData(true);
                await CreateMeasureKernelSizeAndTemplate(true, true, img_inner);
            }
            else
            {
                ImageData img_inner = LoadFocusImageData(true);
                ImageData img_outer = LoadFocusImageData(false);
                await CreateMeasureKernelSizeAndTemplate(true, true, img_inner);
                await CreateMeasureKernelSizeAndTemplate(false, true, img_outer);
            }
           
            //SaveCorrectHeight(true);
            //SaveCorrectHeight(false);
            //AutoCorrectR3();
        }

        /// <summary>
        /// 给无法直接 await 的调用链路派发异步重建任务，避免继续使用 async void。
        /// </summary>
        protected async Task QueueCreateMeasureKernelAsync()
        {
            await CreatMeasureKernelAsync();
            TrainSetting.IsTrained = false;
        }

        #endregion
    }
}
