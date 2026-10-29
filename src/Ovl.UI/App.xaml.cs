using System.Windows;
using System.IO;
using System.Reflection;
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

    static App()
    {
        // vendor 程序集兜底加载：部分 marvel 工程同时以 NuGet 包和工程引用的形式出现，
        // NuGet 会把工程引用标记为“外部已解析”而丢弃 deps.json 条目，
        // 运行时报 FileNotFoundException（DLL 明明就在输出目录）。此处按简单名从输出目录兜底。
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) => ResolveFromBaseDirectory(args);
    }

    private static Assembly? ResolveFromBaseDirectory(ResolveEventArgs args)
    {
        try
        {
            var simpleName = new AssemblyName(args.Name).Name;
            if (string.IsNullOrEmpty(simpleName))
            {
                return null;
            }

            var path = Path.Combine(AppContext.BaseDirectory, simpleName + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }
        catch
        {
            return null;
        }
    }

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

                // 视觉服务管理器：Defect 等业务页 VM 构造时强制依赖（GetMainVision），无此注册即 NRE
                services.AddSingleton<InteVega.CBB.DomainService.Api.Interfaces.IVisionServiceManager,
                    InteVega.CBB.DomainLib.Services.VisionServiceManager>();
                // 无硬件环境的 vendor 服务占位：让业务页 VM 构造函数可完成（真机实现属 RealDevice 阶段）
                services.AddSingleton(_ => Ovl.UI.Services.Stubs.NullServiceProxy.Create<Marvel.Interfaces.Application.IWaferService>());
                services.AddSingleton(_ => Ovl.UI.Services.Stubs.NullServiceProxy.Create<Marvel.Interfaces.Application.IAlignmentService>());
                services.AddSingleton(_ => Ovl.UI.Services.Stubs.NullServiceProxy.Create<Marvel.Interfaces.Application.IDefectDataService>());
                services.AddSingleton(_ => Ovl.UI.Services.Stubs.NullServiceProxy.Create<Megarobo.Ovl.Service.IntfFn.fn.IMotionControllerService>());
                services.AddSingleton(_ => Ovl.UI.Services.Stubs.NullServiceProxy.Create<Megarobo.Ovl.Service.IntfFn.IServiceCalData>());
                services.AddSingleton(_ => Ovl.UI.Services.Stubs.NullServiceProxy.Create<InteVega.CBB.DomainService.Api.Interfaces.IFocusService>());
                services.AddSingleton(_ => Ovl.UI.Services.Stubs.NullServiceProxy.Create<InteVega.CBB.DomainService.Api.Interfaces.IAsyncAblePztService>());
                // Defect 缺陷检测页（原 marvel Defect/View/DefectDetectView）
                services.AddTransient<Ovl.UI.Defect.ViewModel.DefectDetectViewModel>();
            })
            .Build();
        _host.Start();
        AppHost.Services = _host.Services;

        // vendor UI 层（StageControlModule、MotionController.Ui 等）的 VM 基类在构造函数里通过
        // Prism 的 ContainerLocator.Current.Resolve<IServiceProvider>() 反查服务。这里把宿主容器桥接给
        // Prism 的定位器，使这些未改造的 vendor 视图也能正常构造（不改 vendor 源码）。
        var containerExtension = new Prism.DryIoc.DryIocContainerExtension();
        containerExtension.RegisterInstance(typeof(IServiceProvider), _host.Services);
        Prism.Ioc.ContainerLocator.SetContainerExtension(() => containerExtension);

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
