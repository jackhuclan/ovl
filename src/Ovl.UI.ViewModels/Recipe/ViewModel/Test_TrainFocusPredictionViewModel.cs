using AdonisUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Example;
using HalconDotNet;
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
using InteVega.CBB.Prism.UI.Extensions.Dialogs;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.SectionManager.Interface;
using InteVega.CBB.SystemConfig.Interface;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using InteVega.CBB.TemplateMatchingModule.Views;
using Marvel.Application.Extentions;
using Marvel.Application.Services;
using Marvel.DataModels.Core;
using Marvel.DataModels.DeviceParameter;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.CD;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.Interfaces.Application;
using Marvel.TargetSelectionControl.Control;
using Marvel.WaferMapCommon;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using OpenTK.Compute.OpenCL;
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
using System.Windows.Media.Media3D;
using Wafer.Algorithm.Common.DoNet;
using WinCopies;

using Ovl.UI.ViewModels.Controls;
namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
    public partial class Test_TrainFocusPredictionViewModel : ObservableObject
    {
        ~Test_TrainFocusPredictionViewModel()
        {
            _focusFirstLayerService?.Dispose();
        }
        public Test_TrainFocusPredictionViewModel(Test_TrainViewModelBase trainViewMode, IServiceProvider serviceProvider, TestInfo_Overlay testinfo, TestOverlayTrainParam trainParam, Func<IHPImageViewer> getHpImgeViewer)
        {
            _serviceProvider = serviceProvider;
            TestInfo = testinfo;
            _trainParam = trainParam;
            _hpImageviewer = getHpImgeViewer;
            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
            _alignmentService = serviceProvider.GetRequiredService<IAlignmentService>();
            _visionService = serviceProvider.GetService<IVisionServiceManager>().GetMainVision();
            _focusService = serviceProvider.GetService<IFocusService>();
            _cameraLightParamService = serviceProvider.GetService<ICameraLightParamService>();
            _wliService = serviceProvider.GetService<IWliService>();
            _pztservice = serviceProvider.GetService<IAsyncAblePztService>();
            _lightServiceForMeasure = serviceProvider.GetService<ILightServiceForMeasure>();
            _testTrainService = serviceProvider.GetService<ITestTrainService>();
            _setRoiService = serviceProvider.GetService<ISetRoiService>();
            _focusPredictService = serviceProvider.GetService<IFocusPredictService>();
            WaferMapInfo = _alignmentService.WaferMapInfo;

            TrainingMethod = new ObservableCollection<FocusTrainType>(Enum.GetValues<FocusTrainType>());
            PredictioAlgorithm = new ObservableCollection<FocusPredictMethod>(Enum.GetValues<FocusPredictMethod>());
            NumberImages = new ObservableCollection<EnumFocusPredictionImage>(Enum.GetValues<EnumFocusPredictionImage>());

            var param = TestInfo.FocusPredictionParam;
            _useFocus = param.IsEnabled;
            _selectedTrainingMethod = param.TrainType;
            _selectedpredictioAlgorithm = param.PredictMethod;
            _selectedprenumberImages = param.PredictionImageType;
            _minimumScore = param.PredictionMinScore > 0 ? param.PredictionMinScore : 0.7;

            _focusFirstLayerService = serviceProvider.GetService<ITestFocusFirstLayerService>();
            _trainFolder = trainViewMode.TestFilesFolder;
        }

        public TisInfoBase TisUtility { get; set; }
        public WaferMapInfo WaferMapInfo;

        TrainSettings_Overlay _trainSettings_Overlay;
        public void Initialize(ITrainSettings trainSettings, TestOverlayTrainParam trainParam)
        {
            _trainSettings_Overlay = trainSettings as TrainSettings_Overlay;
            _trainParam = trainParam;
            BuildWaferMap();
            BuildTarget();
        }



        private void BuildWaferMap()
        {
            WaferMapControl = new WaferMapControl
            {
                EditMode = EnumEditMode.MultiSelect
            };
            WaferMapControl.Draw(WaferMapInfo);

            var savedFields = TestInfo.FocusPredictionParam.Fields;
            if (savedFields != null && savedFields.Count > 0)
            {
                WaferMapControl.Select(savedFields);
            }
            else
            {
                // 自动选5个均匀分布的Field
                var autoFields = SelectUniformFields(WaferMapInfo.FieldInfos, 5);
                WaferMapControl.Select(autoFields);
                TestInfo.FocusPredictionParam.Fields = autoFields;
            }

            WaferMapControl.ClickSelectAction = OnWaferMapSelectionChanged;
        }

        private void OnWaferMapSelectionChanged()
        {
            TestInfo.FocusPredictionParam.Fields = WaferMapControl.GetSelectedFields() ?? new List<RowColumnInfo>();
        }

        private List<RowColumnInfo> SelectUniformFields(List<RowColumnInfo> allFields, int count)
        {
            if (allFields == null || allFields.Count == 0) return new List<RowColumnInfo>();
            if (allFields.Count <= count) return allFields.ToList();

            var result = new List<RowColumnInfo>();

            // 中心点：距原点最近
            var center = allFields.OrderBy(f => f.RowIndex * f.RowIndex + f.ColumnIndex * f.ColumnIndex).First();
            result.Add(center);

            if (count == 1) return result;

            int maxRow = allFields.Max(f => f.RowIndex);
            int minRow = allFields.Min(f => f.RowIndex);
            int maxCol = allFields.Max(f => f.ColumnIndex);
            int minCol = allFields.Min(f => f.ColumnIndex);

            // 四个方向：目标坐标为极限-1，候选池排除极限行/列，避免选到边缘点
            var directions = new (Func<RowColumnInfo, bool> exclude, RowColumnInfo target)[]
            {
                (f => f.RowIndex == maxRow,    new RowColumnInfo(maxRow - 1, center.ColumnIndex)),
                (f => f.RowIndex == minRow,    new RowColumnInfo(minRow + 1, center.ColumnIndex)),
                (f => f.ColumnIndex == maxCol, new RowColumnInfo(center.RowIndex, maxCol - 1)),
                (f => f.ColumnIndex == minCol, new RowColumnInfo(center.RowIndex, minCol + 1)),
            };

            var remaining = allFields.Where(f => !f.Equals(center)).ToList();
            int needed = System.Math.Min(count - 1, directions.Length);

            for (int i = 0; i < needed && remaining.Count > 0; i++)
            {
                var (exclude, target) = directions[i];
                // 排除极限边缘，找距目标最近的实际 field
                var candidates = remaining.Where(f => !exclude(f)).ToList();
                if (candidates.Count == 0) candidates = remaining;

                var best = candidates
                    .OrderBy(f =>
                    {
                        int dr = f.RowIndex - target.RowIndex;
                        int dc = f.ColumnIndex - target.ColumnIndex;
                        return dr * dr + dc * dc;
                    })
                    .First();
                result.Add(best);
                remaining.Remove(best);
            }
            return result;
        }

        private void BuildTarget()
        {
            TargetSettingControl = new TargetSettingControl()
            {
                IsCheckable = true,
                IsSingleSelect = true   // 单选模式
            };

            TargetSettingControl.Refresh(TestInfo.TargetSetting.EnabledTargetPoints.ToList(),
                                         WaferMapInfo.CoordFieldWidth,
                                         WaferMapInfo.CoordFieldHeight);

            TargetSettingControl.SelectionChanged += OnTargetSelectionChanged;
            EnsureTargetSelected();
        }

        private void OnTargetSelectionChanged(object sender, TargetLocation target)
        {
            TestInfo.FocusPredictionParam.TargetID = target?.ToString();
        }

        /// <summary>
        /// 恢复已保存 Target；无效或未保存时默认选中第一个量测点（与 Field 自动选择一致）。
        /// </summary>
        private bool EnsureTargetSelected()
        {
            var targets = TestInfo.TargetSetting?.EnabledTargetPoints;
            if (targets == null || !targets.Any())
            {
                TestInfo.FocusPredictionParam.TargetID = null;
                return false;
            }

            var target = targets.FirstOrDefault(t => t.ToString() == TestInfo.FocusPredictionParam.TargetID)
                         ?? targets.FirstOrDefault();
            if (target == null)
            {
                TestInfo.FocusPredictionParam.TargetID = null;
                return false;
            }

            TargetSettingControl?.Select(target);
            TestInfo.FocusPredictionParam.TargetID = target.ToString();
            return true;
        }

        protected IServiceProvider _serviceProvider;
        protected IAlignmentService _alignmentService;
        protected IWliService _wliService;
        protected IFocusService _focusService;
        protected IAsyncAblePztService _pztservice;
        protected IVisionService _visionService;
        protected ICameraLightParamService _cameraLightParamService;
        protected IMessageBoxService _messageBoxService;
        protected ILightServiceForMeasure _lightServiceForMeasure;
        protected ITestTrainService _testTrainService;
        protected ISetRoiService _setRoiService;
        private readonly string _trainFolder;
        protected IFocusPredictService _focusPredictService;
        protected TestInfo_Overlay TestInfo;
        protected TestOverlayTrainParam _trainParam;
        protected Func<IHPImageViewer> _hpImageviewer;

        [ObservableProperty]
        public WaferMapControl _waferMapControl;

        [ObservableProperty]
        public TargetSettingControl _targetSettingControl;

        [ObservableProperty]
        private bool _useFocus = true;
        partial void OnUseFocusChanged(bool value)
        {
            TestInfo.FocusPredictionParam.IsEnabled = value;
        }

        [ObservableProperty]
        private ObservableCollection<FocusTrainType> _trainingMethod;

        [ObservableProperty]
        private FocusTrainType _selectedTrainingMethod;
        partial void OnSelectedTrainingMethodChanged(FocusTrainType value)
        {
            TestInfo.FocusPredictionParam.TrainType = value;
        }

        [ObservableProperty]
        private ObservableCollection<FocusPredictMethod> _predictioAlgorithm;

        [ObservableProperty]
        private FocusPredictMethod _selectedpredictioAlgorithm;
        partial void OnSelectedpredictioAlgorithmChanged(FocusPredictMethod value)
        {
            TestInfo.FocusPredictionParam.PredictMethod = value;
        }

        [ObservableProperty]
        private ObservableCollection<EnumFocusPredictionImage> _numberImages;

        [ObservableProperty]
        private EnumFocusPredictionImage _selectedprenumberImages;
        partial void OnSelectedprenumberImagesChanged(EnumFocusPredictionImage value)
        {
            TestInfo.FocusPredictionParam.PredictionImageType = value;
        }

        [ObservableProperty]
        private double _minimumScore = 0.7;
        private readonly ITestFocusFirstLayerService _focusFirstLayerService;

        private string _trainedTargetID;
        private List<RowColumnInfo> _trainedFields;
        private FocusPredictMethod _trainedPredictMethod;

        partial void OnMinimumScoreChanged(double value)
        {
            TestInfo.FocusPredictionParam.PredictionMinScore = value;
        }

        /// <summary>
        /// Tab是否可用：需要先完成基础训练
        /// </summary>
        //public bool IsTabEnabled => TestInfo?.TrainSetting?.IsTrained == true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(TrainFocusModelCommand))]
        private bool _isTraining = false;

        private CancellationTokenSource _trainCts;
        private readonly object _trainLock = new object();

        private bool CanTrainFocusModel() => !IsTraining;

        [RelayCommand(CanExecute = nameof(CanTrainFocusModel))]
        async Task TrainFocusModel()
        {
            if (IsTraining)
            {
                return;
            }

            if (!EnsureTargetSelected())
            {
                _messageBoxService?.WarningDialog("Please select a Target point first");
                return;
            }
            if (TestInfo.FocusPredictionParam.Fields == null || TestInfo.FocusPredictionParam.Fields.Count == 0)
            {
                _messageBoxService?.WarningDialog("Please select at least one Field first");
                return;
            }

            IsTraining = true;
            _visionService.StartGrabbing();
            _trainCts = new CancellationTokenSource();
            var cts = _trainCts;

            try
            {
                _hpImageviewer?.Invoke()?.UpdateROIs();
                await ProgressDialogHelper.ShowDialogAsync(
                    "Focus Prediction Training...",
                    () => DoTrainFocusModel(cts.Token));
                _trainSettings_Overlay.IsTrainedFocusPrediction = true;
                string info = "Focus Prediction training completed";
                LogHelper.LogInfo(info);
                _messageBoxService?.InfoDialog(info);
            }
            catch (OperationCanceledException)
            {
                _trainSettings_Overlay.IsTrainedFocusPrediction = false;
                string info = "Focus Prediction training cancelled";
                LogHelper.LogInfo(info);
                _messageBoxService?.WarningDialog(info);
            }
            catch (Exception ex)
            {
                _trainSettings_Overlay.IsTrainedFocusPrediction = false;
                string info = $"Focus Prediction training failed: {ex.Message}";
                LogHelper.LogError(info);
                _messageBoxService?.ErrorDialog(info);
            }
            finally
            {
                IsTraining = false;
                _trainCts = null;
            }
        }

        private void DoTrainFocusModel(CancellationToken ct)
        {
            if (!Monitor.TryEnter(_trainLock))
            {
                throw new InvalidOperationException("Focus Prediction training is already running");
            }

            try
            {
                DoTrainFocusModelCore(ct);
            }
            finally
            {
                Monitor.Exit(_trainLock);
            }
        }

        private void DoTrainFocusModelCore(CancellationToken ct)
        {
            var param = TestInfo.FocusPredictionParam;

            _focusFirstLayerService.SetCurrentTest(TestInfo, _trainFolder, _trainSettings_Overlay, ECamera.Meas, true, false);

            // 已训练 且 选择未变 → 仅更新算法（如有变化），不重新采图
            if (_trainSettings_Overlay.IsTrainedFocusPrediction && IsTrainSelectionUnchanged(param) && FocusModelFileExists())
            {
                if (param.PredictMethod != _trainedPredictMethod)
                {
                    ProgressDialogHelper.UpdateProgressInfo("Updating focus model...");
                    ct.ThrowIfCancellationRequested();
                    _focusPredictService.UpdateFocusModel(_trainFolder, param.PredictMethod);
                    _trainedPredictMethod = param.PredictMethod;
                }
                return;
            }

            // 全流程：采图 → 收集各 Field 数据 → 训练模型
            var ovlFocusInfo = AcquireBaseLayerFocusInfo(ct);

            // 重复训练时保证在训练的聚焦面

            _focusFirstLayerService.MoveToTrainFocusPosition(
                _trainSettings_Overlay,
                EnumOvlFocusOffsetType.ReferenceBase,
                _trainParam,
                _alignmentService.MacroFocusZ,
                _alignmentService.WaferMapInfo.Thickness);

            var listTargetInfos = CollectFieldFocusInfos(param, ovlFocusInfo, ct);

            ct.ThrowIfCancellationRequested();
            ProgressDialogHelper.UpdateProgressInfo("Training focus model...");
            _focusPredictService.TrainFocusModel(_trainFolder, ovlFocusInfo, listTargetInfos,
            new FocusTrainParam { TrainType = param.TrainType, PredictMethod = param.PredictMethod });

            // 保存本次训练的训练field，供下次判断是否需要重新采图
            _trainedTargetID = param.TargetID;
            _trainedFields = param.Fields?.ToList();
            _trainedPredictMethod = param.PredictMethod;
        }

        private bool FocusModelFileExists()
        {
            if (string.IsNullOrEmpty(_trainFolder))
            {
                return false;
            }

            return File.Exists(Path.Combine(_trainFolder, "focusModelInfo.hdict"))
                || File.Exists(Path.Combine(_trainFolder, "focusModelInfo"));
        }

        private bool IsTrainSelectionUnchanged(FocusPredictionParam param)
        {
            if (_trainedTargetID != param.TargetID) return false;
            if (_trainedFields == null || param.Fields == null) return false;
            if (_trainedFields.Count != param.Fields.Count) return false;
            return _trainedFields.All(f => param.Fields.Any(f.Equals));
        }

        /// <summary>
        /// 采集当前位置的基准层图像，提取 OVL 聚焦信息作为后续各 Field 训练的参考基准
        /// </summary>
        private HTuple AcquireBaseLayerFocusInfo(CancellationToken ct)
        {
            ProgressDialogHelper.UpdateProgressInfo("Acquiring base layer image...");
            ct.ThrowIfCancellationRequested();
            var baseImage = ImageData.ReadImage(Path.Combine(_trainFolder, "PreviewMatch.bmp"));
            var focusSetting = TestInfo.Physical.FocusSettings as FocusPhysical;
            return _focusPredictService.TrainFocusInfoOfOverlay(baseImage, focusSetting.FocusBasedOn, TestInfo.OMParam);
        }

        /// <summary>
        /// 遍历所有选中 Field，依次移台、聚焦、飞拍采图，收集每个 Field 的聚焦训练数据
        /// </summary>
        private List<HTuple> CollectFieldFocusInfos(FocusPredictionParam param, HTuple ovlFocusInfo, CancellationToken ct)
        {
            var targetPoint = TestInfo.TargetSetting.EnabledTargetPoints
                .FirstOrDefault(t => t.ToString() == param.TargetID)
                ?? throw new Exception($"Measurement point with TargetID={param.TargetID} not found, please check correct Target!");

            var result = new List<HTuple>();

            for (int i = 0; i < param.Fields.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var field = param.Fields[i];
                ProgressDialogHelper.UpdateProgressInfo($"Training field {i + 1}/{param.Fields.Count} ({field})...");
                result.Add(CollectFocusInfoForField(field, targetPoint, ovlFocusInfo));
            }
            return result;
        }

        /// <summary>
        /// 对单个 Field 执行移台→FocusLocateFirstLayer→抓图模板匹配→全行程飞拍，返回该 Field 的聚焦训练信息
        /// </summary>
        private HTuple CollectFocusInfoForField(RowColumnInfo field, TargetLocation targetPoint, HTuple ovlFocusInfo)
        {


            var targetAxisPoint = ResolveTargetAxisPoint(field, targetPoint, _trainSettings_Overlay);
            var matchResult = FocusAndMatchMeasureLayer(field, targetAxisPoint, _trainSettings_Overlay);

            var fullRangeFocusInfo = _testTrainService.CreateFullRangeFocusInfo();
            var focusedPosition = _focusFirstLayerService.GetCurrentAxisHeight();
            fullRangeFocusInfo.StartPosition = focusedPosition;
            var pegImages = _focusService.PegScanWithImages(fullRangeFocusInfo)?.PegImages?.ToList();
            if (pegImages == null || pegImages.Count == 0)
                throw new Exception($"Failed to collect focus sequence images for Field {field}");

            var focusSetting = TestInfo.Physical.FocusSettings as FocusPhysical;
            bool isBaseInner = focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner;
            var refMatchItem = isBaseInner ? _trainSettings_Overlay.InnerTrainMatchItem : _trainSettings_Overlay.OuterTrainMatchItem;
            var targetCenter = new Point2d(refMatchItem.Column, refMatchItem.Row);
            var result = _focusPredictService.TrainFocusInfoOfTarget(pegImages, targetCenter, matchResult.BestMatchItem, ovlFocusInfo);

            // Move back to the focused clear plane after full range scan
            _focusFirstLayerService.MoveToFocusPosition(focusedPosition);

            return result;
        }

        /// <summary>
        /// 若有定位模板，先移台聚焦匹配定位模板，再推算量测标记的实际轴坐标；否则直接返回理论坐标
        /// </summary>
        private Point2d ResolveTargetAxisPoint(RowColumnInfo field, TargetLocation targetPoint, TrainSettings_Overlay trainSettings)
        {
            var positionModel = trainSettings.PositioningTemplateInfo;
            var dir = _alignmentService.IsReversed ? -1 : 1;
            var targetAxisPoint = _alignmentService.GetAxisPoint(field, targetPoint, true);

            if (!positionModel.IsAvailable)
                return targetAxisPoint;

            var posModelPoint = positionModel.IsForEachTarget
                ? targetAxisPoint - positionModel.Template.R4
                : _alignmentService.GetAxisPoint(field, positionModel.R3, true);

            _alignmentService.MoveTo(posModelPoint, "FocusPredictionTrain_PositionModel");
            var pmMp = new MatchPointBase { RefMatchItem = positionModel.TrainMatchItem };
            bool pmFocusOk = _focusFirstLayerService.FocusLocateFirstLayer(
                pmMp, positionModel.Template, positionModel.Template.ROI,
                null, positionModel.RoiCurve, positionModel.GlobalCurve,
                positionModel.RoiFocusOffset - positionModel.GlobalFocusOffset, true);

            Point2d actualPosModelPoint = null;
            if (pmFocusOk)
            {
                var pmImg = _visionService.GrabOneImage();
                var pmMr = _alignmentService.FindTemplateForRun(pmImg, positionModel.Template, _alignmentService.IsReversed, null);
                if (pmMr.IsSuccess)
                    actualPosModelPoint = _alignmentService.ConvertImageToAxis(posModelPoint, pmMr.BestMatchItem, pmImg.Width, pmImg.Height);
            }
            actualPosModelPoint ??= _focusFirstLayerService.SprialSearch(posModelPoint,
                new List<SingleTemplateMatchingContext> { positionModel.Template }, out _);
            if (actualPosModelPoint == null)
                throw new Exception($"PositionModel matching failed for Field {field}");

            if (positionModel.IsForEachTarget)
                return actualPosModelPoint + positionModel.Template.R4 * dir;

            var offset = (_alignmentService.WaferMapInfo.ConvertToLlcR3(targetPoint.R3Vector, targetPoint.OrgionPoint) - positionModel.R3).ToPoint2d();
            return actualPosModelPoint + offset * dir;
        }

        /// <summary>
        /// 移台到量测标记，执行 FocusLocateFirstLayer 并模板匹配，返回匹配结果
        /// </summary>
        private MatchResult FocusAndMatchMeasureLayer(RowColumnInfo field, Point2d targetAxisPoint, TrainSettings_Overlay trainSettings)
        {
            _alignmentService.MoveTo(targetAxisPoint, "FocusPredictionTrain");

            var focusSetting = TestInfo.Physical.FocusSettings as FocusPhysical;
            bool isBaseInner = focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner;
            var layerParam = isBaseInner ? _trainParam.InnerLayer : _trainParam.OuterLayer;
            var refMatchItem = isBaseInner ? trainSettings.InnerTrainMatchItem : trainSettings.OuterTrainMatchItem;
            var measTemplate = trainSettings.PreviewTemplate
                               ?? (isBaseInner ? trainSettings.OverlayMark_InnerModels[0]
                                               : trainSettings.OverlayMark_OuterModels[0]);

            var imgForFov = _visionService.GrabOneImage();
            var mp = new MatchPointBase
            {
                RefMatchItem = refMatchItem,
                FovOffset = new Point2d((imgForFov.Width - 1) / 2.0 - refMatchItem.Column,
                                           (imgForFov.Height - 1) / 2.0 - refMatchItem.Row),
                FinalOffset = new Point2d()
            };

            bool focusOk = _focusFirstLayerService.FocusLocateFirstLayer(
                mp, measTemplate, layerParam.MeasureROI.CreateRoi(), layerParam.FocusKernel,
                layerParam.RoiFocusCurve, _trainParam.GlobalFineFocusCurve,
                trainSettings.GetOvlRelativeOffset(focusSetting.FocusBasedOn.ToOffsetType(), EnumOvlFocusOffsetType.Global, _trainParam),
                false);
            if (!focusOk)
                throw new Exception($"FocusLocateFirstLayer failed for Field {field}");

            var matchResult = _alignmentService.FindTemplateForRun(_visionService.GrabOneImage(), measTemplate, _alignmentService.IsReversed, null);
            if (!matchResult.IsSuccess)
                throw new Exception($"Template matching failed for Field {field}");

            return matchResult;
        }
    }
}
