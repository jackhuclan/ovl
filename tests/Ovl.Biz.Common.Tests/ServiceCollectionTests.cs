using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ovl.Biz.Common.Equipment;
using Ovl.Biz.Common.Features;
using Ovl.Biz.Common.Features.Hardware;
using Ovl.Biz.Services;
using Ovl.Biz.Services.Configuration;
using Xunit;

namespace Ovl.Biz.Common.Tests;

/// <summary>
/// 验证基于 Microsoft.Extensions.DependencyInjection（IServiceCollection 标准方式）的服务注册。
/// </summary>
public class ServiceCollectionTests
{
    [Fact]
    public void AddConfManagement_ResolvesConfFileService()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Conf:Root"] = Path.Combine(Path.GetTempPath(), "ovl-di-tests", Guid.NewGuid().ToString("N")),
        }).Build();

        var provider = new ServiceCollection()
            .AddConfManagement(configuration)
            .BuildServiceProvider();

        var conf = provider.GetRequiredService<ConfFileService>();
        Assert.True(Directory.Exists(conf.Root));
        Assert.Same(conf, provider.GetRequiredService<ConfFileService>()); // 单例
    }

    [Fact]
    public void AddEquipment_ResolvesEquipmentWithMockedFeature()
    {
        var camera = new Mock<ICamera>().Object;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Equipment:OM300:Features:Efem"] = "false",
        }).Build();

        var provider = new ServiceCollection()
            .AddSingleton<ICamera>(camera)
            .AddEquipment("OM300", configuration)
            .BuildServiceProvider();

        var equipment = provider.GetRequiredService<IEquipment>();

        Assert.Equal("OM300", equipment.Name);
        Assert.Same(camera, equipment.Features.Get<ICamera>());
        // 配置关闭 + 容器未注册 → 缺失
        Assert.Null(equipment.Features.Get<IEfem>());
        // 特征集合同步注册
        Assert.Same(equipment.Features, provider.GetRequiredService<IFeatureCollection>());
    }

    [Fact]
    public void AddEquipment_UnknownDevice_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddEquipment("OM999"));
    }
}
