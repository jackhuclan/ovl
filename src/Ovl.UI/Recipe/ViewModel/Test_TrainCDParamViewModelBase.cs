using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;
using ImTools;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Enums;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.SystemConfig.Interface;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using InteVega.CBB.TemplateMatchingModule.Views;
using Marvel.Application.Extentions;
using Marvel.DataModels.Config;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Extensions;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.CD;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using SqlSugar;
using Wafer.Algorithm.Common.DoNet;
using WinCopies.Util;

using Ovl.UI.Extensions;
using Ovl.UI.Recipe.View;

namespace Ovl.UI.Recipe.ViewModel;


public partial class Test_TrainCDParamViewModelBase : Test_TrainViewModelBase<TestInfo_CD>
{
    [ObservableProperty]
    private double autoFocusHeight;
    [ObservableProperty]
    private double focusZHeight;
    [ObservableProperty]
    private double focusPztOffset;
    [ObservableProperty]
    private bool focusROI_SetEnabled;
    [ObservableProperty]
    private bool focusROI_ConfirmEnabled;
    [ObservableProperty]
    private bool focusROI_CancelEnabled;
    [ObservableProperty]
    private bool _measurementROI_Enabled;
    [ObservableProperty]
    private List<FeatureType> featureTypes;
    [ObservableProperty]
    private List<ModelMethodType> modelMethodTypes;
    [ObservableProperty]
    private TrainSettings_CD trainSetting_CD;
    [ObservableProperty]
    private string _testValue;
    /// <summary>
    /// 是否跨视野
    /// </summary>
    [ObservableProperty]
    private bool isAcrossTheCameraVision;
    /// <summary>
    /// 是否显示ROI选择，DS非跨视野下可用
    /// </summary>
    [ObservableProperty]
    private bool isShowSelectROI = false;
    [ObservableProperty]
    private ObservableCollection<EnumROIType> rOITypeList;
    //[ObservableProperty]
    //private ObservableCollection<TurrentMeta> _turrentMetaList;
    //[ObservableProperty]
    //private TurrentMeta _turrentMeta;
    [ObservableProperty]
    private Visibility _correctionVisibility = Visibility.Collapsed;
    [ObservableProperty]
    private bool _isCorrectionEnabled;
    [ObservableProperty]
    private ObservableCollection<MultiRoiInfo> _multiRoiList;
    [ObservableProperty]
    private double _correctionHeight = double.NaN;
    [ObservableProperty]
    private double _correctionValue;
    [ObservableProperty]
    private ObservableCollection<CD_Orientention> _cDOrientention;
    /// <summary>
    /// 只有OM40
    /// </summary>
    [ObservableProperty]
    private Visibility _isShowTurrent = Visibility.Collapsed;

    public SingleTemplateControl TemplateControl { get; set; }
    /// <summary>
    /// 全行程聚焦结果
    /// </summary>
    protected FocusPegResult fine_Res;

    protected bool isTrainedFocus;

    IAlignmentService alignmentService;


    public IVisionService _StageController;

    /// <summary>
    /// Physical参数
    /// </summary>
    public Physical_CD PhysicalParam => TestInfo.Physical as Physical_CD;
    public EnumVision Vision = EnumVision.Vision1;


    public const string _messageBoxToken = "TestTrainCDMessage";
    protected string FocusImageName = "";
    protected string MeasureImageName = "MeasureMatch";
    /// <summary>
    /// 重新聚焦需要重新创建定位模板
    /// </summary>
    public bool IsNeedCreateModel = false;
    public readonly WorkFlowConfig _workCfg;


    protected Action<string> _changeCameraInfoAction;
    protected CDCameraType _cameraType;
    IMeasurementService _measService;
    public Test_TrainCDParamViewModelBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        FeatureTypes = new List<FeatureType>() { FeatureType.Enhance, FeatureType.Normal };
        ModelMethodTypes = new List<ModelMethodType>() { ModelMethodType.Shape, ModelMethodType.Ncc };

        ROITypeList = new ObservableCollection<EnumROIType>() { EnumROIType.Rectangle, EnumROIType.Circle };

