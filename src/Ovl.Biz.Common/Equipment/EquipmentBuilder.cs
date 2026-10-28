using Ovl.Biz.Common.Features;

namespace Ovl.Biz.Common.Equipment;

/// <summary>
/// 一条特征注册：声明特征类型、对应的开关键名与实例工厂。
/// </summary>
public sealed record FeatureRegistration(Type FeatureType, string OptionKey, Func<IServiceProvider?, object?> Factory);

/// <summary>
/// 设备构建器：声明设备包含哪些特征，再按 <see cref="EquipmentFeatureSettings"/> 过滤后构建 <see cref="IEquipment"/>。
/// 原 Marvel 项目通过 AddDevice/SetCategory 注册的内容，在新架构中改为注册到各 Equipment 的 Features。
/// </summary>
public sealed class EquipmentBuilder
{
    private readonly string _name;
    private readonly List<FeatureRegistration> _registrations = [];

    public EquipmentBuilder(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _name = name;
    }

    public string Name => _name;

    public IReadOnlyList<FeatureRegistration> Registrations => _registrations;

    /// <summary>
    /// 添加一个特征注册。工厂返回 null 表示该特征实例暂不可用（不注册）。
    /// </summary>
    public EquipmentBuilder AddFeature<TFeature>(string optionKey, Func<IServiceProvider?, TFeature?> factory)
        where TFeature : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(optionKey);
        ArgumentNullException.ThrowIfNull(factory);
        _registrations.Add(new FeatureRegistration(typeof(TFeature), optionKey, services => factory(services)));
        return this;
    }

    /// <summary>
    /// 添加一个由 DI 容器解析的特征注册（无硬件/未注册服务时该特征为空）。
    /// </summary>
    public EquipmentBuilder AddResolvedFeature<TFeature>(string optionKey)
        where TFeature : class
    {
        return AddFeature<TFeature>(optionKey, services => services?.GetService(typeof(TFeature)) as TFeature);
    }

    /// <summary>
    /// 按开关过滤注册特征并构建设备。
    /// </summary>
    /// <param name="services">用于解析特征实例的服务容器（可为 null）。</param>
    /// <param name="settings">该设备的特征开关（null 时全部启用）。</param>
    public IEquipment Build(IServiceProvider? services = null, EquipmentFeatureSettings? settings = null)
    {
        settings ??= new EquipmentFeatureSettings();
        var equipment = new PlainEquipment(_name);
        foreach (var registration in _registrations)
        {
            if (!settings.IsEnabled(registration.OptionKey))
            {
                continue;
            }

            var instance = registration.Factory(services);
            if (instance is null)
            {
                continue;
            }
            equipment.Features[registration.FeatureType] = instance;
        }
        return equipment;
    }

    private sealed class PlainEquipment(string name) : EquipmentBase(name);
}
