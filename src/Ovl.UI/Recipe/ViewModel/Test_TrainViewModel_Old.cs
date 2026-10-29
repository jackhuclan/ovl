using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using Marvel.Application.Services;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using OVL.Algorithm.DoNet;
using Prism.Services.Dialogs;
using Wafer.Algorithm.Common.DoNet;
using Point = HPImageViewer.Core.Primitives.Point;

using Ovl.UI.Controls;
using Ovl.UI.Models;

namespace Ovl.UI.Recipe.ViewModel;

public partial class Test_TrainViewModel_Old : Test_TrainViewModelBase<TestInfo_BiB>
{
    public Test_TrainViewModel_Old(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _pztservice = _serviceProvider.GetService<IServiceZPzt>();

        alignmentService = _serviceProvider.GetRequiredService<IAlignmentService>();
        _servCalDat = serviceProvider.GetService<IServiceCalData>();

        _templateContext2_overlaymark ??= new MultiTemplateMatchingContext();
        _templateContext2_inner ??= new MultiTemplateMatchingContext();
        _templateContext2_outer ??= new MultiTemplateMatchingContext();
        _StageController = serviceProvider.GetService<IVisionService>();

    }
    const string CONST_Single = "Single";
    const string CONST_Inner = "Inner";
    const string CONST_Outer = "Outer";
    private List<CorrelationCurve> CorrelationCurvesList;
    IAlignmentService alignmentService;
    IVisionService _StageController;

    IServiceZPzt _pztservice;
    MultiTemplateMatchingContext _templateContext2_overlaymark;
    MultiTemplateMatchingContext _templateContext2_inner;
    MultiTemplateMatchingContext _templateContext2_outer;

    public ObservableCollection<KeyValuePair<string, List<CDChartCurve>>> Datas = default;
    /// <summary>
    /// 图像控件
    /// </summary>
    public IHPImageViewer HpImageViewer { get; set; }
    IServiceCalData _servCalDat;


    [ObservableProperty]
    MultiTemplateMatchingViewModel _templateViewModel_Inner;
    [ObservableProperty]
    MultiTemplateMatchingViewModel _templateViewModel_Outter;

    [ObservableProperty]
    private ObservableCollection<BIBAlgorithmType> _enumBibAlgorithmType;
    [ObservableProperty]
    private ObservableCollection<CorrelationType> _enumCorrelationType;
    [ObservableProperty]
    private ObservableCollection<MeasurementResultData> _measurementResultDataList;
    [ObservableProperty]
    private double _innerFocusHeight;
    [ObservableProperty]
    private double _outerFocusHeight;
    [ObservableProperty]
    private Visibility _aIMVisibility = Visibility.Visible;
    [ObservableProperty]
    private Visibility _bibVisibility = Visibility.Collapsed;
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
    private EnumFocusBasedOn _focusBasedOn = EnumFocusBasedOn.Inner;
    [ObservableProperty]
    private bool _isReuseMeasurementROI_Inner = true;
    [ObservableProperty]
    private bool _isReuseMeasurementROI_Outer = true;

    [ObservableProperty]
    private bool _isFocusSingle;

    [ObservableProperty]
    private BIBAlgorithmParam _bibAlgorithmParam;
    [ObservableProperty]
    private AIMAlgorithmParam _aIMAlgorithmParam;
    const string OutColor = "#00FFFF";

    TrainSettings_BiB TestTrainSetting
    {
        get
        {
            return TestInfo?.TrainSetting_BiB;
        }
    }
    Physical Physical
    {
        get
        {
            return TestInfo.Physical as Physical;
        }
    }



    [RelayCommand]
    async void Loaded()
    {
        await ChangeMeasureCamera();
        BindCheckBox();
        // LoadOvResult();
        LogHelper.LogInfo($"Test_TrainViewModel Loaded:MacroFocusZ={alignmentService.MacroFocusZ},InnerFocusHeight={TestTrainSetting.InnerFocusHeight},OutFoucsHeight={TestTrainSetting.OuterFocusHeight}");
        if (TestTrainSetting.InnerFocusHeight == 0)
        {
            TestTrainSetting.InnerFocusHeight = alignmentService.WaferMapInfo.Thickness;
        }
        if (TestTrainSetting.OuterFocusHeight == 0)
        {
            TestTrainSetting.OuterFocusHeight = alignmentService.WaferMapInfo.Thickness;
        }
        InnerFocusHeight = TestTrainSetting.InnerFocusHeight + alignmentService.MacroFocusZ;
        OuterFocusHeight = TestTrainSetting.OuterFocusHeight + alignmentService.MacroFocusZ;
        //SetAutoFocusHeight();



        ////主模板和子模板一般使用同一张图片的不同位置制作，这里值显示一张图片即主模板制作时的图片；如果子模板所用图不一样，需要重新从本地或相机取图（这里暂时不做主、子模板用不同图片的切换显示）
        //if (TestTrainSetting.OverlayMarkModels != null && TestTrainSetting.OverlayMarkModels.Count > 0)
        //    HpImageViewer.Image = TestTrainSetting.OverlayMarkModels[0].CreateModelImg;

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
                AIMAlgorithmParam = (AIMAlgorithmParam)TestInfo.OMParam.AlgorithmParam;
                break;
            case EnumTargetType.AIM_XYS:
                break;
            case EnumTargetType.BF:
                break;
            case EnumTargetType.Aperture:
                break;
            default:
                break;
        }

        _templateContext2_inner.TemplateContexts = TestTrainSetting.OverlayMark_InnerModels != null ? TestTrainSetting.OverlayMark_InnerModels : _templateContext2_inner.TemplateContexts;
        _templateContext2_inner.TemplateContexts[0].Name = "Inner Model";

        TemplateViewModel_Inner = _serviceProvider.CreateTemplateViewModel(_templateContext2_inner, HpImageViewer) as MultiTemplateMatchingViewModel;
        HpImageViewer.SetTemplateImage(_templateContext2_inner, _testFilesFolder);

        _templateContext2_outer.TemplateContexts = TestTrainSetting.OverlayMark_OuterModels != null ? TestTrainSetting.OverlayMark_OuterModels : _templateContext2_outer.TemplateContexts;
        _templateContext2_outer.TemplateContexts[0].Name = "Outer Model";

        TemplateViewModel_Outter = _serviceProvider.CreateTemplateViewModel(_templateContext2_outer, HpImageViewer) as MultiTemplateMatchingViewModel;
        HpImageViewer.SetTemplateImage(_templateContext2_outer, _testFilesFolder);

        TemplateViewModel_Inner.UserModified += MultiTemplateMatchingViewModel_Inner_UserModified;
        TemplateViewModel_Outter.UserModified += MultiTemplateMatchingViewModel_Outter_UserModified;
        if (TestTrainSetting.InnerType == EnumTargetType.AIM || TestTrainSetting.InnerType == EnumTargetType.AIM_XYS)
        {
            //AIMVisibility = Visibility.Collapsed;
        }
        else
        {
            BibVisibility = Visibility.Visible;
        }
        InitFocusRoiButton();
        InitMeasurementRoiButton();
        IsFocusSingle = true;