        _workCfg = serviceProvider.GetRequiredService<IConfigLoader>().LoadConfig<WorkFlowConfig>();
        alignmentService = _serviceProvider.GetRequiredService<IAlignmentService>();
        _measService = serviceProvider.GetService<IMeasurementService>();
        CDOrientention = new ObservableCollection<CD_Orientention>() { CD_Orientention.Vertical, CD_Orientention.Horizontal, CD_Orientention.User_Defined };
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
    public void LoadData(IHPImageViewer hPImageViewer, IVisionService visionService,
        TestInfo_CD testInfo, EnumVision vision, string testFilesFolder, Action<string> ChangeCameraInfoAction)
    {
        FocusImageName = $"focus_{vision}";
        MeasureImageName = $"MeasureMatch_{vision}";
        TestInfo = testInfo;
        //判断当前是第一视野还是第二视野
        TrainSetting_CD = vision == EnumVision.Vision1 ? testInfo.TrainSetting_CD : testInfo.TrainSettingSecond_CD;
        TrainSetting = TrainSetting_CD;
        TestFilesFolder = testFilesFolder;
        _StageController = visionService;
        Vision = vision;
        IsCorrectionEnabled = false;
        CorrectionValue = TrainSetting_CD.FocusCorrection;
        HpImageViewer = hPImageViewer;
        SetButtonEnabled(true, false, false);
        InitMeasurementRoiButton();

        ////加载转塔    *****************************临时注释
        //if (IsShowTurrent == Visibility.Visible)
        //{
        //    //LoadTurrent();
        //}

        ////临时指定X20 *****************************后面删除
        //TurrentMeta = new TurrentMeta();
        //TurrentMeta.Uid = (int)EnumTeleconverter‌.X20;


        WliVisible = (MarvelVersion.IsSupportWLI && TestInfo.Physical.FocusSettings.IsUseWLI) ? Visibility.Visible : Visibility.Collapsed;

        _StageController.StartGrabbing();


        if (((Physical_CD)TestInfo.Physical).TestSettings is TestPhysical_LargeCD testSetting)
        {
            IsAcrossTheCameraVision = testSetting.IsAcrossTheCameraVision;
            _cameraType = testSetting.CameraType;
            if (testSetting.IsAcrossTheCameraVision != testSetting.IsAcrossTheCameraVision_Old)
            {
                TrainSetting_CD.ROIDescs = null;
                testSetting.IsAcrossTheCameraVision_Old = testSetting.IsAcrossTheCameraVision;
            }
        }
        if (((Physical_CD)TestInfo.Physical).TestSettings is TestPhysical_LargeCD100 testSetting100)
        {
            IsAcrossTheCameraVision = testSetting100.IsAcrossTheCameraVision;
            _cameraType = testSetting100.CameraType;
            if (testSetting100.IsAcrossTheCameraVision != testSetting100.IsAcrossTheCameraVision_Old)
            {
                TrainSetting_CD.ROIDescs = null;
                testSetting100.IsAcrossTheCameraVision_Old = testSetting100.IsAcrossTheCameraVision;
            }
        }
        if (((Physical_CD)TestInfo.Physical).TestSettings is TestPhysical_LargeCD40 testSetting40)
        {
            IsAcrossTheCameraVision = testSetting40.IsAcrossTheCameraVision;
            _cameraType = CDCameraType.Measure;
            if (testSetting40.IsAcrossTheCameraVision != testSetting40.IsAcrossTheCameraVision_Old)
            {
                TrainSetting_CD.ROIDescs = null;
                testSetting40.IsAcrossTheCameraVision_Old = testSetting40.IsAcrossTheCameraVision;
            }
        }

        if (TrainSetting_CD.Type == EnumTargetType.DS)
        {
            IsShowSelectROI = true;
        }
        if (_cameraType != CDCameraType.Align)
        {
            CorrectionVisibility = TestInfo.Physical.FocusSettings.IsEnableFocusCorrection ? Visibility.Visible : Visibility.Collapsed;
        }
        _changeCameraInfoAction = ChangeCameraInfoAction;

        if (TrainSetting_CD.FocusModuleInfo.BaseFocusHeight == 0)
        {
            TrainSetting_CD.FocusModuleInfo.BaseFocusHeight = _alignmentService.WaferMapInfo.Thickness;
        }
        BindTemplateControl();

        LoadCDMultiRoi();
        //AreaROIEnabled = false;
    }


    #region 定位模板
    /// <summary>
    /// 绑定定位模板
    /// </summary>
    private void BindTemplateControl()
    {
        if (TrainSetting_CD.MarkModels == null)
        {
            TrainSetting_CD.MarkModels = new List<SingleTemplateMatchingContext>() { new() };
            TrainSetting_CD.MarkModels[0].Name = $"MeasureModel_{Vision}";
        }
        TemplateControl.TemplateContext = TrainSetting_CD.MarkModels.FirstOrDefault();
        TemplateControl.ActionUserModified = TemplateUserModified;
        TemplateControl.FuncBeforeCreateModel = TemplateBeforeCreateModel;
        TemplateControl.ImageViewer = HpImageViewer;
    }


