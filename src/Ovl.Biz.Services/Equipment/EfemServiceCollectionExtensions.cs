using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.EfemApi;
using InteVega.CBB.Hal;
using InteVega.CBB.MachineSystem.Interface;
using Megarobo.Ovl.Service.IntfFn.fn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ovl.Biz.Services.Equipment;

/// <summary>
/// EFEM（晶圆传输）相关服务注册入口。
/// <para>
/// 分两类：<br/>
/// 1）<b>已迁移到本层</b>：<see cref="EfemStorageManager"/>（料位/角度/晶圆位置缓存，纯业务逻辑，可持久化）；<br/>
/// 2）<b>真机服务模拟</b>：真空、晶圆在位、EFEM 传输等依赖设备驱动的接口，当前以模拟实现/占位代理注入，
/// 保证 Wafer Handler 页面在无硬件环境可完整渲染与操作，真机接入时改用 TryAdd 之前的正式实现替换。
/// </para>
/// </summary>
public static class EfemServiceCollectionExtensions
{
    /// <summary>
    /// 注册 EFEM 相关服务。缓存目录取配置键 <c>Equipment:Efem:CacheRoot</c>，
    /// 缺省为 <c>%ProgramData%\Megarobo\.ovl\cache</c>（与旧版一致）。
    /// </summary>
    public static IServiceCollection AddEfemServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var cacheRoot = configuration?["Equipment:Efem:CacheRoot"];

        // 已迁移：缓存管理器（业务逻辑，非硬件）
        services.TryAddSingleton<IEfemStorageManager>(sp =>
        {
            var manager = new EfemStorageManager(
                sp.GetService<Microsoft.Extensions.Logging.ILogger<EfemStorageManager>>(),
                cacheRoot);
            manager.InitializeCache();
            return manager;
        });

        // 模拟：晶圆在位状态（决定 Stage/Arm 料位显示与落片风险提示）
        services.TryAddSingleton<IWaferPresenceManager, SimulatedWaferPresenceManager>();

        // 模拟：真空检测（恒为已吸附）
        services.TryAddSingleton<IVacuumService, SimulatedVacuumService>();

        // 占位代理：EFEM 传输、EFEM 状态、设备上下文（真机由 RealDevice 阶段提供正式实现）
        // 覆写少数"界面直接展示"的成员，避免默认值让页面出现 None/空白
        services.TryAddSingleton(_ => DefaultServiceProxy.Create<IEfemService>(
            new Dictionary<string, Func<object?[]?, object?>>
            {
                [nameof(IEfemService.GetLoadPortSize)] = _ => WaferSize.INCH_12,
            }));
        services.TryAddSingleton(_ => DefaultServiceProxy.Create<ITransferService>());
        services.TryAddSingleton(_ => DefaultServiceProxy.Create<IDeviceContext<IEfem>>());

        return services;
    }
}
