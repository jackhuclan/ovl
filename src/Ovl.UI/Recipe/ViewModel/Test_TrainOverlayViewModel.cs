using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common.Algorithm;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using InteVega.CBB.TemplateMatchingModule.Views;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using OVL.Algorithm.DoNet;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;


using Ovl.UI.ViewModels.Controls;
using Ovl.UI.ViewModels.Models;
namespace Ovl.UI.Recipe.ViewModel
{
    public abstract partial class Test_TrainOverlayViewModel<T> : Test_TrainViewModelBase<T> where T : TestInfo_Overlay
    {
        public Test_TrainOverlayViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
        { 
            _pztservice = _serviceProvider.GetService<IAsyncAblePztService>();
            _alignmentService = _serviceProvider.GetRequiredService<IAlignmentService>();
            _servCalDat = serviceProvider.GetService<IServiceCalData>();
        }
        protected const string CONST_Average = "Average";
        protected const string CONST_Inner = "Inner";
        protected const string CONST_Outer = "Outer";
        protected const string _messageBoxToken = "TestTrainMessage";
        public override string Title => "Train Test";
 
        protected List<CorrelationCurve> CorrelationCurvesList;
    
        public Action ActionTrainedFocus { get; set; }
        //MultiTemplateMatchingContext _templateContext2_inner;
        //MultiTemplateMatchingContext _templateContext2_outer;

        public ObservableCollection<KeyValuePair<string, List<CDChartCurve>>> Datas = default;
      
        protected IServiceCalData _servCalDat;
    

        //[ObservableProperty]
        //MultiTemplateMatchingViewModel _templateViewModel_Inner;
        //[ObservableProperty]
        //MultiTemplateMatchingViewModel _templateViewModel_Outter;

        [ObservableProperty]
        private ObservableCollection<BIBAlgorithmType> _enumBibAlgorithmType;
        [ObservableProperty]
        private ObservableCollection<AIMAlgorithmType> _enumAimAlgorithmType;
        [ObservableProperty]
        private ObservableCollection<CorrelationType> _enumCorrelationType;
        [ObservableProperty]
        private ObservableCollection<MeasurementResultData> _measurementResultDataList;
    
        [ObservableProperty]
        private Visibility _aIMVisibility = Visibility.Visible;
        [ObservableProperty]
        private Visibility _bibVisibility = Visibility.Collapsed;

       
        [ObservableProperty]
        private EnumFocusBasedOn _selectedLayer = EnumFocusBasedOn.Inner;
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
        private bool _isShowAutoTrainButton = false;


        protected OMParam _omParam;
        protected TrainSettings_Overlay TrainSetting_Overlay
        {
            get
            {
                return GetTrainSettings() as TrainSettings_Overlay;
            }
        }

      
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

        protected bool _isSingleGrab
        {
            get
            { return _grabMode == EnumGrabMode.SingleGrab_Average || _grabMode == EnumGrabMode.SingleGrab; }
        }

        protected bool _isCPLMode
        {
            get
            { return Physical.TestSettings.GrabMode == EnumGrabMode.CPL; }
        }

        protected override ITrainSettings GetTrainSettings()
        {
            return _testTrainService.TrainSetting;
        }

        bool isOpened = false;
        public override void OpenDialogThread(IDialogParameters parameters)
        {
            _testTrainService.SetCurrentTest(TestInfo, TestFilesFolder);
            _omParam = _testTrainService.OMParam;
            TrainSetting = _testTrainService.TrainSetting;
            TestFilesFolder = _testTrainService.TempTrainFolder;
            base.OpenDialogThread(parameters);
            isOpened = true;
        }

        protected Task WaitLoadTemplates()
        {
            return Task.Run(() =>
            {
                while (!isOpened)
                {
                    Thread.Sleep(10);
                }
            });
        }

        [RelayCommand]
        protected async void Loaded()
        {
            await ChangeMeasureCamera();
            await WaitLoadTemplates();
            BindCheckBox();

            switch (TrainSetting_Overlay.InnerType)
            {
                case EnumTargetType.Bar:
                case EnumTargetType.Box:
                case EnumTargetType.Circle:
                case EnumTargetType.Cross:
                case EnumTargetType.Frame:
                    BibAlgorithmParam = (BIBAlgorithmParam)_omParam.AlgorithmParam;
                    break;
                case EnumTargetType.AIM:
                case EnumTargetType.AIM_XYS:
                    AIMAlgorithmParam = (AIMAlgorithmParam)_omParam.AlgorithmParam;
                    break;
                default:
                    break;
            }
            if (TrainSetting_Overlay.InnerType == EnumTargetType.AIM || TrainSetting_Overlay.InnerType == EnumTargetType.AIM_XYS)
            {
                BibVisibility = Visibility.Collapsed;
            }
            else
            {
                BibVisibility = Visibility.Visible;
            }

            IsFocusSingle = _isSingleGrab;
            IsShowAutoTrainButton = TrainSetting_Overlay.IsTrained;

            LoadTemplates();

            if (HpImageViewer.Image == null)
                _visionService.StartGrabbing();
           
        }