    internal void TemplateUserModified(SingleTemplateControl control, ITemplateMatchingView.UserModifyType type, SingleTemplateMatchingContext context, bool isSuccess)
    {
        if (!isSuccess) return;
        if (type == ITemplateMatchingView.UserModifyType.ModelCreated)
        {
            TrainSetting_CD.TrainMatchItem = context.MatchingItems[0];
            TrainSetting_CD.ModelROI = context.ROI.CreateRoiDesc();
            //创建量测定位模板
            Task.Run(() => CreateFocusOffsetTemplate());

        }
        if (type == ITemplateMatchingView.UserModifyType.ModelCleanUp)
        {
            HpImageViewer.Image = LoadFocusImageData(true);
        }
        IsNeedCreateModel = false;
    }
    internal bool TemplateBeforeCreateModel()
    {
        if (MultiRoiList.Count > 0 && MultiRoiList.Any(m => m.MeasureROI == null))
        {
            _messageBoxService.ErrorDialog("Please create measurement roi!");
            return false;
        }
        var imageData = LoadFocusImageData(true);
        HpImageViewer.Image = imageData;
        if (HpImageViewer.Image == null)
        {
            return false;
        }
        var roiDesc = HpImageViewer.ImageViewerDesc.ROIDescs;
        if (roiDesc == null)
        {
            _messageBoxService.InfoDialog("Please draw the corresponding ROI！");
            return false;
        }
        _visionService.StopGrabbing();
        return true;
    }
    #endregion

    #region 事件
    #region 测试代码
    [RelayCommand]
    public void SavePoint()
    {
        TrainSetting_CD.StageAbsolutePoint = _alignmentService.GetCurrentAxisPosition();
    }
    [RelayCommand]
    public void MovePoint()
    {
        _alignmentService.MoveTo(TrainSetting_CD.StageAbsolutePoint, "cd_vision");
    }
    #endregion
    [RelayCommand]
    public void FocusSettingCD()
    {
        Action<FocusInfo> action = new Action<FocusInfo>(SaveCDActionMethod);
        var parameters = new DialogParameters
        {
            { "FocusInfo",TrainSetting_CD.FocusModuleInfo.FocusInfo},
            { "SaveAction", action }
        };
        AppHelper.DispatcherInvoke(() =>
        {
            _dialogService.ShowDialog(nameof(Test_FocusTrainSettingDialog), parameters, null);
        });
    }
    protected void SaveCDActionMethod(FocusInfo focusInfo)
    {
        TrainSetting_CD.FocusModuleInfo.FocusInfo = focusInfo;
    }
    #region Focus Code---------------------------------------------
    /// <summary>
    /// 执行聚焦
    /// </summary>
    [RelayCommand]
    private void TrainFocus()
    {
        try
        {

            IsNeedCreateModel = true;
            TestInfo.TrainSetting.IsTrained = false;
            LogHelper.LogInfo($"Reset IsTrained,testName={TestInfo?.Name}");
            TrainSetting_CD.StageAbsolutePoint = null;
            TrainSetting_CD.StagePosFovPoint = null;
            IsCorrectionEnabled = false;
            TrainFocusAction();
            TemplateControl.CleanUpData();
            //保存当前坐标位置
            Point2d point2D = _alignmentService.GetCurrentAxisPosition();
            TrainSetting_CD.StageAbsolutePoint = point2D;
            TrainSetting_CD.StagePosFovPoint = _alignmentService.GetWcsPoint(point2D);
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
            _messageBoxService.Error("Failed to train focus .", _messageBoxToken);
        }
    }

    /// <summary>
    /// Focus ROI Set 操作
    /// </summary>
    [RelayCommand]
    private void FocusROI_Set()
    {
        //string imgPath = "D:\\K_AIM-bmp\\BarInBar-bmp\\BarInBar-bmp\\1.bmp";
        //HpImageViewer.Image = ImageDataExtensions.ReadImage(imgPath);
        if (HpImageViewer.Image == null)
        {
            _messageBoxService.InfoDialog("The current image data is invalid！");
            return;
        }
        SetTempFocusRoi();
        SetButtonEnabled(false, true, true);
        return;
    }


