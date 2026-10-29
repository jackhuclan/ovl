using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.StageControlModule.EventAggregators;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule.Views;
using Marvel.DataModels;
using Marvel.DataModels.Core;
using Marvel.DataModels.DeviceParameter;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.DataModels.Recipe40.TestInfo;
using Marvel.DataModels40.DeviceParameter;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using OVL.Algorithm.DoNet;
using Prism.Services.Dialogs;
using Wafer.Algorithm.Common.DoNet;
using WinCopies.Collections;

namespace Ovl.UI.Recipe.ViewModel;

/// <summary>
/// LAngle OVL 跨视野（CrossFov）训练 ViewModel。
/// <para>
/// 界面操作推荐顺序（与右侧 Train 面板自上而下一致）：
/// </para>
/// <list type="number">
///   <item><description>选择 ①Vision_1：训练 Inner（聚焦 ROI → Focus Run → 量测 ROI → Inner 定位模板）</description></item>
///   <item><description>选择 ②Vision_2：训练 Outer（同上，均为 Outer 侧控件）</description></item>
///   <item><description>切换 Vision 时自动记录/恢复各视野机台位（WCS + OM OverFov 参数）</description></item>
///   <item><description>选择 Measurement Run：光学模组、Run 量测、结果与算法参数</description></item>
/// </list>
/// Vision1 固定对应 Inner，Vision2 固定对应 Outer；Measurement 页同时展示内外层训练结果与 Run 入口。
/// </summary>
internal partial class Test_Train40ViewModel_CrossFov : Test_Train40ViewModel_DoubleFace
{
    #region 1. 构造与对话框生命周期

    private readonly IServiceCamera _serviceCamera;

    public Test_Train40ViewModel_CrossFov(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _serviceCamera = serviceProvider.GetRequiredService<IServiceCamera>();
    }

    private EnumVision GetVisionByLayer(bool isInner) =>
        isInner ? EnumVision.Vision1 : EnumVision.Vision2;

    private bool IsVisionInner(EnumVision vision) =>
        vision == EnumVision.Vision1;

    private readonly record struct CrossFovVisionContext(
        EnumVision Vision,
        bool IsInner,
        ECamera Camera,
        AxisType Axis,
        CameraParamBase CameraParam,
        LightSourceParamBase LightSourceParam);

    private bool IsDualCameraCrossFov =>
        TestInfo?.CrossFov == true && TestInfo.IsDoubleFace;

    private ECamera GetSingleCrossFovCamera() =>
        CrossFovCameraSettings?.MeasCamera ?? ECamera.Meas;

    private AxisType GetAxisByCamera(ECamera camera) =>
        camera == ECamera.Meas ? _machine40Config.PatternCameraCfg.MeasCamAxis : _machine40Config.PatternCameraCfg.MeasCamExtAxis;

    private ECamera GetMeasurementCamera() =>
        IsDualCameraCrossFov ? ECamera.Meas : GetSingleCrossFovCamera();

    private CrossFovVisionContext CreateVisionContext(EnumVision vision)
    {
        var isInner = IsVisionInner(vision);
        var camera = IsDualCameraCrossFov
            ? (isInner ? InnerCamera : OuterCamera)
            : GetSingleCrossFovCamera();

        return new CrossFovVisionContext(
            vision,
            isInner,
            camera,
            GetAxisByCamera(camera),
            GetVisionCameraParam(vision),
            GetVisionLightSourceParam(vision));
    }

    private CrossFovVisionContext CreateVisionContext(bool isInner) =>
        CreateVisionContext(GetVisionByLayer(isInner));

    private CrossFovVisionContext CreateVisionContextByCamera(ECamera camera)
    {
        if (!IsDualCameraCrossFov)
            return CreateVisionContext(CrossFovVision);

        return CreateVisionContext(camera == OuterCamera ? EnumVision.Vision2 : EnumVision.Vision1);
    }

    private CameraParamBase GetVisionCameraParam(EnumVision vision) =>
        IsVisionInner(vision) ? CrossFovTrainSettings?.CameraParamsInner : CrossFovTrainSettings?.CameraParamsOuter;

    private LightSourceParamBase GetVisionLightSourceParam(EnumVision vision) =>
        IsVisionInner(vision) ? CrossFovTrainSettings?.LightSourceParamInner : CrossFovTrainSettings?.LightSourceParamOuter;

    private void SetVisionCameraParam(EnumVision vision, CameraParamBase cameraParam)
    {
        if (CrossFovTrainSettings == null)
            return;

        if (IsVisionInner(vision))
            CrossFovTrainSettings.CameraParamsInner = cameraParam;
        else
            CrossFovTrainSettings.CameraParamsOuter = cameraParam;
    }

    private void SetVisionLightSourceParam(EnumVision vision, LightSourceParamBase lightSourceParam)
    {
        if (CrossFovTrainSettings == null)
            return;

        if (IsVisionInner(vision))
            CrossFovTrainSettings.LightSourceParamInner = lightSourceParam;
        else
            CrossFovTrainSettings.LightSourceParamOuter = lightSourceParam;
    }

    private void SwitchToVisionContext(EnumVision vision, bool refreshOpticalModule = false)
    {
        if (TestInfo?.CrossFov != true)
            return;

        var context = CreateVisionContext(vision);
        alignmentService.ChangeCamera(context.Camera, false);
        SetVisionParams(context);
        if (refreshOpticalModule)
            LoadSelectedOpticalModule(vision);
    }

    /// <summary>
    /// 对话框打开（后台线程）：升级 CrossFov 配方类型，清空内外示教点，再走基类初始化。
    /// </summary>
    public override void OpenDialogThread(IDialogParameters parameters)
    {
        TestInfo?.EnsureCrossFovTrainSettings();
        _targetPoint_Inner = null;
        _targetPoint_Outer = null;
        base.OpenDialogThread(parameters);
        InitializeOpticalModuleSelector();
    }

    /// <summary>
    /// 视图 Loaded（XAML LoadCommand）：初始化 CrossFov UI、切换 Vision1 光学上下文（单相机不移动 XY）、绑定内外定位模板控件。
    /// </summary>
    [RelayCommand]
    public async Task Load()
    {
        TestInfo?.EnsureCrossFovTrainSettings();
        await base.Loaded();
        InitializeCrossFovVisionParams();
        if (TestInfo?.CrossFov != true)
        {
            IsAcrossTheCameraVision = false;
            CrossFovUiInnerTrainVisible = true;
            CrossFovUiOuterTrainVisible = true;
            InnerModelSectionVisible = true;
            OuterModelSectionVisible = true;
            return;
        }

        IsAcrossTheCameraVision = true;
        IsShowMeasurement = false;
        CrossFovVision = EnumVision.Vision1;
        RefreshCrossFovTrainUiVisibility();
        SwitchToVisionContext(CrossFovVision);
        InitMeasurementRoiButton();
        _visionService.StartGrabbing();
        BindCrossFovPositioningTemplates();
        LoadSelectedOpticalModule();
        //ApplyCrossFovStageLightDefaults();
        _eventAggregator?.GetEvent<TrainDialogStateEvent>().Publish(true);
    }

    public override void OnDialogClosed()
    {
        _eventAggregator?.GetEvent<TrainDialogStateEvent>().Publish(false);
        base.OnDialogClosed();
    }

    #endregion

    #region 2. 界面绑定属性（Vision 切换与面板显隐）

    /// <summary>Measurement Run 页为 true：显示光学模组/Run/结果；Vision 页为 false，仅显示当前视野训练区。</summary>
    [ObservableProperty]
    private bool _isShowMeasurement;

    /// <summary>是否显示顶部 ①Vision_1 / ②Vision_2 / Measurement Run 切换条。</summary>
    [ObservableProperty]
    private bool _isAcrossTheCameraVision;

    /// <summary>当前选中的跨视野页签（Vision1=Inner 侧，Vision2=Outer 侧）。</summary>
    [ObservableProperty]
    private EnumVision _crossFovVision = EnumVision.Vision1;

    /// <summary>Vision1 或 Measurement 页显示 Inner 训练区（Focus/量测 ROI/定位）。</summary>
    [ObservableProperty]
    private bool _crossFovUiInnerTrainVisible = true;

    /// <summary>Vision2 或 Measurement 页显示 Outer 训练区。</summary>
    [ObservableProperty]
    private bool _crossFovUiOuterTrainVisible = true;

    /// <summary>Vision 页 Inner 量测模板区显隐；Measurement 页始终显示。</summary>
    [ObservableProperty]
    private bool _innerModelSectionVisible = true;

    /// <summary>Vision 页 Outer 量测模板区显隐；Measurement 页始终显示。</summary>
    [ObservableProperty]
    private bool _outerModelSectionVisible = true;

    /// <summary>Vision1 页显示 Inner Positioning Model；Measurement 页隐藏。</summary>
    [ObservableProperty]
    private bool _innerPositioningSectionVisible = true;

    /// <summary>Vision2 页显示 Outer Positioning Model；Measurement 页隐藏。</summary>
    [ObservableProperty]
    private bool _outerPositioningSectionVisible;

    [ObservableProperty]
    private int _selectedOpticalModuleIndex = 0; // 0=Up(Meas), 1=Down(MeasExt)

    [ObservableProperty]
    private string _upCameraMode = "N/A";

    [ObservableProperty]
    private string _downCameraMode = "N/A";

    [ObservableProperty]
    private string _upIntensity = "N/A";

    [ObservableProperty]
    private string _downIntensity = "N/A";

    /// <summary>Measurement 页切换 Up/Down 光学模组。</summary>
    [RelayCommand]
    private void SelectedOpticalClick()
    {
        UpdateOpticalModuleConfiguration(SelectedOpticalModuleIndex, false);
        UpdateOpticalModuleDisplayValues();
    }

