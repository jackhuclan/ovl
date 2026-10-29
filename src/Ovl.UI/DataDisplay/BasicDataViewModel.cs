using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ovl.Biz.Common.Measure.Models;

namespace Ovl.UI.DataDisplay;

/// <summary>BasicData 面板页签节点。</summary>
public sealed record BasicMenuNode(int Id, string Name, bool IsSelect);

/// <summary>异常数据行（对应原版 JobAlarmList 条目）。</summary>
public sealed record JobAlarmRow(int Index, DateTime RestoreTime, string Message, string States);

/// <summary>日志条目（对应原版 LogViewModel.LogItemCollection 条目）。</summary>
public sealed record LogItem(DateTime OperateTime, string Level, string Source, string Message);

/// <summary>
/// 主界面左下角 BasicData 面板 ViewModel（对应原 Ovl.UI.DataDisplay.ViewModel.BasicDataViewModel，
/// 并吸收原 LogViewModel 的绑定面——本地实现中 Log 视图继承本 DataContext，不再经 Prism 容器解析）。
/// 数据源为内存演示数据；真实数据接入（历史结果 / 报警 / 日志服务）随后续里程碑替换。
/// </summary>
public sealed partial class BasicDataViewModel : ObservableObject
{
    public BasicDataViewModel()
    {
        SeedDemoData();
    }

    /// <summary>页签（History / Log / Alarm）。</summary>
    public ObservableCollection<BasicMenuNode> MenuNodeList { get; } =
    [
        new BasicMenuNode(0, "History", true),
        new BasicMenuNode(1, "Log", false),
        new BasicMenuNode(2, "Alarm", false),
    ];

    [ObservableProperty]
    private Visibility _historyVisible = Visibility.Visible;

    [ObservableProperty]
    private Visibility _logVisible = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _errorVisible = Visibility.Collapsed;

    /// <summary>量测值单位换算系数。</summary>
    public int MeasureValueUnitFactor { get; } = 1;

    /// <summary>历史结果表（Overlay 统计历史）。</summary>
    public ObservableCollection<LotStatisticsRow> OverlayJobHistoryData { get; } = [];

    /// <summary>异常数据表。</summary>
    public ObservableCollection<JobAlarmRow> JobAlarmList { get; } = [];

    /// <summary>日志表。</summary>
    public ObservableCollection<LogItem> LogItemCollection { get; } = [];

    /// <summary>日志等级筛选下拉项。</summary>
    public string[] LogLevels { get; } = ["all", "information", "warning", "error", "fatal", "debug"];

    [ObservableProperty]
    private string _levelFilter = "all";

    [ObservableProperty]
    private string _secretWords = string.Empty;

    [ObservableProperty]
    private LogItem? _selectedLogItem;

    /// <summary>原版右键菜单（本地实现暂无菜单项）。</summary>
    public object? RightClickMenu => null;

    [RelayCommand]
    private void OpenGrid(object? parameter)
    {
        var id = parameter is int i ? i : int.TryParse(parameter?.ToString(), out var parsed) ? parsed : 0;
        HistoryVisible = id == 0 ? Visibility.Visible : Visibility.Collapsed;
        LogVisible = id == 1 ? Visibility.Visible : Visibility.Collapsed;
        ErrorVisible = id == 2 ? Visibility.Visible : Visibility.Collapsed;
        for (var idx = 0; idx < MenuNodeList.Count; idx++)
        {
            MenuNodeList[idx] = MenuNodeList[idx] with { IsSelect = MenuNodeList[idx].Id == id };
        }
    }

    /// <summary>双击历史记录（原版跳转结果详情页，此处先记日志）。</summary>
    [RelayCommand]
    private Task LotStatisticsDataMouseDoubleAsync(object? parameter)
        => Task.CompletedTask;

    /// <summary>双击异常记录（原版跳转报警处理页，此处先记日志）。</summary>
    [RelayCommand]
    private void AlarmMouseDouble(object? parameter)
    {
    }

    /// <summary>日志查询（按等级与关键字过滤）。</summary>
    [RelayCommand]
    private void ApplyFilter()
    {
        // 内存演示数据已全量展示，真实日志服务接入后按 LevelFilter/SecretWords 过滤。
    }

    /// <summary>原版保留的过滤事件命令。</summary>
    [RelayCommand]
    private void FilterLog(object? parameter)
    {
    }

    /// <summary>视图尺寸变化（原版用于列宽自适应，本地暂无处理）。</summary>
    [RelayCommand]
    private void ControlSizeChanged(object? parameter)
    {
    }

    private void SeedDemoData()
    {
        OverlayJobHistoryData.Add(new LotStatisticsRow
        {
            DataType = "OVL",
            LotID = "LOT-DEMO-001",
            RecipeName = "OVL_DEMO",
            XMean = 0.0012,
            YMean = -0.0008,
            X3Sigma = 0.0041,
            Y3Sigma = 0.0039,
            XMin = -0.0091,
            YMin = -0.0088,
            XMax = 0.0102,
            YMax = 0.0095,
            Time = DateTime.Now.AddMinutes(-8),
        });
        OverlayJobHistoryData.Add(new LotStatisticsRow
        {
            DataType = "OVL",
            LotID = "LOT-DEMO-000",
            RecipeName = "OVL_DEMO",
            XMean = 0.0009,
            YMean = -0.0011,
            X3Sigma = 0.0038,
            Y3Sigma = 0.0042,
            XMin = -0.0085,
            YMin = -0.0092,
            XMax = 0.0097,
            YMax = 0.0088,
            Time = DateTime.Now.AddMinutes(-42),
        });

        JobAlarmList.Add(new JobAlarmRow(1, DateTime.Now.AddMinutes(-3), "Sample: EFEM 演示异常信息（无硬件桩数据）", "UnProcessed"));

        LogItemCollection.Add(new LogItem(DateTime.Now.AddMinutes(-1), "information", "Main", "系统启动完成（演示数据）"));
        LogItemCollection.Add(new LogItem(DateTime.Now.AddMinutes(-2), "warning", "EAP", "EAP 未连接，使用本地模式"));
        LogItemCollection.Add(new LogItem(DateTime.Now.AddMinutes(-4), "error", "Motion", "轴 Z1 未回零（演示数据）"));
    }
}
