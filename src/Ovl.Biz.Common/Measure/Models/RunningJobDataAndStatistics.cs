using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ovl.Biz.Common.Measure.Models;

/// <summary>
/// 当前运行作业的数据与统计集合。对应原版 Marvel.Main.Models.RunningJobDataAndStatistics：
/// 每个数据表都有 “是否选中（左侧页签）” 与 “是否可见（内容区显示）” 两个开关。
/// </summary>
public sealed partial class RunningJobDataAndStatistics : ObservableObject
{
    /// <summary>Raw Data 表。</summary>
    public ObservableCollection<RawDataRow> RawData { get; } = [];

    /// <summary>OVL 数据表。</summary>
    public ObservableCollection<OvlDataRow> OVLData { get; } = [];

    /// <summary>TIS 原始数据表。</summary>
    public ObservableCollection<TisRawDataRow> TISRawData { get; } = [];

    /// <summary>异常数据表。</summary>
    public ObservableCollection<ErrorDataRow> ErrorData { get; } = [];

    /// <summary>BFCD 结果统计表。</summary>
    public ObservableCollection<BfcdResultRow> BFCDResultsStatistics { get; } = [];

    /// <summary>Lot 级 Misreg 统计。</summary>
    public ObservableCollection<LotStatisticsRow> LotStatistics_Misreg { get; } = [];

    /// <summary>Lot 级 TIS 统计。</summary>
    public ObservableCollection<LotStatisticsRow> LotStatistics_TIS { get; } = [];

    /// <summary>Wafer 级统计。</summary>
    public ObservableCollection<WaferStatisticsRow> WaferStatistics { get; } = [];

    [ObservableProperty]
    private bool _isSelectedRawData;

    [ObservableProperty]
    private bool _isVisibleRawData;

    [ObservableProperty]
    private bool _isSelectedOVLData;

    [ObservableProperty]
    private bool _isVisibleOVLData;

    [ObservableProperty]
    private bool _isSelectedTISRawData;

    [ObservableProperty]
    private bool _isVisibleTISRawData;

    [ObservableProperty]
    private bool _isSelectedErrorData;

    [ObservableProperty]
    private bool _isVisibleErrorData;

    [ObservableProperty]
    private bool _isSelectedBFCDResultsStatistics;

    [ObservableProperty]
    private bool _isVisibleBFCDResultsStatistics;

    [ObservableProperty]
    private bool _isSelectedLotStatistics_Misreg;

    [ObservableProperty]
    private bool _isVisibleLotStatistics_Misreg;

    [ObservableProperty]
    private bool _isSelectedLotStatistics_TIS;

    [ObservableProperty]
    private bool _isVisibleLotStatistics_TIS;

    [ObservableProperty]
    private bool _isSelectedWaferStatistics;

    [ObservableProperty]
    private bool _isVisibleWaferStatistics;
}
