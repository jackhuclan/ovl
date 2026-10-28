using Microsoft.Extensions.DependencyInjection;

namespace Ovl.UI;

/// <summary>
/// 应用级服务定位入口（仅限 View 代码后置解析由 XAML 无法注入的页面级 VM；
/// 业务代码一律走构造函数注入，禁止直接使用本类）。
/// </summary>
internal static class AppHost
{
    /// <summary>Host 构建完成后的根服务提供者。</summary>
    public static IServiceProvider? Services { get; set; }
}
