using CommunityToolkit.Mvvm.ComponentModel;
using Ovl.Biz.Common.Enums;

namespace Ovl.Biz.Common.Measure.Models;

/// <summary>
/// 作业队列条目。字段与原版 Marvel.App.Shared.model.JobInfoModel 及
/// Marvel.DataModels.Job.JobItem 对齐，XAML 绑定路径保持不变。
/// </summary>
public sealed partial class JobInfoModel : ObservableObject
{
    private bool _isSelectJob;

    /// <summary>是否被勾选（批量删除用）。</summary>
    public bool IsSelectJob
    {
        get => _isSelectJob;
        set => SetProperty(ref _isSelectJob, value);
    }

    /// <summary>料盒 Lot 号。</summary>
    public string LotId { get; set; } = string.Empty;

    /// <summary>配方名。</summary>
    public string RecipeName { get; set; } = string.Empty;

    /// <summary>上料口索引（0 基）。</summary>
    public int PortIndex { get; set; }

    /// <summary>上料口显示名。</summary>
    public string Port { get; set; } = string.Empty;

    /// <summary>作业状态。</summary>
    public EnumJobStates State { get; set; } = EnumJobStates.Waiting;

    /// <summary>晶圆迭代次数。</summary>
    public int Iterations { get; set; }

    /// <summary>晶圆测点迭代次数。</summary>
    public int WaferIterations { get; set; }

    /// <summary>是否允许上料盒（原版语义：State == Normal）。</summary>
    public bool LoadPortEnable => State == EnumJobStates.Normal;

    /// <summary>是否允许下料盒（原版语义：State == Waiting）。</summary>
    public bool UnLoadPortEnable => State == EnumJobStates.Waiting;

    /// <summary>是否允许编辑（非运行中、非已完成）。</summary>
    public bool EditEnable => State is not EnumJobStates.Running and not EnumJobStates.Completed;
}
