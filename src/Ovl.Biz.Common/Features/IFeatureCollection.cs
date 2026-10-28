// 移植自 dotnet/aspnetcore src/Extensions/Features/src/IFeatureCollection.cs（MIT License）

namespace Ovl.Biz.Common.Features;

/// <summary>
/// 表示一组特征的集合。
/// </summary>
public interface IFeatureCollection : IEnumerable<KeyValuePair<Type, object>>
{
    /// <summary>
    /// 指示集合是否可修改。
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// 每次修改都会递增，可用于校验缓存结果。
    /// </summary>
    int Revision { get; }

    /// <summary>
    /// 获取或设置指定类型的特征。设置 null 值即移除该特征。
    /// </summary>
    /// <param name="key"></param>
    /// <returns>请求的特征；不存在时返回 null。</returns>
    object? this[Type key] { get; set; }

    /// <summary>
    /// 从集合中检索请求的特征。
    /// </summary>
    /// <typeparam name="TFeature">特征键。</typeparam>
    /// <returns>请求的特征；不存在时返回 null。</returns>
    TFeature? Get<TFeature>();

    /// <summary>
    /// 在集合中设置给定特征。
    /// </summary>
    /// <typeparam name="TFeature">特征键。</typeparam>
    /// <param name="instance">特征值。</param>
    void Set<TFeature>(TFeature? instance);
}
