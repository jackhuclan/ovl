namespace Ovl.UI.ViewModels.Navigation;

/// <summary>
/// 轻量导航服务：Sidebar 点击后以**内嵌方式**在右侧内容区切换页面（不弹窗）。
/// 仅确认 / 警告 / 错误类消息提示保留弹窗（见 IDialogService）。
/// </summary>
public interface INavigationService
{
    /// <summary>当前展示的页面 ViewModel。</summary>
    object? Current { get; }

    /// <summary>当前页面发生变化时触发。</summary>
    event EventHandler? CurrentChanged;

    /// <summary>按注册键切换到对应页面。</summary>
    void NavigateTo(string key);
}

/// <summary>页面 ViewModel 约定：由导航服务写入标题。</summary>
public interface IPageViewModel
{
    /// <summary>页面标题（由导航服务按注册信息写入）。</summary>
    string Title { get; set; }
}
