using Bases = Ovl.UI.ViewModels.Bases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;

using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainLib.Services;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.MotionController;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.SystemConfig.Interface;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule.Views;
using Marvel.Application.Extentions;
using Marvel.Application.Services;
using Marvel.DataModels;
using Marvel.DataModels.Core;
using Marvel.DataModels.DeviceParameter;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Wafer.Algorithm.Common.DoNet;

using Ovl.UI.Recipe.View;
namespace Ovl.UI.Recipe.ViewModel
{
    public partial class Test_TrainViewModelBase : Bases.ViewModelBase
    {
        ~Test_TrainViewModelBase()
        {
        }

        public Test_TrainViewModelBase(IServiceProvider serviceProvider)
        {
        }
        [ObservableProperty]
        private IHPImageViewer _hpImageViewer;

        public SingleTemplateControl PositioningTemplateControl { get; set; }
        /// <summary>
        /// 图像控件
        /// </summary>
        //public IHPImageViewer HpImageViewer { get; set; }
        /// <summary>
        /// Test相关文件夹目录
        /// </summary>
        public string TestFilesFolder { get; protected set; } = string.Empty;
        [ObservableProperty]
        string _changeCameraInfo;

        [ObservableProperty]
        private bool _isVisionParamEnable = true;

        [ObservableProperty]
        private bool _isAxisMoveable = false;

        [ObservableProperty]
        private bool _isFocusable = true;

        /// <summary>
        /// 获取或者设置训练聚焦时机台的位置
        /// </summary>
        public Point2d FocusPosition { get; set; }
        /// <summary>
        /// 获取目标中心点的机台坐标
        /// </summary>
        public virtual Point2d TargetPoint { get; set; }
        public static Action<TestInfoBase, string> NotifySaveAction
        {
            get;
            set;
        }

    }

    public partial class Test_TrainViewModelBase<T> : Test_TrainViewModelBase where T : TestInfoBase
    {

        public Test_TrainViewModelBase(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _dialogService = serviceProvider.GetService<IDialogService>();
            _visionServiceManager = serviceProvider.GetService<IVisionServiceManager>();
            _visionService = _visionServiceManager.GetMainVision();
            _alignmentService = serviceProvider.GetRequiredService<IAlignmentService>();
            _cameraLightParamService = serviceProvider.GetService<ICameraLightParamService>();
            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
            _templateMatchingService = serviceProvider.GetService<ITemplateMatchingService>();
            _wliService = serviceProvider.GetService<IWliService>();
            _focusService = serviceProvider.GetService<IFocusService>();
            _motionControllerService = serviceProvider.GetService<IMotionControllerService>();
            _pztservice = serviceProvider.GetService<IAsyncAblePztService>();
            _lightServiceForMeasure = serviceProvider.GetService<ILightServiceForMeasure>();
            _testTrainService = serviceProvider.GetService<ITestTrainService>();
        }

        /// <summary>
        /// 白光干涉聚焦训练高度
        /// </summary>
        protected double? wliPztOffset;
        protected IDialogService _dialogService;
        protected IVisionService _visionService;
        protected ICameraLightParamService _cameraLightParamService;
        protected IMessageBoxService _messageBoxService;
        protected ILightServiceForMeasure _lightServiceForMeasure;
        protected IServiceProvider _serviceProvider;
        protected ITemplateMatchingService _templateMatchingService;
        protected IAlignmentService _alignmentService;
        protected IWliService _wliService;
        protected IFocusService _focusService;
        protected IAsyncAblePztService _pztservice;
        protected IMotionControllerService _motionControllerService;
        protected ITestTrainService _testTrainService;

        protected IVisionServiceManager _visionServiceManager;
        [ObservableProperty]
        T _testInfo;

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
        /// 是否显示定位模板是否为每个量测点定位的勾选项
        /// </summary>
        [ObservableProperty]
        Visibility _checkPositionModelForeachVisible;

        [ObservableProperty]
        ITrainSettings _trainSetting;

