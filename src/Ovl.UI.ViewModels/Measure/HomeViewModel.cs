using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ovl.Biz.Services.Measure;
using Ovl.UI.ViewModels.Dialogs;
using Ovl.UI.ViewModels.Navigation;

namespace Ovl.UI.ViewModels.Measure;

/// <summary>
/// Home 量测主页面 ViewModel（骨架版）。
/// 对应原 Marvel.App.Shared MainViewModel 的状态子集：Lot/Recipe/Port/运行状态/进度/作业队列。
/// 原版 1970 行 VM 将在 M3b 按功能分块迁入；本版仅保证页面显示与数据绑定链路可用。
/// </summary>
public partial class HomeViewModel : ObservableObject, IPageViewModel
{
    private readonly IMainRunService _mainRun;
    private readonly IRecipeManager _recipeManager;
    private readonly IDialogService _dialogService;

    public HomeViewModel(IMainRunService mainRun, IRecipeManager recipeManager, IDialogService dialogService)
    {
        _mainRun = mainRun;
        _recipeManager = recipeManager;
        _dialogService = dialogService;

        _recipeName = _recipeManager.CurrentRecipeName;
        RunStatusStr = _mainRun.State.ToString();

        _mainRun.StateChanged += OnRunStateChanged;
        _mainRun.ProgressChanged += OnRunProgressChanged;

        // 演示数据（骨架版）：真实作业队列由 EAP/Job 体系在 M4 迁入
        Jobs =
        [
            new MeasureJobInfo("JOB-001", "Recipe.Align", "Done", 100),
            new MeasureJobInfo("JOB-002", "Recipe.Align", "Ready", 0),
        ];
        _selectedJob = Jobs[0];
    }

    /// <summary>页面标题。</summary>
    [ObservableProperty]
    private string _title = "Home";

    [ObservableProperty]
    private string _lotId = "LOT-DEMO-0001";

    [ObservableProperty]
    private string _recipeName;

    [ObservableProperty]
    private int _port = 1;

    [ObservableProperty]
    private string _runStatusStr;

    [ObservableProperty]
    private bool _isEnable;

    [ObservableProperty]
    private string _waferProgress = "0 %";

    [ObservableProperty]
    private double _waferProgressValue;

    [ObservableProperty]
    private string _targetProgress = "0 %";

    [ObservableProperty]
    private double _targetProgressValue;

    [ObservableProperty]
    private string _sectionInfo = "-";

    [ObservableProperty]
    private string _iterations = "-";

    [ObservableProperty]
    private MeasureJobInfo? _selectedJob;

    /// <summary>作业队列。</summary>
    public IReadOnlyList<MeasureJobInfo> Jobs { get; }

    /// <summary>开始运行。</summary>
    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        await _mainRun.StartAsync();
    }

    private bool CanStart() => _mainRun.State is MeasureRunState.Idle or MeasureRunState.Error;

    /// <summary>停止运行（需确认，保留确认弹窗）。</summary>
    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        if (_dialogService.Confirm("确认停止当前量测运行？"))
        {
            await _mainRun.StopAsync();
        }
    }

    private bool CanStop() => _mainRun.State is MeasureRunState.Running or MeasureRunState.Paused;

    /// <summary>页面加载（原 LoadedCommand 骨架）。</summary>
    [RelayCommand]
    private void Loaded()
    {
        RefreshProgress();
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    /// <summary>页面卸载。</summary>
    [RelayCommand]
    private void Unloaded()
    {
    }

    private void OnRunStateChanged(object? sender, MeasureRunStateChangedArgs e)
    {
        RunStatusStr = e.State.ToString();
        IsEnable = e.State is MeasureRunState.Running or MeasureRunState.Paused;
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    private void OnRunProgressChanged(object? sender, EventArgs e)
    {
        RefreshProgress();
    }

    private void RefreshProgress()
    {
        WaferProgressValue = _mainRun.WaferProgressValue;
        WaferProgress = $"{WaferProgressValue:0}%";
        TargetProgressValue = _mainRun.TargetProgressValue;
        TargetProgress = $"{TargetProgressValue:0}%";
    }
}
