using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.ComUI.Helpers;
using InteVega.CBB.Db.Api.Interface;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.Recipe.Ui.ViewModel;
using Marvel.Application.Extentions;
using Marvel.Application.Services;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Job;
using Marvel.DataModels.Recipe;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using Bases = Ovl.UI.Bases;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Job.ViewModel;

public partial class AddJobViewModel : Bases.ViewModelBase
{
    [ObservableProperty]
    string _name;
    [ObservableProperty]
    string _description;
    [ObservableProperty]
    RecipeMenuViewModel _recipeMenuViewModel;
    [ObservableProperty]
    RunParamViewModel _runParamViewModel;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveDataCommand))]
    JobItem _jobItem;
    [ObservableProperty]
    Visibility _isShowBatchAdd;
    [ObservableProperty]
    Visibility _isShowContent = Visibility.Collapsed;
    [ObservableProperty]
    Visibility _isShowFirst = Visibility.Visible;
    private readonly Dispatcher _dispatcher;
    private readonly IRecipeManager _recipeManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEapControlService? _eapControlService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveDataCommand))]
    private bool _isLocalOperationAllowed = true;

    public bool IsAdd { get; set; }
    //  public IEnumerable <JobItem> JobItemList { get; set; }

    /// <summary>
    /// The title of the dialog that will show in the window title bar.
    /// </summary>
    public override string Title { get; set; } = "Add / Edit Job";
    /// <summary>
    /// 保存 Job 回调。返回 true 表示可关闭 Add/Edit Job 对话框，false 表示保留对话框（如 LotId 重复）。
    /// </summary>
    public Func<List<JobItem>, bool> CallbackRefresh { get; set; }

    public Action<JobItem> UnloadJobAction { get; set; }
    private RecipeElementsInfo cur_SelRecipeElementsInfo;


    IMessageBoxService _messageBoxService;
    public AddJobViewModel(IServiceProvider serviceProvicder)
    {
        _serviceProvider = serviceProvicder;
        _eapControlService = serviceProvicder.GetService<IEapControlService>();
        IMessageBoxService messageBoxService = serviceProvicder.GetService<IMessageBoxService>();

        RecipeMenuViewModel recipeMenuViewModel = serviceProvicder.GetService<RecipeMenuViewModel>();
        _dispatcher = Dispatcher.CurrentDispatcher;
        _messageBoxService = messageBoxService;
        RecipeMenuViewModel = recipeMenuViewModel;
        RecipeMenuViewModel.IsShowContextMenu = Visibility.Collapsed;
        recipeMenuViewModel.OutSideLoadMenuTree();
        recipeMenuViewModel.SearchRecipe("");
        RecipeMenuViewModel.GetRecipe = (node, recipe) => OpenRecipe(node, recipe);
        RunParamViewModel = new RunParamViewModel(serviceProvicder , messageBoxService) { IsJobRunParameter = true };
        RunParamViewModel.IsShowSubmit = Visibility.Collapsed;
        RunParamViewModel.ButtonHeight = 0;

        if (_eapControlService != null)
        {
            _eapControlService.StateChanged += OnEapControlStateChanged;
            ApplyEapSnapshot(_eapControlService.Snapshot);
        }
    }

    private void OnEapControlStateChanged(object? sender, EapControlSnapshot snapshot)
    {
        _dispatcher.BeginInvoke(() => ApplyEapSnapshot(snapshot));
    }

    private void ApplyEapSnapshot(EapControlSnapshot snapshot)
    {
        IsLocalOperationAllowed = !snapshot.IsRemote;
        SaveDataCommand.NotifyCanExecuteChanged();
    }

    public override void OnDialogOpened(IDialogParameters parameters)
    {
        base.OnDialogOpened(parameters);
        ApplyToolIdFromStageConfig();
    }

    /// <summary>
    /// 从 StageVisionConfig.StageID 自动填充 JobItem.ToolID
    /// </summary>
    private void ApplyToolIdFromStageConfig()
    {
        if (JobItem == null)
        {
            return;
        }
        var stageVisionConfig = new StageVisionConfig().PrepareConfig(_serviceProvider);
        JobItem.ToolID = stageVisionConfig?.StageID;
    }

    [RelayCommand]
    public void LoadWafers()
    {
        FileDialogHelper.OpenFileDialog("excel 文件|*.xls;*.xlsx;*.csv", (file) =>
        {
            try
            {
                var excelService = _serviceProvider.GetService<IExcelService>();
                if (excelService == null)
                {
                    _messageBoxService.ErrorDialog("Excel service is not available！");
                    return;
                }

                var waferInfos = WaferInfoDataService.LoadWaferInfos(excelService, file);
                if (waferInfos == null || waferInfos.Count == 0)
                {
                    _messageBoxService.WarningDialog("Failed to load wafer infos, or the file has no valid data！");
                    return;
                }

                var waferSelectInfo = RunParamViewModel?.RunParameter?.WaferSelectInfo;
                if (waferSelectInfo == null)
                {
                    _messageBoxService.WarningDialog("Please select a recipe first！");
                    return;
                }

                if (waferSelectInfo.SelectedSlots.Count <= 0)
                {
                    for (int i = 1; i < 26; i++)
                    {
                        waferSelectInfo.SelectedSlots.Add(new SlotData
                        {
                            SlotNum = i,
                            IsSelected = false
                        });
                    }
                }

                foreach (var slot in waferSelectInfo.SelectedSlots)
                {
                    slot.IsSelected = false;
                    slot.WaferID = null;
                    slot.ChuckID = null;
                }

                int appliedCount = 0;
                foreach (var info in waferInfos)
                {
                    int slotNo = info.SlotNum;

                    if (slotNo > waferSelectInfo.SelectedSlots.Count)
                        continue;

                    var slot = new SlotData
                    {
                        SlotNum = slotNo,
                        WaferID = info.WaferID,
                        ChuckID = info.ChuckID,
                        IsSelected = true
                    };
                    waferSelectInfo.SelectedSlots[slotNo - 1] = slot; //此处需要重新赋值，否则界面上的ToolTip 无法更新
                    appliedCount++;
                }


                if (appliedCount == 0)
                {
                    _messageBoxService.WarningDialog("No valid Slot (1-25) found in the file！");
                }
                else
                {
                    _messageBoxService.SuccessDialog($"Loaded {appliedCount} wafers info.");
                }
            }
            catch (Exception ex)
            {
                _messageBoxService.ErrorDialog("Failed to load wafer infos！" + ex.Message);
                LogHelper.LogError(ex, "LoadWafers errored!");
            }
        });
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    public void SaveData()
    {
        if (_eapControlService?.Snapshot.IsRemote == true)
        {
            _messageBoxService.WarningDialog(
                "Job management is unavailable while EAP is in Remote mode.");
            return;
        }

        if (string.IsNullOrWhiteSpace(JobItem.LotId))
        {
            _messageBoxService.WarningDialog("LotId cannot be empty！");
            return;
        }
        if (JobItem.LotId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            _messageBoxService.WarningDialog("LotId are not allowed to have special characters");
            return;
        }
        else if (string.IsNullOrWhiteSpace(JobItem.RecipeId))
        {
            _messageBoxService.WarningDialog("Please select the specified Recipe (double-click to select)！");
            return;
        }
        if (!RecipeMenuViewModel._recipeInfoList.Any(_=>_.Id==JobItem.RecipeId))
        {
            _messageBoxService.WarningDialog("The current recipe does not exist！");
            return;
        }
        if(!RunParamViewModel.CheckTisOptimizeModeEnable())
        {
            return;
        }
        if (!RunParamViewModel.CheckTisOptimizeFoolProof(JobItem.RecipeId))
        {
            return;
        }
        if (!RunParamViewModel.CheckMroRankTisValidation(JobItem.RecipeId))
        {
            return;
        }

        try
        {
            if (!RunParamViewModel.CheckSequences()) return;
            //RunParamViewModel.RunParameter.WaferSelectInfo.SelectMode = RunParamViewModel.SelectMode;
            //RunParamViewModel.RunParameter.TisMode = RunParamViewModel.TisMode;
            //RunParamViewModel.RunParameter.IsTisCalibration = RunParamViewModel.IsTisCalibration;
            RunParamViewModel.RunParameter.AutoRetrainMode = RunParamViewModel.SelectedAutoRetrainItem.Value;
            JobItem.RunParameters = RunParamViewModel.RunParameter;
            List <JobItem > jobItems = new List<JobItem>();
            if(IsAdd )
            {
                int i = JobItem.LotId.Length - 1;
                for (; i >= 0; i--)
                {
                    var c = JobItem.LotId[i];
                    if (c > '9' || c < '0')
                    { break; }
                }
                string lotId = JobItem.LotId.Substring(0, i + 1);
                string lotNum = JobItem.LotId[(i + 1)..];
                long.TryParse(lotNum, out long num);
                for (i = 0; i < JobItem.Batch; i++)
                {
                    JobItem item = CloneHelper.ReflectionClone<JobItem>(JobItem);
                    if (i == 0)
                    {
                        item.LotId = JobItem.LotId;
                    }
                    else
                    {
                        item.LotId = lotId + (num + i);
                    }
                    jobItems.Add(item);
                }
            }
            else
            {
                jobItems.Add(JobItem);
            }
            if (CallbackRefresh?.Invoke(jobItems) ?? true)
            {
                InvokeUnloadAction();
                RaiseRequestClose(new DialogResult());
            }
            //if (JobItem.Id != null)
            //{
            //    _measureDataDbService.EditJobRuningInfo(JobItem, JobItem.RunParameters);
            //}
            ////
            //_measureDataDbService.InsertOrUpdateTestInfo(cur_SelRecipeElementsInfo);
        }
        catch (Exception ex)
        {
            _messageBoxService.ErrorDialog("Failed to save data！"+ex.Message);
        }
    }

    //! check the lotid
    private bool CanSave()
    {

        if (!IsLocalOperationAllowed || JobItem == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(JobItem.LotId))
        {
            return false;
        }
        if (JobItem.LotId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return false;
        }

        return true;
    }
    public void RefreshSaveState()
    {
        SaveDataCommand.NotifyCanExecuteChanged();
    }

    public override void OnDialogClosed()
    {
        if (_eapControlService != null)
            _eapControlService.StateChanged -= OnEapControlStateChanged;

        base.OnDialogClosed();
    }


    /// <summary>
    /// 触发强制Unload的Action
    /// </summary>
    private void InvokeUnloadAction()
    {
        // 如果已经装载了Port的情况下Editjob页面打开并编辑了上料模式则强制Unload
        if (JobItem.PortIndex != 0 && RunParamViewModel.CheckModeChanged())
        {
            UnloadJobAction?.Invoke(JobItem);
        }
    }


    [RelayCommand]
    public void Close()
    {
        RaiseRequestClose(new DialogResult());
    }
    public void OpenRecipe(IRecipeManager recipeManager, RecipeInfo recipeInfo)
    {
        Task.Run(() =>
        {
            ProgressDialogHelper.ShowDialog("Loading recipe...", () =>
            {
                try
                {
                    IRecipe recipeNodeService = recipeManager.GetCurrentRecipe(recipeInfo.Id);
                    string recipeName = recipeInfo.RecipeName;
                    var recipeElementsInfo = recipeNodeService.Get<RecipeElementsInfo>();
                    if (recipeElementsInfo == null)
                    {
                        recipeElementsInfo = new RecipeElementsInfo() { Id = recipeInfo.Id };
                    }
                    RunParamViewModel.RequestData(recipeNodeService, recipeElementsInfo, recipeInfo);
                    cur_SelRecipeElementsInfo = recipeElementsInfo;
                    bool isNext = true;
                    if (recipeElementsInfo.GetTestPointCount() == 0)
                    {
                        var msgRes = _messageBoxService.WarningAskDialog($"If the selected recipe {recipeName} does not have any target points that require measurement, no measurement function will be performed. Is it necessary to add it to the Job", "No target point");
                        if (msgRes != ButtonResult.Yes && msgRes != ButtonResult.OK)
                        {
                            isNext = false;
                            //return;
                        }
                    }
                    if (isNext)
                    {
                        _dispatcher.Invoke(() =>
                        {
                            IsShowContent = Visibility.Visible;
                            IsShowFirst = Visibility.Collapsed;
                            JobItem.RecipeId = recipeInfo.Id;
                            JobItem.RecipeName = recipeName;
                        });
                    }
                }
                catch (Exception ex)
                {
                    string err = "Failed to open recipe! Please make sure that the file exists and that the file format is not corrupted";
                    _messageBoxService.ErrorDialog(err );
                    LogHelper.LogError(ex, "Select recipe for job errored!");
                }
                return true;
            });
        });
    }
}
