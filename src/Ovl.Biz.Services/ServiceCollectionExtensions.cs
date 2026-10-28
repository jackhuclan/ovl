using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ovl.Biz.Common.Equipment;
using Ovl.Biz.Common.Equipment.Devices;
using Ovl.Biz.Services.Configuration;

namespace Ovl.Biz.Services;

/// <summary>
/// 基于 Microsoft.Extensions.DependencyInjection（IServiceCollection 标准方式）的服务注册入口。
/// 全系统 DI 统一使用该官方包，禁止引入其他第三方依赖注入框架。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 conf 配置管理：<see cref="IConfiguration"/>（单例）与 <see cref="ConfFileService"/>（单例）。
    /// conf 根目录取配置键 "Conf:Root"，缺省为当前目录下的 conf。
    /// </summary>
    public static IServiceCollection AddConfManagement(this IServiceCollection services, IConfiguration configuration, string? confRoot = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var root = confRoot ?? configuration["Conf:Root"] ?? Path.Combine(Directory.GetCurrentDirectory(), "conf");
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(sp => new ConfFileService(root, sp.GetService<ILogger<ConfFileService>>()));
        return services;
    }

    /// <summary>
    /// 按设备名注册 <see cref="IEquipment"/>（单例）：特征实例经 DI 解析，开关由配置节 Equipment 绑定。
    /// 设备名支持 OM100/OM300/OM600/OM40；未知名抛 <see cref="ArgumentException"/>。
    /// </summary>
    public static IServiceCollection AddEquipment(this IServiceCollection services, string deviceName, IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);

        var builder = deviceName.ToUpperInvariant() switch
        {
            OM100Equipment.DeviceName => OM100Equipment.CreateBuilder(),
            OM300Equipment.DeviceName => OM300Equipment.CreateBuilder(),
            OM600Equipment.DeviceName => OM600Equipment.CreateBuilder(),
            OM40Equipment.DeviceName => OM40Equipment.CreateBuilder(),
            _ => throw new ArgumentException($"未知设备名：{deviceName}", nameof(deviceName)),
        };

        services.AddSingleton(sp =>
        {
            var settings = configuration?.BindEquipmentFeaturesOptions().Equipments.GetValueOrDefault(deviceName);
            var equipment = builder.Build(sp, settings);
            // 设备依赖的特征服务（如硬件 Mock/真实驱动）也注册在容器中，由 Build 内部经 IServiceProvider 解析
            return equipment;
        });
        services.AddSingleton(sp => sp.GetRequiredService<IEquipment>().Features);
        return services;
    }
}
