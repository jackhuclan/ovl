using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace Ovl.UI.Services;

/// <summary>
/// Prism 对话框服务的适配器：仅用于满足 vendor ViewModel（如 <c>WaferHandlerViewModelBase</c>）
/// 对 <see cref="IDialogService"/> 的构造依赖。
/// <para>
/// 新架构的功能页一律内嵌在 Shell 内容区，弹窗走 <c>Ovl.UI.Dialogs.IDialogService</c>，
/// 因此这里不实际弹出窗口：记录一条警告后以空结果回调，保证调用方不悬挂。
/// 若后续确有 Prism 对话框需要支持，再在此接入真正实现。
/// </para>
/// </summary>
public sealed class PrismDialogServiceAdapter(ILogger<PrismDialogServiceAdapter>? logger = null) : IDialogService
{
    public void ShowDialog(string name, IDialogParameters parameters, Action<IDialogResult> callback)
        => NotifyPending(name);

    public void ShowDialog(string name, IDialogParameters parameters, Action<IDialogResult> callback, string windowName)
        => NotifyPending(name);

    public void Show(string name, IDialogParameters parameters, Action<IDialogResult> callback)
        => NotifyPending(name);

    public void Show(string name, IDialogParameters parameters, Action<IDialogResult> callback, string windowName)
        => NotifyPending(name);

    private void NotifyPending(string name)
        => logger?.LogWarning("Prism 对话框 '{Name}' 未接入（功能页已改为内嵌展示），本次调用被忽略。", name);
}
