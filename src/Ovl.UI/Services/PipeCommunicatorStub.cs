using InteVega.CBB.DomainService.Api.Interfaces;

namespace Ovl.UI.Services;

/// <summary>
/// <see cref="IPipeCommunicatorService"/> 的空实现（骨架版）。
/// <para>
/// 命名管道用于与 EAP/外部进程通信，真机现场由 <c>PipeClient</c> 承担。
/// 当前阶段无 EAP 对端，桩实现只维护连接状态位，不建立真实管道，避免启动期阻塞或抛错。
/// </para>
/// </summary>
public sealed class PipeCommunicatorStub : IPipeCommunicatorService
{
    public event Action<bool>? ConnectionStatusChanged;

    public event Action<string>? MessageReceived;

    public event Action<string>? StatusUpdated;

    public bool IsConnected { get; private set; }

    public Task StartAsync()
    {
        IsConnected = true;
        ConnectionStatusChanged?.Invoke(true);
        StatusUpdated?.Invoke("Pipe communicator stub started (no physical pipe).");
        return Task.CompletedTask;
    }

    public Task<bool> SendMessageAsync(string message) => Task.FromResult(false);

    public Task DisconnectAsync()
    {
        IsConnected = false;
        ConnectionStatusChanged?.Invoke(false);
        return Task.CompletedTask;
    }

    public void Stop() => IsConnected = false;

    public void Dispose() => Stop();
}
