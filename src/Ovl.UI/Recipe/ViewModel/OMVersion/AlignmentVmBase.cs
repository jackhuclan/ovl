using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.TemplateMatching.Core;
using Marvel.Application.Exceptions;
using Marvel.Application.Services;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Marvel.WaferMapCommon;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using Wafer.Algorithm.Common.DoNet;

namespace Ovl.UI.Recipe.ViewModel.OMVersion;

public partial class AlignmentVmBase : RecipeElementViewModelBase
{
    public AlignmentVmBase(IDialogService dialogService,
        IServiceProvider sp,
        IMessageBoxService messageBoxService) : base(sp)
    {
        _serviceProvider = sp;
        _templateMatchingService = sp.GetService<ITemplateMatchingService>();

        _fineAlignTemplateContext ??= new MultiTemplateMatchingContext() { IsForAlignment = true };

        _dialogService = dialogService;
        _motionService = sp.GetService<IMotionControllerService>();
        _messageBoxService = messageBoxService;
        _StageController = sp.GetService<IVisionServiceManager>().GetMainVision();
        _alignmentService = sp.GetService<IAlignmentService>();
        _cameraLightParamService = sp.GetService<ICameraLightParamService>();








    }

    /// <summary>
    /// 进入拉平页面后的默认相机（粗对准相机，OM100没有粗对准相机，所以默认为Align）
    /// </summary>
    protected ECamera _coarseAlignCamera = ECamera.Align;
    protected ICameraLightParamService _cameraLightParamService;
    [ObservableProperty]
    private AlignmentInfo alignmentInfo;
    [ObservableProperty]
    private WaferMapInfo waferInfo;


    [ObservableProperty]
    private WaferMapControl waferMapControlinstance;

    [ObservableProperty]
    private int coarseOrFineAlignmentSel;
    partial void OnCoarseOrFineAlignmentSelChanged(int value)
    {
        OnTableSelectedChanged(value);
    }

    protected virtual void OnTableSelectedChanged(int value)
    {

    }

    [ObservableProperty]
    GridLength stagePanalWidth = new GridLength(28);
    [ObservableProperty]
    bool stageExpanderState;
    [ObservableProperty]
    string _changeCameraInfo;


    /// <summary>
    /// 是否允许界面点击
    /// </summary>
    [ObservableProperty]
    bool _isEnableClick = true;

    partial void OnIsEnableClickChanged(bool value)
    {
        CheckIsEnableSwitch();
    }
    /// <summary>
    /// 是否出现报错
    /// </summary>
    [ObservableProperty]
    bool _isErrored;

    partial void OnIsErroredChanged(bool value)
    {
        CheckIsEnableSwitch();
    }

    private void CheckIsEnableSwitch()
    {
        IsEnableSwitchTable = IsEnableClick && IsErrored == false;
    }
    /// <summary>
    /// 是否允许界面点击
    /// </summary>
    [ObservableProperty]
    bool _isEnableSwitchTable = true;

    [ObservableProperty]
    Point2d _absoluteCow;

    [ObservableProperty]
    Visibility _flatDetectParamVisibility;
    [ObservableProperty]
    Visibility _notchDetectParamVisibility;

    protected IMessageBoxService _messageBoxService;
    protected IVisionService _StageController;
    protected IServiceProvider _serviceProvider;
    protected IMotionControllerService? _motionService;
    protected IAlignmentService _alignmentService;
    public IAlignmentService AlignmentService { get { return _alignmentService; } }
    protected IWaferCenteringService FindWaferCenteringService;
    //
    protected IDialogService _dialogService;

    ///// <summary>
    ///// 保存参数
    ///// </summary>
    [RelayCommand]
    public void SaveAlignmentResult()
    {
        SaveResult(CoarseOrFineAlignmentSel > 0);
    }