    /// <summary>
    /// Focus ROI 保存
    /// </summary>
    [RelayCommand]
    private void FocusROI_Confirm()
    {
        var curRoiDescsNew = new List<RectangleDesc>();
        try
        {
            curRoiDescsNew = HpImageViewer.ImageViewerDesc.ROIDescs.Cast<RectangleDesc>().ToList();
        }
        catch
        {

        }
        if (!(curRoiDescsNew?.Any() ?? false))
        {
            _messageBoxService.ErrorDialog("None Roi, Please 'Cancel' and 'Set' to Create ROI!", "Error");
            return;
        }
        TrainSetting_CD.FocusRoiDesc = curRoiDescsNew;
        HpImageViewer.UpdateROIs();
        SetButtonEnabled(true, false, false);

    }
    /// <summary>
    /// Focus ROI cancel
    /// </summary>
    [RelayCommand]
    private void FocusROI_Cancel()
    {
        SetButtonEnabled(true, false, false);
        HpImageViewer.UpdateROIs(null);
    }

    [RelayCommand]
    private void ShowCorrectionValueSettingView()
    {
        if (!IsCorrectionEnabled)
        {
            _messageBoxService.ErrorDialog("Please complete the drawing of the measurement ROI!", "Error");
            return;
        }
        _visionService.StartGrabbing();
        CheckTargetPosition();
        var dialogService = _serviceProvider.GetService<IDialogService>();
        var parameters = new CorrectionValueParam()
        {
            IsInner = true,
            FocusPosition = GetRoiFocusPosition(),
            CorrectionValue = CorrectionValue,
            Unit = "μm",
            Curve = TrainSetting_CD.RoiCurve,
            CorrectionPointSelectedAction = Vm_CorrectionPointSelected,
            SetFinalHeightAction = GrabCorrectionImageActionMethod,
            HightLightThreshHold = CreateFineTrainInfo().Step / 2
        };
        var dialogParameters = new DialogParameters
        {
            { nameof(CorrectionValueParam), parameters }
        };
        dialogService.ShowDialog(nameof(CorrectionValueSettingDialogView), dialogParameters, _ =>
        {
            parameters.CorrectionPointSelectedAction -= Vm_CorrectionPointSelected;
        });
    }

    /// <summary>
    /// 获取Roi聚焦焦面位置
    /// </summary>
    /// <returns></returns>
    protected virtual double GetRoiFocusPosition()
    {
        return FocusPztOffset;
    }

    /// <summary>
    /// 保存Roi聚焦焦面位置
    /// </summary>
    /// <param name="roiFocusOffset"></param>
    protected virtual void SetRoiFocusPostion(double roiFocusOffset)
    {
        FocusPztOffset = roiFocusOffset;
    }

    protected virtual void Vm_CorrectionPointSelected(bool isInner, double focusPos)
    {
        _pztservice.AMov(focusPos);
    }

    private void GrabCorrectionImageActionMethod(bool isInner, double finalHeight, double correctionValue)
    {
        CorrectionHeight = finalHeight;
        CorrectionValue = correctionValue;// 此InnerCorrectionValue会在run时重新设置
        _visionService.StartGrabbing();
        CheckTargetPosition();
        ChangeCameraInfo = "Correcting Focus ...";
        Task.Run(() =>
        {
            try
            {
                SaveFocusImage(MeasureImageName, null);
                SaveFocusCorrection();
            }
            catch (Exception ex)
            {
                _messageBoxService?.ErrorDialog(ex.Message);
            }
            finally
            {
                ChangeCameraInfo = "";
            }
        });


    }


    /// <summary>
    /// Focus offset 人工补偿后采图
    /// </summary>
    /// <param name="isInner"></param>
    /// <returns></returns>
    private void SaveFocusCorrection()
    {
        //是否完成量测聚焦
        if (!IsCorrectionEnabled) return;
        //如果开启了手动焦面补偿，则采集补偿后的焦面作为量测原图，并以此图来创建量测的定位模板
        if (TestInfo.Physical.FocusSettings.IsEnableFocusCorrection)
        {
            if (CorrectionVisibility != Visibility.Visible)
                return;
            // CorrectionValue = CorrectionHeight - TrainSetting_CD.PztOffset;
            CorrectionValue = CorrectionHeight - GetRoiFocusPosition();
            TrainSetting_CD.FocusCorrection = CorrectionValue;

            LogHelper.LogInfo($"Move to layer correction pztOffset={GetRoiFocusPosition()},FocusCorrection={CorrectionValue}");
            return;
        }
    }


    #endregion

    #region Measurement Code