        [ObservableProperty]
        Visibility _trainSettingVisible = Visibility.Collapsed;

        protected bool IsEnableWLI
        {
            get
            {
                return MarvelVersion.IsSupportWLI && TestInfo.Physical.FocusSettings.IsUseWLI;
            }
        }


        /// <summary>
        /// 定位模板数据
        /// </summary>
        protected virtual PositioningTemplateInfo PositioningModel
        {
            get { return GetTrainSettings ().PositioningTemplateInfo; }
        }

        protected virtual ITrainSettings GetTrainSettings()
        {
            return TrainSetting;
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey(nameof(TestInfoBase)))
            {
                TestInfo = parameters.GetValue<T>(nameof(TestInfoBase));
                TrainSetting = TestInfo.TrainSetting;
                string recipeFolder = _serviceProvider.GetService<IRecipeManager>().GetRecipePath(_alignmentService.RecipeElementsInfo.Id);
                TestFilesFolder = Path.Combine(recipeFolder, TestInfo.Name);
                _templateMatchingService.TemplatesFolder = TestFilesFolder;
            }
            Task.Run(() =>
            {

                try
                {
                    ChangeCameraInfo = "Change camera ...";
                    OpenDialogThread(parameters);
                }
                catch (Exception ex)
                {
                    LogHelper.LogError(ex);
                    _messageBoxService.ErrorDialog(ex.Message);
                }
                finally
                {

                }
            });
        }
        public virtual void OpenDialogThread(IDialogParameters parameters)
        {
            LogHelper.LogInfo($"Test_TrainView loaded!Type={this.GetType()}");
          
            TargetPoint = null;
            FocusPosition = null;
            if (TestInfo != null)
            {
                WliVisible = IsEnableWLI ? Visibility.Visible : Visibility.Collapsed;
                if (IsEnableWLI && TrainSetting.FocusModuleInfo.WliFocusOffset != 0)
                {
                    AbsolateWliHeight = TrainSetting.FocusModuleInfo.BaseFocusHeight + TrainSetting.FocusModuleInfo.WliFocusOffset + _alignmentService.MacroFocusZ;
                }
                if (PositioningTemplateControl != null)
                {
                    AppHelper.DispatcherInvoke(() =>
                    {
                        PositioningTemplateControl.TemplateContext = PositioningModel.Template;
                        PositioningTemplateControl.ActionUserModified = PositionTemplateUserModified;
                        PositioningTemplateControl.FuncBeforeCreateModel = BeforeCreatePostionModel;
                        if (TestInfo.TargetSetting.EnabledTargetPoints.Count() > 1)
                        {
                            CheckPositionModelForeachVisible = Visibility.Visible;
                        }
                        else
                        {
                            CheckPositionModelForeachVisible = Visibility.Collapsed;
                            TrainSetting.PositioningTemplateInfo.IsForEachTarget = true;
                        }
                    });
                }
            }
            _wliService.CloseShutter();
            _alignmentService.Reset();

        }

        protected bool CheckClose()
        {
            if (TrainSetting.IsTrained == false)
            {
                var res = _messageBoxService.AskDialog("The test training is not complete. Are you sure you want to close it?");
                return res == ButtonResult.OK;
            }
            return true;
        }

        protected bool CheckTrain(EnumSubmitMode submitMode) 
        {
            if (TrainSetting.IsTrained == false)
            {
                string askInfo = "The test training is not complete and cannot be run. Are you sure you want to save and close it?";
                if (submitMode == EnumSubmitMode.Save)
                {
                    askInfo = "The test training is not complete and cannot be run. Are you sure you want to save it?";
                }
                var res = _messageBoxService.AskDialog(askInfo);
                return res == ButtonResult.OK;
            }
            return true;
        }

        public override void OnDialogClosed()
        {
            base.OnDialogClosed();

            AssistantBase.ActionErrored = null;
            _alignmentService.Stop();
            StopVisionGrabbingIfNeeded("Train dialog closed");
            LogHelper.LogInfo($"Test_TrainView unloaded!Type={this.GetType()}");
        }

