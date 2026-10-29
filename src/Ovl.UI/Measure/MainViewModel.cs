using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ovl.Biz.Common.Enums;
using Ovl.Biz.Common.Measure.Models;
using Ovl.Biz.Services.Eap;
using Ovl.Biz.Services.Measure;
using Ovl.UI.Dialogs;
using Ovl.UI.Navigation;

namespace Ovl.UI.Measure;

/// <summary>
/// 主量测页面 ViewModel（对应原 Marvel.App.Shared.MainViewModel）。
///
/// 迁移策略：原版 1970 行 VM 深度依赖 HalconDotNet、Prism 事件聚合、MediatR 与厂内
/// DomainService/Recipe/User 等包，M3b-2 先按“界面绑定面”完整重建状态与命令
/// （属性名、命令名与原版一致，XAML 绑定路径不动），服务侧只依赖 Ovl 自有适配接口。
/// 原版的算法/配方/用户权限逻辑随后续里程碑按接口实现替换。
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IPageViewModel, IDisposable
{
    private readonly IMainRunService _runService;
    private readonly IRecipeManager _recipeManager;
    private readonly IEapControlService? _eapService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MainViewModel>? _logger;
    private bool _disposed;

    public MainViewModel(
        IMainRunService runService,
        IRecipeManager recipeManager,
        IDialogService dialogService,
        IEapControlService? eapService = null,
        ILogger<MainViewModel>? logger = null)
    {
        _runService = runService;
        _recipeManager = recipeManager;
        _dialogService = dialogService;
        _eapService = eapService;
        _logger = logger;

        _runService.StateChanged += OnRunStateChanged;
        _runService.ProgressChanged += OnProgressChanged;

        Title = "Home";
        RecipeName = _recipeManager.CurrentRecipeName;
        JobInfoModelList.CollectionChanged += (_, _) => UpdateJobQueueState();
        SeedDemoJobQueue();
    }

    /// <summary>页面标题（导航服务写入）。</summary>
    public string Title { get; set; }

    /// <summary>作业队列。</summary>
    public ObservableCollection<JobInfoModel> JobInfoModelList { get; } = [];

    /// <summary>当前运行作业的数据与统计。</summary>
    [ObservableProperty]
    private RunningJobDataAndStatistics _jobRuningDataList = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditJobCommand))]
    private JobInfoModel? _selectedJob;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(RunJobCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopJobCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
    private EnumRunState _runStatus = EnumRunState.IDLE;

    [ObservableProperty]
    private string _runStatusStr = "Idle";

    [ObservableProperty]
    private string _recipeName = string.Empty;

    [ObservableProperty]
    private string _lotId = string.Empty;

    [ObservableProperty]
    private string _port = "1";

    [ObservableProperty]
    private int _portIndex;

    [ObservableProperty]
    private string _waferProgress = "0/0";

    [ObservableProperty]
    private double _waferProgressValue;

    [ObservableProperty]
    private string _targetProgress = "0/0";

    [ObservableProperty]
    private double _targetProgressValue;

    [ObservableProperty]
    private bool _waferMapsVisible = true;

    [ObservableProperty]
    private bool _qmeritWaferMapsVisible;

    [ObservableProperty]
    private bool _isUpdateWafermap;

    [ObservableProperty]
    private bool _isMroRunning;

    [ObservableProperty]
    private bool _isMroAutoTraining;

    /// <summary>EAP 远程控制是否允许（EAP 未连接时本地可操作）。</summary>
    [ObservableProperty]
    private bool _isEapRemoteEnable = true;

    /// <summary>运行控制区是否可用。</summary>
    [ObservableProperty]
    private bool _isEnable = true;

    /// <summary>相机画面左上角提示文本。</summary>
    [ObservableProperty]
    private string _sectionInfo = string.Empty;

    /// <summary>量测值单位换算系数（原版为 int，来自 MeasureValueUnitFactorConfig，缺省 1）。</summary>
    [ObservableProperty]
    private int _measureValueUnitFactor = 1;

    [ObservableProperty]
    private string _nmPerPixel = "1";

    [ObservableProperty]
    private string _nmPerPixelQmerit = "1";

    /// <summary>Wafer Map 页签选中底色（白色 = 选中）。</summary>
    [ObservableProperty]
    private string _waferMapSelBackGround = "#fff";

    /// <summary>Qmerit Wafer Map 页签选中底色。</summary>
    [ObservableProperty]
    private string _qmeritWaferMapSelBackGround = "Transparent";

    /// <summary>Misreg Wafer Map 承载控件（由 View 在加载时注入实际控件实例）。</summary>
    [ObservableProperty]
    private object? _misregWaferMapControl;

    /// <summary>Qmerit Wafer Map 承载控件（由 View 在加载时注入实际控件实例）。</summary>
    [ObservableProperty]
    private object? _qmeritWaferMapControl;

    /// <summary>是否正在运行（用于按钮可用性）。</summary>
    public bool IsRunning => RunStatus == EnumRunState.Running;

    /// <summary>是否空闲。</summary>
    public bool IsIdle => RunStatus == EnumRunState.IDLE;

    // ── 生命周期命令 ─────────────────────────────────────────────

    [RelayCommand]
    private void Loaded()
    {
        _logger?.LogDebug("主量测页面已加载");
        IsEapRemoteEnable = _eapService?.State != EapConnectionState.Connected;
        SectionInfo = string.Empty;
        UpdateProgressText();
    }

    [RelayCommand]
    private void Unloaded()
    {
        _logger?.LogDebug("主量测页面已卸载");
    }

    // ── 作业队列命令 ─────────────────────────────────────────────

    [RelayCommand]
    private void AddJob()
    {
        var job = new JobInfoModel
        {
            LotId = $"DEMO{JobInfoModelList.Count + 1:0000}",
            RecipeName = string.IsNullOrEmpty(RecipeName) ? "Default" : RecipeName,
            PortIndex = JobInfoModelList.Count % 2,
            State = EnumJobStates.Waiting,
            Iterations = 1,
            WaferIterations = 1,
        };
        job.Port = (job.PortIndex + 1).ToString();
        JobInfoModelList.Add(job);
        SelectedJob = job;
    }

    [RelayCommand(CanExecute = nameof(CanEditJob))]
    private void EditJob()
    {
        if (SelectedJob is null)
        {
            return;
        }

        SelectedJob.LotId = $"{SelectedJob.LotId}".Trim();
        OnPropertyChanged(nameof(JobInfoModelList));
    }

    private bool CanEditJob() => SelectedJob?.EditEnable == true;

    [RelayCommand]
    private void DeleteJobAll()
    {
        var selected = JobInfoModelList.Where(j => j.IsSelectJob).ToList();
        if (selected.Count == 0)
        {
            _dialogService.Warn("请先勾选要删除的作业。");
            return;
        }

        if (!_dialogService.Confirm($"确认删除 {selected.Count} 个作业？"))
        {
            return;
        }

        foreach (var job in selected)
        {
            JobInfoModelList.Remove(job);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunJob))]
    private async Task RunJobAsync()
    {
        if (SelectedJob is null)
        {
            _dialogService.Warn("请先选择一个作业。");
            return;
        }

        SelectedJob.State = EnumJobStates.Running;
        LotId = SelectedJob.LotId;
        RecipeName = SelectedJob.RecipeName;
        PortIndex = SelectedJob.PortIndex;
        Port = (SelectedJob.PortIndex + 1).ToString();
        JobRuningDataList = new RunningJobDataAndStatistics();
        SeedDemoMeasureData();

        await _runService.StartAsync().ConfigureAwait(true);
    }

    private bool CanRunJob() => RunStatus is EnumRunState.IDLE or EnumRunState.IDLE_WITH_ALARM;

    [RelayCommand]
    private async Task RunLastJobAsync()
    {
        var last = JobInfoModelList.LastOrDefault();
        if (last is null)
        {
            _dialogService.Warn("作业队列为空。");
            return;
        }

        SelectedJob = last;
        await RunJobAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private void StopJobQueue()
    {
        if (!_dialogService.Confirm("确认暂停作业队列？（当前作业完成后不再取新作业）"))
        {
            return;
        }

        foreach (var job in JobInfoModelList.Where(j => j.State == EnumJobStates.Waiting))
        {
            job.State = EnumJobStates.Paused;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStopJob))]
    private async Task StopJobAsync()
    {
        if (!_dialogService.Confirm("确认停止当前作业？"))
        {
            return;
        }

        await _runService.StopAsync().ConfigureAwait(true);
        if (SelectedJob is not null)
        {
            SelectedJob.State = EnumJobStates.Paused;
        }
    }

    private bool CanStopJob() => IsRunning;

    [RelayCommand(CanExecute = nameof(CanPause))]
    private async Task PauseAsync()
    {
        if (RunStatus == EnumRunState.PAUSED)
        {
            await _runService.ResumeAsync().ConfigureAwait(true);
            return;
        }

        await _runService.PauseAsync().ConfigureAwait(true);
    }

    private bool CanPause() => RunStatus is EnumRunState.Running or EnumRunState.PAUSED;

    // ── Wafer Map 命令 ───────────────────────────────────────────

    [RelayCommand]
    private void SelectWaferMap(object? parameter)
    {
        var isQmerit = parameter?.ToString() == "QmeritWaferMap";
        WaferMapsVisible = !isQmerit;
        QmeritWaferMapsVisible = isQmerit;
        WaferMapSelBackGround = isQmerit ? "Transparent" : "#fff";
        QmeritWaferMapSelBackGround = isQmerit ? "#fff" : "Transparent";
    }

    [RelayCommand]
    private void NmPixelChange()
    {
        if (!double.TryParse(NmPerPixel, out var scale) || scale <= 0)
        {
            _dialogService.Warn("Wafer scale 必须是大于 0 的数值。");
            return;
        }

        _logger?.LogInformation("更新 Wafer Map 比例：{Scale}", scale);
    }

    /// <summary>调试命令入口（原版保留的隐藏按钮）。</summary>
    [RelayCommand]
    private void TestCmd()
    {
        _logger?.LogDebug("TestCmd 触发");
    }

    // ── 运行服务事件 ─────────────────────────────────────────────

    private void OnRunStateChanged(object? sender, MeasureRunStateChangedArgs e)
    {
        RunStatus = e.State switch
        {
            MeasureRunState.Running => EnumRunState.Running,
            MeasureRunState.Paused => EnumRunState.PAUSED,
            MeasureRunState.Error => EnumRunState.IDLE_WITH_ALARM,
            _ => EnumRunState.IDLE,
        };

        RunStatusStr = GetDescription(RunStatus);
        IsEnable = RunStatus is not EnumRunState.EMERGENCY_STOP;
    }

    private void OnProgressChanged(object? sender, EventArgs e)
    {
        WaferProgressValue = _runService.WaferProgressValue;
        TargetProgressValue = _runService.TargetProgressValue;
        UpdateProgressText();
    }

    private void UpdateProgressText()
    {
        WaferProgress = $"{WaferProgressValue:0}%";
        TargetProgress = $"{TargetProgressValue:0}%";
    }

    private void UpdateJobQueueState()
    {
        OnPropertyChanged(nameof(JobInfoModelList));
    }

    private static string GetDescription(EnumRunState state)
    {
        var field = state.GetType().GetField(state.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? state.ToString();
    }

    // ── 演示数据（无硬件桩阶段：便于校验表格列与统计区渲染） ─────────

    private void SeedDemoJobQueue()
    {
        JobInfoModelList.Add(new JobInfoModel
        {
            LotId = "LOT-DEMO-001",
            RecipeName = "OVL_DEMO",
            PortIndex = 0,
            Port = "1",
            State = EnumJobStates.Waiting,
            Iterations = 1,
            WaferIterations = 25,
        });
        JobInfoModelList.Add(new JobInfoModel
        {
            LotId = "LOT-DEMO-002",
            RecipeName = "OVL_DEMO",
            PortIndex = 1,
            Port = "2",
            State = EnumJobStates.Normal,
            Iterations = 1,
            WaferIterations = 25,
        });
        SelectedJob = JobInfoModelList[0];
    }

    private void SeedDemoMeasureData()
    {
        var random = new Random(20261028);
        for (var i = 0; i < 12; i++)
        {
            var overlay = new OverlayData
            {
                MisregX = Math.Round((random.NextDouble() - 0.5) * 0.02, 5),
                MisregY = Math.Round((random.NextDouble() - 0.5) * 0.02, 5),
                CenterInner = new Point2D { X = Math.Round(random.NextDouble(), 3), Y = Math.Round(random.NextDouble(), 3) },
                CenterOuter = new Point2D { X = Math.Round(random.NextDouble(), 3), Y = Math.Round(random.NextDouble(), 3) },
            };
            JobRuningDataList.RawData.Add(new RawDataRow
            {
                WaferSlotNo = 1 + i / 6,
                WaferIterations = 1,
                Status = i % 7 == 0 ? "Fail" : "Pass",
                Orientation = "0",
                TestName = "OVL",
                TestType = "OVL",
                Field = new RowColumnInfo { ColumnIndex = i % 3, RowIndex = i / 3 },
                Cell = i,
                Index = i + 1,
                R3 = new Point2D { X = i % 3, Y = i / 3 },
                Iterations = 25,
                Overlay = overlay,
                CdResultStr = string.Empty,
            });
        }

        JobRuningDataList.LotStatistics_Misreg.Add(new LotStatisticsRow
        {
            DataType = "OVL",
            LotID = LotId,
            RecipeName = RecipeName,
            XMean = 0.0012,
            YMean = -0.0008,
            X3Sigma = 0.0041,
            Y3Sigma = 0.0039,
            XMeanPlusX3Sigma = 0.0053,
            YMeanPlusY3Sigma = 0.0047,
            XMin = -0.0091,
            YMin = -0.0088,
            XMax = 0.0102,
            YMax = 0.0095,
            TISMean = new Point2D { X = 0.0006, Y = -0.0004 },
            TISMin = new Point2D { X = -0.0035, Y = -0.0031 },
            TISMax = new Point2D { X = 0.0038, Y = 0.0034 },
            TIS3Sigma = new Point2D { X = 0.0019, Y = 0.0018 },
            TISMeanPlusX3Sigma = new Point2D { X = 0.0025, Y = 0.0022 },
            TISMeanPlusY3Sigma = new Point2D { X = 0.0024, Y = 0.0023 },
        });
        JobRuningDataList.LotStatistics_TIS.Add(new LotStatisticsRow
        {
            DataType = "TIS",
            LotID = LotId,
            RecipeName = RecipeName,
            TISMean = new Point2D { X = 0.0006, Y = -0.0004 },
            TISMin = new Point2D { X = -0.0035, Y = -0.0031 },
            TISMax = new Point2D { X = 0.0038, Y = 0.0034 },
            TIS3Sigma = new Point2D { X = 0.0019, Y = 0.0018 },
            TISMeanPlusX3Sigma = new Point2D { X = 0.0025, Y = 0.0022 },
            TISMeanPlusY3Sigma = new Point2D { X = 0.0024, Y = 0.0023 },
        });
        for (var slot = 1; slot <= 2; slot++)
        {
            JobRuningDataList.WaferStatistics.Add(new WaferStatisticsRow
            {
                DataType = "OVL",
                WaferSlotNo = slot,
                LotID = LotId,
                RecipeName = RecipeName,
                XMean = 0.001 * slot,
                YMean = -0.0006 * slot,
                X3Sigma = 0.0038 + (0.0001 * slot),
                Y3Sigma = 0.0036 + (0.0001 * slot),
                XMeanPlusX3Sigma = 0.0048 + (0.0001 * slot),
                YMeanPlusY3Sigma = 0.0042 + (0.0001 * slot),
                XMin = -0.0089,
                YMin = -0.0085,
                XMax = 0.0098,
                YMax = 0.0091,
            });
        }

        JobRuningDataList.RawData.CollectionChanged += (_, _) => OnPropertyChanged(nameof(JobRuningDataList));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _runService.StateChanged -= OnRunStateChanged;
        _runService.ProgressChanged -= OnProgressChanged;
        _disposed = true;
    }
}
