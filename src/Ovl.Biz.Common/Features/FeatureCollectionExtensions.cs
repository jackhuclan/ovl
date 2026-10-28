// 移植自 dotnet/aspnetcore src/Extensions/Features/src/FeatureCollectionExtensions.cs（MIT License）

namespace Ovl.Biz.Common.Features;

/// <summary>
/// 从 <see cref="IFeatureCollection"/> 获取特征的扩展方法。
/// </summary>
public static class FeatureCollectionExtensions
{
    /// <summary>
    /// 从集合中检索请求的特征。
    /// 特征不存在时抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    /// <param name="featureCollection"><see cref="IFeatureCollection"/>。</param>
    /// <typeparam name="TFeature">特征键。</typeparam>
    /// <returns>请求的特征。</returns>
    public static TFeature GetRequiredFeature<TFeature>(this IFeatureCollection featureCollection)
        where TFeature : notnull
    {
        ArgumentNullException.ThrowIfNull(featureCollection);

        return featureCollection.Get<TFeature>() ?? throw new InvalidOperationException($"Feature '{typeof(TFeature)}' is not present.");
    }

    /// <summary>
    /// 从集合中检索请求的特征。
    /// 特征不存在时抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    /// <param name="featureCollection">特征集合。</param>
    /// <param name="key">特征键。</param>
    /// <returns>请求的特征。</returns>
    public static object GetRequiredFeature(this IFeatureCollection featureCollection, Type key)
    {
        ArgumentNullException.ThrowIfNull(featureCollection);
        ArgumentNullException.ThrowIfNull(key);

        return featureCollection[key] ?? throw new InvalidOperationException($"Feature '{key}' is not present.");
    }
}