        private bool FuncBeforeCleanModel()
        {
            if (TargetPoint == null)
            {
                _messageBoxService.InfoDialog("Please redraw the measure ROI！");
                return false;
            }
            return true;
        }

        protected void SetVisionParamsEnable(bool isInner, bool isFirstDirection)
        {
            IsAxisMoveable = isInner && isFirstDirection;
            if (!IsAxisMoveable)
            {
                HpImageViewer.ActivatedTool = ToolType.ToolPointer;
            }
            if (_isCPLMode)
            {
                IsVisionParamEnable = true;
            }
        }

        protected void SetVisionParamsDisable()
        {
            HpImageViewer.ActivatedTool = ToolType.ToolPointer;
            IsAxisMoveable = false;
            if (_isCPLMode)
            {
                IsVisionParamEnable = false;
            }
        }

        protected override void SetVisionParams()
        {
            //CPL模式下不需要在切换相机的时候设置视觉参数
            //在设置Focus roi的时候分别设置内外层的光源参数
            if (!_isCPLMode)
            {
                base.SetVisionParams();
            }
            else
            {
                IsVisionParamEnable = false;
            }
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

        protected void OnFocusCompleted()
        {
            ActionTrainedFocus?.Invoke();
        }

        Point2d _innerMainPoint;
        Point2d _outerMainPoint;

        protected abstract ROIDesc GetMeasureRoiDesc(bool isInner);





     

        protected OMTargetType ConvetTargetType(EnumTargetType innerType)
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
                    return OMTargetType.AIM;
                case EnumTargetType.AIM_XYS:
                    return OMTargetType.XYSAIM;
                default:
                    return OMTargetType.Bar;
            }
        }

        protected void SaveKernelModel(bool isInner, OMROIBase oMROIBase, OMKernelModel oMKernelModel)
        {
            if (isInner)
            {
                _omParam.OmROIInner = oMROIBase;
                _omParam.KernelModelInner = oMKernelModel;
            }
            else
            {
                _omParam.OmROIOuter = oMROIBase;
                _omParam.KernelModelOuter = oMKernelModel;
            }
        }

        /// <summary>
        /// 保存每一层的黄金模板
        /// </summary>
        /// <param name="isInner">是否是内层</param>
        /// <param name="imageData">量测焦面图像</param>
        /// <param name="oMROIBase">roi </param>
        /// <param name="oMTargetType">标记类型</param>
        protected void SaveGoldenModel(bool isInner, ImageData imageData, OMROIBase oMROIBase, OMTargetType oMTargetType, TestOverlayTrainParam trainParam)
        {
            if (!IsDefectInspectSupported(oMTargetType) || imageData == null || oMROIBase == null || trainParam == null)
                return;
            try
            {
                var layer = isInner ? trainParam.InnerLayer : trainParam.OuterLayer;
                var goldenModel = _focusService.GenDefectInspectModel(imageData, oMROIBase, oMTargetType, layer.PolarityType);
                layer.GoldenModel = goldenModel;
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
            }
        }

        protected static bool IsDefectInspectSupported(OMTargetType targetType)
        {
            return targetType == OMTargetType.Bar || targetType == OMTargetType.Frame;
        }
        protected virtual void SaveOMParamDict()
        {
            var (px, py) = _servCalDat.GetUmPixel(ECamera.Meas, _serviceProvider);
            _omParam.PixelSizeX = px;
            _omParam.PixelSizeY = py;
            _omParam.TargetType = ConvetTargetType(TrainSetting_Overlay.InnerType);
            var fileName = Path.Combine(TestFilesFolder, "omParamTest.hdict");
            _omParam.SerializeToDictFile(fileName);
        }

      
        [RelayCommand]
        protected void Correlation()
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

        private void CreateCorrelation()
        {
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

        }

        [ObservableProperty]
        private ChartResultViewModel_Ovl _curveResultViewModel;
        protected void ShowCurveControl()
        {
            if (CurveResultViewModel == null)
            {
                if (CorrelationCurvesList == null)
                    return;

                CreateCorrelation();

                CurveResultViewModel = new ChartResultViewModel_Ovl();
                CurveResultViewModel.SetViewModelDatas(Datas);
            }
        }