    /// <summary>
    /// 采图前检查标记是否有发生移动
    /// </summary>
    protected void CheckTargetPosition()
    {
        var currentAxis = _alignmentService.GetCurrentAxisPosition();
        var targetPosition = TrainSetting_CD.StageAbsolutePoint;
        LogHelper.LogInfo($"Current axis: X={currentAxis.X},Y={currentAxis.Y},TrainPosition：X={targetPosition.X},Y={targetPosition.Y}");
        if (!currentAxis.Equals(targetPosition))
        {
            _alignmentService.MoveTo(targetPosition, "TrainSetting_CD StageAbsolutePoint");
        }
    }
    /// <summary>
    /// 量测ROI聚焦
    /// </summary>
    /// <returns></returns>
    protected bool FocusByMeasureRoi()
    {

        _visionService.StartGrabbing();//启动采集
        if (fine_Res != null)
        {
            LogHelper.LogRunMethodStart();
            CheckTargetPosition();
           var roiFocusOffset = TrainRoiFocus(fine_Res, true);
            SetRoiFocusPostion(roiFocusOffset);
            //量测ROI聚焦完成，释放偏移设置
            if (CorrectionVisibility == Visibility.Visible)
            {
                IsCorrectionEnabled = true;
                //第一次ROI聚焦时，自动计算补偿焦面的高度=roi焦面+偏移量
                if (double.IsNaN(CorrectionHeight))
                {
                    CorrectionHeight = GetRoiFocusPosition() + CorrectionValue;
                }
                else
                {
                    //从第二次ROI聚焦开始，每次ROI聚焦后，补偿焦面高度不变 ，更新Correction值
                    SaveFocusCorrection();
                }
                MoveAndWait(null, CorrectionHeight);
                SaveFocusImage(MeasureImageName, null);
            }
            HpImageViewer.Image = LoadFocusImageData(true);
            LogHelper.LogRunMethodEnd();
            return true;
        }
        return false;
    }



    public AutoRunInfo CreateFineTrainInfo()
    {
        var iCfg = _serviceProvider.GetService<ISystemConfigService>();
        var lCfg = iCfg.GetConfig<FocusCfg>();
        var measCfg = lCfg.MeasPztMeter;
        return new AutoRunInfo() { Range = measCfg.AllRange, Step = measCfg.AllRangeStep, SmallRange = 0 , FPS = measCfg.CreateFinalFps() };
    }

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
    /// <summary>
    /// 切换转塔
    /// </summary>
    [RelayCommand]
    private void SelectTurrentChanged()
    {

    }
    #endregion

    #region Focus ROI 设置
    /// <summary>
    /// 设置ROI
    /// </summary>
    private void SetTempFocusRoi()
    {

        if (HpImageViewer.Image == null)
        {
            _messageBoxService.InfoDialog("The current image data is invalid！");
            return;
        }
        ROIDesc roi = TrainSetting_CD.FocusRoiDesc.GetEnumerableValue(0);
        if (roi != null)
            HpImageViewer.UpdateROIs((RectangleDesc)CloneHelper.ReflectionClone(roi));
        else
        {
            var (l, t, w, h) = GetROISize();
            HpImageViewer.UpdateROIs(new RectangleDesc() { Left = l, Top = t, Width = w, Height = h });
        }
    }
    public (double, double, double, double) GetROISize()
    {
        if (HpImageViewer.Image == null)
        {
            _messageBoxService.InfoDialog("The current image data is invalid！");
        }

        double width = 200;
        double height = 200;
        var top = (((ImageData)HpImageViewer.Image).Height - height) / 2;
        var left = (((ImageData)HpImageViewer.Image).Width - width) / 2;
        if ((TrainSetting_CD?.MarkModels != null) && (TrainSetting_CD?.MarkModels.Any() ?? false))
        {
            var roi = (Rect2d)TrainSetting_CD?.MarkModels.GetEnumerableValue(0)?.ROI ?? null;
            if (roi != null)
            {
                if (roi.X > 0 && roi.Y > 0)
                {
                    left = roi.X;
                    top = roi.Y;
                    width = roi.Width;
                    height = roi.Height;
                }
            }
        }
        return (left, top, width, height);
    }
    #endregion
    #region focus 粗聚焦和精聚焦   #########
    /// <summary>
    /// 训练白光干涉聚焦高度
    /// </summary>
    protected void TrainWLI(string ext)
    {
        if (IsEnableWLI)
        {
            _visionService.StartGrabbing();
            AutoRunInfo interfFocusInfo = _wliService.GetSystemInterfMeter();
            string wliFile = Marvel.Application.Extentions.Extensions.GetWLIFile(TestFilesFolder, ext);
            wliPztOffset = _wliService.TrainWliFocus(interfFocusInfo, TestInfo.Physical.FocusSettings.FocusMode, wliFile);
        }
    }


