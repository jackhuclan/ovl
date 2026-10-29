using InteVega.CBB.EfemApi.Positions;
using InteVega.CBB.Secs.Core.Model;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Job;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Type;

namespace Ovl.UI.Services;

/// <summary>
/// <see cref="IMainRunThread"/> 的空实现（骨架版）。
/// <para>
/// 真机运行线程（MainRunThread_100/300/40）依赖配方服务器、运动控制与相机等硬件服务，
/// 由后续 RealDevice 硬件工厂阶段接入。在此之前 UI 侧（BasicData/Defect/Alignment 等）
/// 仍会通过构造函数解析该接口，故此处提供不做任何实际动作的桩：
/// 仅保留事件挂载点与状态查询，保证界面可正常创建与显示。
/// </para>
/// </summary>
public sealed class MainRunThreadStub : IMainRunThread
{
    public int WaferCount => 0;

    public int WaferProcessedCount => 0;

    public JobItem? CurrentJob => null;

    public LoadPortInfo? CurrentWafer => null;

    public RecipeElementsInfo? CurrentRecipe => null;

    public TestInfoBase? CurrentTest => null;

    public int CurrentLotIter => 0;

    public EnumEapMode CurrentEapMode => EnumEapMode.Local;

    public IEnumerable<ReportEapData>? CurrentEapReportDatas { get; set; }

    public ReportEapData? CurrentEapProcessData { get; set; }

    public EnumStopMode StopMode { get; set; }

    public bool IsRunning => false;

    public bool IsStopping => false;

    public Action<JobItem>? ActionJobStarted { get; set; }

    public Action<JobItem>? ActionJobRemove { get; set; }

    public Action<JobItem>? ActionJobAdd { get; set; }

    public Action<int, int>? ActionWaferProgressChanged { get; set; }

    public Action<int, int>? ActionTargetProgressChanged { get; set; }

    public Action<MroProgressInfo>? ActionMroProgressChanged { get; set; }

    public Action<EnumMroState>? ActionMroRunningStateChanged { get; set; }

    public Action<RealTimeOnePointResult>? ActionTargetDataUpdated { get; set; }

    public Action<List<TargetOVLResult>>? ActionOVLUserExcelDataUpdated { get; set; }

    public Action? ActionClearTargetData { get; set; }

    public Action<bool>? ActionEapModeChanged { get; set; }

    public Action? ActionSubscribeRunActionInfo { get; set; }

    public Action<JobItem, RealTimeStatisticsResult>? ActionStatisticsDataUpdated { get; set; }

    public Action? InitJobList { get; set; }

    public void Build() { }

    public void ExcuteCoarseFocus(RecipeElementsInfo recipe, ECamera eCamera) { }

    public void FocusAlign(bool isAngle0, RecipeElementsInfo recipe, RunParameter runParameter) { }

    public void UpdateJobs(IEnumerable<JobItem> jobs) { }

    public void SetTestPointCount(int totalPointCount) { }

    public void OnActionTargetComplate(int currentTargetIndex, IEnumerable<TargetResultBase> targetResults, bool if180angle, TestInfoBase testinfo) { }

    public void EmergenceStop() { }

    public void ExceptionStop() { }

    public void Pause(bool isChangeState = true) { }

    public void Resume() { }

    public void StopRun(EnumStopMode mode) { }

    public void StopRun() { }

    public void ResetStatus() { }

    public bool IsTotalRunning() => false;

    public void GoEapRemote() { }

    public void GoEapLocal() { }

    public void StartRun() { }
}
