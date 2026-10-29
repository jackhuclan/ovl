using System.Windows;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Prism.Services.Dialogs;

namespace Ovl.UI.Services;

/// <summary>
/// <see cref="IMessageBoxService"/> 的 WPF 实现。
/// <para>
/// vendor 侧同名实现（InteVega.CBB.Prism.UI.Extensions.MessageBox.MessageBoxService）为 internal，
/// 且依赖 Prism 区域宿主；当前壳层使用 MS DI，故在此提供等价实现。
/// </para>
/// </summary>
public sealed class MessageBoxService : IMessageBoxService
{
    public void Info(string message, string token = "MainMessage") => Show(message, "Info", MessageBoxImage.Information);

    public void Success(string message, string token = "MainMessage") => Show(message, "Success", MessageBoxImage.Information);

    public void Warning(string message, string token = "MainMessage") => Show(message, "Warning", MessageBoxImage.Warning);

    public void Error(string message, string token = "MainMessage") => Show(message, "Error", MessageBoxImage.Error);

    public void InfoDialog(string message, string caption = "Info", string messageDetail = "") => Show(Compose(message, messageDetail), caption, MessageBoxImage.Information);

    public void InfoDialogNonModal(string message, string caption = "Info", string messageDetail = "") => Show(Compose(message, messageDetail), caption, MessageBoxImage.Information);

    public void SuccessDialog(string message, string caption = "Success", string messageDetail = "") => Show(Compose(message, messageDetail), caption, MessageBoxImage.Information);

    public void WarningDialog(string message, string caption = "Warning", string messageDetail = "") => Show(Compose(message, messageDetail), caption, MessageBoxImage.Warning);

    public ButtonResult WarningAskDialog(string message, string caption = "Warning", string messageDetail = "")
        => Ask(Compose(message, messageDetail), caption, MessageBoxImage.Warning);

    public void ErrorDialog(string message, string caption = "Error", string messageDetail = "") => Show(Compose(message, messageDetail), caption, MessageBoxImage.Error);

    public ButtonResult AskDialog(string message, string caption = "Confirm", string messageDetail = "")
        => Ask(Compose(message, messageDetail), caption, MessageBoxImage.Question);

    public ButtonResult YesNoCancelDialog(string message, string caption = "Confirm", string messageDetail = "")
    {
        var result = MessageBox.Show(Compose(message, messageDetail), caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return result switch
        {
            MessageBoxResult.Yes => ButtonResult.Yes,
            MessageBoxResult.No => ButtonResult.No,
            _ => ButtonResult.Cancel,
        };
    }

    private static string Compose(string message, string messageDetail)
        => string.IsNullOrWhiteSpace(messageDetail) ? message : $"{message}{Environment.NewLine}{Environment.NewLine}{messageDetail}";

    private static void Show(string message, string caption, MessageBoxImage icon)
        => MessageBox.Show(message, caption, MessageBoxButton.OK, icon);

    private static ButtonResult Ask(string message, string caption, MessageBoxImage icon)
        => MessageBox.Show(message, caption, MessageBoxButton.OKCancel, icon) == MessageBoxResult.OK
            ? ButtonResult.OK
            : ButtonResult.Cancel;
}
