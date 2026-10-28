using Microsoft.Extensions.Configuration;
using Ovl.Biz.Common.Equipment;

namespace Ovl.Biz.Services.Configuration;

/// <summary>
/// 从 Microsoft.Extensions.Configuration 标准配置 API 绑定 <see cref="EquipmentFeaturesOptions"/>。
/// 配置节结构：Equipment:{设备名}:Features:{特征选项}=true/false。
/// </summary>
public static class EquipmentFeaturesOptionsExtensions
{
    /// <summary>
    /// 从配置节 Equipment 绑定各设备的特征开关；未配置的设备/特征由 <see cref="EquipmentFeatureSettings.IsEnabled"/> 默认开启。
    /// </summary>
    public static EquipmentFeaturesOptions BindEquipmentFeaturesOptions(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new EquipmentFeaturesOptions();
        var section = configuration.GetSection(EquipmentFeaturesOptions.SectionName);
        foreach (var deviceSection in section.GetChildren())
        {
            var settings = new EquipmentFeatureSettings();
            foreach (var featureSection in deviceSection.GetSection("Features").GetChildren())
            {
                settings.Features[featureSection.Key] = featureSection.Get<bool>();
            }
            options.Equipments[deviceSection.Key] = settings;
        }
        return options;
    }
}
