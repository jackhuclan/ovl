using System.ComponentModel;

namespace Ovl.Biz.Common.Enums;

/// <summary>
/// 运行状态。原样移植自 Marvel.DataModels.Enums.EnumRunState（成员名与 Description 不变，
/// 以保证原版 XAML 中基于该枚举的样式触发器与显示文本完全一致）。
/// </summary>
public enum EnumRunState
{
    /// <summary>初始化状态</summary>
    [Description("Init")]
    INIT,

    /// <summary>空闲状态</summary>
    [Description("Idle")]
    IDLE,

    /// <summary>空闲中报警状态</summary>
    [Description("Idle with alarm")]
    IDLE_WITH_ALARM,

    /// <summary>运行状态</summary>
    [Description("Running")]
    Running,

    /// <summary>流程状态</summary>
    [Description("Process")]
    PROCESS,

    /// <summary>扫描Cassette，当前批队列进入预处理状态直到第一块晶圆可获取</summary>
    [Description("Setup")]
    SETUP,

    /// <summary>当前批处于Active状态，传动装置来料盒取晶圆</summary>
    [Description("Ready")]
    READY,

    /// <summary>上料状态</summary>
    [Description("Load")]
    LOAD,

    /// <summary>量测状态</summary>
    [Description("Working")]
    WORKING,

    /// <summary>下料状态</summary>
    [Description("Unload")]
    UNLOAD,

    /// <summary>停止中状态</summary>
    [Description("Stoping")]
    STOPPING,

    /// <summary>暂停状态</summary>
    [Description("Paused")]
    PAUSED,

    /// <summary>流程暂停状态</summary>
    [Description("Process paused")]
    PROCESS_PAUSED,

    /// <summary>流程中因为报警暂停状态</summary>
    [Description("Alarm paused")]
    ALARM_PAUSED,

    /// <summary>更换料盒</summary>
    [Description("Replace cassette")]
    REPLACE_CASSETTE,

    /// <summary>调试中：设备调试、配方设置、标定、系统设置、手动上下料等</summary>
    [Description("Debug")]
    DEBUGGING_MANUAL,

    /// <summary>急停</summary>
    [Description("EMERGENCY_STOP")]
    EMERGENCY_STOP,
}

/// <summary>
/// 状态机事件。原样移植自 Marvel.DataModels.Enums.Event。
/// </summary>
public enum MesEvent
{
    InitializationComplete,
    StartProduction,
    CassetteScanned,
    LoadWafer,
    WaferLoaded,
    WaferProcessingComplete,
    WaferUnloaded,
    BatchComplete,
    StopComplete,
    AlarmDetected,
    ClearAlarm,
    Pause,
    Resume,
    Stop,
}