    /// <summary>对话框打开时默认 Up 光学模组（Stage Light Positions=Up）。</summary>
    private void InitializeOpticalModuleSelector()
    {
        SelectedOpticalModuleIndex = 0;
    }

    /// <summary>
    /// 进入 CrossFov Train 时，把 StageControl 的光源模块和快门状态同步到当前选中的光学模块。
    /// </summary>
    private void ApplyCrossFovStageLightDefaults()
    {
        if (TestInfo?.CrossFov != true)
            return;

        try
        {
            var visionManager = _serviceProvider.GetRequiredService<IVisionServiceManager>();
            var lightService = _serviceProvider.GetService<ILightServiceForMeasure>();
            var lightModuleId = SelectedOpticalModuleIndex == 0 ? ECamera.Meas : ECamera.MeasExt;
            var relativeLightModuleId = lightModuleId == ECamera.Meas ? ECamera.MeasExt : ECamera.Meas;

            visionManager.SetLightModuleId(CurCamera, lightModuleId);
            _machineDomainService.CameraLightPairConfig.CurCameraLightPairs[CurCamera] = lightModuleId;

            // ShutterOn=true 表示 UI 勾选；硬件 GetOn 与 ShutterOn 相反
            lightService?.SetOn(false, lightModuleId);
            lightService?.SetOn(true, relativeLightModuleId);

            _visionService.NotifyParamsChanged();
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"ApplyCrossFovStageLightDefaults error: {ex.Message}");
        }
    }

    /// <summary>Loaded 后从配方恢复光学模组，无记录时默认 Up。</summary>
    private void LoadSelectedOpticalModule(EnumVision? vision = null)
    {
        var targetVision = vision ?? CrossFovVision;
        if (GetVisionLightSourceParam(targetVision) is MeasureLightSourceParam40 measureLight40)
            SelectedOpticalModuleIndex = measureLight40.Camera == ECamera.Meas ? 0 : 1;
        else
            SelectedOpticalModuleIndex = 0;

        UpdateOpticalModuleConfiguration(SelectedOpticalModuleIndex, true, targetVision);
        UpdateOpticalModuleDisplayValues();
    }

    private void UpdateOpticalModuleConfiguration(int index, bool isForLoad, EnumVision? vision = null)
    {
        try
        {
            var targetVision = vision ?? CrossFovVision;
            bool isUpLight = index == 0;
            ECamera lightModuleId = isUpLight ? ECamera.Meas : ECamera.MeasExt;

            var visionManager = _serviceProvider.GetRequiredService<IVisionServiceManager>();
            visionManager.SetLightModuleId(CurCamera, lightModuleId);

            var lightSourceParam = GetVisionLightSourceParam(targetVision);
            if (isForLoad && lightSourceParam != null)
                _cameraLightParamService40.SetLightSourceParamMeasureSingleFace(lightSourceParam);

            _visionService.NotifyParamsChanged();
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"UpdateOpticalModuleConfiguration error: {ex.Message}");
            _messageBoxService.Error($"光学模块切换失败: {ex.Message}");
        }
    }

    private void UpdateOpticalModuleDisplayValues()
    {
        try
        {
            var upCameraAttr = _serviceCamera.GetCameraAttr(ECamera.Meas);
            UpCameraMode = upCameraAttr.SpecBand.ToString();

            var upLightParam = _cameraLightParamService40.GetLightSourceParam(ECamera.Meas);
            if (upLightParam is MeasureLightSourceParam40 upParam)
                UpIntensity = upParam.Intensity.ToString("F0");

            var downCameraAttr = _serviceCamera.GetCameraAttr(ECamera.MeasExt);
            DownCameraMode = downCameraAttr.SpecBand.ToString();

            var downLightParam = _cameraLightParamService40.GetLightSourceParam(ECamera.MeasExt);
            if (downLightParam is MeasureLightSourceParam40 downParam)
                DownIntensity = downParam.Intensity.ToString("F0");
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

    #endregion

    #region 3. 页签切换 — ①Vision_1 / ②Vision_2 / Measurement Run

    /// <summary>
    /// 切换 Vision1/Vision2（CommandParameter：0=Vision1，1=Vision2）。
    /// 隐藏 Measurement 页，按页签刷新 Inner/Outer 面板；双相机时移到对应视野已记录的机台位，单相机仅切换光学上下文。
    /// </summary>
    [RelayCommand]
    void OpenVision(object vision)
    {
        IsShowMeasurement = false;
        if (TestInfo?.CrossFov != true)
        {
            _visionService.StartGrabbing();
            return;
        }

        CrossFovVision = vision?.ToString() == "1" ? EnumVision.Vision2 : EnumVision.Vision1;
        RefreshCrossFovTrainUiVisibility();
        SwitchToVisionContext(CrossFovVision, refreshOpticalModule: true);
        _visionService.StartGrabbing();
        InitMeasurementRoiButton();
    }

    /// <summary>
    /// 进入 Measurement Run 页：同时显示 Inner/Outer 全部训练区与 Run 量测面板。
    /// </summary>
    [RelayCommand]
    void OpenMeasurement()
    {
        IsShowMeasurement = true;
        RefreshCrossFovTrainUiVisibility();
    }

    partial void OnIsShowMeasurementChanged(bool value) => RefreshCrossFovTrainUiVisibility();

    partial void OnCrossFovVisionChanged(EnumVision value) => RefreshCrossFovTrainUiVisibility();

    /// <summary>
    /// 按当前页签刷新 Inner/Outer 各面板的 Visibility（Vision 页只显示当前层；Measurement 页全部显示）。
    /// </summary>
    private void RefreshCrossFovTrainUiVisibility()
    {
        if (TestInfo?.CrossFov != true || IsShowMeasurement)
        {
            CrossFovUiInnerTrainVisible = true;
            CrossFovUiOuterTrainVisible = true;
            InnerModelSectionVisible = false;
            OuterModelSectionVisible = false;
            InnerPositioningSectionVisible = false;
            OuterPositioningSectionVisible = false;
            RefreshCrossFovCorrectionVisibility();
            return;
        }

        CrossFovUiInnerTrainVisible = CrossFovVision == EnumVision.Vision1;
        CrossFovUiOuterTrainVisible = CrossFovVision == EnumVision.Vision2;
        InnerModelSectionVisible = CrossFovVision == EnumVision.Vision1;
        OuterModelSectionVisible = CrossFovVision == EnumVision.Vision2;
        InnerPositioningSectionVisible = CrossFovVision == EnumVision.Vision1;
        OuterPositioningSectionVisible = CrossFovVision == EnumVision.Vision2;
        RefreshCrossFovCorrectionVisibility();
    }

    private void RefreshCrossFovCorrectionVisibility()
    {
        if (TestInfo?.CrossFov != true || IsShowMeasurement)
        {
            InnerCorrectionVisibility = Visibility.Visible;
            OuterCorrectionVisibility = Visibility.Visible;
            return;
        }

        InnerCorrectionVisibility = CrossFovVision == EnumVision.Vision1 ? Visibility.Visible : Visibility.Collapsed;
        OuterCorrectionVisibility = CrossFovVision == EnumVision.Vision2 ? Visibility.Visible : Visibility.Collapsed;
    }

    #endregion

    #region 4. 机台位记录与恢复（跨视野 Stage / WCS）

    private TrainSettings40_CrossFov CrossFovTrainSettings =>
        TestInfo?.TrainSetting_CrossFov;

    private Point2d _crossFovFocusRoiStagePointVision1;
    private Point2d _crossFovFocusRoiStagePointVision2;

    /// <summary>训练关键步骤完成后尝试保存当前 Vision 机台位（失败仅写日志，不阻断流程）。</summary>
    private void TrySaveCurrentCrossFovVisionStage()
    {
        if (TestInfo?.CrossFov != true)
            return;
        try
        {
            SaveCurrentCrossFovVisionStage();
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
        }
    }

    private void SaveCurrentCrossFovVisionStage()
    {
        // 第一阶段：FocusROI Run 后记录当前视野中心，后续确认 MeasureROI 时以此为换算基准。
        var pos = alignmentService.GetCurrentAxisPosition();
        if (CrossFovVision == EnumVision.Vision1)
            _crossFovFocusRoiStagePointVision1 = pos;
        else
            _crossFovFocusRoiStagePointVision2 = pos;

        SaveCrossFovVisionStage(CrossFovVision, pos);
    }

    /// <summary>
    /// 将指定 Stage 绝对坐标转换成 WCS 写入 CrossFov 配方，并同步到 CDOvl OverFov 算法参数。
    /// FocusROI 阶段写入当前视野中心；MeasureROI 阶段会用量测 ROI 中心换算后的点位覆盖。
    /// </summary>
    private void SaveCrossFovVisionStage(EnumVision vision, Point2d pos)
    {
        var settings = CrossFovTrainSettings;
        if (settings == null || TestInfo?.CrossFov != true || pos == null)
            return;

        var wcs = alignmentService.GetWcsPoint(pos);
        var umPos = pos * 1000;
        if (vision == EnumVision.Vision1)
        {
            CDOvlOfOverFovAlgorithmParam.StagePosFov1 = umPos;
            CDOvlOfOverFovAlgorithmParam.StageXIsSameDirectionWithImage = true;
            if (TestInfo.OMParam_Double?.AlgorithmParam is CDOvlOfOverFovAlgorithmParam doubleParam)
            {
                doubleParam.StagePosFov1 = umPos;
                doubleParam.StageXIsSameDirectionWithImage = true;
            }
            settings.StagePosFovPointVision1 = wcs;
            UpdateCrossFovVisionOffset(settings);
            LogHelper.LogDebug($"TrainViewModel Set StagePosFovPointVision1 = {wcs}");

        }
        else
        {
            CDOvlOfOverFovAlgorithmParam.StagePosFov2 = umPos;
            CDOvlOfOverFovAlgorithmParam.StageYIsSameDirectionWithImage = false;
            if (TestInfo.OMParam_Double?.AlgorithmParam is CDOvlOfOverFovAlgorithmParam doubleParam)
            {
                doubleParam.StagePosFov2 = umPos;
                doubleParam.StageYIsSameDirectionWithImage = false;
            }
            settings.StagePosFovPointVision2 = wcs;
            UpdateCrossFovVisionOffset(settings);

            LogHelper.LogDebug($"TrainViewModel Set StagePosFovPointVision2 = {wcs}");
        }
    }

    private void UpdateCrossFovVisionOffset(TrainSettings40_CrossFov settings)
    {
        if (settings == null)
            return;

        if (settings.StagePosFovPointVision1 == null || IsZeroStagePoint(settings.StagePosFovPointVision1) ||
            settings.StagePosFovPointVision2 == null || IsZeroStagePoint(settings.StagePosFovPointVision2))
        {
            return;
        }

        settings.StagePosFovOffsetVision1ToVision2 = settings.StagePosFovPointVision2 - settings.StagePosFovPointVision1;
        LogHelper.LogDebug($"TrainViewModel Set StagePosFovOffsetVision1ToVision2 = {settings.StagePosFovOffsetVision1ToVision2}");
    }

    private void SaveCrossFovMeasureRoiStage(bool isInner, Point2d measureRoiStagePoint)
    {
        SaveCrossFovVisionStage(isInner ? EnumVision.Vision1 : EnumVision.Vision2, measureRoiStagePoint);
    }

    private Point2d GetCrossFovVisionStageAxisPoint(bool isInner)
    {
        var point = isInner ? _crossFovFocusRoiStagePointVision1 : _crossFovFocusRoiStagePointVision2;
        if (point == null)
            throw new InvalidOperationException($"{(isInner ? "Vision1" : "Vision2")} FocusROI stage position is not recorded. Please train focus before confirming Measure ROI.");
        return point;
    }

    /// <summary>移到指定 Vision 上次记录的 WCS 点位（Find target0、Test Position 等显式流程仍可使用）。</summary>
    private void TryMoveToSavedVision(EnumVision vision)
    {
        var settings = CrossFovTrainSettings;
        if (settings == null)
            return;

        var pt = vision == EnumVision.Vision1 ? settings.StagePosFovPointVision1 : settings.StagePosFovPointVision2;
        if (pt != default && !IsZeroStagePoint(pt))
        {
            var context = CreateVisionContext(vision);
            var attr = _serviceCamera.GetCameraAttr(context.Camera);
            var point = _alignmentService.GetAxisPoint(pt, context.Camera.GetCameraWaferCoordi(), attr);
            alignmentService.MoveTo(point, "LAngleOVL_CrossFov_" + vision);
        }
    }

    private static bool IsZeroStagePoint(WcsPoint pt)
    {
        const double eps = 1e-9;
        return Math.Abs(pt.X) < eps && Math.Abs(pt.Y) < eps;
    }

    #endregion

    #region 5. 相机/轴与 CrossFov 定位模板数据访问

    /// <summary>CrossFov Vision1 对应的启动相机；单相机模式使用配方的 MeasCamera。</summary>
    protected override ECamera FirstCamera => CreateVisionContext(EnumVision.Vision1).Camera;

    internal IVisionService CurVision => _visionService;

    internal ECamera CurCamera =>
        IsShowMeasurement ? GetMeasurementCamera() : CreateVisionContext(CrossFovVision).Camera;

    internal AxisType CurAxis =>
        IsShowMeasurement ? GetAxisByCamera(CurCamera) : CreateVisionContext(CrossFovVision).Axis;

    protected PositioningTemplateInfo PositioningModel_Inner =>
        CrossFovTrainSettings?.PositioningTemplateInfo_Inner;

    protected PositioningTemplateInfo PositioningModel_Outer =>
        CrossFovTrainSettings?.PositioningTemplateInfo_Outer;

    protected override PositioningTemplateInfo GetPositioningModel(bool isInner) =>
        isInner ? PositioningModel_Inner : PositioningModel_Outer;

    /// <summary>跨视野时按当前 Vision 解析“当前侧”定位数据；供基类单入口逻辑兼容。</summary>
    protected override PositioningTemplateInfo PositioningModel
    {
        get
        {
            if (TestInfo?.CrossFov == true)
                return CrossFovVision == EnumVision.Vision1 ? PositioningModel_Inner : PositioningModel_Outer;
            return base.PositioningModel;
        }
    }

    #endregion

    #region 5b. FocusModule Inner/Outer（参照 DoubleFace，Vision1→Inner / Vision2→Outer）

    private TrainSettings40_CrossFov GetCrossFovTrainSettingsOrThrow()
    {
        var settings = CrossFovTrainSettings;
        if (settings == null)
            throw new InvalidOperationException("CrossFov train settings cannot be null.");
        return settings;
    }

    private FocusModuleInfo GetFocusModuleInfo(bool isInner)
    {
        var settings = GetCrossFovTrainSettingsOrThrow();
        if (!isInner && settings.FocusModuleInfoOuter == null)
            settings.FocusModuleInfoOuter = new FocusModuleInfo();
        return isInner ? settings.FocusModuleInfoInner : settings.FocusModuleInfoOuter;
    }

    private bool IsCrossFovVisionInner =>
        TestInfo?.CrossFov == true && CrossFovVision == EnumVision.Vision1;

    protected override FocusInfo FocusInfo
    {
        get
        {
            if (TestInfo == null)
                throw new InvalidOperationException("TestInfo cannot be null");
            if (TestInfo.CrossFov != true)
                return base.FocusInfo;
            return GetFocusModuleInfo(IsCrossFovVisionInner).FocusInfo;
        }
        set
        {
            if (TestInfo?.CrossFov != true)
            {
                base.FocusInfo = value;
                return;
            }
            GetFocusModuleInfo(IsCrossFovVisionInner).FocusInfo = value;
        }
    }

    protected override FocusInfo GetTestFocusOrDefault(ECamera camera = ECamera.Meas)
    {
        if (TestInfo?.CrossFov == true)
            return GetTestFocusOrDefault(IsCrossFovVisionInner, camera);
        return base.GetTestFocusOrDefault(camera);
    }

    protected FocusInfo GetTestFocusOrDefault(bool isInner, ECamera camera = ECamera.Meas)
    {
        var focusInfo = GetFocusModuleInfo(isInner)?.FocusInfo;
        return focusInfo ?? _serviceProvider.GetService<ITestService>().GetSystemFocusInfo(camera);
    }

    protected override void SetVisionParams(ECamera camera = ECamera.Meas)
    {
        if (TestInfo?.CrossFov != true)
        {
            base.SetVisionParams(camera);
            return;
        }

        SetVisionParams(CreateVisionContextByCamera(camera));
    }

    private void SetVisionParams(CrossFovVisionContext context)
    {
        try
        {
            EnsureVisionContextParams(context);

            var cameraParam = GetVisionCameraParam(context.Vision);
            if (cameraParam != null)
                _cameraLightParamService40.SetCamearaParam(context.Camera, cameraParam);

            if (GetVisionLightSourceParam(context.Vision) is MeasureLightSourceParam40 lightSourceParam)
            {
                var runtimeLightParam = CloneHelper.ReflectionClone<MeasureLightSourceParam40>(lightSourceParam);
                _cameraLightParamService40.SetLightSourceParamMeasureSingleFace(runtimeLightParam);

                var visionManager = _serviceProvider.GetRequiredService<IVisionServiceManager>();
                visionManager.SetLightModuleId(context.Camera, runtimeLightParam.Camera);
                _machineDomainService.CameraLightPairConfig.CurCameraLightPairs[context.Camera] = runtimeLightParam.Camera;
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
            _messageBoxService.Error(ex.Message);
        }

        _visionService.NotifyParamsChanged();
    }

    private void EnsureVisionContextParams(CrossFovVisionContext context)
    {
        if (CrossFovTrainSettings == null)
            return;

        // CrossFov 即使是单相机训练，也要保留 Vision1/2 各自的参数副本；这里只补空值，避免覆盖已保存的光学模块选择。
        if (context.CameraParam == null)
            SetVisionCameraParam(context.Vision, _cameraLightParamService.GetCamearaParam(context.Camera));

        if (context.LightSourceParam == null)
        {
            var lightSourceParam = _cameraLightParamService.GetLightSourceParam(context.Camera);
            if (lightSourceParam is MeasureLightSourceParam40 measureLight)
                measureLight.IsDoubleGrab = _grabMode == EnumGrabMode.DoubleGrab;

            SetVisionLightSourceParam(context.Vision, lightSourceParam);
        }
    }

    private void InitializeCrossFovVisionParams()
    {
        if (TestInfo?.CrossFov != true)
            return;

        EnsureVisionContextParams(CreateVisionContext(EnumVision.Vision1));
        EnsureVisionContextParams(CreateVisionContext(EnumVision.Vision2));
    }

    protected override void SaveVisionParams()
    {
        if (TestInfo?.CrossFov != true)
        {
            base.SaveVisionParams();
            return;
        }

        SaveVisionParams(CreateVisionContext(CrossFovVision));
    }

    private void SaveVisionParams(CrossFovVisionContext context)
    {
        try
        {
            var cameraParam = _cameraLightParamService.GetCamearaParam(context.Camera);
            SetVisionCameraParam(context.Vision, cameraParam);

            var visionServiceManager = _serviceProvider.GetRequiredService<IVisionServiceManager>();
            var lightModuleCamera = visionServiceManager.GetLightModuleId(context.Camera);
            var lightSourceParam = _cameraLightParamService.GetLightSourceParam(lightModuleCamera);
            if (lightSourceParam is MeasureLightSourceParam40 measureLight)
                measureLight.IsDoubleGrab = _grabMode == EnumGrabMode.DoubleGrab;

            SetVisionLightSourceParam(context.Vision, lightSourceParam);
            SaveAttenuationCoef(context.Camera, cameraParam);
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
        }
    }

    protected override ECamera GetECamera(bool isInner) =>
        CreateVisionContext(isInner).Camera;

    protected override AxisType GetZAxis(bool isInner) => CurAxis;

    protected override void SetFocusZHeight()
    {
        if (TestInfo?.CrossFov != true)
        {
            base.SetFocusZHeight();
            return;
        }

        if (!TrainSetting.IsTrained)
        {
            InnerFocusHeight = 0.0d;
            OuterFocusHeight = 0.0d;
            return;
        }

        InnerFocusHeight = GetCrossFovFocusHeightByLayer(true);
        OuterFocusHeight = GetCrossFovFocusHeightByLayer(false);
    }

    private double GetCrossFovFocusHeightByLayer(bool isInner)
    {
        var focusModule = GetFocusModuleInfo(isInner);
        if (focusModule.BaseFocusHeight == 0)
            focusModule.BaseFocusHeight = _alignmentService.WaferMapInfo.Thickness;

        var context = CreateVisionContext(isInner);
        var offsetZ = context.Axis == AxisType.Z ? _alignmentService40.WaferMapInfo.Thickness : 0;
        return focusModule.BaseFocusHeight
            + _alignmentService40.GetMacroFocusZ(context.Camera).z
            + offsetZ;
    }

    #endregion

    #region 6. Focus — 聚焦 ROI 绘制与 Run（Vision1=Inner / Vision2=Outer）

    /// <summary>Inner Focus Run（界面 Inner 行 Run 按钮）。</summary>
    protected override async Task TrainFocus_InnerSingleMethod() => await TrainFocus_SingleMethod();

    /// <summary>Outer Focus Run（界面 Outer 行 Run 按钮）。</summary>
    protected override async Task TrainFocus_OuterSingleMethod() => await TrainFocus_SingleMethod();

    /// <summary>
    /// Focus 面板 Run：按当前 Vision 校验对应层 Focus ROI，全视野精聚焦 + ROI 曲线聚焦，并记录机台位。
    /// </summary>
    protected override async Task TrainFocus_SingleMethod()
    {
        if (CrossFovVision == EnumVision.Vision1)
        {
            if (TrainParam.InnerLayer.FocusROI == null)
            {
                _messageBoxService.ErrorDialog("Please draw the corresponding Inner ROI！");
                return;
            }
        }
        else
        {
            if (TrainParam.OuterLayer.FocusROI == null)
            {
                _messageBoxService.ErrorDialog("Please draw the corresponding Outer ROI！");
                return;
            }
        }

        LogHelper.LogRunMethodStart();
        ChangeCameraInfo = "Focusing …";
        IsTrained = false;
        try
        {
            await TrainFineFocus();
            TrySaveCurrentCrossFovVisionStage();
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
            string errmsg = "Focusing failed. Please modify the focusing parameters and try again.\n" + $"Detail:{ex.Message}";
            AppHelper.DispatcherInvoke(() => _messageBoxService.ErrorDialog(errmsg));
        }
        finally
        {
            ChangeCameraInfo = "";
            HpImageViewer.UpdateROIs();
            LogHelper.LogRunMethodEnd("", nameof(TrainFocus_SingleMethod));
        }
    }

    /// <summary>
    /// 当前 Vision 单层精聚焦：全视野 TrainGlobal → 对应层 ROI 曲线聚焦 → 采平均图。
    /// </summary>
    protected async Task TrainFineFocus()
    {
        HpImageViewer.UpdateROIs();
        SetRoiColors(CrossFovVision == EnumVision.Vision1);

        if (CrossFovVision == EnumVision.Vision1)
        {
            await Task.Run(() => FineResInner = TrainGlobal());
            await FocusInnerOuterSingleGrab(isMeasure: false, isInner: true);
        }
        else
        {
            await Task.Run(() => FineResOuter = TrainGlobal());
            await FocusInnerOuterSingleGrab(isMeasure: false, isInner: false);
        }
    }

    /// <summary>
    /// 单抓/平均模式：基于已有全视野结果对指定层做 ROI 聚焦，移到焦高并可选采图写清晰度。
    /// </summary>
    /// <param name="isMeasure">true=量测 ROI 确认后的采图链路。</param>
    /// <param name="isInner">Vision1 传 true（Inner），Vision2 传 false（Outer）。</param>
    private async Task FocusInnerOuterSingleGrab(bool isMeasure, bool isInner)
    {
        _visionService.StartGrabbing();
        var midZHeight = (InnerFocusHeight + OuterFocusHeight) / 2;

        if (isInner)
        {
            await TrainRoiFocus(FineResInner, true, isMeasure);
            midZHeight = GetMeasureConfirmFocusHeight(true);
        }
        else
        {
            await TrainRoiFocus(FineResOuter, false, isMeasure);
            midZHeight = GetMeasureConfirmFocusHeight(false);
        }

        JudgeFocusHeightException();
        MoveAndWait(midZHeight);
        await SaveFocusImage_ByGrab(CONST_Average, CurVision, false, null, isMeasure);

        if (isMeasure)
        {
            var image = CurVision.GrabOneImage();
            var globalResult = isInner ? FineResInner : FineResOuter;
            UpdateFoucsHeightSharpness(image, globalResult.ZFit, isInner);
        }

        _visionService.StopGrabbing();
    }

    /// <summary>高亮当前 Vision 对应层的 Focus ROI 颜色，便于图像控件区分。</summary>
    protected void SetRoiColors(bool inner)
    {
        if (inner)
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

    /// <summary>双抓 PEG 模式下，内外焦高过近则抛异常。</summary>
    protected void JudgeFocusHeightException()
    {
        if (_grabMode == EnumGrabMode.DoubleGrab
            && Physical.AnraSettings.DoubleGrabMode == EnumDoubleGrabMode.PEG
            && Math.Abs(InnerFocusHeight - OuterFocusHeight) < 0.0001)
        {
            throw new Exception("Focus offset less than 0.1um, not adjust to 'Double Grab Mode: PEG'");
        }
    }

    #endregion

    #region 7. Measurement ROI — 量测 ROI 确认（Inner / Outer）

    [RelayCommand]
    private async Task CrossFovInnerMeasureRoi_Confirm() => await ConfirmMeasureRoi(true);

    [RelayCommand]
    private async Task CrossFovOuterMeasureRoi_Confirm() => await ConfirmMeasureRoi(false);

    /// <summary>
    /// 确认当前视野下的量测 ROI：写入 TrainParam、更新 TargetPoint、按量测 ROI 重新聚焦并保存 Inner/Outer.bmp。
    /// </summary>
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

        if (isInner)
        {
            TestTrainSetting.TrainParam.InnerLayer.MeasureROI = rOIDescs as RectangleDesc;
            InneredgeConfirmRois = null;
        }
        else
        {
            rOIDescs.Color = OuterColor;
            OuteredgeConfirmRois = null;
            TestTrainSetting.TrainParam.OuterLayer.MeasureROI = rOIDescs as RectangleDesc;
        }

        var roiCenter = rOIDescs.CreateRoi().GetCenter();
        var imagedata = HpImageViewer.Image as ImageData;
        ChangeCameraInfo = "Measure Focusing...";
        try
        {
            var cam = GetECamera(isInner);
            if (ShouldUpdateTargetPointAfterConfirmMeasureRoi(isInner))
            {
                var visionStagePoint = GetCrossFovVisionStageAxisPoint(isInner);
                // 第二阶段：MeasureROI 中心必须基于第一阶段的视野中心换算，避免当前轴位置被手动移动或 UI 操作污染。
                var targetPoint = _alignmentService40.ConvertImageToAxisBySpecCam(
                    visionStagePoint,
                    new MatchingItem { Column = roiCenter.X, Row = roiCenter.Y },
                    imagedata.Width, imagedata.Height, cam);
                UpdateTargetPointAfterConfirmMeasureRoi(isInner, targetPoint);
                SaveCrossFovMeasureRoiStage(isInner, targetPoint);
                LogHelper.LogDebug($"ROI Center = {roiCenter}, CrossFov measure stage point = {targetPoint} ");
            }

            IsUpdateMeasureRoi = true;
            if (isInner)
                IsInnerCorrectionEnabled = true;
            else
                IsOuterCorrectionEnabled = true;

            SaveVisionParams();
            InitMeasurementRoiButton();
            await FindMeasureRoiFocusLayer(isInner);
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

    /// <summary>量测 ROI 确认后：按单抓/双抓模式使用量测 ROI 重新聚焦并更新落盘图像。</summary>
    protected override async Task FindMeasureRoiFocusLayer(bool isInner)
    {
        await Task.Run(async () =>
        {
            //if (_grabMode == EnumGrabMode.DoubleGrab)
            //{
            //    if (FineResInner == null || FineResOuter == null)
            //        throw new Exception("Please run inner & outer focus operation completely!");
            //}

            _visionService.StartGrabbing();
            if (_grabMode == EnumGrabMode.DoubleGrab)
                await FocusInnerOuterDoubleGrab(isMeasure: true, isInner);
            else
                await FocusInnerOuterSingleGrab(isMeasure: true, isInner);
            _visionService.StopGrabbing();
        });
    }

    /// <summary>双抓模式下，在对应层焦高执行量测侧聚焦。</summary>
    private async Task FocusInnerOuterDoubleGrab(bool isMeasure, bool isInner)
    {
        if (!isMeasure)
            return;

        if (isInner)
        {
            MoveAndWait(InnerFocusHeight);
            await FocusInner(isMeasure);
        }
        else
        {
            MoveAndWait(OuterFocusHeight);
            await FocusOuter(isMeasure);
        }
    }

    #endregion

    #region 8. Positioning Model — 内外定位模板（Enable / 建模 / FindTarget0 / Test Position）

    /// <summary>从配方目录加载内外定位模板 ModelInfo，并绑定到 Inner/Outer 模板控件。</summary>
    internal void BindCrossFovPositioningTemplates()
    {
        if (TestInfo?.CrossFov != true || CrossFovTrainSettings == null)
            return;

        LoadCrossFovPositioningModelInfo();

        if (PositioningTemplateControl_Inner != null && PositioningModel_Inner != null)
        {
            PositioningTemplateControl_Inner.TemplateContext = PositioningModel_Inner.Template;
            PositioningTemplateControl_Inner.TemplateContext.Name = "Positioning Model Inner";
            PositioningTemplateControl_Inner.ActionUserModified = (c, t, ctx, ok) =>
                OnCrossFovPositionTemplateUserModified(PositioningModel_Inner, t, ctx, ok);
            PositioningTemplateControl_Inner.FuncBeforeCreateModel = () => BeforeCreatePostionModel(true);
        }

        if (PositioningTemplateControl_Outer != null && PositioningModel_Outer != null)
        {
            PositioningTemplateControl_Outer.TemplateContext = PositioningModel_Outer.Template;
            PositioningTemplateControl_Outer.TemplateContext.Name = "Positioning Model Outer";
            PositioningTemplateControl_Outer.ActionUserModified = (c, t, ctx, ok) =>
                OnCrossFovPositionTemplateUserModified(PositioningModel_Outer, t, ctx, ok);
            PositioningTemplateControl_Outer.FuncBeforeCreateModel = () => BeforeCreatePostionModel(false);
        }

        if (TestInfo.TargetSetting.EnabledTargetPoints.Count() <= 1)
        {
            CheckPositionModelForeachVisible = Visibility.Collapsed;
            PositioningModel_Inner.IsForEachTarget = true;
            PositioningModel_Outer.IsForEachTarget = true;
        }
        else
        {
            CheckPositionModelForeachVisible = Visibility.Visible;
        }
    }

    private void LoadCrossFovPositioningModelInfo()
    {
        if (CrossFovTrainSettings == null || string.IsNullOrEmpty(TestFilesFolder))
            return;

        if (PositioningModel_Inner?.Template != null)
            _templateMatchingService.ReadTemplatesModelInfo(PositioningModel_Inner.Template, TestFilesFolder);
        if (PositioningModel_Outer?.Template != null)
            _templateMatchingService.ReadTemplatesModelInfo(PositioningModel_Outer.Template, TestFilesFolder);

        PositioningModel_Inner?.RefreshIsAvailable();
        PositioningModel_Outer?.RefreshIsAvailable();
    }

    /// <summary>定位模板建模成功：写 TrainMatchItem、R3、R4（R4 = 对应侧 TargetPoint - 模板中心）。</summary>
    private void OnCrossFovPositionTemplateUserModified(
        PositioningTemplateInfo positioningModel,
        ITemplateMatchingView.UserModifyType type,
        SingleTemplateMatchingContext context,
        bool isSuccess)
    {
        if (!isSuccess || type != ITemplateMatchingView.UserModifyType.ModelCreated || HpImageViewer.Image is not ImageData imageData)
            return;

        positioningModel.TrainMatchItem = context.MatchingItems[0];
        var templatePosition = alignmentService.ConvertImageToAxis(
            alignmentService.GetCurrentAxisPosition(),
            positioningModel.TrainMatchItem,
            imageData.Width,
            imageData.Height);
        if (!positioningModel.IsForEachTarget)
            positioningModel.R3 = alignmentService.GetFieldR3(templatePosition).R3;

        var isInner = positioningModel == PositioningModel_Inner;
        var targetPoint = GetTargetPoint(isInner);
        if (targetPoint == null)
            return;

        context.R4 = targetPoint - templatePosition;
        positioningModel.RefreshIsAvailable();
    }

    protected bool BeforeCreatePostionModel(bool isInner)
    {
        var context = CreateVisionContext(isInner);
        return BeforeCreatePositionModel_Common(isInner, () => (context.Axis, context.Camera, GetPositioningModel(isInner)));
    }

    [RelayCommand]
    private async Task FindTarget0Inner() => await FindTarget0CoreAsync(true);

    [RelayCommand]
    private async Task FindTarget0Outer() => await FindTarget0CoreAsync(false);

    /// <summary>定位模板区「Positioning for each target」：按侧查找 target0 并移动到示教点。</summary>
    private async Task FindTarget0CoreAsync(bool isInner)
    {
        var positioningModel = GetPositioningModel(isInner);
        if (positioningModel?.IsForEachTarget == true)
            return;

        if (GetTargetPoint(isInner) == null)
        {
            _messageBoxService.ErrorDialog("Please set/reset the measurement roi first!");
            return;
        }

        try
        {
            ChangeCameraInfo = "Find target0 ...";
            HpImageViewer.UpdateROIs();
            var target0 = TestInfo.TargetSetting.EnabledTargetPoints.FirstOrDefault()
                ?? throw new Exception("There is no available measurement target point!");

            await Task.Run(() =>
            {
                if (TestInfo?.CrossFov == true)
                    TryMoveToSavedVision(isInner ? EnumVision.Vision1 : EnumVision.Vision2);

                var teachingPoint = _alignmentService.GetCurrentAxisPosition();
                var field = _alignmentService.GetFieldR3(teachingPoint).Field;
                _visionService.StartGrabbing();
                var point = _alignmentService.GetAxisPoint(field, target0, true);
                var resolved = GetTargetAxisPoint(point, isInner);
                SetTargetPoint(isInner, resolved);
                if (resolved == null)
                    throw new Exception("The first available measurement target point was not matched!");
                _alignmentService.MoveTo(resolved);
            });
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
            _messageBoxService.ErrorDialog(ex.Message);
            if (positioningModel != null)
                positioningModel.IsForEachTarget = true;
        }
        finally
        {
            _visionService.StopGrabbing();
            ChangeCameraInfo = "";
        }
    }

    [RelayCommand]
    private async Task TestPositionInner() => await TestPositionCoreAsync(true);

    [RelayCommand]
    private async Task TestPositionOuter() => await TestPositionCoreAsync(false);

    /// <summary>Test Positioning 匹配定位模板并移动到目标点。</summary>
    private async Task TestPositionCoreAsync(bool isInner)
    {
        var positioningModel = GetPositioningModel(isInner);
        await Task.Run(() =>
        {
            try
            {
                ChangeCameraInfo = "Test positioning target ...";
                if (positioningModel == null || !positioningModel.IsAvailable)
                {
                    _messageBoxService?.ErrorDialog("Please create a positioning template first");
                    return;
                }

                //if (TestInfo?.CrossFov == true)
                //{
                //    TryMoveToSavedVision(isInner ? EnumVision.Vision1 : EnumVision.Vision2);
                //}

                HpImageViewer.UpdateROIs();
                _testTrainService.TestPositionModel(positioningModel);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService?.ErrorDialog(ex.Message);
            }
            finally
            {
                ChangeCameraInfo = "";
            }
        });
    }

    #endregion

    #region 9. TargetPoint — 内外层示教点（量测 ROI 中心 → 定位 R4）

    private Point2d _targetPoint_Inner;
    private Point2d _targetPoint_Outer;

    /// <summary>当前 Vision 对应的示教点；供基类单入口逻辑及定位 R4 计算。</summary>
    public override Point2d TargetPoint
    {
        get => GetTargetPoint(CrossFovVision == EnumVision.Vision1);
        set => SetTargetPoint(CrossFovVision == EnumVision.Vision1, value);
    }

    private Point2d GetTargetPoint(bool isInner) =>
        isInner ? _targetPoint_Inner : _targetPoint_Outer;

    private void SetTargetPoint(bool isInner, Point2d targetPoint)
    {
        if (isInner)
            _targetPoint_Inner = targetPoint;
        else
            _targetPoint_Outer = targetPoint;
    }

    protected override bool ShouldUpdateTargetPointAfterConfirmMeasureRoi(bool isInner) => true;

    protected override void UpdateTargetPointAfterConfirmMeasureRoi(bool isInner, Point2d targetPoint) =>
        SetTargetPoint(isInner, targetPoint);

    /// <summary>按 Inner/Outer 量测模板将 Field 内示教点匹配为机台坐标。</summary>
    protected Point2d GetTargetAxisPoint(Point2d teachingPoint, bool isInner)
    {
        var settings = GetCrossFovTrainSettingsOrThrow();
        var measureModels = isInner
            ? settings.OverlayMark_InnerModels[0]
            : settings.OverlayMark_OuterModels[0];
        return _alignmentService.FindSingleTemplate(teachingPoint, measureModels, true, false, out _);
    }

    protected override Point2d GetTargetAxisPoint(Point2d teachingPoint)
    {
        if (TestInfo?.CrossFov == true)
            return GetTargetAxisPoint(teachingPoint, CrossFovVision == EnumVision.Vision1);
        return base.GetTargetAxisPoint(teachingPoint);
    }

    protected override void SetPreviewTemplate(bool isInner, SingleTemplateMatchingContext template)
    {
        if (TestInfo?.CrossFov != true)
        {
            base.SetPreviewTemplate(isInner, template);
            return;
        }

        var settings = GetCrossFovTrainSettingsOrThrow();
        if (isInner)
            settings.PreviewTemplateInner = template;
        else
            settings.PreviewTemplateOuter = template;
    }

    #endregion

    #region 10. Measurement Run — Run 量测、校验与 OM 参数

    /// <summary>
    /// Measurement Run 页 Run：加载 Inner/Outer 焦面图，必要时重建 Kernel，调用 MeasureOverlayOverFov 并展示结果。
    /// </summary>
    protected override async Task RunMeasureMethod()
    {
        if (TestInfo?.CrossFov != true)
        {
            await base.RunMeasureMethod();
            return;
        }

        try
        {
            if (!TryValidateCrossFovMeasurePrerequisites(out string err))
            {
                _messageBoxService.ErrorDialog(err);
                return;
            }


            ImageData img_inner = LoadFocusImageData(true);
            ImageData img_outer = LoadFocusImageData(false);

            if (IsUpdateMeasureRoi || IsInnerCorrectioned || IsOuterCorrectioned)
            {
                JudgeFocusHeightException();
                await CreateMeasureKernelSizeAndTemplate(true, true, img_inner);
                await CreateMeasureKernelSizeAndTemplate(false, true, img_outer);

                SaveFocusImageCore(CONST_PREVIEW_INNER, null, img_inner);
                SaveFocusImageCore(CONST_PREVIEW_OUTER, null, img_outer);
                await CreatePreviewTemplate(true, CONST_PREVIEW_INNER, img_inner);
                await CreatePreviewTemplate(false, CONST_PREVIEW_OUTER, img_outer);

                SaveCorrectHeight(true);
                SaveCorrectHeight(false);
                await AutoCorrectR3();
                IsTrained = true;
            }
            else
            {
                img_inner = LoadFocusImageData(true);
                img_outer = LoadFocusImageData(false);
            }

            HObject imgObjectInner = img_inner?.ToHObject();
            if (imgObjectInner == null)
            {
                _messageBoxService.ErrorDialog("Inner capture image is invalid.");
                return;
            }

            HObject imgObjectOuter = img_outer?.ToHObject();

            var bib = TestInfo.GetTrainSettings() as TrainSettings_BiB;
            var trainParam = bib?.TrainParam;
            if (!HasCrossFovKernelModels()
                || trainParam?.InnerLayer?.MeasureROI == null || trainParam?.OuterLayer?.MeasureROI == null)
            {
                _messageBoxService.ErrorDialog("Please set measurement roi first!");
                return;
            }

            HpImageViewer.UpdateROIs();

            var trainSettings = TestInfo.GetTrainSettings();
            var transMatInner = GetTransmat(trainSettings.InnerTrainMatchItem, trainSettings.InnerTrainMatchItem);
            var transMatOuter = GetCrossFovOuterTransMat(trainSettings, transMatInner);
            var (offsetX, offsetY) = Physical?.GetOvlOffset() ?? (0, 0);

            var ovResult = MeasureCrossFovForTrainView(imgObjectInner, imgObjectOuter, transMatInner, transMatOuter, offsetX, offsetY);
            ShowResult(ovResult, imgObjectInner, imgObjectOuter);
            Reset();
        }
        catch (Exception ex)
        {
            _messageBoxService.ErrorDialog($"{ex.Message}");
        }
    }

    private bool HasCrossFovKernelModels() =>
        GetCrossFovKernelModel(true) != null && GetCrossFovKernelModel(false) != null;

    private OMKernelModel GetCrossFovKernelModel(bool isInner) =>
        (IsDualCameraCrossFov, isInner) switch
        {
            (true, true) => TestInfo.OMParam_Double?.KernelModelInner,
            (true, false) => TestInfo.OMParam_Double?.KernelModelOuter,
            (false, true) => TestInfo.OMParam?.KernelModelInner,
            _ => TestInfo.OMParam?.KernelModelOuter,
        };

    private HTuple GetCrossFovOuterTransMat(TrainSettings_BiB trainSettings, HTuple transMatInner) =>
        IsDualCameraCrossFov || FocusBasedOn == EnumFocusBasedOn.Outer
            ? GetTransmat(trainSettings.OuterTrainMatchItem, trainSettings.OuterTrainMatchItem)
            : transMatInner;

    private OvResultBase MeasureCrossFovForTrainView(HObject imgObjectInner, HObject imgObjectOuter, HTuple transMatInner, HTuple transMatOuter, double offsetX, double offsetY) =>
        IsDualCameraCrossFov
            ? MeasureCrossFovDoubleForTrainView(imgObjectInner, imgObjectOuter, transMatInner, transMatOuter, offsetX, offsetY)
            : MeasureCrossFovSingleForTrainView(imgObjectInner, imgObjectOuter, transMatInner, transMatOuter, offsetX, offsetY);

    private OvResultBase MeasureCrossFovDoubleForTrainView(HObject imgObjectInner, HObject imgObjectOuter, HTuple transMatInner, HTuple transMatOuter, double offsetX, double offsetY)
    {
        UpdateOMPara(InnerCamera, TestInfo.OMParam_Double);
        UpdateOMPara(OuterCamera, TestInfo.OMParam_Double);
        SaveOMParamToFile(TestInfo.OMParam_Double);
        var baseCamera = _machine40Config.PatternCameraCfg.IsInnerBase ? InnerCamera : OuterCamera;
        return _measService.MeasureOverlayOverFov_Double(
            imgObjectInner, imgObjectOuter, TestInfo.OMParam_Double,
            transMatInner, transMatOuter, baseCamera, offsetX, offsetY);
    }

    private OvResultBase MeasureCrossFovSingleForTrainView(HObject imgObjectInner, HObject imgObjectOuter, HTuple transMatInner, HTuple transMatOuter, double offsetX, double offsetY)
    {
        var oMParam = TestInfo.OMParam;
        UpdateOMPara(CurCamera, oMParam);
        SaveOMParamToFile(oMParam);
        return _measService.MeasureOverlayOverFov(
            imgObjectInner, imgObjectOuter, TestInfo.OMParam,
            transMatInner, transMatOuter, offsetX, offsetY);
    }

    /// <summary>Run 前校验：两视野点位已记录且内外量测 ROI 均已配置。</summary>
    private bool TryValidateCrossFovMeasurePrerequisites(out string errorMessage)
    {
        errorMessage = null;
        var settings = CrossFovTrainSettings;
        if (settings == null)
        {
            errorMessage = "Train settings are missing.";
            return false;
        }

        if (settings.StagePosFovPointVision1 == default || IsZeroStagePoint(settings.StagePosFovPointVision1))
        {
            errorMessage = "Vision1 stage position is not recorded. Please switch to Vision1, train focus, confirm Measure ROI, then run again.";
            return false;
        }

        if (settings.StagePosFovPointVision2 == default || IsZeroStagePoint(settings.StagePosFovPointVision2))
        {
            errorMessage = "Vision2 stage position is not recorded. Please switch to Vision2, train focus, confirm Measure ROI, then run again.";
            return false;
        }

        if (settings.StagePosFovOffsetVision1ToVision2 == null || IsZeroStagePoint(settings.StagePosFovOffsetVision1ToVision2))
        {
            errorMessage = "Vision1 to Vision2 WCS offset is not recorded. Please confirm Inner and Outer Measure ROI, then run again.";
            return false;
        }

        var bib = TestInfo.GetTrainSettings() as TrainSettings_BiB;
        var trainParam = bib?.TrainParam;
        if (trainParam?.InnerLayer?.MeasureROI == null || trainParam?.OuterLayer?.MeasureROI == null)
        {
            errorMessage = "Please set inner and outer measurement ROI first.";
            return false;
        }

        return true;
    }

    /// <summary>Run 前自动创建量测/定位模板（若已启用定位且搜索区不足则自动扩展）。</summary>
    protected override async Task AutoCreateModel(bool isInner, bool isShowTestResult)
    {
        var modelControl = isInner ? InnerTemplateControl : OuterTemplateControl;
        var positioningModel = GetPositioningModel(isInner);
        if (positioningModel.IsAvailable)
        {
            var shapeBase = TestTrainSetting.TrainParam.GetRoiDesc(isInner, true).CreateTemplateRoi();
            var roiRect = CreateRectByShapeBase(shapeBase);
            var searchRect = modelControl.TemplateContext.SelectedMatchModelGroup.MatchModel.SearchingRect;
            if (searchRect == null || searchRect.Width < roiRect.Width || searchRect.Height < roiRect.Height)
                modelControl.TemplateContext.SelectedMatchModelGroup.MatchModel.SearchingRect = roiRect;
        }

        LogHelper.LogDebug($"AutoCreateModel isInner = {isInner},searchRect = {modelControl.TemplateContext.SelectedMatchModelGroup.MatchModel.SearchingRect}, searchRect180 = {modelControl.TemplateContext.SelectedMatchModelGroup.MatchModel180.SearchingRect}");
        await modelControl.CreateModelAsync(isShowTestResult);
    }

    /// <summary>Run 前刷新 OM 像素当量与目标类型。</summary>
    protected void UpdateOMPara(ECamera camera, OMParam oMParam)
    {
        var (px, py) = _servCalDat.GetUmPixel(camera, _serviceProvider);
        oMParam.PixelSizeX = px;
        oMParam.PixelSizeY = py;
        var settings = GetCrossFovTrainSettingsOrThrow();
        var targetType = _focusSetting.FocusBasedOn == EnumFocusBasedOn.Outer
            ? settings.OuterType
            : settings.InnerType;
        oMParam.TargetType = targetType.ToOMTargetType();
    }

    #endregion

    #region 11. 聚焦底层实现（全视野 / ROI 曲线 / 采图 / 清晰度）

    private bool IsInBaseOn(bool isInner) =>
        isInner && _isBaseOnInner || !isInner && !_isBaseOnInner;

    private bool _isBaseOnInner =>
        _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner;

    /// <summary>在指定层 ROI 上训练锐度曲线，保存 Inner/Outer.bmp 并更新焦高。</summary>
    protected async Task<double> TrainRoiFocus(FocusPegResult focusPegResult, bool isInner, bool isMeasure)
    {
        var trainLayerParam = isInner ? TrainParam.InnerLayer : TrainParam.OuterLayer;
        var roiDesc = TrainParam.GetRoiDesc(isInner, isMeasure);
        string imageName = isInner ? CONST_Inner : CONST_Outer;
        var fineFocusInfo = GetTestFocusOrDefault(isInner, GetECamera(isInner)).FineFocusInfo;
        var focusImages = focusPegResult.PegImages;
        var trainArg = CreateTrainArg(_grabMode);
        var roi = roiDesc.CreateRoi();

        var ret = _focusService.TrainCurve(fineFocusInfo, roi, focusImages, trainArg);
        trainLayerParam.RoiFocusCurve = ret.Curve;
        var finalRoiHeight = ret.Curve.RefMaximumZ.First();
        MoveAndWait(finalRoiHeight);
        if (isInner)
            InnerFocusHeight = finalRoiHeight;
        else
            OuterFocusHeight = finalRoiHeight;

        if (isMeasure)
            LogRoiForAlgoTest($"************TrainRoiFocus isInner ={isInner}", roi);

        if (!IsCorrectionGrabbed(isInner))
        {
            var img = await SaveFocusImage_ByGrab(imageName, CurVision, isInner, roi, isMeasure);
            TrainGlobalFocusCurve(fineFocusInfo, GetFocusPegResult(isInner), isInner);
            if (isMeasure)
                SaveFocusImageCore(isInner ? CONST_PREVIEW_INNER : CONST_PREVIEW_OUTER, null, img);
            else if (IsInBaseOn(isInner))
                SavePreviewImage(img);

            UpdateFocusModuleHeights(isInner, GetFocusPegResult(isInner).ZFit, isMeasure ? img : null);

            LogHelper.LogInfo($"Save Focus Image = {isMeasure}, imageName : {imageName}.bmp");
        }

        return finalRoiHeight;
    }

    protected async Task FocusInner(bool isMeasure)
    {
        if (!isMeasure)
            await Task.Run(() => FineResInner = TrainGlobal());
        await TrainRoiFocus(FineResInner, true, isMeasure);
        MoveAndWait(GetMeasureConfirmFocusHeight(true));
    }

    protected async Task FocusOuter(bool isMeasure)
    {
        if (!isMeasure)
            await Task.Run(() => FineResOuter = TrainGlobal());
        await TrainRoiFocus(FineResOuter, false, isMeasure);
        MoveAndWait(GetMeasureConfirmFocusHeight(false));
    }

    private double GetMeasureConfirmFocusHeight(bool isInner)
    {
        if (IsCorrectionGrabbed(isInner))
            return isInner ? InnerCorrectionHeight : OuterCorrectionHeight;

        return isInner ? InnerFocusHeight : OuterFocusHeight;
    }

    protected FocusPegResult TrainGlobal()
    {
        var allRangeFocusInfo = CreateFullTrainInfo(CurCamera);
        return ExecuteGlobalFineFocus(allRangeFocusInfo);
    }

    private FocusPegResult ExecuteGlobalFineFocus(AutoRunInfo allRangeFocusInfo)
    {
        allRangeFocusInfo.StartPosition = _servPlat.GetCurrentZ(CurAxis);
        var res = _focusService40.FineFocusSingleCamTrain(CurCamera, allRangeFocusInfo, null);
        MoveAndWait(res.ZFit);
        return res;
    }

    private FocusPegResult TrainGlobalFocusCurve(AutoRunInfo focusInfo, FocusPegResult focusResult, bool isInner)
    {
        var trainArg = CreateTrainArg(_grabMode);
        var ret2 = _focusService.TrainCurve(focusInfo, null, focusResult.PegImages, trainArg);
        if (TestInfo?.CrossFov == true)
        {
            var settings = GetCrossFovTrainSettingsOrThrow();
            if (isInner)
                settings.GlobalFocusCurveInner = ret2.Curve;
            else
                settings.GlobalFocusCurveOuter = ret2.Curve;
        }
        else
        {
            TrainParam.GlobalFineFocusCurve = ret2.Curve;
        }

        return ret2;
    }

    protected virtual FocusPegResult GetFocusPegResult(bool isInner) =>
        isInner ? FineResInner : FineResOuter;

    protected void MoveAndWait(double? focusZHeight)
    {
        if (focusZHeight != null)
            _servPlat.MoveZ(false, CurAxis, focusZHeight.Value);
    }

    /// <summary>按 Inner/Outer 分别写入 FocusModuleInfoInner / FocusModuleInfoOuter（参照 DoubleFace.UpdateFocusHeightSharpness）。</summary>
    private void UpdateFocusModuleHeights(bool isInner, double globalHeight, ImageData image)
    {
        var settings = GetCrossFovTrainSettingsOrThrow();
        var focusModule = GetFocusModuleInfo(isInner);
        var focusHeight = isInner ? InnerFocusHeight : OuterFocusHeight;
        var context = CreateVisionContext(isInner);
        var offsetZ = context.Axis == AxisType.Z ? _alignmentService40.WaferMapInfo.Thickness : 0;
        var zCal = _alignmentService40.GetMacroFocusZ(context.Camera).z;

        if (image != null)
        {
            focusModule.Sharpness = _focusService.GetImageClarity(image);
            var roiDesc = isInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
            if (roiDesc != null)
                focusModule.RoiSharpness = _focusService.GetImageClarity(image, roiDesc.CreateRoi());
        }

        if (isInner)
        {
            TrainParam.InnerLayer.SharpnessFocusOffset = focusHeight - globalHeight;
            settings.InnerHeight = focusHeight;
        }
        else
        {
            TrainParam.OuterLayer.SharpnessFocusOffset = focusHeight - globalHeight;
            settings.OuterHeight = focusHeight;
        }

        focusModule.BaseFocusHeight = focusHeight - zCal - offsetZ;
        focusModule.GlobalFocusOffset = focusHeight - globalHeight;

        LogHelper.LogInfo(
            $"CrossFov Focus Result: isInner={isInner}, focusHeight={focusHeight:0.######}, globalHeight={globalHeight:0.######}, " +
            $"BaseFocusHeight={focusModule.BaseFocusHeight:0.######}, GlobalFocusOffset={focusModule.GlobalFocusOffset:0.######}, " +
            $"SharpnessFocusOffset={(isInner ? TrainParam.InnerLayer.SharpnessFocusOffset : TrainParam.OuterLayer.SharpnessFocusOffset):0.######}");
    }

    protected void UpdateFoucsHeightSharpness(ImageData image, double globalHeight, bool isInner)
    {
        if (image == null)
        {
            AppHelper.DispatcherInvoke(() =>
                _messageBoxService.ErrorDialog("Image is null ,update foucs height sharpness failed."));
            return;
        }

        UpdateFocusModuleHeights(isInner, globalHeight, image);
    }

    protected void SavePreviewImage(ImageData img)
    {
        LogHelper.LogInfo($"Move to previewmatch layer measure focus:Base Layer={_focusSetting.FocusBasedOn}");
        SaveFocusImageCore(CONST_PREVIEW, null, img);
    }

    protected override bool IsCorrectionGrabbed(bool isInner)
    {
        if (!_focusSetting.IsEnableFocusCorrection)
            return false;

        var correctionVisibility = isInner ? InnerCorrectionVisibility : OuterCorrectionVisibility;
        if (correctionVisibility != Visibility.Visible)
            return false;

        return isInner ? IsInnerCorrectioned : IsOuterCorrectioned;
    }

    #endregion


    #region 12. CrossFov Measurement 显示与 EdgeSelect（SingleFace 单 viewer + 按 FocusBasedOn 分层）

    /*
     * EdgeSelect 对比（SingleFace / DoubleFace / CrossFov）：
     * - UI：EdgeSelect 按钮 + Confirm / Cancel / Clear；EdgeSelect=true 显示入口，false 显示操作钮（基类 EdgeSelect 属性）。
     * - SingleFace：单 HpImageViewer；RequiresInner/Outer 按 FocusBasedOn/DoubleGrab；快照/十字/Confirm 均走主 viewer。
     * - DoubleFace：Inner/Outer 各一个 compare viewer；RequiresInner/Outer 恒 true；快照/十字/Confirm 分 viewer 读写。
     * - CrossFov：单 HpImageViewer（同 SingleFace）；RequiresInner/Outer 按 Measurement 页 FocusBasedOn 只显示当前层；
     *   SetCdOvlCompensation → 仅当前层可拖十字；Confirm → 写回边点并重建 Kernel；Cancel → 恢复快照；Clear → 清空边点。
     */

    private HObject _crossFovLastImgInner;
    private HObject _crossFovLastImgOuter;

    /// <summary>CrossFov 仅按 Measurement 页 Inner/Outer 单选显示对应层补偿十字。</summary>
    protected override bool RequiresInnerCdOvlEdgeForDisplay() =>
        FocusBasedOn == EnumFocusBasedOn.Inner;

    protected override bool RequiresOuterCdOvlEdgeForDisplay() =>
        FocusBasedOn == EnumFocusBasedOn.Outer;

    /// <summary>Run 后切换 Inner/Outer 单选时刷新当前层十字与 ROI。</summary>
    protected override void OnFocusBasedOnChangedCore(EnumFocusBasedOn value)
    {
        if (TestInfo?.CrossFov != true || !IsTrained)
            return;

        RefreshCrossFovMeasurementOverlay(IsCdOvlCrossEditMode, includeMeasureRois: EdgeSelect);
    }

    private bool IsCrossFovCdOvlMeasure() =>
        TestInfo?.CrossFov == true && SupportsCdOvlEdgeCompensation();

    /// <summary>CrossFov Cancel 时若无快照，恢复当前层 ROI + 锁定十字（SingleFace 单 viewer 逻辑）。</summary>
    protected override bool TryRestoreCdOvlSnapshotBeforeEdgeOnlyMode()
    {
        if (TestInfo?.CrossFov != true)
            return base.TryRestoreCdOvlSnapshotBeforeEdgeOnlyMode();

        if (CdOvlRoiSnapshotBeforeEdgeOnlyMode is { Count: > 0 })
            return base.TryRestoreCdOvlSnapshotBeforeEdgeOnlyMode();

        RefreshCrossFovMeasurementOverlay(false, includeMeasureRois: true);
        ClearCdOvlRoiSnapshot();
        return false;
    }

    protected override void RefreshCdOvlCrosshair(bool isMovable)
    {
        if (TestInfo?.CrossFov == true)
        {
            RefreshCrossFovMeasurementOverlay(isMovable, includeMeasureRois: false);
            return;
        }

        base.RefreshCdOvlCrosshair(isMovable);
    }

    protected override void RestoreCdOvlFullOverlayAndCrosses(bool crossesMovable)
    {
        if (TestInfo?.CrossFov == true)
        {
            RefreshCrossFovMeasurementOverlay(crossesMovable, includeMeasureRois: true);
            ClearCdOvlRoiSnapshot();
            return;
        }

        base.RestoreCdOvlFullOverlayAndCrosses(crossesMovable);
    }

    protected override void RebuildCdOvlViewerMeasureRoisOnly()
    {
        if (TestInfo?.CrossFov == true)
        {
            RefreshCrossFovMeasurementOverlay(false, includeMeasureRois: true);
            return;
        }

        base.RebuildCdOvlViewerMeasureRoisOnly();
    }

    private void RefreshCrossFovMeasurementOverlay(bool crossesMovable, bool includeMeasureRois)
    {
        if (!IsCrossFovCdOvlMeasure())
            return;

        var isInner = FocusBasedOn == EnumFocusBasedOn.Inner;
        var rois = new List<ROIDesc>();

        if (includeMeasureRois)
        {
            var img = isInner ? _crossFovLastImgInner : _crossFovLastImgOuter;
            if (img != null)
            {
                rois.AddRange(GetProfileForImageView(
                    img,
                    GetCrossFovOmRoi(isInner),
                    GetCrossFovTargetType(isInner)));
            }

            var measureRoi = isInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
            if (measureRoi != null)
                rois.Add(measureRoi);
        }

        AppendCdOvlCrosshair(rois, crossesMovable);
        HpImageViewer?.UpdateROIs(rois.ToArray());

        var displayImg = isInner ? _crossFovLastImgInner : _crossFovLastImgOuter;
        if (displayImg != null)
            HpImageViewer.Image = displayImg.ToImageData();
    }

    private OMROIBase GetCrossFovOmRoi(bool isInner) =>
        (IsDualCameraCrossFov, isInner) switch
        {
            (true, true) => TestInfo.OMParam_Double.OmROIInner,
            (true, false) => TestInfo.OMParam_Double.OmROIOuter,
            (false, true) => TestInfo.OMParam.OmROIInner,
            _ => TestInfo.OMParam.OmROIOuter,
        };

    private OMTargetType GetCrossFovTargetType(bool isInner) =>
        (IsDualCameraCrossFov, isInner) switch
        {
            (true, true) => TestInfo.OMParam_Double.TargetTypeInner,
            (true, false) => TestInfo.OMParam_Double.TargetTypeOuter,
            _ => TestInfo.OMParam.TargetType,
        };

    private HTuple CreateCrossFovOmParamTuple() =>
        IsDualCameraCrossFov
            ? CreateOmParamTuple(TestInfo.OMParam_Double)
            : CreateOmParamTuple(TestInfo.OMParam);

    private static HTuple CreateOmParamTuple(OMParam omParam)
    {
        omParam.ConvertToDict(out HTuple hTuple);
        return hTuple;
    }

    private static HTuple CreateOmParamTuple(OMParam_Double omParam)
    {
        omParam.ConvertToDict(out HTuple hTuple);
        return hTuple;
    }

    #endregion

    /// <summary>将算法生成的 OM ROI 与 Kernel 模型写入当前 Test 的 <see cref="OMParam"/>（内/外分别字段）。</summary>
    protected override void SaveKernelModel(bool isInner, OMROIBase oMROIBase, OMKernelModel oMKernelModel)
    {
        switch (IsDualCameraCrossFov, isInner)
        {
            case (true, true):
                TestInfo.OMParam_Double.OmROIInner = oMROIBase;
                TestInfo.OMParam_Double.KernelModelInner = oMKernelModel;
                break;
            case (true, false):
                TestInfo.OMParam_Double.OmROIOuter = oMROIBase;
                TestInfo.OMParam_Double.KernelModelOuter = oMKernelModel;
                break;
            case (false, true):
                TestInfo.OMParam.OmROIInner = oMROIBase;
                TestInfo.OMParam.KernelModelInner = oMKernelModel;
                break;
            case (false, false):
                TestInfo.OMParam.OmROIOuter = oMROIBase;
                TestInfo.OMParam.KernelModelOuter = oMKernelModel;
                break;
        }
    }

    /// <summary>
    /// 量测完成后：停采流、按抓取模式叠加轮廓与量测 ROI、切换显示图像、加载数值结果；
    /// CD OVL 时叠加可拖十字并缓存原始中心/Misreg 供界面补偿。
    /// </summary>
    protected override void ShowResult(OvResultBase ovResult, HObject imgObjectInner, HObject imgObjectOuter)
    {
        _visionService.StopGrabbing();
        _crossFovLastImgInner = imgObjectInner;
        _crossFovLastImgOuter = imgObjectOuter;

        var hTuple = CreateCrossFovOmParamTuple();
        OvResultBase centeringOVL = ovResult;
        if (TestInfo.TargetSetting.TargetCentering == EnumCalOverlayInOrOutEdge.Inner)
        {
            centeringOVL = (OvResultBase)CloneHelper.ReflectionClone(ovResult);
        }

        CorrelationCurvesList = GetCorrelationCurve(imgObjectInner, imgObjectOuter, hTuple);

        HpImageViewer.FitImageToArea();
        SetFocusPredictionTabEnabled();
        LoadOvResult(centeringOVL);
        CaptureRawCdOvlResult(centeringOVL);
        RefreshCrossFovMeasurementOverlay(IsCdOvlCrossEditMode, includeMeasureRois: EdgeSelect);
        HpImageViewer.FitImageToArea();
        //ShowCurveControl();
    }
    [ObservableProperty]
    bool _isFocusPredictionTabEnabled;


    protected TrainSettings40_CrossFov CrossFovCameraSettings =>
        TestInfo?.TrainSetting_CrossFov;

    private void SetFocusPredictionTabEnabled()
    {
        IsFocusPredictionTabEnabled = TrainSetting?.IsTrained ?? false;
    }

    /// <summary>
    /// 进入 EdgeSelect 编辑态前，分别保存 inner/outer compare viewer 当前 ROI。
    /// </summary>
    protected override void CaptureCdOvlRoiSnapshotForRestore()
    {
        var isInner = FocusBasedOn == EnumFocusBasedOn.Inner;
        var rois = new List<ROIDesc>();
        var img = isInner ? _crossFovLastImgInner : _crossFovLastImgOuter;
        if (img != null)
        {
            rois.AddRange(GetProfileForImageView(
                img,
                GetCrossFovOmRoi(isInner),
                GetCrossFovTargetType(isInner)));
        }

        var measureRoi = isInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
        if (measureRoi != null)
            rois.Add(measureRoi);
        if (isInner)
        {
            _cdOvlInnerRoiSnapshotBeforeEdgeOnlyMode = rois;
        }
        else
        {
            _cdOvlOuterRoiSnapshotBeforeEdgeOnlyMode = rois;
        }
    }

    /// <summary>
    /// 从 inner compare viewer 读取确认后的内层补偿十字位置。
    /// </summary>
    protected override List<Point2d> GetConfirmedInnerCdOvlCompensationPoints()
    {
        var isInner = FocusBasedOn == EnumFocusBasedOn.Inner;
        if (isInner)
        {
            return GetConfirmedCdOvlCompensationPoints(HpImageViewer, InnerCompCrossColor, "Inner");
        }
        else
        {
            return InneredgeShowRois.ToList();
        }
    }

    /// <summary>
    /// 从 outer compare viewer 读取确认后的外层补偿十字位置。
    /// </summary>
    protected override List<Point2d> GetConfirmedOuterCdOvlCompensationPoints()
    {
        var isInner = FocusBasedOn == EnumFocusBasedOn.Inner;
        if (!isInner)
        {
            return GetConfirmedCdOvlCompensationPoints(HpImageViewer, OuterCompCrossColor, "Outer");
        }
        else
        {
            return OuteredgeShowRois.ToList();
        }

    }
}
