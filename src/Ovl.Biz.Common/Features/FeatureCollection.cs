// 移植自 dotnet/aspnetcore src/Extensions/Features/src/FeatureCollection.cs（MIT License）

using System.Collections;
using System.Diagnostics;

namespace Ovl.Biz.Common.Features;

/// <summary>
/// <see cref="IFeatureCollection"/> 的默认实现。
/// </summary>
[DebuggerDisplay("Count = {GetCount()}")]
public class FeatureCollection : IFeatureCollection
{
    private static readonly KeyComparer FeatureKeyComparer = new();
    private readonly IFeatureCollection? _defaults;
    private readonly int _initialCapacity;
    private IDictionary<Type, object>? _features;
    private volatile int _containerRevision;

    /// <summary>
    /// 初始化 <see cref="FeatureCollection"/> 的新实例。
    /// </summary>
    public FeatureCollection()
    {
    }

    /// <summary>
    /// 使用初始容量初始化 <see cref="FeatureCollection"/> 的新实例。
    /// </summary>
    /// <param name="initialCapacity">初始可容纳的特征数量。</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="initialCapacity"/> 小于 0 时抛出。</exception>
    public FeatureCollection(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

        _initialCapacity = initialCapacity;
    }

    /// <summary>
    /// 以给定的默认集合初始化 <see cref="FeatureCollection"/> 的新实例。
    /// </summary>
    /// <param name="defaults">默认特征集合。</param>
    public FeatureCollection(IFeatureCollection defaults)
    {
        _defaults = defaults;
    }

    /// <inheritdoc />
    public virtual int Revision => _containerRevision + (_defaults?.Revision ?? 0);

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public object? this[Type key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            return _features != null && _features.TryGetValue(key, out var result) ? result : _defaults?[key];
        }
        set
        {
            ArgumentNullException.ThrowIfNull(key);

            if (value == null)
            {
                if (_features != null && _features.Remove(key))
                {
                    _containerRevision++;
                }
                return;
            }

            if (_features == null)
            {
                _features = new Dictionary<Type, object>(_initialCapacity);
            }
            _features[key] = value;
            _containerRevision++;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
    {
        if (_features != null)
        {
            foreach (var pair in _features)
            {
                yield return pair;
            }
        }

        if (_defaults != null)
        {
            // 不返回被外层遮蔽的特征。
            foreach (var pair in _features == null ? _defaults : _defaults.Except(_features, FeatureKeyComparer))
            {
                yield return pair;
            }
        }
    }

    /// <inheritdoc />
    public TFeature? Get<TFeature>()
    {
        if (typeof(TFeature).IsValueType)
        {
            var feature = this[typeof(TFeature)];
            if (feature is null && Nullable.GetUnderlyingType(typeof(TFeature)) is null)
            {
                throw new InvalidOperationException(
                    $"{typeof(TFeature).FullName} does not exist in the feature collection " +
                    $"and because it is a struct the method can't return null. Use 'featureCollection[typeof({typeof(TFeature).FullName})] is not null' to check if the feature exists.");
            }
            return (TFeature?)feature;
        }
        return (TFeature?)this[typeof(TFeature)];
    }

    /// <inheritdoc />
    public void Set<TFeature>(TFeature? instance)
    {
        this[typeof(TFeature)] = instance;
    }

    // 供调试器使用。需要通过枚举统计才能获得正确值。
    private int GetCount() => this.Count();

    private sealed class KeyComparer : IEqualityComparer<KeyValuePair<Type, object>>
    {
        public bool Equals(KeyValuePair<Type, object> x, KeyValuePair<Type, object> y)
        {
            return x.Key.Equals(y.Key);
        }

        public int GetHashCode(KeyValuePair<Type, object> obj)
        {
            return obj.Key.GetHashCode();
        }
    }
}
