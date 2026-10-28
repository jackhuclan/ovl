namespace Ovl.Biz.Common.Enums;

/// <summary>
/// 作业（Job）状态。与遗留系统 Marvel.DataModels.Enums.EnumJobStates 成员保持一致，
/// XAML 中 DataTrigger 直接以成员名作为字符串比较值（Running / Waiting）。
/// </summary>
public enum EnumJobStates
{
    /// <summary>空闲/正常，可上料。</summary>
    Normal,

    /// <summary>排队等待。</summary>
    Waiting,

    /// <summary>运行中。</summary>
    Running,

    /// <summary>暂停。</summary>
    Paused,

    /// <summary>已完成。</summary>
    Completed,

    /// <summary>失败。</summary>
    Failed,
}
