using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPImageViewer;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.ComUI.Helpers;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.SectionManager.Interface;
using InteVega.CBB.SectionManager.Interface.Model;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using Marvel.DataModels.Core;
using Marvel.DataModels.Defect;
using Marvel.DataModels.DeviceParameter;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.Interface;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using InteVega.CBB.MotionProof.Abstraction;
using static InteVega.CBB.TemplateMatching.Core.ITemplateMatchingView;

namespace Ovl.UI.ViewModels.Recipe.ViewModel.OMVersion
{
    public partial class AlignmentVm_40 : AlignmentVmBase
    {
        IServiceCalData _calService;
        IWaferService _waferService;
        IMainRunThread _mainRunThread;
        IMotionControllerService _motionService;
        IDefectDataService _defectDataService;
        IAlignmentService _alignmentService;
        IServicePlat _servicePlat;
        IMachineDomainService? _machinePos;
        IVisionService _visionService;
        public AlignmentVm_40(IDialogService dialogService, IServiceProvider serviceProvicder, IMessageBoxService messageBoxService) : base(dialogService, serviceProvicder, messageBoxService)
        {
            _alignmentService = serviceProvicder.GetService<IAlignmentService>();
            _mainRunThread = serviceProvicder.GetService<IMainRunThread>();
            _waferService = serviceProvicder.GetService<IWaferService>();

            _visionService = serviceProvicder.GetService<IVisionService>();
            _defectDataService = serviceProvicder.GetService<IDefectDataService>();
            _calService = serviceProvicder.GetService<IServiceCalData>();

            _motionService = serviceProvicder.GetService<IMotionControllerService>();
            _servicePlat = serviceProvicder.GetService<IServicePlatNoProof>() as IServicePlat;
            _machinePos = serviceProvicder.GetService<IMachineDomainService>();
            _coarseAlignTemplateContext ??= new MultiTemplateMatchingContext() { IsForAlignment = true };
            _coarseAlignCamera = ECamera.Align;
        }
        private bool isSetFirstMark = false;
        private bool isSetSecondMark = false;
        private Point2d AlignFirstMarkPositionOrignal = null;
        private Point2d AlignSecondMarkPositionOrignal = null;
        [ObservableProperty]
        private string _alignFirstMarkScoreString = "NA";
        [ObservableProperty]
        private string _alignSecondMarkScoreString = "NA";
        protected override void Loaded()
        {
            try
            {
                base.Loaded();
                if (AlignmentInfo != null)
                    AlignmentInfo.LLCModels[0].Name = "AlignMark1 Model";

                if (AlignmentInfo.AlignFirstMarkPosition == null)
                {
                    isSetFirstMark = false;
                }
                else
                {
                    AlignFirstMarkPositionOrignal = AlignmentInfo.AlignFirstMarkPosition;
                    isSetFirstMark = true;
                }

                if (AlignmentInfo.AlignSecondMarkPosition == null)
                {
                    isSetSecondMark = false;
                }
                else
                {
                    AlignSecondMarkPositionOrignal = AlignmentInfo.AlignSecondMarkPosition;
                    isSetSecondMark = true;
                }

                if (AlignmentInfo.CoarseAlignModels != null)
                {
                    _coarseAlignTemplateContext.TemplateContexts = AlignmentInfo.CoarseAlignModels;
                }
                else
                {
                    AlignmentInfo.CoarseAlignModels = _coarseAlignTemplateContext.TemplateContexts;
                    AlignmentInfo.CoarseAlignModels[0].Name = "Coarse Align Model";
                }

                CoarseAlignTemplateViewModel = _serviceProvider.CreateTemplateViewModel(_coarseAlignTemplateContext, HpImageViewer) as MultiTemplateMatchingViewModel;
                CoarseAlignTemplateViewModel.UserModified += TemplateViewModel_UserModified;
                initDisplayAlignInfo();
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, "AlignMark1 Model Loaded");
            }
        }
        /// <summary>
        /// 粗对准模板匹配信息
        /// </summary>
        MultiTemplateMatchingContext _coarseAlignTemplateContext;

        /// <summary>
        /// 粗对准模板匹配ViewModel
        /// </summary>
        [ObservableProperty]
        MultiTemplateMatchingViewModel _coarseAlignTemplateViewModel;
        public override void RequestData(IRecipe data, RecipeElementsInfo recipe, RecipeInfo recipeInfo)
        {
            _isFineAlignment = false;
            base.RequestData(data, recipe, recipeInfo);
            ChangeCamera(_coarseAlignCamera, axisShift: false);
        }