    /// <summary>
    /// 保存结果。返回是否保存成功（AI 工具 save_recipe 依赖该结果）。
    /// </summary>
    /// <param name="isFine">是否是精对齐界面</param>
    private bool SaveResult(bool isSaveModel = false, bool isShowDialog = true, bool isCheckAlignFields = true)
    {
        try
        {
            bool isErrored = false;
            if (isSaveModel)
            {
                SaveModelInifos();
                if (_isFineAlignment && isCheckAlignFields)
                {
                    AppHelper.DispatcherInvoke(() =>
                    {
                        var fields = WaferMapControlinstance.GetSelectedFields();
                        if (!(fields != null && fields.Count >= 2 && fields.Count <= 4))
                        {
                            _messageBoxService.ErrorDialog("Please select 2-4 fields！", "RecipeMessage");
                            isErrored = true;
                            return;
                        }
                        _recipeElementsInfo.Alignment.CustomizeFieldList = fields;
                    });
                }
                _recipeElementsInfo.Alignment.ModuleState = EnumModuleState.SetUp;

            }
            if (isErrored)
                return false;
            SaveCameraParams();
            SaveRecipeFile();
            if (isShowDialog)
                AppHelper.DispatcherInvoke(() =>
                {
                    _messageBoxService.SuccessDialog("Data saved successfully！");
                });
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex, "Failed to save data！");

            AppHelper.DispatcherInvoke(() =>
            {
                if (isShowDialog)
                    _messageBoxService.ErrorDialog("Failed to save data！");
                else
                    _messageBoxService.Error("Failed to save data！");
            });
            return false;
        }

    }




    public override void RequestData(IRecipe data, RecipeElementsInfo recipe, RecipeInfo recipeInfo)
    {
        try
        {
            base.RequestData(data, recipe, recipeInfo);
            AlignmentInfo = _alignmentService.RecipeElementsInfo.Alignment;
            WaferInfo = _alignmentService.WaferMapInfo;
            if (AlignmentInfo == null) return;
            AlignmentInfo.EdgeParams ??= new NotchAndEdgeDetectPar();

            NotchDetectParamVisibility = WaferInfo.NotchType == EnumNotchType.Notch ? Visibility.Visible : Visibility.Collapsed;
            FlatDetectParamVisibility = WaferInfo.NotchType == EnumNotchType.Flat ? Visibility.Visible : Visibility.Collapsed;
            //100机台的对准相机视野太小，无法显示完整的notch 口，所以无法使用notch 口拉平
            if (MarvelVersion.CurrentVersion == EnumOMVersion.OM100)
            {
                IsUseNotchVisible = Visibility.Collapsed;
                AlignmentInfo.IsUseNotch = false;
            }
            SetWaferMapField(recipe);
        }
        catch (Exception ex)
        {
            _messageBoxService.ErrorDialog("Failed to load data！", "RecipeMessage");
        }
    }

    /// <summary>
    /// 显示和操作waferMap
    /// </summary>
    /// <param name="recipe"></param>
    private void SetWaferMapField(RecipeElementsInfo recipe)
    {
        WaferMapControlinstance = new WaferMapControl() { IsEnableScale = false };
        WaferMapControlinstance.Draw(recipe.WaferMap);
        WaferMapControlinstance.FuncCheckField = () =>
        {
            System.Collections.Generic.List<RowColumnInfo> rowColumnList = WaferMapControlinstance.GetSelectedFields();

            if (rowColumnList.Count >= 4)
            {
                _messageBoxService.Warning("Only 2-4 fields can be selected", "RecipeMessage");
                return false;
            }
            return true;
        };
        var alignmentFields = _recipeElementsInfo.Alignment.CustomizeFieldList;
        if (alignmentFields == null || alignmentFields.Count < 2)
        {
            alignmentFields = _alignmentService.LoadDefault3Field();
        }
        WaferMapControlinstance.Select(alignmentFields);
    }

    protected void ChangeCamera(ECamera camera, Action actChangedCamera = null, bool axisShift = true)
    {
        ChangeCameraInfo = "Camera switching …";
        Task.Run(() =>
        {
            try
            {
                // await Task.Delay(2000);
                _StageController.SetCameraCoordAsNull();
                _alignmentService.ChangeCamera(camera, axisShift);
                UpdataStageCameraParamAndWriteToCamera(camera);
                _StageController.NotifyParamsChanged();
                GenWaferEdgeThreePointRandom();
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
                        _messageBoxService.Warning("switch camera out of softlimt,please move axis to appropriate position and try again", "RecipeMessage");
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
            finally
            {
                AbsoluteCow = _alignmentService.GetAbsoluteCow();
                ChangeCameraInfo = "";
                MoveToFocusZ();
                _StageController.StartGrabbing();
                initDisplayAlignInfo();
                actChangedCamera?.Invoke();
            }
        });
    }
    protected virtual void MoveToFocusZ()
    {
        //不同的相机及最佳焦面不一样，切换相机后需要自动移动到最佳焦面
        double z = AlignmentInfo.FocusHeight;
        if (z == 0)
            z = WaferInfo.Thickness;
        _alignmentService.MoveZTo(z, "WaferInfo Thickness");

    }

    protected void initDisplayAlignInfo()
    {
        Angle_FineAlign = AlignmentInfo.FineAngle;
        Angle_CoarseAlign = AlignmentInfo.CoarseAngle;
        AngleThreshold_CoarseAlign = AlignmentInfo.CoarseAngleThreshold;
        DisplayAlignmentMarkPos();
    }

    protected void MoveToTemplateMark()
    {
        //已经有手动移到模板位置，不需要再自动移动了
        //if (AlignmentMarkPos != Point2d.Zero)
        //{
        //   // _alignmentService.MoveTo(AlignmentMarkPos);
        //}
    }
    private void AlignmentHandleError(AssistantBase assistantBase, RunException runException)
    {
        try
        {
            IsEnableClick = true;
            IsErrored = true;
            ResetFineAlignEnableState(true);//区分find edge还是拉平，暂时屏蔽
            if (runException is RunException re)
            {
                AppHelper.DispatcherInvoke(() =>
                {
                    switch (re.RunParam)
                    {
                        case FocusRunArg focusRunArg:
                            _messageBoxService.ErrorDialog("Focus failed, please move the pattern more obvious area to focus, or move the Z axis to a clear surface!", "Exception");
                            break;
                        case FindEdgeArg edgeArg:
                            _messageBoxService.ErrorDialog($"{edgeArg} Then click the \"Run\" button to continue execution", "Exception");// ($"获取第{index}个边缘点出错，请移动机台使晶圆边缘出现在相机视野中！然后点击“运行”按钮继续执行", "异常");
                            break;
                        case FindAlignTemplateArg findTemplateArg:
                            _messageBoxService.ErrorDialog($"{findTemplateArg}. Then click the \"Run\" button to continue execution", "Exception");// ($"获取Mark点出错，请移动机台使Mark点出现在相机视野中。然后点击“运行”按钮继续执行", "异常");
                            break;

                        default:
                            _messageBoxService.ErrorDialog($"Error:{re.Message}", "Exception");
                            break;
                    }
                });

            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
        }
        finally
        {
            ChangeCameraInfo = "";
            _StageController.StartGrabbing();
        }

    }

    private void ContinueExecute()
    {
        IsEnableClick = false;
        IsErrored = false;
        AssistantBase.Retry();
    }

    partial void OnStageExpanderStateChanged(bool value)
    {
        StagePanalWidth = value ? new GridLength(350) : new GridLength(28);
    }
    protected virtual void UpdataStageCameraParamAndWriteToCamera(ECamera camera)
    {


    }

}
