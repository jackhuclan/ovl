using Microsoft.Extensions.DependencyInjection;
using Ovl.UI.ViewModels.Measure;
using Ovl.UI.ViewModels.Navigation;
using Ovl.UI.ViewModels.Placeholder;

namespace Ovl.UI.Services;

/// <summary>
/// 轻量导航服务实现：按注册键从 DI 容器解析页面 ViewModel 并切换，
/// 不使用弹窗；未来迁入真实功能页时在注册表中把 VM 类型替换即可。
/// </summary>
public sealed class NavigationService(IServiceProvider services) : INavigationService
{
    /// <summary>导航键 → 页面 VM 类型与标题。与 ShellViewModel 的菜单键保持一致。</summary>
    private static readonly Dictionary<string, (Type VmType, string Title)> Registry =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Measure"] = (typeof(HomeViewModel), "Home"),
            ["StageControl"] = (typeof(PlaceholderViewModel), "Stage Control"),
            ["WaferHandler"] = (typeof(PlaceholderViewModel), "Wafer Handler"),
            ["Diagnostics"] = (typeof(PlaceholderViewModel), "Diagnostics"),
            ["Batch"] = (typeof(PlaceholderViewModel), "Batch"),
            ["Recipe"] = (typeof(PlaceholderViewModel), "Recipe"),
            ["Result"] = (typeof(PlaceholderViewModel), "Result"),
            ["Setting"] = (typeof(PlaceholderViewModel), "Setting"),
            ["Help"] = (typeof(PlaceholderViewModel), "Help"),
            ["Motion"] = (typeof(PlaceholderViewModel), "Motion"),
            ["Defect"] = (typeof(PlaceholderViewModel), "Defect"),
            ["Alarm"] = (typeof(PlaceholderViewModel), "Alarm"),
        };

    public object? Current { get; private set; }

    public event EventHandler? CurrentChanged;

    public void NavigateTo(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!Registry.TryGetValue(key, out var entry))
        {
            throw new ArgumentException($"未注册的导航键：{key}", nameof(key));
        }

        var viewModel = services.GetRequiredService(entry.VmType);
        if (viewModel is IPageViewModel page)
        {
            page.Title = entry.Title;
        }

        Current = viewModel;
        CurrentChanged?.Invoke(this, EventArgs.Empty);
    }
}
