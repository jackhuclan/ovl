namespace Ovl.Biz.Services.Eap;

/// <summary>EAP 连接状态。</summary>
public enum EapConnectionState
{
    Disconnected,
    Connecting,
    Connected,
}

/// <summary>EAP 下发的运行命令。</summary>
public enum EapRunCommand
{
    Start,
    Pause,
    Resume,
    Stop,
}

/// <summary>EAP 选定的配方信息。</summary>
public sealed record EapRecipeInfo(string RecipeId, string Name, string Version);

/// <summary>
/// EAP 控制服务接口。原 Marvel 项目为 IEapControlService（Marvel.Application）。
/// EAP 无对应服务端时，由测试/运行环境注入 Mock 实现。
/// </summary>
public interface IEapControlService
{
    /// <summary>当前连接状态。</summary>
    EapConnectionState State { get; }

    /// <summary>连接状态变化通知。</summary>
    event EventHandler<EapConnectionState>? StateChanged;

    /// <summary>连接 EAP 服务端。</summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>断开 EAP 连接。</summary>
    Task DisconnectAsync();

    /// <summary>下发运行命令，返回是否被接受。</summary>
    Task<bool> SendRunCommandAsync(EapRunCommand command, CancellationToken cancellationToken = default);

    /// <summary>获取 EAP 选定的配方；未选择时返回 null。</summary>
    Task<EapRecipeInfo?> GetSelectedRecipeAsync(CancellationToken cancellationToken = default);
}