        protected void LoadOvResult(OvResultBase result)
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
            if (!CheckClose()) return;
            var result = new DialogResult(ButtonResult.OK, new DialogParameters() { { "modifiedTestInfo", TestInfo }, { "isClose", true } });
            RaiseRequestClose(result);
            NotifySaveAction = null;
        }
        [RelayCommand]
        void Submit()
        {
            //if (!CheckClose()) return;
            if (!Submit(true, EnumSubmitMode.Submit)) return;
            var result = new DialogResult(ButtonResult.OK, new DialogParameters() { { "modifiedTestInfo", TestInfo } });
            RaiseRequestClose(result);
            NotifySaveAction = null;
        }
        [RelayCommand]
        void Save()
        {
            Submit(true, EnumSubmitMode.Save);
        }

        /// <summary>
        /// 更新预匹配模板的最小匹配分数，因为界面上没有提供修改预匹配模板的最小匹配分数的地方，所以此处根据FocusBaseOn自动更新最小匹配分数
        /// </summary>
        protected void UpdateTemplateModelScore()
        {

            var measureTemplate = _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner ? TrainSetting_Overlay.OverlayMark_InnerModels[0] : TrainSetting_Overlay.OverlayMark_OuterModels[0];
            if (measureTemplate == null || TrainSetting_Overlay.PreviewTemplate == null)
            {
                LogHelper.LogInfo($"UpdateTemplateModelScore returned; measureTemplate={measureTemplate},PreviewTemplate={TrainSetting_Overlay.PreviewTemplate}");
                return;
            }
            TrainSetting_Overlay.PreviewTemplate.UpdateMinScoreAndSearchRect(measureTemplate, (log) => { LogHelper.LogInfo(log); });

        }

        protected override Point2d GetTargetAxisPoint(Point2d teachingPoint)
        {
            return _alignmentService.FindSingleTemplate(teachingPoint, TrainSetting_Overlay.OverlayMark_OuterModels[0], true, false, out _);
        }

        protected bool Submit(bool isCheckData, EnumSubmitMode submitMode =  EnumSubmitMode.None )
        {
            try
            {
                LogHelper.LogInfo($"Save train result,isCheckData={isCheckData},test={TestInfo.Name},IsTrained={_testTrainService?.TrainSetting?.IsTrained}");
                TrainSetting_Overlay.RoiExtendSize_Inner = RoiExtendSize_Inner;
                TrainSetting_Overlay.RoiExtendSize_Outer = RoiExtendSize_Outer;
                //TestTrainSetting.IsReuseMeasurementROI_Inner = !TemplateViewModel_Inner.IsShowCreateModel;
                //TestTrainSetting.IsReuseMeasurementROI_Outer = !TemplateViewModel_Outter.IsShowCreateModel;
                UpdateTestInfoData(isCheckData);

                if (submitMode != EnumSubmitMode.None)
                {
                    if (CheckTrain(submitMode))
                    {
                        _testTrainService.SaveTrainFiles();
                        ExecuteSomeMethod(_messageBoxToken);
                    }
                    else
                    {
                        return false;
                    }
                }
                //_messageBoxService.Success("Data saved successfully！", "TestTrainMessage");
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.Error($"Failed to save data!{ex.Message}", _messageBoxToken);
            }
            return true;
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


        [RelayCommand]
        public async void TestAutoTrain()
        {
            if (!_alignmentService.IsFinishAlignment)
            {
                var res = _messageBoxService.WarningAskDialog("An alignment operation must be performed before automatically training the test,.Otherwise, the results of the calculation may not be accurate.\r\nAre you sure it's aligned?");
                if (res != ButtonResult.OK)
                    return;
            }
            await Task.Run(() =>
            {
                ChangeCameraInfo = "Auto Trainning...";
                try
                {
                    IMroService mroService = _serviceProvider.GetService<IMroService>();
                    string recipeFolder = _serviceProvider.GetService<IRecipeManager>().GetRecipePath(_alignmentService.RecipeElementsInfo.Id);
                    mroService.AutoTrainRecipeTest(recipeFolder, TestInfo);
                    ShowBaseLayerImage();
                }
                catch (Exception ex)
                {
                    _messageBoxService.ErrorDialog(ex.Message);
                }
                finally
                {
                    ChangeCameraInfo = "";
                }
            });
        }

        /// <summary>
        /// 自动训练完成后图像控件显示base层图像
        /// </summary>
        protected void ShowBaseLayerImage()
        {
            var focusBase = (TestInfo.PhysicalOverlay.FocusSettings as FocusPhysical).FocusBasedOn;
            _visionService.StopGrabbing();
            HpImageViewer.Image = LoadTemplateModeImage(focusBase == EnumFocusBasedOn.Inner);

        }

        [RelayCommand]
        public async void TestPreview()
        {
            var image = HpImageViewer.Image as ImageData;
            var _context = TrainSetting_Overlay.PreviewTemplate;
            var matchResult = _templateMatchingService.FindTemplateForRun(image, _context, false);

            if (matchResult.IsSuccess)
            {
                var item = matchResult.BestMatchItem;
                var roi = new RotatedRectDesc()
                {
                    Color = "#00FF00",
                    CenterX = item.Column,
                    CenterY = item.Row,
                    Width = _context.SelectedMatchModelGroup.CroppedRect.Width,
                    Height = _context.SelectedMatchModelGroup.CroppedRect.Height,
                    Angle = item.Angle,
                };
                HpImageViewer.UpdateROIs(roi);
            }
        }
    }

    public  enum EnumSubmitMode
    {
        None = 0,
        Save,
        Submit,
    }
}