        protected override void OnTableSelectedChanged(int value)
        {
            if (value == -1) return;
            if (value == 2)
            {
                _isFineAlignment = true;
            }
            else
            {
                _isFineAlignment = false;
            }
            //DisplayAlignmentMarkPos();
        }
        [ObservableProperty]
        DefectResultInfo _defectResult;
        /// <summary>
        /// 上一步
        /// </summary>
        [RelayCommand]
        protected virtual void ToLastStep()
        {
            if (CoarseOrFineAlignmentSel > 0)
                CoarseOrFineAlignmentSel--;
        }

        /// <summary>
        /// 下一步，进入精对齐
        /// </summary>
        [RelayCommand]
        protected virtual void ToNextStep()
        {
            if (CoarseOrFineAlignmentSel < 2)
                CoarseOrFineAlignmentSel++;
        }

        double alignPercent = 5;
        double loadImagePercent;
        [ObservableProperty]
        bool isRunning = false;
        [RelayCommand]
        public void Scan()
        {
            RunAsync("Aligning...", () =>
            {
                var pixelInfo = _calService.GetPixelSize(ECamera.Align,null);
                //测试代码
                //CachePanorama(cachePath => { FileHelper.CopyFile(@"E:\截图\4.jpg", cachePath); });
                //pixelInfo.Item1 = _alignmentService.WaferMapInfo.Diameter / ImageWidth;
                //pixelInfo.Item2 = _alignmentService.WaferMapInfo.Diameter / ImageHeight;

                InvokeAlign(alignPercent);
                ProgressShowCancelDialogHelper.UpdateProgressValueInfo(alignPercent, "Scaning...");
                loadImagePercent = new Random().NextDouble() * 3;
                InvokeScan();
                //像素当量在此处需要除以缩放比
                var zoom = _visionService.CurrentStageCameraConfig.Zoom;
                DefectResult = new DefectResultInfo() { RecipeID = SelectedRecipe.Id, RecipeName = SelectedRecipe.Name, DefectInfos = new ObservableCollection<DefectInfo>(), PixelEquivalentX = pixelInfo.Item1 / zoom, PixelEquivalentY = pixelInfo.Item2 / zoom };

                //var cow = GetCowAxisPoint();
                ////获取相机中心的机台坐标
                //var cameraCenter = _alignmentService.GetAxisPoint(new WcsPoint(), IServiceCamera.CurCamera.GetCameraCoordi());
                //var pixelPoint = ConverToPixelPoint(cow - cameraCenter);
                ////相机中心的坐标与图像中心的坐标是重合的 
                ////根据两者的像素偏移量，计算出晶圆中心的像素坐标
                //DefectResult.CowX = ImageWidth / 2 + pixelPoint.X;
                //DefectResult.CowY = ImageHeight / 2 + pixelPoint.Y;

                DefectResult.CowX = ImageWidth / 2;
                DefectResult.CowY = ImageHeight / 2;
            });
        }

        private void RunAsync(string info, Action act)
        {
            LogHelper.LogInfo(info + $",IsRunning={IsRunning}");
            if (IsRunning) return;
            bool isStopping = false;
            IsRunning = true;
            Task task = Task.Run(() =>
            {
                try
                {
                    act?.Invoke();
                    return true;
                }
                catch (Exception ex)
                {
                    LogHelper.LogError(ex);
                    if (!isStopping)
                        _messageBoxService.ErrorDialog(ex.Message);
                    return false;
                }
                finally
                {
                    LogHelper.LogInfo("IsRunning=false");
                    IsRunning = false;
                    ProgressShowCancelDialogHelper.UpdateProgressValueInfo(0, "");
                }
            });
            ProgressShowCancelDialogHelper.ShowCancelDialog(info, task, () =>
            {
                if (!IsRunning)
                {
                    CloseDialog();
                    return;
                }
                var res = _messageBoxService.AskDialog("Are you sure you want to stop?");
                if (res != ButtonResult.OK)
                    return;

                _alignmentService.Stop();
                _waferService.StopScan();
                isStopping = true;
            }, new InteVega.CBB.Common.Range(0, 100), null, CloseDialog);
        }
        public override void MoveToCenter()
        {
            if (AlignmentInfo == null)
                return;
            double z = GetAlignFocusAbsoluteZ();
            _alignmentService.MoveTo(_alignmentService.GetCoarseFocusPosition(), "Last focus position");
            _alignmentService.MoveZSpecTo(AxisType.Z_ALIGN, z, false);
        }
        protected override void MoveToFocusZ()
        {
            // 切换相机的 finally 会在 GetCalData 刷新 MacroFocusZ 之前执行。
            // 不能使用上一台相机残留的 MacroFocusZ，也不能把晶圆厚度叠到已经是绝对高度的位置上。
            double z = GetAlignFocusAbsoluteZ();
            _servicePlat?.MoveZ(AxisType.Z_ALIGN, z);
            LogHelper.LogInfo($"Move axis Z_ALIGN to foucs z:{z}");
        }

