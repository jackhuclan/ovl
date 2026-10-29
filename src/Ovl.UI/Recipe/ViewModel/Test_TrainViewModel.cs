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
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.StageControlModule.Views;
using InteVega.CBB.SystemConfig.Interface;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using Marvel.Application.Services;
using Marvel.DataModels.Core;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Wafer.Algorithm.Common.DoNet;
using Point = HPImageViewer.Core.Primitives.Point;

using Ovl.UI.Controls;
using Ovl.UI.Models;
namespace Ovl.UI.Recipe.ViewModel
{
    public partial class Test_TrainViewModel : Test_TrainViewModelBase<TestInfo_BiB>
    {
        public Test_TrainViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _pztservice = _serviceProvider.GetService<IAsyncAblePztService>();

            alignmentService = _serviceProvider.GetRequiredService<IAlignmentService>();
            _servCalDat = serviceProvider.GetService<IServiceCalData>();


            _templateContext2_inner ??= new MultiTemplateMatchingContext();
            _templateContext2_outer ??= new MultiTemplateMatchingContext();


        }
        const string CONST_Average = "Average";
        const string CONST_Inner = "Inner";
        const string CONST_Outer = "Outer";
        const string CONST_PREVIEW = "PreviewMatch";
        const string _messageBoxToken = "TestTrainMessage";
        private List<CorrelationCurve> CorrelationCurvesList;
        private bool IsTrained { get; set; }
        IAlignmentService alignmentService;
        IAsyncAblePztService _pztservice;

        MultiTemplateMatchingContext _templateContext2_inner;
        MultiTemplateMatchingContext _templateContext2_outer;

        public ObservableCollection<KeyValuePair<string, List<CDChartCurve>>> Datas = default;
        /// <summary>
        /// 图像控件
        /// </summary>
        public IHPImageViewer HpImageViewer { get; set; }
        public StageFocusControlView StageFocusControlView { get; set; }
        IServiceCalData _servCalDat;


        [ObservableProperty]
        MultiTemplateMatchingViewModel _templateViewModel_Inner;
        [ObservableProperty]
        MultiTemplateMatchingViewModel _templateViewModel_Outter;

        [ObservableProperty]
        private ObservableCollection<BIBAlgorithmType> _enumBibAlgorithmType;
        [ObservableProperty]
        private ObservableCollection<AIMAlgorithmType> _enumAimAlgorithmType;
        [ObservableProperty]
        private ObservableCollection<CorrelationType> _enumCorrelationType;
        [ObservableProperty]
        private ObservableCollection<MeasurementResultData> _measurementResultDataList;
        [ObservableProperty]
        private double _innerFocusHeight;
        [ObservableProperty]
        private double _outerFocusHeight;
      
        [ObservableProperty]
        private Visibility _bibVisibility = Visibility.Collapsed;
        [ObservableProperty ]
        private Visibility _reuseVisibility= Visibility.Collapsed;
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

        //被点中的层
        [ObservableProperty]
        private EnumFocusBasedOn _checkedLayer = EnumFocusBasedOn.Inner;
        [ObservableProperty]
        private bool _isReuseMeasurementROI_Inner = true;
        [ObservableProperty]
        private bool _isReuseMeasurementROI_Outer = true;


        [ObservableProperty]
        private int _roiExtendSize_Inner = 40;
        [ObservableProperty]
        private int _roiExtendSize_Outer = 40;

        [ObservableProperty]
        private bool _isFocusSingle;

        [ObservableProperty]
        private BIBAlgorithmParam _bibAlgorithmParam;
        [ObservableProperty]
        private AIMAlgorithmParam _aIMAlgorithmParam;
        [ObservableProperty]
        Visibility _correctionVisibility;
        [ObservableProperty]
        Visibility _innerCorrectionVisibility;
        [ObservableProperty]
        Visibility _outerCorrectionVisibility;
        [ObservableProperty]
        double _innerCorrectionValue;
        [ObservableProperty]
        double _outerCorrectionValue;

        double _lastInnerCorrection = 10000;
        double _lastOuterCorrection = 10000;
        bool isUpdatedMeasureRoi = false;
        /// <summary>
        /// 全行程聚焦结果
        /// </summary>
        [ObservableProperty]
        FocusPegResult fine_Res;
        const string OutColor = "#00FFFF";


        partial void OnInnerCorrectionValueChanged(double value)
        {
            if (Fine_Res == null) return;
            _pztservice.AMov(InnerPztOffset + value);
        }

        partial void OnOuterCorrectionValueChanged(double value)
        {
            if (Fine_Res == null) return;
            _pztservice.AMov(OuterPztOffset + value);
        }

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

        FocusPhysical _focusSetting
        {
            get
            {
                return Physical.FocusSettings as FocusPhysical;
            }
        }

        EnumGrabMode _grabMode
        {
            get
            { return Physical.TestSettings.GrabMode; }
        }

        TestOverlayTrainParam TrainParam
        {
            get
            {
                return TestTrainSetting.TrainParam;
            }
        }

        bool _isSingleGrab
        {
            get
            { return _grabMode == EnumGrabMode.SingleGrab_Average || _grabMode == EnumGrabMode.SingleGrab; }
        }

