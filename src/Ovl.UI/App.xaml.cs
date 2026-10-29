using System.Windows;
using IntellVega.CBB.EasyConfig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ovl.Biz.Services;
using Ovl.UI.Services;
using Ovl.UI.Views.Shell;

using Ovl.UI.DataDisplay.ViewModel;
using Ovl.UI.Dialogs;
using Ovl.UI.Measure;
using Ovl.UI.Navigation;
using Ovl.UI.Placeholder;
using Ovl.UI.Recipe;
using Ovl.UI.Setting;
using Ovl.UI.Shell;
using Ovl.UI.Tools;
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
                // EasyConfig 配置集：vendor 域（DomainService.Api 的 ConfigHelper.PrepareConfig）依赖
                services.AddEasyConfig();
                // SystemConfig 体系：vendor 域 IConfigLoader / ISystemConfigService（等价于 SystemConfigModule.Register）
                services.AddSingleton<InteVega.CBB.SystemConfig.Interface.ISystemConfigCollection, InteVega.CBB.SystemConfig.SystemConfigCollection>();
                services.AddSingleton<InteVega.CBB.SystemConfig.Interface.ISystemConfigService, InteVega.CBB.SystemConfig.SystemConfigService>();
                services.AddSingleton<InteVega.CBB.SystemConfig.Interface.IConfigLoader, InteVega.CBB.SystemConfig.ConfigLoader>();
                services.AddEquipment("OM300", context.Configuration);
                services.AddMeasureStubs();
                // 主运行线程桩：真机运行线程（MainRunThread_100/300/40）待 RealDevice 硬件工厂阶段接入
                services.AddSingleton<Marvel.Interfaces.Application.IMainRunThread, MainRunThreadStub>();
                services.AddSingleton<InteVega.CBB.DomainService.Api.Interfaces.IPipeCommunicatorService, PipeCommunicatorStub>();
                services.AddSingleton<InteVega.CBB.Prism.UI.Extensions.MessageBox.IMessageBoxService, MessageBoxService>();

                // UI 基础设施
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<ShellViewModel>();
                services.AddTransient<PlaceholderViewModel>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<BasicDataViewModel>();
                services.AddSingleton<RecipeViewModel>();
                services.AddSingleton<SettingViewModel>();
                services.AddSingleton<AlarmViewModel>();
                services.AddSingleton<ResultViewModel>();
                services.AddSingleton<HelpViewModel>();
            })
            .Build();
        _host.Start();
        AppHost.Services = _host.Services;

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