        /// <summary>
        /// 对准相机绝对焦面 = 对准相机标定焦面 + 相对聚焦高度。
        /// 相对高度优先用 Alignment.FocusHeight，未训练时才退回晶圆厚度。
        /// </summary>
        private double GetAlignFocusAbsoluteZ()
        {
            double relativeZ = AlignmentInfo?.FocusHeight ?? 0;
            if (relativeZ == 0)
                relativeZ = WaferInfo?.Thickness ?? 0;
            var camAttr = _serviceProvider.GetRequiredService<IServiceCamera>().GetCameraAttr(ECamera.Align);
            double macroZ = _calService.GetMacroFocusZ(ECamera.Align, camAttr).Item1;
            double z = macroZ + relativeZ;
            LogHelper.LogInfo($"Align focus Z={z}, macro={macroZ}, relative={relativeZ}, focusHeight={AlignmentInfo?.FocusHeight}, thickness={WaferInfo?.Thickness}, cachedMacro={_alignmentService.MacroFocusZ}");
            return z;
        }
        public bool CanMoveZ(AxisType axisType)
        {
            if (string.IsNullOrEmpty(_motionService.GetAxisIdByType(axisType)))
            {
                LogHelper.LogInfo($"{axisType} don't exist. Can not Move");
                return false;
            }
            return true;
        }
        protected override void SaveModelInifos()
        {
            base.SaveModelInifos();
            AlignmentInfo.CoarseAlignModels = _coarseAlignTemplateContext.TemplateContexts;
        }
        private void CloseDialog()
        {
            try
            {
                ProgressShowCancelDialogHelper.CloseDialog();
            }
            catch
            { }
        }
        private void InvokeAlign(double totalPercent)
        {
            LogHelper.LogRunMethodStart();
            bool isAligning = true;
            //添加拉平过程的模拟进度
            Task.Run(() =>
            {
                for (int i = 1; i <= 10; i++)
                {
                    if (!IsRunning || !isAligning)
                        break;
                    Thread.Sleep(1000);
                    double p = new Random().NextDouble() * 0.1 + 0.9;
                    double progress = totalPercent * p / 10 * i;
                    ProgressShowCancelDialogHelper.UpdateProgressValueInfo(progress, "Aligning...");
                }
            });
            _serviceProvider.GetService<ISectionManager>().SetSectionToJob(new JobSectionInfo("ScanWafer", DateTime.Now.ToString("HHmmss_fff"), SelectedRecipe.Name));
            _alignmentService.Init(SelectedRecipe, true);
            // _motionService.SingleAxisToPoint(AxisType.T, SelectedRecipe.Alignment.StageAngle, false);
            _alignmentService.RotateTo(SelectedRecipe.Alignment.StageAngle);
            _mainRunThread.FocusAlign(true, SelectedRecipe, SelectedRecipe.RunParameter);

            isAligning = false;
            ProgressShowCancelDialogHelper.UpdateProgressValueInfo(totalPercent, "Aligning...");
            Thread.Sleep(100);
            LogHelper.LogRunMethodEnd();
        }
        [ObservableProperty]
        private RecipeElementsInfo _selectedRecipe;
        private void InvokeScan()
        {
            //获取晶圆中心的机台坐标
            var cow = GetCowAxisPoint();
            CachePanorama(panoramCache =>
            {
                var zoom = _visionService.CurrentStageCameraConfig.Zoom;
                var ratio = _visionService.CurrentStageCameraConfig.ScanRangeRatio;
                ratio = Math.Max(ratio, 0.01);
                ratio = Math.Min(ratio, 1);
                //现在扫描范围在0.1-1之间
                _waferService.ScanFullPicture(SelectedRecipe.WaferMap.Diameter * ratio, cow, panoramCache, zoom);
            });
        }
        /// <summary>
        /// 获取当前相机下的晶圆中心的机台坐标
        /// </summary>
        /// <returns>返回机台坐标</returns>
        private Point2d GetCowAxisPoint()
        {
            return _alignmentService.Cow;
            //return _alignmentService.GetAxisPoint(new WcsPoint(), _coarseAlignCamera.GetCameraWaferCoordi(), CameraAttr.Default(null));
        }
        public ImageViewer ImageViewer { get; set; }
        [ObservableProperty]
        double _imageWidth;
        [ObservableProperty]
        double _imageHeight;
        [ObservableProperty]
        BitmapSource _waferImage;
        /// <summary>
        /// 缓存全景图到文件，并且加载全景图到界面
        /// </summary>
        /// <param name="act">缓存全景图的方法（扫描或者拷贝）</param>
        private void CachePanorama(Action<string> act)
        {
            _defectDataService.CachePanorama(panoramCache =>
            {
                AppHelper.DispatcherInvoke(() => { ImageSourceHelper.ClearImage(WaferImage); });
                act?.Invoke(panoramCache);
                AppHelper.DispatcherInvoke(() =>
                {
                    var imagedata = ImageData.ReadImage(panoramCache);
                    ImageViewer.Image = imagedata;
                    ImageWidth = imagedata.Width;
                    ImageHeight = imagedata.Height;
                    return;
                    using (FileStream stream = new FileStream(panoramCache, FileMode.Open, FileAccess.Read))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze(); // 可选，使图片跨线程可用
                                         // 使用图片...
                        WaferImage = bitmap;
                        ImageWidth = bitmap.PixelWidth;
                        ImageHeight = bitmap.PixelHeight;

                    }

                });
            });
        }
        internal void MoveTo(Point p)
        {
            var point2d = new Point(p.X - DefectResult.CowX, p.Y - DefectResult.CowY);
            var wcsPoint = ConvertToWcsPoint(point2d);
            var axisPoint = _alignmentService.GetAxisPoint(wcsPoint, _coarseAlignCamera.GetCameraWaferCoordi(), CameraAttr.Default(null));
            LogHelper.LogInfo($"Click Picture to move:Pixel={p},wcsPoint={wcsPoint},axisPoint={axisPoint}");
            _alignmentService.MoveTo(axisPoint);
        }
        private Point ConverToPixelPoint(Point2d point2d)
        {
            var x = point2d.X / DefectResult.PixelEquivalentX;
            var y = -point2d.Y / DefectResult.PixelEquivalentY; //Y方向需要取反，因为与晶圆的Y方向相反
            return new Point(x, y);
        }

        public Point GetAxisPoint()
        {
            if (DefectResult == null) return new Point();
            var axisPoint = _alignmentService.GetCurrentAxisPosition();
            var cow = GetCowAxisPoint();
            var pixelPoint = ConverToPixelPoint(axisPoint - cow);
            var x = pixelPoint.X + DefectResult.CowX;
            var y = pixelPoint.Y + DefectResult.CowY;
            return new Point(x, y);
        }
        private WcsPoint ConvertToWcsPoint(Point point2d)
        {
            var x = point2d.X * DefectResult.PixelEquivalentX;
            var y = -point2d.Y * DefectResult.PixelEquivalentY; //Y方向需要取反，因为与晶圆的Y方向相反
            return new WcsPoint(x, y);
        }


        /// <summary>
        /// 走到晶圆中心位置
        /// </summary>
        [RelayCommand]
        public void GotoAlignFirstMark()
        {
            if (AlignmentInfo?.AlignFirstMarkPosition != null)
            {
                _alignmentService.MoveTo(AlignmentInfo.AlignFirstMarkPosition + GetCowAxisPoint());
            }
            else
            {
                _messageBoxService.InfoDialog("Please set first mark.");
            }
        }
        /// <summary>
        /// 走到晶圆中心位置
        /// </summary>
        [RelayCommand]
        public void GotoAlignSecondMark()
        {
            if (AlignmentInfo?.AlignSecondMarkPosition != null)
            {
                _alignmentService.MoveTo(AlignmentInfo.AlignSecondMarkPosition + GetCowAxisPoint());
            }
            else
            {
                _messageBoxService.InfoDialog("Please set second mark.");
            }
        }

        /// <summary>
        /// Align && TrainField
        /// </summary>
        [RelayCommand]
        public async Task CoarseAlignTrain()
        {

            if (AlignmentInfo.AlignFirstMarkPosition == null)
            {
                LogHelper.LogInfo("AlignFirstMarkPosition is null");
                _messageBoxService.ErrorDialog("Align first mark is null");
                return;
            }
            if (AlignmentInfo.AlignSecondMarkPosition == null)
            {
                LogHelper.LogInfo("AlignSecondMarkPosition is null");
                _messageBoxService.ErrorDialog("Align second mark is null");
                return;
            }
            if (Math.Abs(_alignmentService.CalculateAngle(AlignmentInfo.AlignFirstMarkPosition, AlignmentInfo.AlignSecondMarkPosition)) > 5)
            {
                LogHelper.LogInfo("Rot Angle is over five degree！");
                _messageBoxService.ErrorDialog("Rotation Angle is over five degrees! Please check the wafer status.");
                return;
            }
            await ExecuteAlignment(false);
        }
        /// <summary>
        /// 匹配Mark2
        /// </summary>
        [RelayCommand]
        public async Task MatchSecondMark()
        {
            isSetSecondMark = false;
            if (!CheckCreateModel())
            {
                return;
            }
            await TestModelMethod();
            AlignSecondMarkScoreString = CoarseAlignTemplateViewModel.SelectedSingleTemplateMatchingViewModel.TestModelViewModel.MaxScoreString;
            AlignmentInfo.AlignSecondMarkPosition = _alignmentService.GetCurrentAxisPosition() - GetCowAxisPoint();
            if (!CheckDistanceOk())
            {
                return;
            }

            if (AlignSecondMarkPositionOrignal != null && AlignmentInfo.AlignSecondMarkPosition != AlignSecondMarkPositionOrignal)
            {
                isSetSecondMark = true;
                AlignSecondMarkPositionOrignal = AlignmentInfo.AlignSecondMarkPosition;
            }
        }
        private bool CheckDistanceOk()
        {
            bool isCheckOk = true;
            if (AlignmentInfo.AlignFirstMarkPosition != null && AlignmentInfo.AlignSecondMarkPosition != null)
            {
                if (!CheckDistanceOfAlignMarks())
                {
                    RestSecondMarkInfo();
                    isCheckOk = false;
                }
            }
            return isCheckOk;
        }
        private void RestSecondMarkInfo()
        {
            AlignmentInfo.AlignSecondMarkPosition = null;
            AlignSecondMarkPositionOrignal = null;
            AlignSecondMarkScoreString = "NA";
        }
        private void RestFirstMarkInfo()
        {
            AlignmentInfo.AlignFirstMarkPosition = null;
            AlignFirstMarkPositionOrignal = null;
            AlignFirstMarkScoreString = "NA";
        }
        private bool CheckDistanceOfAlignMarks()
        {
            var startPoint = AlignmentInfo.AlignFirstMarkPosition;
            var endPoint = AlignmentInfo.AlignSecondMarkPosition;
            var dx = Math.Abs(startPoint.X - endPoint.X);
            var dy = Math.Abs(startPoint.Y - endPoint.Y);
            if (!(dx >= _recipeElementsInfo.WaferMap.FieldWidth * 2 && dy < _recipeElementsInfo.WaferMap.FieldHeight / 2))
            {
                LogHelper.LogInfo($"AlignmentInfo.AlignFirstMarkPosition: ({AlignmentInfo.AlignFirstMarkPosition.X}, {AlignmentInfo.AlignFirstMarkPosition.Y})");
                LogHelper.LogInfo($"AlignmentInfo.AlignSecondMarkPosition: ({AlignmentInfo.AlignSecondMarkPosition.X}, {AlignmentInfo.AlignSecondMarkPosition.Y})");
                LogHelper.LogInfo($"dx: {dx}");
                LogHelper.LogInfo($"dy: {dy}");
                _messageBoxService.ErrorDialog("The distance between the two points is too close. It is recommended to take the marks at both ends of the central horizontal line.");
                return false;
            }
            return true;
        }
        private bool CheckCreateModel()
        {
            if (CoarseAlignTemplateViewModel.SelectedSingleTemplateMatchingViewModel.Context.SelectedMatchModelGroup.MatchModel.ModelInfo == null)
            {
                _messageBoxService.InfoDialog("Please create model first.");
                return false;
            }
            return true;
        }
        private async Task TestModelMethod()
        {
            IsRunning = true;
            bool isSuccess = true;
            try
            {
                await CoarseAlignTemplateViewModel.TestModelMethod();
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogHelper.LogError(ex, ex.Message);
            }
            finally
            {
                CoarseAlignTemplateViewModel.OnUserModified(UserModifyType.ModelTested, CoarseAlignTemplateViewModel.SelectedSingleTemplateMatchingViewModel.Context, isSuccess);

                IsRunning = false;
            }
        }

        /// <summary>
        /// 匹配Mark1
        /// </summary>
        [RelayCommand]
        public async Task MatchFirstMark()
        {
            if (!CheckCreateModel())
            {
                return;
            }
            await TestModelMethod();
            AlignFirstMarkScoreString = CoarseAlignTemplateViewModel.SelectedSingleTemplateMatchingViewModel.TestModelViewModel.MaxScoreString;
            AlignmentInfo.AlignFirstMarkPosition = _alignmentService.GetCurrentAxisPosition() - GetCowAxisPoint();
            if (!CheckDistanceOk())
            {
                return;
            }
            if (AlignFirstMarkPositionOrignal != null && AlignmentInfo.AlignFirstMarkPosition != AlignFirstMarkPositionOrignal)
            {
                isSetFirstMark = true;
                AlignFirstMarkPositionOrignal = AlignmentInfo.AlignFirstMarkPosition;
                isSetSecondMark = false;
                if (AlignmentInfo.AlignSecondMarkPosition != null)
                {
                    _messageBoxService.InfoDialog("Please press the confirm button to update the second mark position.");
                    AlignSecondMarkPositionOrignal = null;
                    AlignmentInfo.AlignSecondMarkPosition = null;
                    AlignSecondMarkScoreString = "NA";

                }
            }
        }
        /// <summary>
        /// 匹配Mark1
        /// </summary>
        [RelayCommand]
        public async Task CreateFirstMark()
        {
            var checkRes = CoarseAlignTemplateViewModel.SelectedSingleTemplateMatchingViewModel.CheckRoi();
            if (!string.IsNullOrEmpty(checkRes))
            {
                _messageBoxService.InfoDialog(checkRes);
                return;
            }
            IsRunning = true;
            bool isSuccess = true;
            try
            {
                await CoarseAlignTemplateViewModel.SelectedSingleTemplateMatchingViewModel.CreateModelAsync(true);
            }
            catch (Exception ex)
            {
                isSuccess = false;
                if (ex != null)
                {
                    LogHelper.LogError(ex, ex.Message);
                    if (ex is ErrorCodeException)
                    {
                        int errorCode = ((ErrorCodeException)ex).ErrorCode;

                        EnumTemplateError errorType = (EnumTemplateError)Enum.Parse(
                            typeof(EnumTemplateError),
                            errorCode.ToString()
                        );
                        _messageBoxService.ErrorDialog(errorType.ToString());
                    }
                    else
                    {
                        _messageBoxService.ErrorDialog(ex.Message.ToString());
                    }
                }
            }
            finally
            {
                CoarseAlignTemplateViewModel.OnUserModified(UserModifyType.ModelTested, CoarseAlignTemplateViewModel.SelectedSingleTemplateMatchingViewModel.Context, isSuccess);
                IsRunning = false;
            }
            if (isSuccess)
            {
                await MatchFirstMark();
            }
        }

        protected override void SaveCameraParams()
        {
            if (_isFineAlignment)
            {
                SaveFineAlignCamaraParams();
            }
            else
            {
                SaveCoarseAlignCamaraParams();
            }
        }

        protected override void UpdataStageCameraParamAndWriteToCamera(ECamera camera)
        {
            LogHelper.LogInfo("open algnment view, loading alignment camera params...");

            if (camera == ECamera.Align)
            {
                _cameraLightParamService.SetCamearaParam(camera, AlignmentInfo.AlignCameraParms);
                _cameraLightParamService.SetLightSourceParam(camera, AlignmentInfo.AlignPointLightParms);
            }
        }

        private void SaveCoarseAlignCamaraParams()
        {
            AlignmentInfo.CoarseAlignCameraParms = _cameraLightParamService.GetCamearaParam(ECamera.Align) as AlignmentCameraParam;
            AlignmentInfo.CoarseAlignPointLightParms = _cameraLightParamService.GetLightSourceParam(ECamera.Align) as AlignmentPointLightParam;
        }

    }
}
