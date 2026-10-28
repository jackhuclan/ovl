using Ovl.Biz.Common.Equipment;
using Ovl.Biz.Common.Equipment.Devices;
using Ovl.Biz.Common.Features;
using Ovl.Biz.Common.Features.Hardware;
using Xunit;

namespace Ovl.Biz.Common.Tests;

/// <summary>测试用轻量服务容器。</summary>
public sealed class DictionaryServiceProvider(params (Type Type, object Instance)[] items) : IServiceProvider
{
    private readonly Dictionary<Type, object> _items = items.ToDictionary(i => i.Type, i => i.Instance);

    public object? GetService(Type serviceType) => _items.GetValueOrDefault(serviceType);
}

public class EquipmentFeatureOptionTests
{
    [Fact]
    public void Build_WithoutSettings_RegistersAllResolvedFeatures()
    {
        var camera = new Mock<ICamera>().Object;
        var services = new DictionaryServiceProvider((typeof(ICamera), camera));

        var equipment = OM300Equipment.Create(services);

        Assert.Equal("OM300", equipment.Name);
        Assert.Same(camera, equipment.Features.Get<ICamera>());
    }

    [Fact]
    public void Build_WithoutServices_UnresolvedFeatureAbsent()
    {
        var equipment = OM300Equipment.Create();

        // 无硬件/无服务时特征缺失，Get 返回 null（不抛异常）
        Assert.Null(equipment.Features.Get<ICamera>());
        Assert.Throws<InvalidOperationException>(() => equipment.Features.GetRequiredFeature<ICamera>());
    }

    [Fact]
    public void Build_DisabledFeature_Skipped()
    {
        var camera = new Mock<ICamera>().Object;
        var services = new DictionaryServiceProvider((typeof(ICamera), camera));
        var settings = new EquipmentFeaturesOptions();
        settings.Equipments["OM300"] = new EquipmentFeatureSettings
        {
            Features = { ["Camera"] = false },
        };

        var equipment = OM300Equipment.Create(services, settings.Equipments["OM300"]);

        Assert.Null(equipment.Features.Get<ICamera>());
    }

    [Fact]
    public void IsEnabled_UnconfiguredOption_DefaultsTrue()
    {
        var settings = new EquipmentFeatureSettings();

        Assert.True(settings.IsEnabled("Camera"));
        Assert.True(settings.IsEnabled("AnythingElse"));
    }

    [Fact]
    public void Build_EnabledFeature_Registered()
    {
        var ffu = new Mock<IFfu>().Object;
        var services = new DictionaryServiceProvider((typeof(IFfu), ffu));
        var settings = new EquipmentFeatureSettings { Features = { ["Ffu"] = true } };

        var equipment = OM300Equipment.Create(services, settings);

        Assert.Same(ffu, equipment.Features.Get<IFfu>());
    }

    [Theory]
    [InlineData("OM100")]
    [InlineData("OM300")]
    [InlineData("OM600")]
    [InlineData("OM40")]
    public void AllDevices_ExposeSameFeatureSet(string deviceName)
    {
        var equipment = deviceName switch
        {
            OM100Equipment.DeviceName => OM100Equipment.Create(),
            OM300Equipment.DeviceName => OM300Equipment.Create(),
            OM600Equipment.DeviceName => OM600Equipment.Create(),
            _ => OM40Equipment.Create(),
        };

        Assert.Equal(deviceName, equipment.Name);
        // 无服务时全部为空槽，但声明集合一致
        Assert.Empty(equipment.Features);
    }
}