    /// <summary>
    /// 聚焦
    /// </summary>
    /// <param name="coarseFocusAction">如果是量测相机返回不带roi的粗聚焦训练+粗聚焦小步进方法  如果是Align相机返回带roi的粗聚焦方法+粗聚焦小步进方法</param>
    /// <param name="fineAutoFocusAction"> 如果是量测相机返回pzt精聚焦方法 如果是对准相机返回空，对准相机CD量测不需要精聚焦建模动作</param>
    protected virtual void TrainFocusAction()
    {

    }
    public FocusInfo GetTestFocusOrDefault_new(ECamera camera = ECamera.Meas)
    {
        return TrainSetting_CD.FocusModuleInfo.FocusInfo ?? _serviceProvider.GetService<ITestService>().GetSystemFocusInfo(camera);

    }




    /// <summary>
    /// 全视野聚焦，并训练聚焦曲线
    /// </summary>
    /// <returns></returns>
    public virtual FocusPegResult TrainGlobal()
    {
        var trainArg = CreateTrainArg(EnumGrabMode.SingleGrab);
        var allRangeFocusInfo = CreateFineTrainInfo();
        var res = _focusService.FineFocusTrain(allRangeFocusInfo, trainArg, out var maxSharpness);
        TrainSetting_CD.GlobalCurve = res.Curve;//添加全视野曲线
        return res;


    }
    /// <summary>
    /// 训练量测ROI聚焦并保存焦面图
    /// 当时量测聚焦时，同时保存预匹配焦面图
    /// 临时聚焦和量测ROI聚焦时使用
    /// </summary>
    /// <param name="focusPegResult"></param>
    /// <param name="roiDesc"></param>
    /// <param name="isMeasureRoi"></param>
    /// <returns></returns>
    protected virtual double TrainRoiFocus(FocusPegResult focusPegResult, bool isMeasureRoi)
    {
        List<ROIDesc> roiDescList = new List<ROIDesc>();
        ShapeBase shapeBase = null;
        if (isMeasureRoi)
        {
            roiDescList = MultiRoiList.Select(s => s.MeasureROI).ToList();
            shapeBase = MultiRoiList.CreateMultiDimensionROI(IsAcrossTheCameraVision).ToDimension2d();
        }
        else
        {
            roiDescList = TrainSetting_CD.FocusRoiDesc.ToList();
            shapeBase = roiDescList.FirstOrDefault().CreateRoi();
        }
        HpImageViewer.UpdateROIs(roiDescList.ToArray());
        var fineFocusInfo = GetTestFocusOrDefault_new(IServiceCamera.CurCamera).FineFocusInfo;
        var focusImages = focusPegResult.PegImages;
        var trainArg = CreateTrainArg(EnumGrabMode.SingleGrab);
        //使用全视野的聚焦序列图进行内、外层聚焦，并且获取聚焦曲线
        var ret = _focusService.TrainCurve(fineFocusInfo, shapeBase, focusImages, trainArg);
        TrainSetting_CD.RoiCurve = ret.Curve;
        var pztOffset = ret.Curve.RefMaximumZ.First();
        MoveAndWait(null, pztOffset);
        SaveFocusImage(FocusImageName, shapeBase, isMeasureRoi);
        //创建量测模板用图
        SaveFocusImage(MeasureImageName, null);

        //此处不需要考虑有没有焦面偏移，只有在焦面偏移值发生变更的时候
        //if (!(TrainSetting_CD.FocusCorrection != 0 && CorrectionVisibility == Visibility.Visible && isMeasureRoi))
        //{
        //    MoveAndWait(pztOffset);
        //    SaveFocusImage(FocusImageName, shapeBase, isMeasureRoi);
        //}
        //else
        //{
        //    MoveAndWait(pztOffset + TrainSetting_CD.FocusCorrection);
        //}

        return pztOffset;
    }

    protected virtual void MoveAndWait(double? focusZHeight, double? pztHeight)
    {
        throw new NotImplementedException();
        //Task.Delay(2000).Wait();
    }

