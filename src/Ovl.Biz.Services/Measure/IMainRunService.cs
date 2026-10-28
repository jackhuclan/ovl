namespace Ovl.Biz.Services.Measure;

/// <summary>运行状态（对应原 EnumRunState 的骨架子集）。</summary>
public enum MeasureRunState
{
    /// <summary>空闲。</summary>
    Idle,

    /// <summary>运行中。</summary>
    Running,

    /// <summary>暂停。</summary>
    Paused,

    /// <summary>错误。</summary>
    Error,
}

/// <summary>一次状态变化的通知参数。</summary>
public sealed record MeasureRunStateChangedArgs(MeasureRunState State, string Message);

/// <summary>
/// 主量测运行线程的适配接口（对应原 IMainRunThread，骨架版仅状态机与进度推进）。
/// 真实实现将在 M3b/M4 接入量测流程；桩实现 <see cref="MeasureRunStub"/> 供无硬件运行。
/// </summary>
public interface IMainRunService
{
    /// <summary>当前运行状态。</summary>
    MeasureRunState State { get; }

    /// <summary>状态变化事件。</summary>
    event EventHandler<MeasureRunStateChangedArgs>? StateChanged;

    /// <summary>晶圆进度 0~100。</summary>
    double WaferProgressValue { get; }

    /// <summary>目标点进度 0~100。</summary>
    double TargetProgressValue { get; }

    /// <summary>进度推进事件（节流由实现方保证）。</summary>
    event EventHandler? ProgressChanged;

    /// <summary>开始运行（Idle 时有效）。</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>暂停（Running 时有效）。</summary>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>恢复（Paused 时有效）。</summary>
    Task ResumeAsync(CancellationToken cancellationToken = default);

    /// <summary>停止并回到 Idle（Running/Paused 时有效）。</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>作业队列条目（对应原 JobInfoModel 骨架子集）。</summary>
public sealed record MeasureJobInfo(string JobName, string RecipeName, string Status, double Progress);

/// <summary>
/// 配方管理适配接口（对应原 IRecipeManager，骨架版仅当前配方查询）。
/// </summary>
public interface IRecipeManager
{
    /// <summary>当前加载的配方名（无则空串）。</summary>
    string CurrentRecipeName { get; }

    /// <summary>加载配方（骨架版仅记录名称）。</summary>
    Task LoadAsync(string recipeName, CancellationToken cancellationToken = default);
}