        protected void StopVisionGrabbingIfNeeded(string reason)
        {
            try
            {
                if (_visionService?.Camera?.IsGrabbing == true)
                {
                    _visionService.StopGrabbing();
                    LogHelper.LogInfo($"StopGrabbing: {reason}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, $"StopGrabbing failed: {reason}");
            }
        }

        protected virtual async Task ChangeMeasureCamera(ECamera camera = ECamera.Meas, bool isShiftCamera = true)
        {
            ChangeCameraInfo = "Camera switching …";
            await Task.Run(() =>
            {
                try
                {
                    _alignmentService.ChangeCamera(camera, isShiftCamera);
                    _visionService.StartGrabbing();
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
                    SetVisionParams();

                    ChangeCameraInfo = "";
                }
            });
        }

        /// <summary>
        /// 设置量测相机参数
        /// </summary>
        protected virtual void SetVisionParams()
        {
            try
            {
                //Todo double face cameraparams
                _cameraLightParamService.SetTrainSettinsParam(IServiceCamera.CurCamera, TrainSetting);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.Error(ex.Message);
            }

            _visionService.NotifyParamsChanged();
        }

        protected virtual void FocusSettingMethod()
        {
            Action<FocusInfo> action = new Action<FocusInfo>(SaveActionMethod);
            var parameters = new DialogParameters
            {
                { "FocusInfo", FocusInfo},
                { "SaveAction", action }
            };

            AppHelper.DispatcherInvoke(() =>
            {
                _dialogService.ShowDialog(nameof(Test_FocusTrainSettingDialog_OM40), parameters, null);
            });
        }

        [RelayCommand]
        public void FocusSetting()
        {
            FocusSettingMethod();
        }

        protected void SaveActionMethod(FocusInfo focusInfo)
        {
            FocusInfo = focusInfo;
        }

        protected virtual FocusInfo FocusInfo
        {
            get
            {
                return TrainSetting.FocusModuleInfo.FocusInfo;
            }
            set
            {
                TrainSetting.FocusModuleInfo.FocusInfo = value;
            }
        }

        protected virtual FocusInfo GetTestFocusOrDefault(ECamera camera = ECamera.Meas)
        {
            //如果没有设置过配方的聚焦参数，则使用系统默认的聚焦参数，
            //但是，，，，此处不能将系统的默认聚焦参数赋值给配方的聚焦参数，否则系统的聚焦参数改变了之后，配方的聚焦参数不会变
            return FocusInfo ?? _serviceProvider.GetService<ITestService>().GetSystemFocusInfo(camera);
        }

        protected ParamFocus GetFineParamFocus(ECamera camera = ECamera.Meas)
        {
            return GetTestFocusOrDefault(camera).CloneFineParamFocus();
        }

        protected TrainArg CreateTrainArg(EnumGrabMode grabMode, int errorFactor = 2)
        {
            return _testTrainService.CreateTrainArg(grabMode, errorFactor);
            //return new TrainArg()
            //{
            //    ErrorFactor = errorFactor,
            //    GrabMode = grabMode == EnumGrabMode.DoubleGrab ? CurveModel.Double : CurveModel.Single
            //};
        }

        protected virtual void SaveVisionParams()
        {
            try
            {
                TrainSetting.CameraParams = _cameraLightParamService.GetCamearaParam(IServiceCamera.CurCamera);
                TrainSetting.LightSourceParam = _cameraLightParamService.GetLightSourceParam(IServiceCamera.CurCamera);
                SaveLightAgeInitialInfo(IServiceCamera.CurCamera, TrainSetting.CameraParams, TrainSetting.LightSourceParam);
            }
            catch (Exception e)
            {
                LogHelper.LogError(e);
            }
        }

        protected Rect2d CreateRectByShapeBase(ShapeBase shape)
        {
            Rect2d rect2d = new Rect2d();
            if (shape is Dimension2d dimension2D)
            {
                var destCenter = dimension2D.GetCenter();// 获取ROI中心点坐标
                dimension2D.GetSmallestRectangle(out var smallesRectangle); // 获取最小外接矩形
                var smallestRectCenter = smallesRectangle.GetCenter();
                var destOffset = destCenter - smallestRectCenter;// 计算中心点偏移量
                rect2d = smallesRectangle.Move(destOffset.X, destOffset.Y) as Rect2d;// 将最小外接矩形移动到ROI中心点位置
            }
            else
            {
                rect2d = shape.CreateRect();
            }
            return rect2d;
        }
        /// <summary>
        /// 训练保存视觉参数时记录Recipe初始LightAge系数。
        /// </summary>
        protected void SaveLightAgeInitialInfo(ECamera camera, CameraParamBase cameraParam, LightSourceParamBase lightParam)
        {
            if (cameraParam is not MeasureCameraParam measureCameraParam)
                return;

            var age = _serviceProvider.GetService<IServiceCalData>()?.GetLightAge(camera, measureCameraParam.CameraAttr) ?? 1;
            measureCameraParam.AttenuationCoef = age;

            TestInfo.IsLightPmExecuted = true;
            LogHelper.LogDebug($"Set Age:{age}, Camera: {camera}");
        }


        #region 独立的定位模板



        /// <summary>
        /// 通过模板匹配获取标记的机台坐标
        /// </summary>
        /// <param name="teachingPoint"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual Point2d GetTargetAxisPoint(Point2d teachingPoint)
        {

            throw new NotImplementedException();
        }

        internal bool BeforeCreatePostionModel()
        {

            if (TargetPoint == null)
            {
                _messageBoxService.ErrorDialog($"Please set/reset the measurement roi first!");
                return false;
            }
            if (HpImageViewer.Image is not ImageData imageData || imageData.IsEmpty || imageData.Width <= 0 || imageData.Height <= 0)
            {
                _messageBoxService.ErrorDialog($"The current image data is invalid！");
                return false;
            }
            var roiDesc = HpImageViewer.ImageViewerDesc?.ROIDescs?.FirstOrDefault() as RectangleDesc;
            if (roiDesc == null)
            {
                _messageBoxService.ErrorDialog($"Please draw a rectangle ROI before creating the model!");
                return false;
            }
            ChangeCameraInfo = "Focusing ...";
            try
            {
                _testTrainService.TrainPositionModelFocus(PositioningModel, roiDesc);
                return true;
                //var fine_Res = _testTrainService.TrainGlobal(EnumGrabMode.SingleGrab);
                //if (fine_Res == null)
                //{
                //    return false;
                //}
                //PositioningModel.GlobalCurve = fine_Res.Curve;
                //PositioningModel.GlobalFocusOffset = fine_Res.ZFit;
                //var roi = roiDesc.CreateRoi();
                //var focusImages = fine_Res.PegImages;
                //var trainArg = CreateTrainArg(EnumGrabMode.SingleGrab);
                //var fineFocusInfo = GetTestFocusOrDefault().FineFocusInfo;
                ////使用全视野的聚焦序列图进行内、外层聚焦，并且获取聚焦曲线
                //var ret = _focusService.TrainCurve(fineFocusInfo, roi, focusImages, trainArg);
                //PositioningModel.RoiCurve = ret.Curve;
                //var pztOffset = ret.ZFit;
                //_pztservice?.AMov(pztOffset);
                //PositioningModel.RoiFocusOffset = pztOffset;
                //SavePositioningImage();
                //var wliPztOffset = _testTrainService.TrainWLI(PositioningModel.Template.Name);
                //if (wliPztOffset != null)
                //{
                //    PositioningModel.WliFocusOffset = wliPztOffset.Value;
                //}
                //return true;
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

        //private void SavePositioningImage()
        //{
        //    var filePath = Path.Combine(TestFilesFolder, "Positioning.bmp");
        //    FileHelper.DeleteFile(filePath);
        //    var imageData = _alignmentService.MoveAndGrab();
        //    imageData.SaveBmp(filePath);
        //}

        internal void PositionTemplateUserModified(SingleTemplateControl control, ITemplateMatchingView.UserModifyType type, SingleTemplateMatchingContext context, bool isSuccess)
        {

            if (!isSuccess || type != ITemplateMatchingView.UserModifyType.ModelCreated || HpImageViewer.Image is not ImageData imageData) return;
            PositioningModel.TrainMatchItem = context.MatchingItems[0];
            var templatePosition = _alignmentService.ConvertImageToAxis(_alignmentService.GetCurrentAxisPosition(), PositioningModel.TrainMatchItem, imageData.Width, imageData.Height);
            if (PositioningModel.IsForEachTarget == false)
            {
                PositioningModel.R3 = _alignmentService.GetFieldR3(templatePosition).R3;
            }

            // 如果TargetPoint为空，则说明没有设置量测点的ROI，无法创建定位模板
            if (TargetPoint == null) return;
            context.R4 = TargetPoint - templatePosition;
            PositioningModel.RefreshIsAvailable();
        }

        ///// <summary>
        ///// 全视野聚焦，并训练聚焦曲线
        ///// </summary>
        ///// <returns></returns>
        //private FocusTrainResult TrainGlobal(EnumGrabMode grabMode)
        //{

        //    var trainArg = CreateTrainArg(grabMode);
        //    var allRangeFocusInfo = CreateFineTrainInfo();
        //    var res = _focusService.FineFocusTrain(allRangeFocusInfo, trainArg, out var maxSharpness);

        //    return res;
        //}

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


        [RelayCommand]
        protected async void FindTarget0()
        {
            if (TrainSetting.PositioningTemplateInfo.IsForEachTarget)
                return;
            if (TargetPoint == null)
            {
                _messageBoxService.ErrorDialog($"Please set/reset the measurement roi first!");
                return;
            }
            try
            {
                ChangeCameraInfo = "Find target0 ...";
                HpImageViewer.UpdateROIs();
                var target0 = TestInfo.TargetSetting.EnabledTargetPoints.FirstOrDefault();
                if (target0 == null)
                {
                    throw new Exception("There is no available measurement target point!");
                }
                await Task.Run(() =>
                {
                    var teachingPoint = _alignmentService.GetCurrentAxisPosition();
                    var field = _alignmentService.GetFieldR3(teachingPoint).Field;
                    var fieldLLC = _alignmentService.GetAxisPoint(field, new WcsPoint(), true);
                    _visionService.StartGrabbing();
                    var point = _alignmentService.GetAxisPoint(field, target0, true);
                    TargetPoint = GetTargetAxisPoint(point);
                    if (TargetPoint == null)
                    {
                        throw new Exception("The first available measurement target point was not matched!");
                    }
                    else
                    {
                        _alignmentService.MoveTo(TargetPoint);
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.ErrorDialog(ex.Message);
                TrainSetting.PositioningTemplateInfo.IsForEachTarget = true;
            }
            finally
            {
                _visionService.StopGrabbing();
                ChangeCameraInfo = "";
            }
        }

        [RelayCommand]
        protected async void TestPosition()
        {
            await Task.Run(() =>
            {
                try
                {
                    ChangeCameraInfo = "Test positioning target ...";
                    if (!PositioningModel.IsAvailable)
                    {
                        _messageBoxService?.ErrorDialog("Please create a positioning template first");
                        return;
                    }
                    HpImageViewer.UpdateROIs();
                    _testTrainService.TestPositionModel(PositioningModel);

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

        [RelayCommand]
        protected void ToPositionModel()
        {
            try
            {
                _testTrainService.ToPositionModel(TargetPoint, PositioningModel);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.ErrorDialog(ex.Message);
            }
        }

        /// <summary>
        /// 自动修正r3值
        /// </summary>
        protected async Task AutoCorrectR3()
        {
            await Task.Run(() =>
            {
                _testTrainService.AutoCorrectR3(GetTargetAxisPoint);
            });
        }
        #endregion 

    }
}
