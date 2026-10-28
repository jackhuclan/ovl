using Microsoft.Extensions.Configuration;
using Ovl.Biz.Common.Equipment;
using Ovl.Biz.Services.Configuration;
using Xunit;

namespace Ovl.Biz.Common.Tests;

/// <summary>
/// 验证 EquipmentFeaturesOptions 通过 Microsoft.Extensions.Configuration 标准 API 绑定。
/// </summary>
public class EquipmentOptionsBindingTests
{
    [Fact]
    public void Bind_FromConfiguration_DisablesConfiguredFeatures()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Equipment:OM300:Features:Ffu"] = "False",
                ["Equipment:OM300:Features:APzt"] = "false",
                ["Equipment:OM100:Features:ModBus"] = "false",
            })
            .Build();

        var options = config.BindEquipmentFeaturesOptions();

        var om300 = options.Equipments["OM300"];
        Assert.False(om300.IsEnabled("Ffu"));
        Assert.False(om300.IsEnabled("APzt"));
        Assert.True(om300.IsEnabled("Camera")); // 未配置默认开启

        var om100 = options.Equipments["OM100"];
        Assert.False(om100.IsEnabled("ModBus"));
    }

    [Fact]
    public void Bind_FromJsonFile_LoadsEquipmentSwitches()
    {
        var root = Path.Combine(Path.GetTempPath(), "ovl-conf-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "appsettings.json"),
                """{ "Equipment": { "OM300": { "Features": { "Ffu": false } } } }""");

            var config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(root, "appsettings.json"))
                .Build();
            var options = config.BindEquipmentFeaturesOptions();

            Assert.False(options.Equipments["OM300"].IsEnabled("Ffu"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