        var physical = TestInfo.Physical as Physical;
        //切换显示
        if (physical.TestSettings.GrabMode == EnumGrabMode.DoubleGrab)
        {
            IsFocusSingle = false;
        }
        //是否显示创建模板按钮，如果是复用量测Roi的时候，则不显示创建模板和删除模板按钮
        TemplateViewModel_Inner.IsShowCreateModel = !TestTrainSetting.IsReuseMeasurementROI_Inner;
        TemplateViewModel_Outter.IsShowCreateModel = !TestTrainSetting.IsReuseMeasurementROI_Outer;

        if (HpImageViewer.Image == null)
            _visionService.StartGrabbing();
    }

    private void SetAutoFocusHeight()
    {
        try
        {
            var physical = TestInfo.Physical as Physical;
            var focusSetting = (physical.FocusSettings as FocusPhysical);
            var baseOn = focusSetting.FocusBasedOn;
            var zHeight = (InnerFocusHeight + OuterFocusHeight) / 2;
            var pztHeight = (InnerPztOffset + OuterPztOffset) / 2;
            switch (baseOn)
            {
                case EnumFocusBasedOn.Inner:
                    // zHeight = InnerFocusHeight;
                    pztHeight = InnerPztOffset;

                    break;
                case EnumFocusBasedOn.Outer:
                    // zHeight = OuterFocusHeight;
                    pztHeight = OuterPztOffset;
                    break;
            }
            AutoFocusHeight = zHeight + _pztservice.Direction * pztHeight / 1000;
            //此处的高度是绝对坐标
            alignmentService.MoveZTo(zHeight, "TrainedFocusHeight", false);
            _pztservice.AMov(pztHeight);
        }
        catch (Exception)
        {
        }
    }

    Point2d _innerMainPoint;
    Point2d _outerMainPoint;


    private async void ModelModified(ITemplateMatchingView.UserModifyArgs e, bool isInner)
    {
        var models = isInner ? TestTrainSetting.OverlayMark_InnerModels : TestTrainSetting.OverlayMark_OuterModels;
        if (e.IsSuccess && (e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelCreated || e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelTested))
        {
            var model = e.Context;
            var imageData = HpImageViewer.Image as ImageData;
            if (imageData == null)
                return;
            var point = alignmentService.ConvertImageToAxis(alignmentService.GetCurrentAxisPosition(), model.MatchingItems[0], imageData.Width, imageData.Height);
            //如果不是主模板，需要计算其与主模板直接的偏移量
            if (model == models[0])
            {
                if (isInner)
                { _innerMainPoint = point; }
                else
                {
                    _outerMainPoint = point;
                }
                Submit(false);
            }
            else
            {
                var pointMain = isInner ? _innerMainPoint : _outerMainPoint;
                if (pointMain != null)
                {
                    //此处为左下角模板，获取到的相对于Field的位置就是相对于主模板的位置
                    model.R4 = point - pointMain;
                    Submit(false);
                }
                else
                {
                    _messageBoxService.ErrorDialog("Before creating a backup template, it is necessary to locate the main template");
                    await _templateMatchingService.ClearModelAsync(model);
                }
            }
        }

        if (e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelCleanUp)
        {
            LoadFocusImageData(isInner ? EnumFocusBasedOn.Inner : EnumFocusBasedOn.Outer);
        }
    }

    private void MultiTemplateMatchingViewModel_Outter_UserModified(object sender, ITemplateMatchingView.UserModifyArgs e)
    {
        ModelModified(e, false);
    }

    private void MultiTemplateMatchingViewModel_Inner_UserModified(object sender, ITemplateMatchingView.UserModifyArgs e)
    {
        ModelModified(e, true);
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
    void LoadOvResult(OvResultBase result)
    {

        if (result != null && result.CenterInner != null)
        {
            MeasurementResultDataList = new ObservableCollection<MeasurementResultData>();
            MeasurementResultDataList.Add(new MeasurementResultData
            {
                Axit = "X",
                CenterInner = result.CenterInner.X,
                QMeritInner = result.QMerit.InnerX,
                CenterOuter = result.CenterOuter.X,
                OVLMisreg = result.MisregX,
                QMeritOuter = result.QMerit.OuterX,
                QMeritTarget = result.QMerit.X
            });
            MeasurementResultDataList.Add(new MeasurementResultData
            {
                Axit = "Y",
                CenterInner = result.CenterInner.Y,
                QMeritInner = result.QMerit.InnerY,
                CenterOuter = result.CenterOuter.Y,
                OVLMisreg = result.MisregY,
                QMeritOuter = result.QMerit.OuterY,
                QMeritTarget = result.QMerit.Y
            });
        }
    }


    [RelayCommand]
    void CloseView()
    {
        UpdateTestInfoData(false);
        var result = new DialogResult(ButtonResult.OK, new DialogParameters() { { "modifiedTestInfo", TestInfo } });
        RaiseRequestClose(result);
        NotifySaveAction = null;
    }
    [RelayCommand]
    void Submit()
    {
        Submit(true);
    }

    private void Submit(bool isCheckData)
    {
        try
        {
            TestTrainSetting.IsReuseMeasurementROI_Inner = !TemplateViewModel_Inner.IsShowCreateModel;
            TestTrainSetting.IsReuseMeasurementROI_Outer = !TemplateViewModel_Outter.IsShowCreateModel;
            UpdateTestInfoData(isCheckData);
            ExecuteSomeMethod("TestTrainMessage");
            //_messageBoxService.Success("Data saved successfully！", "TestTrainMessage");
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
            _messageBoxService.Error($"Failed to save data!{ex.Message}");
        }
    }


    void CheckTemplateData(List<SingleTemplateMatchingContext> templates, string key)
    {
        foreach (SingleTemplateMatchingContext template in templates)
        {
            _templateMatchingService.ReadTemplatesModelInfo(template, _testFilesFolder);
            if (template.SelectedMatchModelGroup == null || template.SelectedMatchModelGroup.MatchModel.ModelInfo == null)
            {
                throw new Exception($"The {key} template is empty!");
            }
        }
    }

    void UpdateTestInfoData(bool isCheckData)
    {
        if (isCheckData)
        {
            CheckTemplateData(_templateContext2_inner.TemplateContexts, CONST_Inner);
            CheckTemplateData(_templateContext2_outer.TemplateContexts, CONST_Outer);
        }
        TestTrainSetting.OverlayMark_InnerModels = _templateContext2_inner.TemplateContexts;
        TestTrainSetting.OverlayMark_OuterModels = _templateContext2_outer.TemplateContexts;
    }
    public override string Title => "Train Test";

    public override void OnDialogOpened(IDialogParameters parameters)
    {
        base.OnDialogOpened(parameters);
    }
    public override void OnDialogClosed()
    {
        base.OnDialogClosed();
    }
    /// <summary>
    /// TisOptimziePreRun之前的操作
    /// </summary>
    private void TisOptiPreRun()
    {
        HpImageViewer.UpdateROIs(null);
    }

    private void ExecuteSomeMethod(string messageToken)
    {
        NotifySaveAction?.Invoke(TestInfo, messageToken);
    }
    void ModelConcentric_Rect(bool isInner)
    {
        if (HpImageViewer.Image == null)
        {
            _messageBoxService.InfoDialog("The current image data is invalid！");
        }

        double width = 200;
        double height = 200;
        var imageData = (ImageData)HpImageViewer.Image;
        var top = (imageData.Height - height) / 2;
        var left = (imageData.Width - width) / 2;

        //var inRoi = TestTrainSetting.OverlayMark_InnerModels[0].ROI;
        //var outRoi = TestTrainSetting.OverlayMark_OuterModels[0].ROI;
        //if (TestInfo is TestInfo_AIM)
        //{
        //    outRoi = inRoi;
        //}
        //var roi = isInner ? inRoi : outRoi;
        //if (roi != null)
        //{
        //    var roiRect = roi.CreateRect();
        //    if (roiRect.X > 0 && roiRect.Y > 0)
        //    {
        //        left = roiRect.X;
        //        top = roiRect.Y;
        //        width = roiRect.Width;
        //        height = roiRect.Height;
        //    }
        //}

        ROIDesc rOIDescs = null;
        var type = isInner ? TestTrainSetting.InnerType : TestTrainSetting.OuterType;
        switch (type)
        {
            case EnumTargetType.Circle:
            case EnumTargetType.Box:
            case EnumTargetType.Bar:
            case EnumTargetType.Frame:
                if (type != EnumTargetType.Circle)
                {
                    try
                    {
                        OMTargetType omType = OMTargetType.Bar;
                        if (type == EnumTargetType.Frame)
                            omType = OMTargetType.Frame;
                        else if (type == EnumTargetType.Box)
                            omType = OMTargetType.Box;
                        var roi = HpImageViewer.ImageViewerDesc.ROIDescs?.FirstOrDefault()?.CreateRoi();
                        //如果不是矩形roi，则忽略其范围，如果是矩形roi，则在指定的矩形roi内自动查找内外侧
                        if (roi != null && roi.GetType() != typeof(Rect2d))
                        {
                            roi = null;
                        }
                        if (roi == null)
                        {
                            roi = new Rect2d(0, 0, imageData.Width, imageData.Height);
                        }
                        using var hImage = imageData.ToHObject();
                        OMUtil.AutoGenMeasureRoiOfBIB(hImage, roi, omType, OMTargetLayer.InnerAndOuter, out Bib2d innerRoi, out Bib2d outerRoi);
                        var bib2d = isInner ? innerRoi : outerRoi;
                        var bibAxis = bib2d?.Box?.Axis;
                        if (bibAxis != null && bibAxis.Width != 0 && bibAxis.Height != 0)
                        {
                            rOIDescs = new QuadRectangleDesc() { Top = bibAxis.Y, Left = bibAxis.X, Width = bibAxis.Width, Height = bibAxis.Height, BandWidth = bib2d.Box.Width, BandLength = bib2d.BandLength };
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError(ex, "Auto create roi errored!");
                    }
                }
                if (rOIDescs == null)
                    rOIDescs = new QuadRectangleDesc() { Top = top, Left = left, Width = width, Height = height, BandWidth = width * 0.3, Color = "#FFFF00", BandLength = width * 0.5 };
                break;
            case EnumTargetType.Cross:
                rOIDescs = new Cross2dRectangleDesc() { Top = top, Left = left, Width = width, Height = height, BandWidth = 0, ColumnWidth = width * 0.3, Color = "#FFFF00", BandLength = width * 0.2, IntervalLength = width * 0.2 };
                break;
            case EnumTargetType.AIM:
                var roiAim = HpImageViewer.ImageViewerDesc.ROIDescs?.FirstOrDefault()?.CreateRoi();
                //如果不是矩形roi，则忽略其范围，如果是矩形roi，则在指定的矩形roi内自动查找内外侧
                if (roiAim != null && roiAim.GetType() != typeof(Rect2d))
                {
                    roiAim = null;
                }
                if (roiAim == null)
                {
                    roiAim = new Rect2d(0, 0, imageData.Width, imageData.Height);
                }
                AimOrientation aimOrientation = TestInfo.TargetSetting.OrientationType == EnumOrientationType.Counterclockwise ? AimOrientation.CounterClockWise : AimOrientation.ClockWise;
                using var hImageAim = imageData.ToHObject();
                OMUtil.AutoGenMeasureRoiOfAIM(hImageAim, roiAim, OMTargetType.AIM, aimOrientation, out Aim2d innerAimRoi, out Aim2d outerAimRoi);
                var aim2d = isInner ? innerAimRoi : outerAimRoi;
                aim2d.Rect.X = aim2d.Rect.X == 0 ? left : aim2d.Rect.X;
                aim2d.Rect.Y = aim2d.Rect.Y == 0 ? top : aim2d.Rect.Y;
                aim2d.Rect.Width = aim2d.Rect.Width == 0 ? width : aim2d.Rect.Width;
                aim2d.Rect.Height = aim2d.Rect.Height == 0 ? height : aim2d.Rect.Height;
                rOIDescs = new AimRectangleDesc()
                {
                    Top = aim2d.Rect.Y,
                    Left = aim2d.Rect.X,
                    Width = aim2d.Rect.Width,
                    Height = aim2d.Rect.Height,
                    RegionWidth = aim2d.RegionWidth,
                    RegionHeight = aim2d.RegionHeight,
                    BandWidth = width * 0.2,
                    OffsetX = aim2d.OffsetX,
                    OffsetY = aim2d.OffsetY,
                    Color = "#FFFF00",
                };
                break;
            case EnumTargetType.AIM_XYS:
                double RegionWidth = TestInfo.TargetSetting.OrientationType == EnumOrientationType.Clockwise ? width * 0.2 : width * 0.4;
                double RegionHeight = TestInfo.TargetSetting.OrientationType == EnumOrientationType.Clockwise ? height * 0.4 : height * 0.2;
                rOIDescs = new AimRectangleDesc()
                {
                    Top = top,
                    Left = left,
                    Width = width,
                    Height = height,
                    RegionWidth = RegionWidth,
                    RegionHeight = RegionHeight,
                    BandWidth = width * 0.2,
                    Color = "#FFFF00",
                    //BandLength = width * 0.3, IntervalLength = width * 0.3
                };
                break;
            case EnumTargetType.BF:
                break;
            case EnumTargetType.Aperture:
                break;
            default:
                break;
        }
        if (!isInner)
        { rOIDescs.Color = OutColor; }
        HpImageViewer.UpdateROIs(rOIDescs);
    }

    /// <summary>
    /// inner Set ROI
    /// </summary>
    [RelayCommand]
    void InnerMeasureRoi_Set()
    {
        ImageData imageData = LoadFocusImageData(EnumFocusBasedOn.Inner);
        if (imageData == null)
        {
            return;
        }
        _StageController.StopGrabbing();
        HpImageViewer.Image = imageData;
        if (TestTrainSetting.InnerRectangle.Top != 100 && TestTrainSetting.InnerRectangle.Left != 100 && TestTrainSetting.InnerRectangle.Width != 100 && TestTrainSetting.InnerRectangle.Height != 100)
            HpImageViewer.UpdateROIs((RectangleDesc)CloneHelper.ReflectionClone(TestTrainSetting.InnerRectangle));
        else
            ModelConcentric_Rect(true);
        MeasurementROI_SetEnabled = false;
        MeasurementROI_InnerEnabled = true;
        MeasurementROI_OuterEnabled = false;
    }

    [RelayCommand]
    void InnerMeasureRoi_ReSet()
    {
        ImageData imageData = LoadFocusImageData(EnumFocusBasedOn.Inner);
        if (imageData == null)
        {
            return;
        }
        _StageController.StopGrabbing();
        HpImageViewer.Image = imageData;
        ModelConcentric_Rect(true);
    }


    [RelayCommand]
    void OuterMeasureRoi_Set()
    {
        ImageData imageData = LoadFocusImageData(EnumFocusBasedOn.Outer);
        if (imageData == null)
        {
            return;
        }
        _StageController.StopGrabbing();
        HpImageViewer.Image = imageData;
        var outMeasureRoi = TestTrainSetting?.OuterRectangle;
        if (outMeasureRoi != null && outMeasureRoi.Top != 100 && outMeasureRoi.Left != 100 && outMeasureRoi.Width != 100 && outMeasureRoi.Height != 100)
        {
            outMeasureRoi.Color = OutColor;
            HpImageViewer.UpdateROIs((RectangleDesc)CloneHelper.ReflectionClone(outMeasureRoi));
        }
        else
            ModelConcentric_Rect(false);
        MeasurementROI_SetEnabled = false;
        MeasurementROI_InnerEnabled = false;
        MeasurementROI_OuterEnabled = true;
    }


    [RelayCommand]
    void OuterMeasureRoi_ReSet()
    {
        ImageData imageData = LoadFocusImageData(EnumFocusBasedOn.Inner);
        if (imageData == null)
        {
            return;
        }
        _StageController.StopGrabbing();
        HpImageViewer.Image = imageData;
        ModelConcentric_Rect(false);
    }


    [RelayCommand]
    void MeasureRoi_Cancel()
    {
        InitMeasurementRoiButton();
        HpImageViewer.UpdateROIs();
    }


    [RelayCommand]
    async void Run()
    {
        //此处只是用来调试时验证数据使用的，需要加断点动态修改isTest的值
        bool isTest = false;
        if (isTest) //此处为了便于在调试中拖动代码
        {
            isTest = true;
        }
        LogHelper.LogRunMethodStart();
        using var imgObjectInner = LoadFocusImageData(EnumFocusBasedOn.Inner, isTest)?.ToHObject();
        if (imgObjectInner == null)
        { return; }
        using var imgObjectOuter = LoadFocusImageData(EnumFocusBasedOn.Outer, isTest)?.ToHObject();
        if (imgObjectInner == null || imgObjectOuter == null) return;
        if (TestInfo.OMParam.KernelModelInner == null || TestInfo.OMParam.KernelModelOuter == null)
        {
            _messageBoxService.ErrorDialog("Please set measurement roi first!");// ("Physical 数据不完整！");
            return;
        }
        //清空Roi
        HpImageViewer.UpdateROIs();
        var _measService = _serviceProvider.GetService<IMeasurementService>();
        //是否时测试模式
        if (isTest)
        {
            var testovResult = TestOvl(_measService, imgObjectInner, imgObjectOuter);
            LoadOvResult(testovResult);
            return;
        }
        OMParam oMParam = TestInfo.OMParam;
        var (px, py) = _servCalDat.GetUmPixel(ECamera.Meas);
        oMParam.PixelSizeX = px;
        oMParam.PixelSizeY = py;
        oMParam.TargetType = ConvetTargetType(TestTrainSetting.InnerType);
        string dirPath = Path.Combine(_serviceProvider.GetService<IRecipeManager>().GetRecipePath(alignmentService.RecipeElementsInfo.Id), TestInfo.Name);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        var fileName = Path.Combine(dirPath, "omParamTest.hdict");
        TestInfo.OMParam.SerializeToDictFile(fileName);

        var x = TestInfo.TrainSetting_BiB.InnerRectangle.CenterX;
        var y = TestInfo.TrainSetting_BiB.InnerRectangle.CenterY;
        var transMat = _measService.GetTransMat(y, x, new MatchingItem() { Column = x, Row = y }, 0);
        //var imageData = HpImageViewer.Image as ImageData;
        //HObject imageObject = imageData.ToHObject();
        var (offsetX, offsetY) = Physical?.GetOvlOffset() ?? (0, 0);
        var ovResult = _measService.MeasureOverlay(imgObjectInner, imgObjectOuter, TestInfo.OMParam, transMat, transMat, offsetX, offsetY);
        ShowResult(ovResult, imgObjectInner, imgObjectOuter);
        Submit();
        LogHelper.LogRunMethodEnd();
    }

    private OvResultBase TestOvl(IMeasurementService _measService, HObject _imgInner, HObject _imgOuter)
    {
        bool isReverse = false;
        double angle = isReverse ? 180 : 0;
        var innerModel = TestInfo.TrainSetting_BiB.OverlayMark_InnerModels[0];
        var outerModel = TestInfo.TrainSetting_BiB.OverlayMark_OuterModels[0];
        var innerRoi = innerModel.MatchingItems[0].ToPoint();
        var innerCenterX = innerRoi.X;
        var innerCenterY = innerRoi.Y;
        var outerRoi = outerModel.MatchingItems[0].ToPoint();
        var outerCenterX = outerRoi.X;
        var outerCenterY = outerRoi.Y;
        var matchItemInner = alignmentService.FindTemplateForRun(_imgInner.ToImageData(), innerModel);
        var matchItemOuter = alignmentService.FindTemplateForRun(_imgOuter.ToImageData(), outerModel);
        var transMatInner = _measService.GetTransMat(innerCenterY, innerCenterX, matchItemInner, angle);
        var transMatOuter = _measService.GetTransMat(outerCenterY, outerCenterX, matchItemOuter, angle);
        var (ovlOffsetX, ovlOffsetY) = Physical?.GetOvlOffset() ?? (0, 0);
        var ovl = _measService.MeasureOverlay(_imgInner, _imgOuter, TestInfo.OMParam, transMatInner, transMatOuter, ovlOffsetX, ovlOffsetY);
        List<ROIDesc> rOIDescs = new List<ROIDesc>();
        var innerDesc = innerModel.ROI.CreateRect();
        innerDesc.X = matchItemInner.Column - innerDesc.Width / 2;
        innerDesc.Y = matchItemInner.Row - innerDesc.Height / 2;
        rOIDescs.Add(innerDesc.CreateRoiDesc());
        var outerDesc = outerModel.ROI.CreateRect();
        outerDesc.X = matchItemOuter.Column - outerDesc.Width / 2;
        outerDesc.Y = matchItemOuter.Row - outerDesc.Height / 2;
        rOIDescs.Add(outerDesc.CreateRoiDesc());
        HpImageViewer.UpdateROIs(rOIDescs.ToArray());
        return ovl;
    }

    private void ShowResult(OvResultBase ovResult, HObject imgObjectInner, HObject imgObjectOuter)
    {
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
        if (Physical.TestSettings.GrabMode == EnumGrabMode.SingleGrab || FocusBasedOn == EnumFocusBasedOn.Inner)
        {
            rOIDescs.AddRange(GetProfileForImageView(imgObjectInner, oMParam.OmROIInner, oMParam.TargetType));
            rOIDescs.Add(TestTrainSetting.InnerRectangle);
        }
        if (Physical.TestSettings.GrabMode == EnumGrabMode.SingleGrab || FocusBasedOn == EnumFocusBasedOn.Outer)
        {
            rOIDescs.AddRange(GetProfileForImageView(imgObjectOuter, oMParam.OmROIOuter, oMParam.TargetType));
            rOIDescs.Add(TestTrainSetting.OuterRectangle);
        }

        HpImageViewer.FitImageToArea();
        if (Physical.TestSettings.GrabMode == EnumGrabMode.DoubleGrab && FocusBasedOn == EnumFocusBasedOn.Inner)
            HpImageViewer.Image = imgObjectInner.ToImageData();
        else if (Physical.TestSettings.GrabMode == EnumGrabMode.DoubleGrab && FocusBasedOn == EnumFocusBasedOn.Outer)
            HpImageViewer.Image = imgObjectOuter.ToImageData();
        else
            HpImageViewer.Image = imgObjectInner.ToImageData();//inner 和outer相同

        // TestInfo.OvResultBase = centeringOVL;
        TestInfo.IsTrained = true;
        LoadOvResult(centeringOVL);
        HpImageViewer.UpdateROIs(rOIDescs.ToArray());
    }

    private OMTargetType ConvetTargetType(EnumTargetType innerType)
    {
        switch (innerType)
        {
            case EnumTargetType.Bar:
                return OMTargetType.Bar;
            case EnumTargetType.Box:
                return OMTargetType.Box;
            case EnumTargetType.Frame:
                return OMTargetType.Frame;
            case EnumTargetType.Circle:
                return OMTargetType.Box;
            case EnumTargetType.Cross:
                return OMTargetType.Cross;
            case EnumTargetType.AIM:
            case EnumTargetType.AIM_XYS:
                return OMTargetType.AIM;
            default:
                return OMTargetType.Bar;
        }
    }

    private List<ROIDesc> GetProfileForImageView(HObject imageObject, OMROIBase omROI, OMTargetType oMTargetType)
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
    private List<CorrelationCurve> GetCorrelationCurve(HObject imageObjectInner, HObject imageObjectOuter, HTuple hTuple)
    {
        OMUtil.GetCorrelationCurve(imageObjectInner, imageObjectOuter, hTuple, out List<CorrelationCurve> curves);
        return curves;
    }
    /// <summary>
    /// 创建bib内框 kernel Size调整框
    /// </summary>
    [RelayCommand]
    private void InnerMeasureRoi_Confirm()
    {
        CreateKernelSizeRect(true);
    }

    [RelayCommand]
    private void OuterMeasureRoi_Confirm()
    {
        CreateKernelSizeRect(false);
    }


    private void CreateKernelSizeRect(bool isInner)
    {
        if (HpImageViewer.Image == null)
        {
            _messageBoxService.InfoDialog("The current image data is invalid！");
        }
        ExResult exResult = null;
        try
        {
            switch (TestTrainSetting.InnerType)
            {
                case EnumTargetType.Bar:
                case EnumTargetType.Box:
                case EnumTargetType.Frame:
                case EnumTargetType.Circle:
                    exResult = GenKernelRoiOfBIB(isInner);
                    break;
                case EnumTargetType.Cross:
                    exResult = GenKernelRoiOfCross(isInner);
                    break;
                case EnumTargetType.AIM:
                case EnumTargetType.AIM_XYS:
                    exResult = GenKernelRoiOfAIM(isInner);
                    break;
                default:
                    break;
            }
            if (exResult != null)
            {
                if (exResult.IsOK)
                {
                    //  _messageBoxService.InfoDialog("Creation of measurement template data completed！");
                    InitMeasurementRoiButton();
                    HpImageViewer.UpdateROIs(null);
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
        }

    }

    public void CreateKernelSizeRectAgain(bool isInner)
    {
        if (isInner)
        {
            HpImageViewer.UpdateROIs(TestTrainSetting.InnerRectangle);
        }
        else
        {
            HpImageViewer.UpdateROIs(TestTrainSetting.OuterRectangle);
        }
        CreateKernelSizeRect(isInner);
    }

    private BIBROI GenKernelRoiOfBIB(RectangleDesc roi, out ExResult exResult)
    {
        var rectRoi = roi as QuadRectangleDesc;
        var trainRoi = new Rect2d() { X = rectRoi.Left, Y = rectRoi.Top, Width = rectRoi.Width, Height = rectRoi.Height };
        Box2d box2D = new Box2d()
        {
            Axis = trainRoi,
            Width = rectRoi.BandWidth
        };
        int lengthPercent = (int)(rectRoi.BandLength / rectRoi.Height * 100);
        exResult = OMUtil.GenKernelRoiOfBIB(box2D, lengthPercent, out BIBROI kernelRoi);

        return kernelRoi;
    }



    private CrossROI GenKernelRoiOfCross(RectangleDesc roi, out ExResult exResult)
    {
        var rectRoi = roi as Cross2dRectangleDesc;
        Cross2d cross2D = new Cross2d();
        cross2D.Axis = new Polygon2d();
        cross2D.Axis.Axis = new List<Point2d>();
        cross2D.Width = rectRoi.ColumnWidth;
        foreach (var item in rectRoi.PointList)
        {
            cross2D.Axis.Axis.Add(new Point2d(item.Item1, item.Item2));
        }
        int persent = (int)((rectRoi.BandLength / rectRoi.PointLength) * 100);
        exResult = OMUtil.GenKernelRoiOfCross(cross2D, persent, out CrossROI kernelRoi);
        return kernelRoi;
    }


    private AIMROI GenKernelRoiOfAIM(RectangleDesc roi, out ExResult exResult)
    {
        var rectRoi = roi as AimRectangleDesc;
        AimOrientation aimOrientation = TestInfo.TargetSetting.OrientationType == EnumOrientationType.Clockwise ? AimOrientation.ClockWise : AimOrientation.CounterClockWise;

        var trainRoi = new Aim2d()
        {
            Rect = new Rect2d() { X = rectRoi.Left, Y = rectRoi.Top, Width = rectRoi.Width, Height = rectRoi.Height },
            OffsetX = rectRoi.OffsetX,
            OffsetY = rectRoi.OffsetY,
            RegionHeight = rectRoi.RegionHeight,
            RegionWidth = rectRoi.RegionWidth,
        };
        exResult = OMUtil.GenKernelRoiOfAIM(trainRoi, aimOrientation, out AIMROI kernelRoi);
        return kernelRoi;
    }

    /// <summary>
    /// 自动创建定位模板
    /// </summary>
    /// <param name="isInner"></param>
    private void AutoCreateModel(bool isInner)
    {
        var templateVM = isInner ? TemplateViewModel_Inner : TemplateViewModel_Outter;
        if (!templateVM.IsShowCreateModel)
        {
            templateVM.TrainModel();
        }
    }
    /// <summary>
    /// 创建bib外框 kernel Size调整框
    /// </summary>
    private ExResult GenKernelRoiOfBIB(bool isInner)
    {
        ExResult exResult = null;
        CreateKernelModel(isInner, (rectRoi) =>
        {
            AutoCreateModel(isInner);
            return GenKernelRoiOfBIB(rectRoi, out exResult);
        });


        return exResult;
    }
    /// <summary>
    ///  创建Cross外框 kernel Size调整框
    /// </summary>
    private ExResult GenKernelRoiOfCross(bool isInner)
    {
        ExResult exResult = null;
        CreateKernelModel(isInner, (roi) =>
        {
            AutoCreateModel(isInner);
            return GenKernelRoiOfCross(roi, out exResult);
        });
        return exResult;
    }
    /// <summary>
    ///  创建AIM外框 kernel Size调整框
    /// </summary>
    private ExResult GenKernelRoiOfAIM(bool isInner)
    {
        ExResult exResult = null;
        CreateKernelModel(isInner, (roi) =>
        {
            var result= GenKernelRoiOfAIM(roi, out exResult);
            AutoCreateModel(isInner);
            return result; });
        return exResult;
    }
    private ExResult CreateKernelModel(bool isInner, Func<RectangleDesc, OMROIBase> funcGenKernelRoi)
    {
        var rectRoi = HpImageViewer.ImageViewerDesc.ROIDescs?.FirstOrDefault() as RectangleDesc;
        OMROIBase oMROIBase = funcGenKernelRoi(rectRoi);
        var imageData = HpImageViewer.Image as ImageData;
        using var imageInner = imageData.ToHObject();
        var targetType = isInner ? TestTrainSetting.InnerType : TestTrainSetting.OuterType;
        OMTargetType oMTargetType = ConvetTargetType(targetType);
        var result = OMUtil.GenKernelModel(imageInner, oMROIBase, oMTargetType, out OMKernelModel oMKernelModel);
        if (isInner)
        {
            TestInfo.OMParam.OmROIInner = oMROIBase;
            TestInfo.OMParam.KernelModelInner = oMKernelModel;
            TestTrainSetting.InnerRectangle = CloneHelper.ReflectionClone(rectRoi) as RectangleDesc;
        }
        else
        {
            TestInfo.OMParam.OmROIOuter = oMROIBase;
            TestInfo.OMParam.KernelModelOuter = oMKernelModel;
            TestTrainSetting.OuterRectangle = CloneHelper.ReflectionClone(rectRoi) as RectangleDesc;
        }

        Submit(false);
        return result;
    }
    /// <summary>
    /// 量测添加ROI前 加载聚焦生成的原图
    /// </summary>
    private ImageData LoadFocusImageData(EnumFocusBasedOn focusBasedOn, bool isTest = false)
    {
        if (Physical == null || Physical.TestSettings == null)
        {
            _messageBoxService.ErrorDialog("Physical data is incomplete!");// ("Physical 数据不完整！");
            return null;
        }
        string imgName = string.Empty;

        //加载是Single还是double
        if (Physical.TestSettings.GrabMode == EnumGrabMode.DoubleGrab)
            imgName = focusBasedOn == EnumFocusBasedOn.Inner ? CONST_Inner : CONST_Outer; //加载Inner 还是 Outer
        else
            imgName = CONST_Single;
        if (isTest)
        {
            imgName += "_Test";
        }
        string imgPath = GetImageAbsolutePath(imgName);
        if (!File.Exists(imgPath))
        {
            _messageBoxService.ErrorDialog($"Please complete the {focusBasedOn}  focusing operation!");
            return null;
        }
        try
        {
            Bitmap bitmap = new Bitmap(imgPath);
            return ImageDataExtensions.ToImageData(bitmap);
        }
        catch (Exception e)
        {
            _messageBoxService.ErrorDialog(e.Message);
            return null;
        }
    }

    /// <summary>
    /// Single Focus
    /// </summary>
    [RelayCommand]
    void TrainFocus_Single()
    {
        //TestTrainSetting.SingleFocusImagePath = Path.Combine(_recipePath, "Single.bmp");
        //TestTrainSetting.OuterFocusImagePath ="Outer.bmp";
        //TestTrainSetting.InnerFocusImagePath ="Inner.bmp";
        var focusPhysical = Physical.FocusSettings as FocusPhysical;


        if ((focusPhysical.FocusBasedOn == EnumFocusBasedOn.Inner && TestTrainSetting.InnerFocusROI == null) ||
            (focusPhysical.FocusBasedOn == EnumFocusBasedOn.Outer && TestTrainSetting.OuterFocusROI == null))
        {
            _messageBoxService.ErrorDialog("Please draw the corresponding ROI！");
            return;
        }
        if ((TestTrainSetting.InnerFocusROI.Top == 100 && TestTrainSetting.InnerFocusROI.Left == 100 && TestTrainSetting.InnerFocusROI.Width == 100 && TestTrainSetting.InnerFocusROI.Height == 100) || (TestTrainSetting.OuterFocusROI.Top == 100 && TestTrainSetting.OuterFocusROI.Left == 100 && TestTrainSetting.OuterFocusROI.Width == 100 && TestTrainSetting.OuterFocusROI.Height == 100))
        {
            _messageBoxService.ErrorDialog("Please draw the corresponding ROI！");
            return;
        }

        TrainFocusAction(
            (innerRoiDesc, innerROI, fineFocusInfo, fineCurveInfo) =>
            {
                HpImageViewer.UpdateROIs(innerRoiDesc);
                var res_inner = _focusService.FineFocusTrain(fineFocusInfo,
                                                                  CreateTrainArg(EnumGrabMode.SingleGrab),
                                                                  out double sharpness,
                                                                  innerROI);
                fineCurveInfo.SetTrainCurve(CurveSelector.Inner, res_inner.Curve);
                InnerPztOffset = res_inner.ZFit;
                MoveAndWait(null, InnerPztOffset);
                if (focusPhysical.FocusBasedOn != EnumFocusBasedOn.Outer && Physical.TestSettings.GrabMode == EnumGrabMode.SingleGrab)
                    SaveFocusImage(CONST_Single);
                else if (Physical.TestSettings.GrabMode == EnumGrabMode.DoubleGrab)
                    SaveFocusImage(CONST_Inner);
            },
            (outRoiDesc, outROI, fineFocusInfo, fineCurveInfo) =>
            {
                _pztservice.AMov(0.0);
                HpImageViewer.UpdateROIs(outRoiDesc);
                fineFocusInfo.StartPosition = 0;
                var res_outer = _focusService.FineFocusTrain(fineFocusInfo,
                                                                   CreateTrainArg(EnumGrabMode.SingleGrab),
                                                                  out double sharpness,
                                                                  outROI);
                fineCurveInfo.SetTrainCurve(CurveSelector.Outer, res_outer.Curve);
                OuterPztOffset = res_outer.ZFit;

                MoveAndWait(null, OuterPztOffset);
                if (focusPhysical.FocusBasedOn == EnumFocusBasedOn.Outer && Physical.TestSettings.GrabMode == EnumGrabMode.SingleGrab)
                    SaveFocusImage(CONST_Single);
                else if (Physical.TestSettings.GrabMode == EnumGrabMode.DoubleGrab)
                    SaveFocusImage(CONST_Outer);

            }
        );
    }

    /// <summary>
    /// 获取图片绝对路径
    /// </summary>
    /// <param name="imageName">图片名称</param>
    /// <returns></returns>
    private string GetImageAbsolutePath(string imageName)
    {
        var filePath = Path.Combine(_testFilesFolder, imageName + ".bmp");
        return filePath;
    }
    private void SaveFocusImage(string imageName)
    {
        var filePath = GetImageAbsolutePath(imageName);
        var imageData = alignmentService.MoveAndGrab();
        imageData.SaveBmp(filePath);
    }

    //[RelayCommand]
    //void TrainFocus_Double_Inner()
    //{
    //    //TestTrainSetting.InnerFocusImagePath = Path.Combine("Images", "FocusImage", TestInfo.Name + "_inner.bmp");
    //    var focusPhysical = Physical.FocusSettings as FocusPhysical;
    //    if ((focusPhysical.FocusBasedOn == EnumFocusBasedOn.Inner && TestTrainSetting.InnerFocusROI == null))
    //    {
    //        _messageBoxService.ErrorDialog("请绘制对应得ROI！");
    //        return;
    //    }
    //    TrainFocusAction((innerRoiDesc, innerROI, fineFocusInfo, recipeFocusService) => {
    //        HpImageViewer.UpdateROIs(innerRoiDesc);
    //        InnerPztOffset = recipeFocusService.FineAutoClarityFocus(fineFocusInfo, innerROI);
    //        TestTrainSetting.InnerFocusImagePath = recipeFocusService.SaveFocusImage(_recipePath, TestInfo.Name+ "_inner");
    //        MoveAndWait(null, InnerPztOffset);
    //    }, null);
    //}
    //[RelayCommand]
    //void TrainFocus_Double_Outer()
    //{
    //    //TestTrainSetting.OuterFocusImagePath = Path.Combine("Images", "FocusImage", TestInfo.Name + "_outer.bmp");
    //    var focusPhysical = Physical.FocusSettings as FocusPhysical;
    //    if (focusPhysical.FocusBasedOn == EnumFocusBasedOn.Outer && TestTrainSetting.OuterFocusROI == null)
    //    {
    //        _messageBoxService.ErrorDialog("请绘制对应的ROI！");
    //        return;
    //    }
    //    TrainFocusAction(null,(outRoiDesc, outROI, fineFocusInfo, recipeFocusService) => {
    //        _pztservice.AMov(0.0);
    //        HpImageViewer.UpdateROIs(outRoiDesc);
    //        fineFocusInfo.StartPosition = 0;
    //        OuterPztOffset = recipeFocusService.FineAutoClarityFocus(fineFocusInfo, outROI);
    //        TestTrainSetting.OuterFocusImagePath = recipeFocusService.SaveFocusImage(_recipePath, TestInfo.Name+ "_outer");
    //        MoveAndWait(null, OuterPztOffset);
    //    });
    //}
    /// <summary>
    /// 聚焦方法
    /// </summary>
    /// <param name="innererAutoClarityFocus">inner 精聚焦</param>
    /// <param name="outerAutoClarityFocus">outer 精聚焦</param>
    void TrainFocusAction(Action<RectangleDesc, ShapeBase, AutoRunInfo, CurveInfo> innererAutoClarityFocus,
                          Action<RectangleDesc, ShapeBase, AutoRunInfo, CurveInfo> outerAutoClarityFocus)
    {
        LogHelper.LogRunMethodStart();
        ChangeCameraInfo = "Focusing …";
        Task.Run(() =>
        {
            try
            {
                HpImageViewer.UpdateROIs();
                _StageController.StartGrabbing();
                var waferThickness = alignmentService.WaferMapInfo.Thickness;

                var focusInfo = GetTestFocusOrDefault();
                var macroFocusZ = alignmentService.MacroFocusZ;
                //粗聚焦的开始位置为标定的聚焦位置+ 晶圆的厚度
                var coarseFocusInfo = focusInfo.CourseFocusInfo;
                coarseFocusInfo.StartPosition = macroFocusZ + waferThickness;
                var coarseCurveInfo = GetTestFocusCurveOrDefault(true);
                var fineFocusInfo = focusInfo.FineFocusInfo;
                fineFocusInfo.StartPosition = 0;
                var fineCurveInfo = GetTestFocusCurveOrDefault(false);
                LogHelper.LogInfo($"Focus Info:MacroFocus.Z={macroFocusZ:0.######},Wafer Thickness={waferThickness},CoarseFocus ={coarseFocusInfo},FineFocus={fineFocusInfo}");



                //使用内外模板分别获取内外模板的粗聚焦高度 （Z轴高度）
                var innerRoiDesc = TestTrainSetting.InnerFocusROI;
                var innerROI = innerRoiDesc.CreateRoi();
                innerRoiDesc.Color = "#FFFF00";
                var outRoiDesc = TestTrainSetting.OuterFocusROI;
                var outROI = outRoiDesc.CreateRoi();
                outRoiDesc.Color = OutColor;
                HpImageViewer.UpdateROIs(innerRoiDesc, outRoiDesc);

                _pztservice.AMov(0.0);
                var physical = TestInfo.Physical as Physical;
                var trainArg = CreateTrainArg(physical.TestSettings.GrabMode);
                var res = _focusService.CoarseFocusTrain(coarseFocusInfo, trainArg, null, null);
                coarseCurveInfo.SetTrainCurve(CurveSelector.Global, res.Curve);

                InnerFocusHeight = res.Inner;
                OuterFocusHeight = res.Outer;
                var midHeight = (res.Inner + res.Outer) / 2;
                //将Z轴移动到内外框高度的平均值位置，对内层模板进行精聚焦，获取pzt的偏移量
                alignmentService.MoveZTo(midHeight, "MidFocusHeight", false);
                MoveAndWait(null, null);
                // 全视野精聚焦训练
                var fine_res = _focusService.FineFocusTrain(fineFocusInfo, trainArg, out var maxSharpness);
                fineCurveInfo.SetTrainCurve(CurveSelector.Global, fine_res.Curve);
                //Inner 自动精聚焦
                innererAutoClarityFocus(innerRoiDesc, innerROI, fineFocusInfo, fineCurveInfo);
                //outer 自动精聚焦
                outerAutoClarityFocus(outRoiDesc, outROI, fineFocusInfo, fineCurveInfo);
                TrainWLI();
                SetAutoFocusHeight();
                var image = _alignmentService.MoveAndGrab(null);
                //此处需要保存全图的清晰度
                var paramFocus = GetFineParamFocus(IServiceCamera.CurCamera);
                var sharpness = _focusService.GetImageClarity(image, paramFocus: paramFocus);
                TestInfo.TrainSetting.Sharpness = sharpness;
                TestTrainSetting.InnerFocusHeight = midHeight + _pztservice.Direction * InnerPztOffset / 1000 - macroFocusZ;
                TestTrainSetting.OuterFocusHeight = midHeight + _pztservice.Direction * OuterPztOffset / 1000 - macroFocusZ;
                TestTrainSetting.GlobalFocusHeight = midHeight + _pztservice.Direction * fine_res.ZFit / 1000 - macroFocusZ;

                LogHelper.LogInfo($"Focus Result:InnerFocusHeight={InnerFocusHeight:0.######},OuterFocusHeight={OuterFocusHeight:0.######},InnerPztOffset={InnerPztOffset:0.######},OuterPztOffset={OuterPztOffset:0.######},AutoFocusPostion={AutoFocusHeight:0.######}");
            }
            catch (Exception ex)
            {
                AppHelper.DispatcherInvoke(() =>
                {
                    _messageBoxService.ErrorDialog(ex.Message);
                });
            }
            finally
            {
                _StageController.StartGrabbing();
                ChangeCameraInfo = "";
                HpImageViewer.UpdateROIs();
                LogHelper.LogRunMethodEnd("", nameof(TrainFocusAction));
            }
        });

    }

    private void MoveAndWait(double? focusZHeight, double? pztHeight)
    {

        if (focusZHeight != null)
        {
            alignmentService.MoveZTo(focusZHeight.Value, "", false);
        }
        if (pztHeight != null)
        {
            _pztservice.AMov(pztHeight.Value);
        }
        Task.Delay(2000).Wait();
    }

    [RelayCommand]
    void Correlation()
    {
        if (CorrelationCurvesList == null)
            return;

        //var cDChartCurves = new List<CDChartCurve>();
        Datas = new ObservableCollection<KeyValuePair<string, List<CDChartCurve>>>();

        // 校验曲线数量是否符合要求
        if (CorrelationCurvesList.Count != 4)
        {
            _messageBoxService.Error($"CorrelationCurvesList Count Error!, Count = {CorrelationCurvesList?.Count}");
            return;
        }


        // 构建曲线数据
        var configurations = new[]
        {
            new { Name = "Right To Left", Indices = new[] { 0, 2 } },
            new { Name = "Bottom To Top", Indices = new[] { 1, 3 } }
        };

        foreach (var config in configurations)
        {
            var curves = config.Indices.Select(index => new CDChartCurve
            {
                Name = index == 0 || index == 1 ? "Inner" : "Outer",
                Xs = CorrelationCurvesList[index].Xs,
                Ys = CorrelationCurvesList[index].Ys,
                IsSelected = false
            }).ToList();

            Datas.Add(new KeyValuePair<string, List<CDChartCurve>>(config.Name, curves));
        }

        var dialogParameters = new DialogParameters
        {
            { "Datas", Datas }
        };

        _dialogService.ShowDialog(nameof(ChartResult_ovl), dialogParameters, _ => { });
    }

    #region Focus ROI
    #region Inner
    /// <summary>
    ///
    /// </summary>
    [RelayCommand]
    public void InnerFocusROI_Set()
    {
        if (HpImageViewer.Image == null)
        {
            _messageBoxService.InfoDialog("The current image data is invalid！");
            return;
        }
        var innerRoi = TestTrainSetting?.InnerFocusROI;
        if (innerRoi != null && innerRoi.Top != 100 && innerRoi.Left != 100 && innerRoi.Width != 100 && innerRoi.Height != 100)
            HpImageViewer.UpdateROIs((RectangleDesc)CloneHelper.ReflectionClone(innerRoi));
        else
            FocusROI(true);

        FocusROI_SetEnabled = false;
        FocusROI_InnerEnabled = true;
        FocusROI_OuterEnabled = false;
    }
    /// <summary>
    ///
    /// </summary>
    [RelayCommand]
    public void InnerFocusROI_Confirm()
    {
        //ButtonResult confirm =  _messageBoxService.AskDialog("确认保存这个ROI设定嘛！");
        //if (confirm==ButtonResult.OK)
        //{
        if (HpImageViewer.Image == null)
        {
            _messageBoxService.InfoDialog("The current image data is invalid！");
        }
        var roi = HpImageViewer.ImageViewerDesc.ROIDescs?.FirstOrDefault() as RectangleDesc;
        if (roi != null)
            TestTrainSetting.InnerFocusROI = roi;
        InitFocusRoiButton();
        HpImageViewer.UpdateROIs();

        //}
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
    #endregion

    [RelayCommand]
    public void OuterFocusROI_Set()
    {
        if (HpImageViewer.Image == null)
        {
            _messageBoxService.InfoDialog("The current image data is invalid！");
            return;
        }
        var outerRoi = TestTrainSetting?.OuterFocusROI;
        if (outerRoi != null && outerRoi.Top != 100 && outerRoi.Left != 100 && outerRoi.Width != 100 && outerRoi.Height != 100)
            HpImageViewer.UpdateROIs((RectangleDesc)CloneHelper.ReflectionClone(outerRoi));
        else
            FocusROI(false);

        FocusROI_SetEnabled = false;
        FocusROI_InnerEnabled = false;
        FocusROI_OuterEnabled = true;

    }
    [RelayCommand]
    public void OuterFocusROI_Confirm()
    {
        //ButtonResult confirm = _messageBoxService.AskDialog("确认保存这个ROI设定嘛！");
        //if (confirm == ButtonResult.OK)
        //{
        if (HpImageViewer.Image == null)
        {
            _messageBoxService.InfoDialog("The current image data is invalid！");
        }
        var roi = HpImageViewer.ImageViewerDesc.ROIDescs?.FirstOrDefault() as RectangleDesc;
        if (roi != null)
            TestTrainSetting.OuterFocusROI = roi;
        InitFocusRoiButton();
        HpImageViewer.UpdateROIs();
        //}
    }

    public void FocusROI(bool isInner)
    {
        if (HpImageViewer.Image == null)
        {
            _messageBoxService.InfoDialog("The current image data is invalid！");
        }

        double width = 200;
        double height = 200;
        var top = (((ImageData)HpImageViewer.Image).Height - height) / 2;
        var left = (((ImageData)HpImageViewer.Image).Width - width) / 2;
        var inRoi = TestTrainSetting.OverlayMark_InnerModels[0].ROI;
        var outRoi = TestTrainSetting.OverlayMark_OuterModels[0].ROI;
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
    }
    public void InitMeasurementRoiButton()
    {
        MeasurementROI_SetEnabled = true;
        MeasurementROI_InnerEnabled = false;
        MeasurementROI_OuterEnabled = false;
    }
    #endregion

}