    protected ECamera GetTestCamera()
    {
        return PhysicalParam.TestSettings.GetECamera();
    }
    /// <summary>
    /// 检查聚焦参数是否有效
    /// </summary>
    /// <returns></returns>
    protected bool CheckFocusParam()
    {
        if (TrainSetting_CD.FocusRoiDesc == null)
        {
            _messageBoxService.WarningDialog("The focusing action has not been completed!");
            return false;
        }
        return true;


    }
    /// <summary>
    /// 预匹配
    /// 量测ROI聚焦后，执行预匹配，创建量测模板
    /// </summary>
    /// <returns></returns>
    private async Task CreateFocusOffsetTemplate()
    {
        //偏移后的模板
        var image = LoadFocusImageData(false);
        if (image == null)
        {
            return;
        }
        //此处需要保存全图的清晰度
        var paramFocus = GetTestFocusOrDefault_new(IServiceCamera.CurCamera).CloneFineParamFocus();
        var sharpness = _focusService.GetImageClarity(image, paramFocus: paramFocus);

        TrainSetting_CD.FocusModuleInfo.Sharpness = sharpness;

        if (fine_Res != null)
        {
            var zFit = fine_Res.ZFit;
            TrainSetting_CD.FocusModuleInfo.GlobalFocusOffset = zFit - GetRoiFocusPosition();//FocusPztOffset - fine_Res.ZFit;
        }

        //点击Confirm按钮时，重绘量测Roi，避免被其它roi影响
        var roiDesc = TrainSetting_CD.ModelROI;
        if (roiDesc == null)
        {
            _messageBoxService.InfoDialog("Please draw the corresponding ROI！");
            return;
        }
        var cdROI = roiDesc.CreateRoi();
        var roiSharpness = _focusService.GetImageClarity(image, cdROI, paramFocus);
        TrainSetting_CD.FocusModuleInfo.RoiSharpness = roiSharpness;

        _visionService.StopGrabbing();
        try
        {
            HpImageViewer.Image = image;
            HpImageViewer.UpdateROIs(roiDesc);
            var previewTemplate = TrainSetting_CD.MarkModels[0];
            previewTemplate = (SingleTemplateMatchingContext)CloneHelper.ReflectionClone(previewTemplate);
            previewTemplate.MainTemplateName = "";
            previewTemplate.Name = $"PreviewModel_{Vision}";
            TrainModelViewModel trainModelVM = new TrainModelViewModel(_serviceProvider, previewTemplate, false, () => HpImageViewer, false);
            await trainModelVM.TrainModelAsync(previewTemplate.SelectedMatchModelGroup.MatchModel.ParamMatch.MinScore);
            TrainSetting_CD.PreviewTemplate = previewTemplate;
        }
        finally
        {
            HpImageViewer.Image = LoadFocusImageData(true);
        }
    }
    #endregion

    #region 量测ROI创建，查询，保存    ##########
    /// <summary>
    /// 打开量测ROI
    /// </summary>
    /// <param name="isEdge">是否是边缘ROI</param>
    /// <param name="cD_Orientention">CD方向</param>
    /// <param name="rOIDescs">加载的ROI，重置设为null</param>
    protected bool SetMeasureRoi(CD_Orientention cD_Orientention, List<ROIDesc> rOIDescs)
    {
        if (rOIDescs == null || rOIDescs.Count <= 0 || rOIDescs[0] is not RotatedPickersDesc)
        {
            List<ROIDesc> roiList = new List<ROIDesc>();
            //如果是DSCD,且不跨视野,则给出两个ROI
            if (IsShowSelectROI)
            {
                if (TrainSetting_CD.DsROIType1 == 0 || (TrainSetting_CD.DsROIType2 == 0 && !IsAcrossTheCameraVision))
                {
                    _messageBoxService.InfoDialog("Please select ROI type！");
                    return false;
                }
                roiList.Add(CreateMeasureROI(PhysicalParam.CDSettings.Orientention, TrainSetting_CD.DsROIType1));
                if (!IsAcrossTheCameraVision)
                {
                    roiList.Add(CreateMeasureROI(PhysicalParam.CDSettings.Orientention, TrainSetting_CD.DsROIType2));
                }
            }
            else
            {
                roiList.Add(CreateMeasureROI(cD_Orientention));
            }
            HpImageViewer.UpdateROIs(roiList.ToArray());
        }
        else
        {
            ((RotatedPickersDesc)rOIDescs[0]).CenterX = ((RotatedPickersDesc)rOIDescs[0]).CenterX;
            ((RotatedPickersDesc)rOIDescs[0]).CenterY = ((RotatedPickersDesc)rOIDescs[0]).CenterY;
            HpImageViewer.UpdateROIs(rOIDescs.ToArray());
        }
        return true;
    }
    /// <summary>
    /// 根据类型创建不同的量测ROI
    /// </summary>
    /// <param name="enumROIType"></param>
    /// <returns></returns>
    private ROIDesc CreateMeasureROI(CD_Orientention cD_Orientention, EnumROIType enumROIType = EnumROIType.Rectangle)
    {
        switch (TestInfo.TrainSetting_CD.Type)
        {
            case EnumTargetType.LS:
            case EnumTargetType.Aperture:
                double startAngle = 0;
                int pickerCount = 2;
                //跨视野只显示一个
                if (IsAcrossTheCameraVision)
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
                if (Vision == EnumVision.Vision2)
                {
                    var angle = (TestInfo.TrainSetting_CD.MultiRoiList[0].MeasureROI as RotatedPickersDesc).StartAngle;
                    startAngle = angle + 180;
                }
                return CreateDefaultDescRois(pickerCount, startAngle, cD_Orientention).GetEnumerableValue(0);
            case EnumTargetType.DS:

                switch (enumROIType)
                {
                    case EnumROIType.Rectangle:
                        return CreateDefaultDescRois(4, 0, CD_Orientention.Vertical).GetEnumerableValue(0);
                    case EnumROIType.Circle:
                        return CreateDefaultDescRois(12, 0, CD_Orientention.Circle).GetEnumerableValue(0);
                    default: break;
                }
                break;
            default:
                break;
        }
        return null;
    }
    private IEnumerable<ROIDesc> CreateDefaultDescRois(int pickerCount, double startAngle, CD_Orientention cD_Orientention = 0)
    {
        var imageData = HpImageViewer.Image as ImageData;

        if (cD_Orientention == 0)
        {
            cD_Orientention = PhysicalParam.CDSettings.Orientention;
        }
        return ROIRenderExtensions.CreateDescROI(RoiDefaultParam_CD_Factory.Build(cD_Orientention, pickerCount, startAngle),
                                      imageData.Width / 2,
                                      imageData.Height / 2,
                                      imageData.Width / 5,
                                      imageData.Height / 5) as List<ROIDesc>;
    }