        public override string Title => "Train Test";

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);
            if (TestInfo != null)
            {
                if (IsEnableWLI && TestInfo.TrainSetting_BiB.FocusModuleInfo.WliFocusOffset != 0)
                {
                    AbsolateWliHeight = TestInfo.TrainSetting_BiB.GetBibFocusHeight(EnumOvlFocusOffsetType.Wli) + _alignmentService.MacroFocusZ;
                }
            }
        }

        [RelayCommand]
        async void Loaded()
        {
            await ChangeMeasureCamera();
            BindCheckBox();
            // LoadOvResult();
            LogHelper.LogInfo($"Test_TrainViewModel Loaded:MacroFocusZ={alignmentService.MacroFocusZ},BaseFocusHeight={TestTrainSetting.FocusModuleInfo.BaseFocusHeight}");
            if (TestTrainSetting.FocusModuleInfo.BaseFocusHeight == 0)
            {
                TestTrainSetting.FocusModuleInfo.BaseFocusHeight = alignmentService.WaferMapInfo.Thickness;
            }

            InnerFocusHeight = TestTrainSetting.GetBibFocusHeight(EnumOvlFocusOffsetType.Inner) + alignmentService.MacroFocusZ;
            OuterFocusHeight = TestTrainSetting.GetBibFocusHeight(EnumOvlFocusOffsetType.Outer) + alignmentService.MacroFocusZ;
            RoiExtendSize_Inner = TestTrainSetting.RoiExtendSize_Inner;
            RoiExtendSize_Outer = TestTrainSetting.RoiExtendSize_Outer;


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
                case EnumTargetType.LS:
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
                BibVisibility = Visibility.Collapsed;
            }
            else
            {
                BibVisibility = Visibility.Visible;
            }
            CorrectionVisibility = _grabMode != EnumGrabMode.SingleGrab_Average && _focusSetting.IsEnableFocusCorrection ? Visibility.Visible : Visibility.Collapsed;
            InnerCorrectionVisibility = !_isSingleGrab || _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner ? Visibility.Visible : Visibility.Collapsed;
            OuterCorrectionVisibility = !_isSingleGrab || _focusSetting.FocusBasedOn == EnumFocusBasedOn.Outer ?
                Visibility.Visible : Visibility.Collapsed;
            InnerCorrectionValue = TestTrainSetting.TrainParam.InnerLayer.FocusCorrection;
            OuterCorrectionValue = TestTrainSetting.TrainParam.OuterLayer.FocusCorrection;

            InitFocusRoiButton();
            InitMeasurementRoiButton();
            IsFocusSingle = true;
            IsTrained = TestInfo.IsTrained;

            //切换显示
            if (_grabMode == EnumGrabMode.DoubleGrab)
            {
                IsFocusSingle = false;
            }

            if (MarvelVersion.CurrentVersion < EnumOMVersion.OM100)
            {
                ReuseVisibility = Visibility.Visible;
            }
            else
            {
                TestTrainSetting.IsReuseMeasurementROI_Inner = true;
                TestTrainSetting.IsReuseMeasurementROI_Outer = true; ;
            }
            //是否显示创建模板按钮，如果是复用量测Roi的时候，则不显示创建模板和删除模板按钮
            TemplateViewModel_Inner.IsShowCreateModel = !TestTrainSetting.IsReuseMeasurementROI_Inner;
            TemplateViewModel_Outter.IsShowCreateModel = !TestTrainSetting.IsReuseMeasurementROI_Outer;

            if (HpImageViewer.Image == null)
                _visionService.StartGrabbing();
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
            EnumAimAlgorithmType = new ObservableCollection<AIMAlgorithmType>();
            foreach (var e in Enum.GetValues<AIMAlgorithmType>())
            {
                EnumAimAlgorithmType.Add(e);
            }
        }
        private void SetAutoFocusHeight(double midZHeight, double midPztHeight)
        {
            try
            {
                var baseOn = _focusSetting.FocusBasedOn;
                //此处SigleGrab模式移动pzt到具体的BaseOn层，其它模式都移动到中间层
                if (_grabMode == EnumGrabMode.SingleGrab)
                {
                    switch (baseOn)
                    {
                        case EnumFocusBasedOn.Inner:
                            midPztHeight = InnerPztOffset;
                            break;
                        case EnumFocusBasedOn.Outer:
                            midPztHeight = OuterPztOffset;
                            break;
                    }
                }
                AutoFocusHeight = midZHeight + _pztservice.Direction * midPztHeight / 1000;
                //此处的高度是绝对坐标
                alignmentService.MoveZTo(midZHeight, "TrainedFocusHeight", false);
                _pztservice.AMov(midPztHeight);
            }
            catch (Exception)
            {
            }
        }

        Point2d _innerMainPoint;
        Point2d _outerMainPoint;

        #region Focus ROI

        /// <summary>
        /// 设置临时聚焦Roi
        /// </summary>
        /// <param name="isInner"></param>
        private void SetTempFocusRoi(bool isInner)
        {
            if (HpImageViewer.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return;
            }
            var roi = isInner ? TrainParam.InnerLayer.FocusROI : TrainParam.OuterLayer.FocusROI;
            if (roi != null)
                HpImageViewer.UpdateROIs((RectangleDesc)CloneHelper.ReflectionClone(roi));
            else
                FocusROI(isInner);

            if (_isCPLMode)
                SetCPLParam(isInner);

            FocusROI_SetEnabled = false;
            FocusROI_InnerEnabled = isInner;
            FocusROI_OuterEnabled = !isInner;
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

        #endregion

        /// <summary>
        /// Single Focus 
        /// </summary>
        [RelayCommand]
        void TrainFocus_Single()
        {

            if (TrainParam.InnerLayer.FocusROI == null || TrainParam.OuterLayer.FocusROI == null)
            {
                _messageBoxService.ErrorDialog("Please draw the corresponding ROI！");
                return;
            }

            LogHelper.LogRunMethodStart();
            ChangeCameraInfo = "Focusing …";
            Task.Run(() =>
            {
                try
                {
                    HpImageViewer.UpdateROIs();
                    _visionService.StartGrabbing();
                    var waferThickness = alignmentService.WaferMapInfo.Thickness;

                    var focusInfo = GetTestFocusOrDefault();
                    var macroFocusZ = alignmentService.MacroFocusZ;
                    //粗聚焦的开始位置为标定的聚焦位置+ 晶圆的厚度
                    var coarseFocusInfo = focusInfo.CourseFocusInfo;
                    coarseFocusInfo.StartPosition = macroFocusZ + waferThickness;
                    var fineFocusInfo = focusInfo.FineFocusInfo;


                    LogHelper.LogInfo($"Focus Info:MacroFocus.Z={macroFocusZ:0.######},Wafer Thickness={waferThickness},CoarseFocus ={coarseFocusInfo}");

                    //使用内外模板分别获取内外模板的粗聚焦高度 （Z轴高度）
                    var innerRoiDesc = TrainParam.InnerLayer.FocusROI;
                    innerRoiDesc.Color = "#FFFF00";
                    var outRoiDesc = TrainParam.OuterLayer.FocusROI;
                    outRoiDesc.Color = OutColor;
                    HpImageViewer.UpdateROIs(innerRoiDesc, outRoiDesc);

                    _pztservice.AMov(0.0);

                    var trainArg = CreateTrainArg(_grabMode);
                    var res = _focusService.CoarseFocusTrain(coarseFocusInfo, trainArg);
                    TrainParam.CoarseFocusCurve = res.Curve;

                    InnerFocusHeight = res.ZFit;
                    OuterFocusHeight = res.ZFit;
                    var midHeight = (InnerFocusHeight + OuterFocusHeight) / 2;
                    //将Z轴移动到内外框高度的平均值位置，对内层模板进行精聚焦，获取pzt的偏移量
                    alignmentService.MoveZTo(midHeight, "MidFocusHeight", false);
                    MoveAndWait(null, null);

                    // 全视野、pzt全行程、小步进精聚焦训练
                    var allRangeFocusInfo = CreateFineTrainInfo();
                    // fine_Res = _focusService.FineFocusWithImages(allRangeFocusInfo , out var maxSharpness);
                    Fine_Res = _focusService.FineFocusTrain(allRangeFocusInfo, trainArg, out var maxSharpness);
                    TrainParam.GlobalFineFocusCurve = Fine_Res.Curve;
                    InnerPztOffset = FocusLayer(EnumFocusBasedOn.Inner, false);
                    OuterPztOffset = FocusLayer(EnumFocusBasedOn.Outer, false);
                    SaveFocusRoiLayersImage();
                 
                    StageFocusControlView.SetAxisZMoveable(false);
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
                    _visionService.StopGrabbing();
                    HpImageViewer.UpdateROIs();
                    LogHelper.LogRunMethodEnd("", nameof(TrainFocus_Single));
                }
            });

        }

        /// <summary>
        /// 创建精聚焦训练行程和步进
        /// 行程和步进使用系统配置的参数，默认全行程为40，步进用0.1
        /// </summary>
        /// <returns></returns>
        private AutoRunInfo CreateFineTrainInfo()
        {
            var iCfg = _serviceProvider.GetService<ISystemConfigService>();
            var lCfg = iCfg.GetConfig<FocusCfg>();
            var measCfg = lCfg.MeasPztMeter;
            return new AutoRunInfo() { Range = measCfg.AllRange, Step = measCfg.AllRangeStep, SmallRange = 0 };
        }

        /// <summary>
        /// 采集聚焦Roi的内外层和中间层的图片，用于后面做量测Roi 
        /// </summary>
        private void SaveFocusRoiLayersImage()
        {
            //采图前需要开启
         
            SaveLayerImage(EnumFocusBasedOn.Inner, InnerPztOffset, false);
            SaveLayerImage(EnumFocusBasedOn.Outer, OuterPztOffset, false);
            SaveAverageImage();
           
        }

        /// <summary>
        /// 聚焦内外层
        /// </summary>
        /// <param name="layer">层</param>
        /// <param name="isMeasure">是否是量测roi 聚焦</param>
        /// <returns></returns>
        private double FocusLayer(EnumFocusBasedOn layer, bool isMeasure)
        {
            var layerParam = layer == EnumFocusBasedOn.Inner ? TestTrainSetting.TrainParam.InnerLayer : TestTrainSetting.TrainParam.OuterLayer;
            var roi = isMeasure ? layerParam.MeasureROI.CreateRoi() : layerParam.FocusROI.CreateRoi();
            var focusImages = Fine_Res.PegImages;
            var trainArg = CreateTrainArg(_grabMode);
            var fineFocusInfo = GetTestFocusOrDefault().FineFocusInfo;
            //使用全视野的聚焦序列图进行内、外层聚焦，并且获取聚焦曲线
            var ret = _focusService.TrainCurve(fineFocusInfo, roi, focusImages, trainArg);
            double pztOffset = ret.ZFit;
            layerParam.RoiFocusCurve = ret.Curve;
            return pztOffset;
        }


        /// <summary>
        /// 获取某层的最佳焦面并且采集量测图片
        /// </summary>
        /// <param name="layer">内、外层</param>
        /// <param name="isMeasure">是否是量测过程</param>
        /// <returns>返回最佳焦面的pzt位置</returns>
        private void SaveLayerImage(EnumFocusBasedOn layer, double pztOffset, bool isMeasure)
        {
            var layerParam = layer == EnumFocusBasedOn.Inner ? TestTrainSetting.TrainParam.InnerLayer : TestTrainSetting.TrainParam.OuterLayer;
            var roi = isMeasure ? layerParam.MeasureROI.CreateRoi() : layerParam.FocusROI.CreateRoi();
            var imgName = layer == EnumFocusBasedOn.Inner ? CONST_Inner : CONST_Outer;

            LogHelper.LogInfo($"Move to {layer} layer focus:pztOffset={pztOffset} ");
            MoveAndWait(null, pztOffset);
            //存储对应层自动聚焦最佳焦面的图片，并且使用该图片获取FocusKernel
            SaveFocusImage(imgName, roi);

            if (isMeasure)
            {
                if (layer == _focusSetting.FocusBasedOn)
                {
                    //采集Base层的图片，并且使用该图片创建预匹配模板
                    LogHelper.LogInfo($"Move to previewmatch layer measure focus:Base Layer={_focusSetting.FocusBasedOn}");
                    SaveFocusImage(CONST_PREVIEW, null);
                }
            }
        }

        private void SaveAverageImage()
        {
            var midPztHeight = (InnerPztOffset + OuterPztOffset) / 2;
            var midZHeight = (InnerFocusHeight + OuterFocusHeight) / 2;
            SetAutoFocusHeight(midZHeight, midPztHeight);
            //如果不是基于中间层量测，则不需要采集中间层的图像
            if (_grabMode != EnumGrabMode.SingleGrab_Average) return;
            MoveAndWait(null, midPztHeight);
            SaveFocusImage(CONST_Average, null);
        }
        private bool SaveCorrectImage(EnumFocusBasedOn layer, double pztOffset)
        {
            if (Fine_Res == null) return false; //没有聚焦过，则不采集焦面补偿图片，因为此时的焦面补偿图片的位置不一定准确


            //如果开启了手动焦面补偿，则采集补偿后的焦面作为量测原图，并以此图来创建量测的定位模板
            if (_focusSetting.IsEnableFocusCorrection)
            {
                var correctionVisibility = layer == EnumFocusBasedOn.Inner ? InnerCorrectionVisibility : OuterCorrectionVisibility;
                if (correctionVisibility != Visibility.Visible)
                    return false;
                //如果补偿焦面未发生变化，则不需要重新采图
                //如果更新了量测roi ，相应的内外层最佳焦面也会发生变化，所以需要重新获取补偿后的焦面图像
                if (layer == EnumFocusBasedOn.Inner && DoubleHelper.EqualsExt(InnerCorrectionValue, _lastInnerCorrection) && !isUpdatedMeasureRoi)
                {
                    return false;
                }
                else if (layer == EnumFocusBasedOn.Outer && DoubleHelper.EqualsExt(OuterCorrectionValue, _lastOuterCorrection) && !isUpdatedMeasureRoi)
                {
                    return false;
                }

                var layerParam = layer == EnumFocusBasedOn.Inner ? TestTrainSetting.TrainParam.InnerLayer : TestTrainSetting.TrainParam.OuterLayer;
                var imgName = layer == EnumFocusBasedOn.Inner ? CONST_Inner : CONST_Outer;
                var correctionValue = layer == EnumFocusBasedOn.Inner ? InnerCorrectionValue : OuterCorrectionValue;
                LogHelper.LogInfo($"Move to {layer} layer correction focus:pztOffset={pztOffset},FocusCorrection={correctionValue}");
                MoveAndWait(null, pztOffset + correctionValue);
                layerParam.FocusCorrection = correctionValue;
                //此处的Roi 需要置为空，不能使用补偿后的焦面获取FocusKernel
                _visionService.StartGrabbing();
                SaveFocusImage(imgName, null);
                if (layer == EnumFocusBasedOn.Inner)
                    _lastInnerCorrection = InnerCorrectionValue;
                else
                    _lastOuterCorrection = OuterCorrectionValue;
                return true;
            }
            return false;
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
            //Task.Delay(2000).Wait();
        }


        #region MeasureRoi

        private ImageData ReadImageFile(string filePath)
        {
            try
            {
                return ImageDataExtensions.ReadImage(filePath);
            }
            catch (Exception e)
            {
                _messageBoxService.ErrorDialog(e.Message);
                return null;
            }
        }

        /// <summary>
        /// 量测添加ROI前 加载聚焦生成的原图
        /// </summary>
        private ImageData LoadFocusImageData(bool isInner)
        {

            if (Physical == null || Physical.TestSettings == null)
            {
                _messageBoxService.ErrorDialog("Physical data is incomplete!");// ("Physical 数据不完整！");
                return null;
            }

            //SingleGrab-Averagae 两层都取 Average的图
            //SingleGrab 两层都取BaseOn的图
            //DoubleGrab / CPL 取内、外层不同的图
            string imgName = CONST_Average;

            if (_grabMode == EnumGrabMode.SingleGrab)
            {
                imgName = _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner ? CONST_Inner : CONST_Outer;
            }
            else if (!_isSingleGrab)
            {
                imgName = isInner ? CONST_Inner : CONST_Outer;
            }

            return LoadImageData(imgName);
        }

        private ImageData LoadImageData(string imgName)
        {
            string imgPath = GetImageAbsolutePath(imgName);
            if (!File.Exists(imgPath))
            {
                _messageBoxService.ErrorDialog($"Please complete the {imgName}  focusing operation!");
                return null;
            }
            return ReadImageFile(imgPath);
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


        private void SaveFocusImage(string imageName, ShapeBase roi)
        {
            var imageData = alignmentService.MoveAndGrab();
            //删除原图
            var filePath = GetImageAbsolutePath(imageName);
            LogHelper.LogError($"Save layer image,path={filePath}");
            FileHelper.DeleteFile(filePath);
            imageData.SaveBmp(filePath);

            if (roi != null)
            {
                //测试代码
                //if(imageData ==null )
                //{
                //    imageData =LoadImageData(imageName);
                //}
                var kernel = _focusService.GetImageKernel(imageData, roi);
                if (imageName == CONST_Inner)
                {
                    TrainParam.InnerLayer.FocusKernel = kernel;
                }
                else
                {
                    TrainParam.OuterLayer.FocusKernel = kernel;
                }
            }
        }


        void CreateMeasureRoi(bool isInner)
        {

            if (HpImageViewer.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return;
            }


            var imageData = (ImageData)HpImageViewer.Image;
            double width = imageData.Width / 2;
            double height = imageData.Height / 2;
            var top = (imageData.Height - height) / 2;
            var left = (imageData.Width - width) / 2;

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
                            var roi = GetRangeRoi(imageData);
                            using var hImage = imageData.ToHObject();
                            OMUtil.AutoGenMeasureRoiOfBIB(hImage, roi, omType, OMTargetLayer.InnerAndOuter, out Bib2d innerRoi, out Bib2d outerRoi);
                            var bib2d = isInner ? innerRoi : outerRoi;
                            var bibAxis = bib2d?.Box?.Axis;
                            if (bibAxis != null && bibAxis.Width != 0 && bibAxis.Height != 0)
                            {
                                rOIDescs = new QuadRectangleDesc() { Top = bibAxis.Y, Left = bibAxis.X, Width = bibAxis.Width, Height = bibAxis.Height, BandWidth = bib2d.Box.Width, BandLength = bib2d.BandLength   };
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
                    var roiAim = GetRangeRoi(imageData);
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
                case EnumTargetType.LS:
                    break;
                case EnumTargetType.Aperture:
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

        private Rect2d GetRangeRoi(ImageData imageData)
        {
            var roi = HpImageViewer.ImageViewerDesc.ROIDescs?.FirstOrDefault()?.CreateRoi() as Rect2d;
            //如果不是矩形roi，则忽略其范围，如果是矩形roi，则在指定的矩形roi内自动查找内外侧
            if (roi != null)
            {
                TestInfo.TrainSetting_BiB.AutoRoiRange = roi;
            }
            else
            {
                roi = TestInfo.TrainSetting_BiB.AutoRoiRange;
            }
            if (roi == null)
            {
                roi = new Rect2d(0, 0, imageData.Width, imageData.Height);
            }

            return roi;
        }

        void SetMeasureRoi(bool isInner)
        {

            var roi = isInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
            if (_isCPLMode)
                SetCPLParam(isInner);
            ImageData imageData = LoadFocusImageData(isInner);
            if (imageData == null)
            {
                return;
            }
            _visionService.StopGrabbing();
            HpImageViewer.Image = imageData;
            if (roi != null)
                HpImageViewer.UpdateROIs((RectangleDesc)CloneHelper.ReflectionClone(roi));
            else
                CreateMeasureRoi(isInner);
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
            if (_isCPLMode)
                SetCPLParam(isInner);

            ImageData imageData = LoadFocusImageData(isInner);
            if (imageData == null)
            {
                return;
            }
            _visionService.StopGrabbing();
            HpImageViewer.Image = imageData;
            CreateMeasureRoi(isInner);
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

        bool isConfirming = false;
        private async void ConfirmMeasureRoi(bool isInner)
        {
            if (isConfirming)
                return;
            isConfirming = true;
            try
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
                if (Fine_Res == null)
                {
                    _messageBoxService.ErrorDialog($"Please complete the focusing operation!");
                    return;
                }
                if (isInner)
                {

                    InnerPztOffset =await  MeasureRoiFocus(rOIDescs, EnumFocusBasedOn.Inner);
                }
                else
                {
                    rOIDescs.Color = OutColor;
                    OuterPztOffset =await  MeasureRoiFocus(rOIDescs, EnumFocusBasedOn.Outer);
                }
                SaveVisionParams();
                InitMeasurementRoiButton();
            }
            finally
            {
                isConfirming = false;
            }
        }

        private Task<double> MeasureRoiFocus(ROIDesc rOIDescs, EnumFocusBasedOn layer)
        {
            return Task.Run(() =>
             {
                 if (_isCPLMode)
                     SetCPLParam(layer == EnumFocusBasedOn.Inner);
                 _visionService.StartGrabbing();
                 var layerParam = layer == EnumFocusBasedOn.Inner ? TrainParam.InnerLayer : TrainParam.OuterLayer;
                 //这两行代码不能写反了,因为FocusLayer需要用到MeasureRoi
                 layerParam.MeasureROI = rOIDescs as RectangleDesc;
                 var pztOffset = FocusLayer(layer, true);
                 SaveLayerImage(layer, pztOffset, true);
                 _visionService.StopGrabbing();
                 isUpdatedMeasureRoi = true;// 内层或者外层聚焦之后，需要重新采集中间层图像
                 return pztOffset;
             });
        }

        [RelayCommand]
        void MeasureRoi_Cancel()
        {
            InitMeasurementRoiButton();

        }

        public void InitMeasurementRoiButton(bool isClearRoi = true)
        {
            MeasurementROI_SetEnabled = true;
            MeasurementROI_InnerEnabled = false;
            MeasurementROI_OuterEnabled = false;
            if (isClearRoi)
                HpImageViewer.UpdateROIs();
        }

        #endregion MeasureRoi


        /// <summary>
        /// 创建量测的KernelSize和定位模板
        /// </summary>
        /// <param name="isInner"></param>
        /// <param name="isShowTestResult">是否显示模板匹配结果</param>
        public async Task CreateMeasureKernelSizeAndTemplate(bool isInner, bool isShowTestResult)
        {
            var imageData = LoadFocusImageData(isInner);
            if (imageData  == null)
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
            _visionService.StopGrabbing();
            HpImageViewer.Image = imageData;
            HpImageViewer.UpdateROIs(roiDesc);

            ExResult exResult = null;
            try
            {

                switch (TestTrainSetting.InnerType)
                {
                    case EnumTargetType.Bar:
                    case EnumTargetType.Box:
                    case EnumTargetType.Frame:
                    case EnumTargetType.Circle:
                        exResult = await GenKernelRoiOfBIB(isInner, isShowTestResult);
                        break;
                    case EnumTargetType.Cross:
                        exResult = await GenKernelRoiOfCross(isInner, isShowTestResult);
                        break;
                    case EnumTargetType.AIM:
                    case EnumTargetType.AIM_XYS:
                        SetRoiExtendSize(isInner);
                        exResult = await GenKernelRoiOfAIM(isInner, isShowTestResult);
                        break;
                    default:
                        break;
                }


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
            }

        }

        private async Task CreatePreviewTemplate(ImageData previewImageData)
        {
            //训练预匹配的模板

            var previewTemplate = _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner ? TestTrainSetting.OverlayMark_InnerModels[0] : TestTrainSetting.OverlayMark_OuterModels[0];
            //点击Confirm按钮时，重绘量测Roi，避免被其它roi影响
            var roiDesc = _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
            if (roiDesc == null)
            {
                _messageBoxService.InfoDialog("Please draw the corresponding ROI！");
                return;
            }
            _visionService.StopGrabbing();
            HpImageViewer.Image = previewImageData;
            HpImageViewer.UpdateROIs(roiDesc);
            previewTemplate = (SingleTemplateMatchingContext)CloneHelper.ReflectionClone(previewTemplate);
            previewTemplate.MainTemplateName = "";
            previewTemplate.Name = "PreviewModel";
            TrainModelViewModel trainModelVM = new TrainModelViewModel(_serviceProvider, previewTemplate, false, () => HpImageViewer, false);
            await trainModelVM.TrainModelAsync(previewTemplate.SelectedMatchModelGroup.MatchModel.ParamMatch.MinScore);
            TestTrainSetting.PreviewTemplate = previewTemplate;
        }

        private BIBROI GenKernelRoiOfBIB(RectangleDesc roi, out ExResult exResult)
        {
            //var rectRoi = roi as QuadRectangleDesc;
            //var trainRoi = new Rect2d() { X = rectRoi.Left, Y = rectRoi.Top, Width = rectRoi.Width, Height = rectRoi.Height };
            //Box2d box2D = new Box2d()
            //{
            //    Axis = trainRoi,
            //    Width = rectRoi.BandWidth
            //};
            //int lengthPercent = (int)(rectRoi.BandLength / rectRoi.Height * 100);
            //exResult = OMUtil.GenKernelRoiOfBIB(box2D, lengthPercent, out BIBROI kernelRoi);
            var bib2d = roi.CreateRoi() as Bib2d;
            exResult = OMUtil.GenKernelRoiOfBIB(bib2d, out BIBROI kernelRoi);

            return kernelRoi;
        }



        private CrossROI GenKernelRoiOfCross(RectangleDesc roi, out ExResult exResult)
        {
            var rectRoi = roi as Cross2dRectangleDesc;
            //Cross2d cross2D = new Cross2d();
            //cross2D.Axis = new Polygon2d();
            //cross2D.Axis.Axis = new List<Point2d>();
            //cross2D.Width = rectRoi.ColumnWidth;
            //foreach (var item in rectRoi.PointList)
            //{
            //    cross2D.Axis.Axis.Add(new Point2d(item.Item1, item.Item2));
            //}
            int persent = (int)((rectRoi.BandLength / rectRoi.PointLength) * 100);
            Cross2d cross2D = rectRoi.CreateRoi() as Cross2d;
            exResult = OMUtil.GenKernelRoiOfCross(cross2D, persent, out CrossROI kernelRoi);
            return kernelRoi;
        }


        private AIMROI GenKernelRoiOfAIM(RectangleDesc roi, out ExResult exResult)
        {

            AimOrientation aimOrientation = TestInfo.TargetSetting.OrientationType == EnumOrientationType.Clockwise ? AimOrientation.ClockWise : AimOrientation.CounterClockWise;
            //var rectRoi = roi as AimRectangleDesc;
            //var trainRoi = new Aim2d()
            //{
            //    Rect = new Rect2d() { X = rectRoi.Left, Y = rectRoi.Top, Width = rectRoi.Width, Height = rectRoi.Height },
            //    OffsetX = rectRoi.OffsetX,
            //    OffsetY = rectRoi.OffsetY,
            //    RegionHeight = rectRoi.RegionHeight,
            //    RegionWidth = rectRoi.RegionWidth,
            //};
            var aim2d = roi.CreateRoi() as Aim2d;
            exResult = OMUtil.GenKernelRoiOfAIM(aim2d, aimOrientation, out AIMROI kernelRoi);
            return kernelRoi;
        }

        /// <summary>
        /// 自动创建定位模板
        /// </summary>
        /// <param name="isInner"></param>
        private async Task AutoCreateModel(bool isInner, bool isTest)
        {
            var templateVM = isInner ? TemplateViewModel_Inner : TemplateViewModel_Outter;
            if (!templateVM.IsShowCreateModel)
            {
                await templateVM.TrainModel(isTest);
            }
        }
        /// <summary>
        /// 创建bib外框 kernel Size调整框
        /// </summary>
        private async Task<ExResult> GenKernelRoiOfBIB(bool isInner, bool isTest)
        {
            ExResult exResult = null;
            await AutoCreateModel(isInner, isTest);
            CreateKernelModel(isInner, (rectRoi) =>
            {
                return GenKernelRoiOfBIB(rectRoi, out exResult);
            });
            return exResult;
        }
        /// <summary>
        ///  创建Cross外框 kernel Size调整框
        /// </summary>
        private async Task<ExResult> GenKernelRoiOfCross(bool isInner, bool isTest)
        {
            ExResult exResult = null;
            await AutoCreateModel(isInner, isTest);
            CreateKernelModel(isInner, (roi) =>
            {

                return GenKernelRoiOfCross(roi, out exResult);
            });
            return exResult;
        }
        /// <summary>
        ///  创建AIM外框 kernel Size调整框
        /// </summary>
        private async Task<ExResult> GenKernelRoiOfAIM(bool isInner, bool isTest)
        {
            ExResult exResult = null;
            await AutoCreateModel(isInner, isTest);
            CreateKernelModel(isInner, (roi) =>
            {
                var result = GenKernelRoiOfAIM(roi, out exResult);
                return result;
            });
            return exResult;
        }
        private ExResult CreateKernelModel(bool isInner, Func<RectangleDesc, OMROIBase> funcGenKernelRoi)
        {
            var rectRoi = isInner ? TrainParam.InnerLayer.MeasureROI : TrainParam.OuterLayer.MeasureROI;
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
            }
            else
            {
                TestInfo.OMParam.OmROIOuter = oMROIBase;
                TestInfo.OMParam.KernelModelOuter = oMKernelModel;

            }

            return result;
        }


        private async void ModelModified(ITemplateMatchingView.UserModifyArgs e, bool isInner)
        {
            var models = isInner ? TestTrainSetting.OverlayMark_InnerModels : TestTrainSetting.OverlayMark_OuterModels;
            if (e.IsSuccess && (e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelCreated || e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelTested))
            {
                var model = e.Context;
                var imageData = HpImageViewer.Image as ImageData;
                if (imageData == null)
                    return;
                var firstMatchItem = model.MatchingItems[0];
                var point = alignmentService.ConvertImageToAxis(alignmentService.GetCurrentAxisPosition(), firstMatchItem, imageData.Width, imageData.Height);
                //如果不是主模板，需要计算其与主模板直接的偏移量
                if (model == models[0])
                {
                    if (isInner)
                    {
                        _innerMainPoint = point;
                    }
                    else
                    {
                        _outerMainPoint = point;
                    }
                    //只有在创建模板的时候才会保存数据，匹配模板时不更新配方信息
                    if (e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelCreated)
                    {
                        if (isInner)
                        {
                            TestTrainSetting.InnerTrainMatchItem = firstMatchItem;
                        }
                        else
                        {
                            TestTrainSetting.OuterTrainMatchItem = firstMatchItem;
                        }
                        Submit(false);
                    }
                }
                else
                {
                    //只有在创建模板的时候才会保存数据，匹配模板时不更新配方信息
                    if (e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelCreated)
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
            }

            if (e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelCleanUp)
            {
                HpImageViewer.Image = LoadFocusImageData(isInner);
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



        [RelayCommand]
        async void Run()
        {
            //todo FocusCorrect需要判断fine_Res
            //此处只是用来调试时验证数据使用的，需要加断点动态修改isTest的值
            bool isTest = true;//此处为了便于在调试中拖动代码
            isTest = false;
            if (isTest)
            {
                TestMeasurement();
                return;
            }
            bool isInnerImageChanged=false ;
            bool isOuterImageChanged=false ;
            await Task.Run(async () =>
            {
                LogHelper.LogRunMethodStart();



                _visionService.StartGrabbing();
                if (isUpdatedMeasureRoi)
                {
                    //此处需要先采集中间层图片，然后再创建模板，因为如果是基于中间层量测时，需要使用到中间层的图像
                    SaveAverageImage();
                }
                //使用量测Roi更新聚焦位置及聚焦图片
                //只要correction的值发生变化，就需要更新焦面补偿图片
              isInnerImageChanged  = SaveCorrectImage(EnumFocusBasedOn.Inner, InnerPztOffset);
              
                isOuterImageChanged  = SaveCorrectImage(EnumFocusBasedOn.Outer, OuterPztOffset);
                 

                DebuggerService.DebugSharpness(TrainParam.InnerLayer);

                if (!isUpdatedMeasureRoi) return;
                _visionService.StartGrabbing();


                var image = LoadImageData(CONST_PREVIEW);
                //此处需要保存全图的清晰度
                var paramFocus = GetFineParamFocus(IServiceCamera.CurCamera);
                var sharpness = _focusService.GetImageClarity(image, paramFocus: paramFocus);
                await CreatePreviewTemplate(image);



                TrainWLI();
                _visionService.StopGrabbing();
                if (wliPztOffset != null)
                {
                    TestTrainSetting.FocusModuleInfo.WliFocusOffset = wliPztOffset.Value;
                }
                TestInfo.TrainSetting.FocusModuleInfo.Sharpness = sharpness;
                TrainParam.InnerLayer.SharpnessFocusOffset = InnerPztOffset;
                TrainParam.OuterLayer.SharpnessFocusOffset = OuterPztOffset;
                var zFit = Fine_Res.ZFit;
                TestTrainSetting.FocusModuleInfo.GlobalFocusOffset = zFit;
                TestTrainSetting.FocusModuleInfo.BaseFocusHeight = _motionControllerService.GetCurrentZ() - alignmentService.MacroFocusZ;

                LogHelper.LogInfo($"Focus Result:InnerFocusHeight={InnerFocusHeight:0.######},OuterFocusHeight={OuterFocusHeight:0.######},InnerPztOffset={InnerPztOffset:0.######},OuterPztOffset={OuterPztOffset:0.######},GlobalOffset={zFit:0.######},AutoFocusPostion={AutoFocusHeight:0.######}");

                IsTrained = true;
               
            });

            //创建模板的时候，必须在UI线程调用，否则后面显示模板时会报异常
            if (isInnerImageChanged || isUpdatedMeasureRoi)
            {
                //自动创建定位模板和 kernel Model
                await CreateMeasureKernelSizeAndTemplate(true, false);
            }
            if (isOuterImageChanged || isUpdatedMeasureRoi)
            {
                //自动创建定位模板和 kernel Model
                await CreateMeasureKernelSizeAndTemplate(false, false);
            }
            isUpdatedMeasureRoi = false;
            using var imgObjectInner = LoadFocusImageData(true)?.ToHObject();
            if (imgObjectInner == null) return;
            using var imgObjectOuterOwned = _grabMode == EnumGrabMode.DoubleGrab ? LoadFocusImageData(false)?.ToHObject() : null;
            var imgObjectOuter = imgObjectOuterOwned ?? imgObjectInner;
            if (imgObjectOuter == null) return;

            if (imgObjectInner == null || imgObjectOuter == null) return;
            if (TestInfo.OMParam.KernelModelInner == null || TestInfo.OMParam.KernelModelOuter == null)
            {
                _messageBoxService.ErrorDialog("Please set measurement roi first!");// ("Physical 数据不完整！");
                return;
            }

            //清空Roi
            HpImageViewer.UpdateROIs();
            var _measService = _serviceProvider.GetService<IMeasurementService>();

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

            var x = TrainParam.InnerLayer.MeasureROI.CenterX;
            var y = TrainParam.InnerLayer.MeasureROI.CenterY;
            var transMat = _measService.GetTransMat(y, x, new MatchingItem() { Column = x, Row = y }, 0);

            var (ovlOffsetX, ovlOffsetY) = Physical?.GetOvlOffset() ?? (0, 0);
            var ovResult = _measService.MeasureOverlay(imgObjectInner, imgObjectOuter, TestInfo.OMParam, transMat, transMat, ovlOffsetX, ovlOffsetY);
            ShowResult(ovResult, imgObjectInner, imgObjectOuter);
            //run状态不更新状态
            //Submit(false);
            LogHelper.LogRunMethodEnd();
        }



        private void TestMeasurement()
        {
            var _measService = _serviceProvider.GetService<IMeasurementService>();
            OvResultBase testovResult;
            if (!_isSingleGrab)
            {
                string imageInner = "";
                string imageOuter = "";
                using var imgObjectInner = ReadImageFile(imageInner).ToHObject();
                using var imgObjectOuter = ReadImageFile(imageOuter).ToHObject();
                testovResult = TestOvl(_measService, imgObjectInner, imgObjectOuter);
            }
            else
            {
                using var imgObjectInner = (HpImageViewer.Image as ImageData).ToHObject();
                testovResult = TestOvl(_measService, imgObjectInner, imgObjectInner);
            }
            LoadOvResult(testovResult);
            return;
        }

        private OvResultBase TestOvl(IMeasurementService _measService, HObject _imgInner, HObject _imgOuter)
        {
            bool isReverse = false;
            double angle = isReverse ? 180 : 0;
            var innerModel = TestInfo.TrainSetting_BiB.OverlayMark_InnerModels[0];
            var outerModel = TestInfo.TrainSetting_BiB.OverlayMark_OuterModels[0];
            var innerRefMatchItem = innerModel.MatchingItems[0].ToPoint();
            var innerCenterX = innerRefMatchItem.X;
            var innerCenterY = innerRefMatchItem.Y;
            var outerRefMatchItem = outerModel.MatchingItems[0].ToPoint();
            var outerCenterX = outerRefMatchItem.X;
            var outerCenterY = outerRefMatchItem.Y;
            var matchItemInner = alignmentService.FindTemplateForRun(_imgInner.ToImageData(), innerModel, alignmentService.IsReversed)?.BestMatchItem;
            var matchItemOuter = alignmentService.FindTemplateForRun(_imgOuter.ToImageData(), outerModel, alignmentService.IsReversed)?.BestMatchItem;
            // matchItemOuter = matchItemInner;
            var transMatInner = _measService.GetTransMat(innerCenterY, innerCenterX, matchItemInner, angle);
            var transMatOuter = _measService.GetTransMat(outerCenterY, outerCenterX, matchItemOuter, angle);
            var (offsetX, offsetY) = Physical?.GetOvlOffset() ?? (0, 0);
            var ovl = _measService.MeasureOverlay(_imgInner, _imgOuter, TestInfo.OMParam, transMatInner, transMatOuter, offsetX, offsetY);
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
            if (_isSingleGrab || CheckedLayer == EnumFocusBasedOn.Inner)
            {
                rOIDescs.AddRange(GetProfileForImageView(imgObjectInner, oMParam.OmROIInner, oMParam.TargetType));
                rOIDescs.Add(TrainParam.InnerLayer.MeasureROI);
            }
            if (_isSingleGrab || CheckedLayer == EnumFocusBasedOn.Outer)
            {
                rOIDescs.AddRange(GetProfileForImageView(imgObjectOuter, oMParam.OmROIOuter, oMParam.TargetType));
                rOIDescs.Add(TrainParam.OuterLayer.MeasureROI);
            }

            HpImageViewer.FitImageToArea();
            if (_grabMode == EnumGrabMode.DoubleGrab && CheckedLayer == EnumFocusBasedOn.Inner)
                HpImageViewer.Image = imgObjectInner.ToImageData();
            else if (_grabMode == EnumGrabMode.DoubleGrab && CheckedLayer == EnumFocusBasedOn.Outer)
                HpImageViewer.Image = imgObjectOuter.ToImageData();
            else
                HpImageViewer.Image = imgObjectInner.ToImageData();//inner 和outer相同

            // TestInfo.OvResultBase = centeringOVL;
            //TestInfo.IsTrained = true;

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


        [RelayCommand]
        void Correlation()
        {
            if (CorrelationCurvesList == null)
                return;

            //var cDChartCurves = new List<CDChartCurve>();
            Datas = new ObservableCollection<KeyValuePair<string, List<CDChartCurve>>>();

            // 校验曲线数量是否符合要求
            if (CorrelationCurvesList.Count % 4 != 0)
            {
                _messageBoxService.Error($"CorrelationCurvesList Count Error!, Count = {CorrelationCurvesList?.Count}", _messageBoxToken);
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
        private void Submit(bool isCheckData)
        {
            try
            {
                TestInfo.IsTrained = IsTrained;
                TestTrainSetting.RoiExtendSize_Inner = RoiExtendSize_Inner;
                TestTrainSetting.RoiExtendSize_Outer = RoiExtendSize_Outer;
                TestTrainSetting.IsReuseMeasurementROI_Inner = !TemplateViewModel_Inner.IsShowCreateModel;
                TestTrainSetting.IsReuseMeasurementROI_Outer = !TemplateViewModel_Outter.IsShowCreateModel;
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


        void CheckTemplateData(List<SingleTemplateMatchingContext> templates, string key)
        {
            foreach (SingleTemplateMatchingContext template in templates)
            {
                _templateMatchingService.ReadTemplatesModelInfo(template, _testFilesFolder);
                if (template.SelectedMatchModelGroup == null || template.SelectedMatchModelGroup.MatchModel.ModelInfo == null)
                {
                    _messageBoxService.Error($"The {key} template is empty!", _messageBoxToken);
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
        void SetRoiExtendSize(bool isInner)
        {
            if (isInner)
            {
                foreach (SingleTemplateMatchingContext template in _templateContext2_inner.TemplateContexts)
                {
                    if (template.SelectedMatchModelGroup != null && !TemplateViewModel_Inner.IsShowCreateModel)
                    {

                        template.SelectedMatchModelGroup.RoiExtendSize = RoiExtendSize_Inner;
                    }
                }
            }
            else
            {
                foreach (SingleTemplateMatchingContext template in _templateContext2_outer.TemplateContexts)
                {

                    if (template.SelectedMatchModelGroup != null && !TemplateViewModel_Outter.IsShowCreateModel)
                    {
                        template.SelectedMatchModelGroup.RoiExtendSize = RoiExtendSize_Outer;
                    }
                }
            }
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


        [RelayCommand]
        private void Test()
        {
            // DebuggerService.DebugFocusMove(_serviceProvider, TestInfo, @"E:\工作\开发\调试数据\OM300\OM300-1\图片\聚焦存图\20250409_155654_729");
        }
    }
}
