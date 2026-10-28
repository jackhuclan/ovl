using Ovl.Biz.Common.Features.Hardware;

namespace Ovl.Biz.Common.Equipment.Devices;

// 四种设备（OM100/OM300/OM600/OM40）的特征声明。
// 对应原 Marvel.App.OMxxx 中 E100Kit/E300Kit/E600Kit/E40Kit 的注册清单；
// 差异统一由 EquipmentFeaturesOptions 配置开关决定，真实硬件工厂在后续里程碑接入 DI。

public static class OM100Equipment
{
    public const string DeviceName = "OM100";

    public static EquipmentBuilder CreateBuilder() => new EquipmentBuilder(DeviceName)
        .AddResolvedFeature<IMotionController>("MotionControl")
        .AddResolvedFeature<ICamera>("Camera")
        .AddResolvedFeature<ILightSource>("LightSource")
        .AddResolvedFeature<IMotorInterface>("Motor")
        .AddResolvedFeature<IPztController>("Pzt")
        .AddResolvedFeature<IZpztControl>("ZPzt")
        .AddResolvedFeature<IModBus>("ModBus")
        .AddResolvedFeature<IThpd>("Thermal")
        .AddResolvedFeature<IFfu>("Ffu")
        .AddResolvedFeature<IUps>("Ups")
        .AddResolvedFeature<IAPzt>("APzt")
        .AddResolvedFeature<IEfem>("Efem");

    public static IEquipment Create(IServiceProvider? services = null, EquipmentFeatureSettings? settings = null)
        => CreateBuilder().Build(services, settings);
}

public static class OM300Equipment
{
    public const string DeviceName = "OM300";

    public static EquipmentBuilder CreateBuilder() => new EquipmentBuilder(DeviceName)
        .AddResolvedFeature<IMotionController>("MotionControl")
        .AddResolvedFeature<ICamera>("Camera")
        .AddResolvedFeature<ILightSource>("LightSource")
        .AddResolvedFeature<IMotorInterface>("Motor")
        .AddResolvedFeature<IPztController>("Pzt")
        .AddResolvedFeature<IZpztControl>("ZPzt")
        .AddResolvedFeature<IModBus>("ModBus")
        .AddResolvedFeature<IThpd>("Thermal")
        .AddResolvedFeature<IFfu>("Ffu")
        .AddResolvedFeature<IUps>("Ups")
        .AddResolvedFeature<IAPzt>("APzt")
        .AddResolvedFeature<IEfem>("Efem");

    public static IEquipment Create(IServiceProvider? services = null, EquipmentFeatureSettings? settings = null)
        => CreateBuilder().Build(services, settings);
}

public static class OM600Equipment
{
    public const string DeviceName = "OM600";

    public static EquipmentBuilder CreateBuilder() => new EquipmentBuilder(DeviceName)
        .AddResolvedFeature<IMotionController>("MotionControl")
        .AddResolvedFeature<ICamera>("Camera")
        .AddResolvedFeature<ILightSource>("LightSource")
        .AddResolvedFeature<IMotorInterface>("Motor")
        .AddResolvedFeature<IPztController>("Pzt")
        .AddResolvedFeature<IZpztControl>("ZPzt")
        .AddResolvedFeature<IModBus>("ModBus")
        .AddResolvedFeature<IThpd>("Thermal")
        .AddResolvedFeature<IFfu>("Ffu")
        .AddResolvedFeature<IUps>("Ups")
        .AddResolvedFeature<IAPzt>("APzt")
        .AddResolvedFeature<IEfem>("Efem");

    public static IEquipment Create(IServiceProvider? services = null, EquipmentFeatureSettings? settings = null)
        => CreateBuilder().Build(services, settings);
}

public static class OM40Equipment
{
    public const string DeviceName = "OM40";

    public static EquipmentBuilder CreateBuilder() => new EquipmentBuilder(DeviceName)
        .AddResolvedFeature<IMotionController>("MotionControl")
        .AddResolvedFeature<ICamera>("Camera")
        .AddResolvedFeature<ILightSource>("LightSource")
        .AddResolvedFeature<IMotorInterface>("Motor")
        .AddResolvedFeature<IPztController>("Pzt")
        .AddResolvedFeature<IZpztControl>("ZPzt")
        .AddResolvedFeature<IModBus>("ModBus")
        .AddResolvedFeature<IThpd>("Thermal")
        .AddResolvedFeature<IFfu>("Ffu")
        .AddResolvedFeature<IUps>("Ups")
        .AddResolvedFeature<IAPzt>("APzt")
        .AddResolvedFeature<IEfem>("Efem");

    public static IEquipment Create(IServiceProvider? services = null, EquipmentFeatureSettings? settings = null)
        => CreateBuilder().Build(services, settings);
}
