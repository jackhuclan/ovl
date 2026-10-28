using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ovl.Biz.Services;
using Ovl.UI.Services;
using Ovl.UI.ViewModels.Dialogs;
using Ovl.UI.ViewModels.Navigation;
using Ovl.UI.ViewModels.Placeholder;
using Ovl.UI.ViewModels.Shell;
using Ovl.UI.Views.Shell;

namespace Ovl.UI;

/// <summary>
/// 应用启动壳：仅做 Host 构建与 DI 装配，禁止业务逻辑。
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                // 配置与设备（conf/ 目录 + EquipmentFeaturesOptions 开关，特征实例当前由测试侧 Mock 提供）
                services.AddConfManagement(context.Configuration);
                services.AddEquipment("OM300", context.Configuration);

                // UI 基础设施
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<ShellViewModel>();
                services.AddTransient<PlaceholderViewModel>();
            })
            .Build();
        _host.Start();

        var shell = new ShellWindow
        {
            DataContext = _host.Services.GetRequiredService<ShellViewModel>(),
        };
        MainWindow = shell;
        shell.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
