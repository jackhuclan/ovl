using Ovl.Biz.Common.Features;

namespace Ovl.Biz.Common.Equipment;

/// <summary>
/// <see cref="IEquipment"/> 的基类实现。
/// </summary>
public abstract class EquipmentBase : IEquipment
{
    protected EquipmentBase(string name)
    {
        Name = name;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IFeatureCollection Features { get; } = new FeatureCollection();
}