    #endregion


    #region 获取/保存图片
    public ImageData SaveFocusImage(string imageName, ShapeBase roi, bool isMeasure = false)
    {
        var filePath = GetImageAbsolutePath(imageName);
        FileHelper.DeleteFile(filePath);

        var imageData = Task.Run(() => _alignmentService.MoveAndGrab()).Result;
        imageData.SaveBmp(filePath);
        if (roi != null && isMeasure)
        {
            var kernel = _focusService.GetImageKernel(imageData, roi);
            TrainSetting_CD.FocusKernelModel = kernel;
            LogHelper.LogInfo($"Save FocusKernel,imageName={imageName}");
        }
        return imageData;
    }
    /// <summary>
    /// 量测添加ROI前 加载聚焦或量测生成的原图
    /// </summary>
    public ImageData LoadFocusImageData(bool isMeasureImage)
    {
        string imageName = isMeasureImage ? MeasureImageName : FocusImageName;
        if (string.IsNullOrWhiteSpace(imageName))
        {
            imageName = FocusImageName;
        }
        string imgPath = GetImageAbsolutePath(imageName);
        if (!File.Exists(imgPath))
        {
            _messageBoxService.ErrorDialog($"Please complete the focusing operation!");
            return null;
        }
        try
        {
            ImageData imageData = ImageData.ReadImage(imgPath);
            if (imageData == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
            }
            return imageData;
        }
        catch (Exception e)
        {
            _messageBoxService.ErrorDialog(e.Message);
            return null;
        }
    }
    /// <summary>
    /// 获取图片绝对路径
    /// </summary>
    /// <param name="imageName">图片名称</param>
    /// <returns></returns>
    public string GetImageAbsolutePath(string imageName)
    {

        var filePath = Path.Combine(TestFilesFolder, imageName + ".bmp");
        return filePath;
    }
    #endregion

    #region 按钮控制
    /// <summary>
    /// 算法按钮控制
    /// </summary>
    /// <param name="isClearRoi"></param>
    public void InitMeasurementRoiButton(bool isClearRoi = true)
    {
        MeasurementROI_Enabled = false;
        if (isClearRoi)
            HpImageViewer.UpdateROIs();
    }
    /// <summary>
    /// 聚焦按钮控制
    /// </summary>
    /// <param name="roisSetEnabled">Set Button</param>
    /// <param name="roiConfirmEnabled">confirm button</param>
    /// <param name="cancelEnabled">cancel button</param>
    public void SetButtonEnabled(bool roisSetEnabled, bool roiConfirmEnabled, bool cancelEnabled)
    {
        FocusROI_SetEnabled = roisSetEnabled;
        FocusROI_ConfirmEnabled = roiConfirmEnabled;
        FocusROI_CancelEnabled = cancelEnabled;
        if (roisSetEnabled)
        {
            HpImageViewer.UpdateROIs();
        }
    }
    #endregion


}
