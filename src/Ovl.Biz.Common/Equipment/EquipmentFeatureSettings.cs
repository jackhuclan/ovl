namespace Ovl.Biz.Common.Equipment;

/// <summary>
/// 各 Equipment 的特征开关配置。未配置的特征默认开启（与原 SetCategory 行为一致）。
/// </summary>
public sealed class EquipmentFeaturesOptions
{
    /// <summary>appsettings.json 中的配置节名。</summary>
    public const string SectionName = "Equipment";

    /// <summary>按设备名（如 OM300）分段的特征开关。</summary>
    public Dictionary<string, EquipmentFeatureSettings> Equipments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// 单台设备的特征开关集合。
/// </summary>
public sealed class EquipmentFeatureSettings
{
    /// <summary>特征选项名 → 是否启用；未配置视为启用。</summary>
    public Dictionary<string, bool> Features { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 判断指定特征选项是否启用。未配置时默认开启。
    /// </summary>
    public bool IsEnabled(string optionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(optionKey);
        return !Features.TryGetValue(optionKey, out var enabled) || enabled;
    }
}
