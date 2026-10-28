using System.Windows;
using Ovl.UI.ViewModels.Dialogs;

namespace Ovl.UI.Services;

/// <summary>
/// 消息提示对话框服务实现（仅确认 / 警告 / 错误三类保留弹窗）。
/// </summary>
public sealed class DialogService : IDialogService
{
    private static Window OwnerWindow =>
        Application.Current?.MainWindow
            ?? (Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0] : null)
        ?? throw new InvalidOperationException("当前没有可用的宿主窗口。");

    public bool Confirm(string message, string caption = "确认")
    {
        return MessageBox.Show(OwnerWindow, message, caption, MessageBoxButton.OKCancel, MessageBoxImage.Question)
            == MessageBoxResult.OK;
    }

    public void Warn(string message, string caption = "警告")
    {
        MessageBox.Show(OwnerWindow, message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void Error(string message, string caption = "错误")
    {
        MessageBox.Show(OwnerWindow, message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
