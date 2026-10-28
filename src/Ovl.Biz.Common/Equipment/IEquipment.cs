namespace Ovl.Biz.Common.Equipment;

/// <summary>
/// 设备抽象：每种设备（OM600/OM300/OM100/OM40）提供自己的特征集合。
/// </summary>
public interface IEquipment
{
    /// <summary>设备名称（如 OM300）。</summary>
    string Name { get; }

    /// <summary>该设备已启用的特征集合。</summary>
    Features.IFeatureCollection Features { get; }
}
