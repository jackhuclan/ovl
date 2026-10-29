using System.IO;
using Microsoft.Extensions.DependencyInjection;

using Ovl.UI.Measure;
using Ovl.UI.Navigation;
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
            ["WaferHandler"] = (typeof(Ovl.UI.WaferHandler.ViewModel.WaferHandlerViewModel), "Wafer Handler"),
            ["Diagnostics"] = (typeof(PlaceholderViewModel), "Diagnostics"),
            ["Batch"] = (typeof(PlaceholderViewModel), "Batch"),
            ["Recipe"] = (typeof(Ovl.UI.Recipe.RecipeViewModel), "Recipe"),
            ["Result"] = (typeof(Ovl.UI.Tools.ResultViewModel), "Result"),
            ["Setting"] = (typeof(Ovl.UI.Setting.SettingViewModel), "Setting"),
            ["Help"] = (typeof(Ovl.UI.Tools.HelpViewModel), "Help"),
            ["Motion"] = (typeof(PlaceholderViewModel), "Motion"),
            ["Defect"] = (typeof(Ovl.UI.Defect.ViewModel.DefectDetectViewModel), "Defect"),
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

        try
        {
            var viewModel = services.GetRequiredService(entry.VmType);
            if (viewModel is IPageViewModel page)
            {
                page.Title = entry.Title;
            }

            Current = viewModel;
            CurrentChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            // 页面 VM 构造失败时 WPF 绑定会静默吞掉异常，页面停留原样很难排查，故落盘一份可诊断日志
            WriteNavigationError(key, entry.VmType, ex);

            // 回退到占位页：保证内容区切换到目标模块（并显性提示未完成），而不是停留在上一个页面
            var fallback = services.GetService<PlaceholderViewModel>();
            if (fallback is not null)
            {
                fallback.Title = $"{entry.Title}（迁移中）";
                Current = fallback;
                CurrentChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            throw;
        }
    }

    private static void WriteNavigationError(string key, Type vmType, Exception ex)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "ovl-nav-error.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] navigate '{key}' -> {vmType.FullName}{Environment.NewLine}{ex}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}");
        }
        catch
        {
            // 诊断日志失败不得影响导航流程
        }
    }
}
