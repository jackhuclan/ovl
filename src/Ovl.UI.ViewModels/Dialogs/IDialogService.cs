namespace Ovl.UI.ViewModels.Dialogs;

/// <summary>
/// 消息提示对话框服务。Sidebar 页面切换一律内嵌展示；
/// 仅确认 / 警告 / 错误三类消息提示保留弹窗。
/// </summary>
public interface IDialogService
{
    /// <summary>确认对话框（确定 / 取消）。返回用户是否点了确定。</summary>
    bool Confirm(string message, string caption = "确认");

    /// <summary>警告提示。</summary>
    void Warn(string message, string caption = "警告");

    /// <summary>错误提示。</summary>
    void Error(string message, string caption = "错误");
}
