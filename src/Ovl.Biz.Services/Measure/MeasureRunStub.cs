using Microsoft.Extensions.Logging;

namespace Ovl.Biz.Services.Measure;

/// <summary>
/// <see cref="IMainRunService"/> 的桩实现：无硬件环境下模拟量测运行——
/// Start 后按固定节拍推进 Wafer/Target 进度并推送事件，Pause/Resume/Stop 状态机齐备，
/// 用于 Home 页数据绑定演示与单元测试。
/// </summary>
public sealed class MeasureRunStub : IMainRunService, IAsyncDisposable
{
    private readonly ILogger<MeasureRunStub>? _logger;
    private readonly int _stepCount;
    private readonly int _stepDelayMs;
    private CancellationTokenSource? _cts;
    private Task? _runLoop;
    private bool _disposed;

    public MeasureRunStub(ILogger<MeasureRunStub>? logger = null, int stepCount = 10, int stepDelayMs = 200)
    {
        _logger = logger;
        _stepCount = stepCount > 0 ? stepCount : 10;
        _stepDelayMs = stepDelayMs >= 0 ? stepDelayMs : 200;
    }

    public MeasureRunState State { get; private set; } = MeasureRunState.Idle;

    public double WaferProgressValue { get; private set; }

    public double TargetProgressValue { get; private set; }

    public event EventHandler<MeasureRunStateChangedArgs>? StateChanged;

    public event EventHandler? ProgressChanged;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (State is not (MeasureRunState.Idle or MeasureRunState.Error))
        {
            throw new InvalidOperationException($"当前状态 {State} 不允许 Start。");
        }

        WaferProgressValue = 0;
        TargetProgressValue = 0;
        SetState(MeasureRunState.Running, "开始运行");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runLoop = Task.Run(() => RunLoopAsync(_cts.Token), CancellationToken.None);
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
    }

    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (State != MeasureRunState.Running)
        {
            throw new InvalidOperationException($"当前状态 {State} 不允许 Pause。");
        }

        SetState(MeasureRunState.Paused, "已暂停");
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
    }

    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (State != MeasureRunState.Paused)
        {
            throw new InvalidOperationException($"当前状态 {State} 不允许 Resume。");
        }

        SetState(MeasureRunState.Running, "已恢复");
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (State is not (MeasureRunState.Running or MeasureRunState.Paused))
        {
            throw new InvalidOperationException($"当前状态 {State} 不允许 Stop。");
        }

        _cts?.Cancel();
        if (_runLoop is not null)
        {
            try
            {
                await _runLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
        }

        SetState(MeasureRunState.Idle, "已停止");
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        return ValueTask.CompletedTask;
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        for (var step = 1; step <= _stepCount; step++)
        {
            token.ThrowIfCancellationRequested();
            if (State == MeasureRunState.Paused)
            {
                // 暂停：等待恢复（轮询节拍）
                do
                {
                    await Task.Delay(_stepDelayMs, token).ConfigureAwait(false);
                }
                while (State == MeasureRunState.Paused);
            }

            await Task.Delay(_stepDelayMs, token).ConfigureAwait(false);
            WaferProgressValue = Math.Min(100.0, WaferProgressValue + 100.0 / _stepCount);
            TargetProgressValue = Math.Min(100.0, TargetProgressValue + 100.0 / _stepCount);
            ProgressChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SetState(MeasureRunState state, string message)
    {
        State = state;
        _logger?.LogInformation("量测运行状态：{State}（{Message}）", state, message);
        StateChanged?.Invoke(this, new MeasureRunStateChangedArgs(state, message));
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
