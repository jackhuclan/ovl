using Microsoft.Extensions.DependencyInjection;

using Ovl.UI.Navigation;
using Ovl.UI.Measure;
using Ovl.UI.Placeholder;
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
            ["Measure"] = (typeof(MainViewModel), "Home"),
            ["StageControl"] = (typeof(PlaceholderViewModel), "Stage Control"),
            ["WaferHandler"] = (typeof(PlaceholderViewModel), "Wafer Handler"),
            ["Diagnostics"] = (typeof(PlaceholderViewModel), "Diagnostics"),
            ["Batch"] = (typeof(PlaceholderViewModel), "Batch"),
            ["Recipe"] = (typeof(Ovl.UI.Recipe.RecipeViewModel), "Recipe"),
            ["Result"] = (typeof(Ovl.UI.Tools.ResultViewModel), "Result"),
            ["Setting"] = (typeof(Ovl.UI.Setting.SettingViewModel), "Setting"),
            ["Help"] = (typeof(Ovl.UI.Tools.HelpViewModel), "Help"),
            ["Motion"] = (typeof(PlaceholderViewModel), "Motion"),
            ["Defect"] = (typeof(PlaceholderViewModel), "Defect"),
            ["Alarm"] = (typeof(Ovl.UI.Tools.AlarmViewModel), "Alarm"),
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
