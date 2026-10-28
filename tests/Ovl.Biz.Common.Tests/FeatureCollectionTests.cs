using Ovl.Biz.Common.Features;
using Xunit;

namespace Ovl.Biz.Common.Tests;

public interface ITestFeature
{
    string Name { get; }
}

public class TestFeature : ITestFeature
{
    public string Name => "Test";
}

public class AnotherFeature : ITestFeature
{
    public string Name => "Another";
}

public class FeatureCollectionTests
{
    [Fact]
    public void Set_ThenGet_ReturnsInstance()
    {
        var features = new FeatureCollection();
        var feature = new TestFeature();

        features.Set<ITestFeature>(feature);

        Assert.Same(feature, features.Get<ITestFeature>());
        Assert.Same(feature, features[typeof(ITestFeature)]);
    }

    [Fact]
    public void Set_Null_RemovesFeature()
    {
        var features = new FeatureCollection();
        features.Set<ITestFeature>(new TestFeature());

        features.Set<ITestFeature>(null);

        Assert.Null(features.Get<ITestFeature>());
    }

    [Fact]
    public void Revision_Increments_On_Set_And_Remove()
    {
        var features = new FeatureCollection();
        var initial = features.Revision;

        features.Set<ITestFeature>(new TestFeature());
        Assert.Equal(initial + 1, features.Revision);

        features.Set<ITestFeature>(null);
        Assert.Equal(initial + 2, features.Revision);
    }

    [Fact]
    public void Get_MissingReferenceType_ReturnsNull()
    {
        var features = new FeatureCollection();

        Assert.Null(features.Get<ITestFeature>());
    }

    [Fact]
    public void Get_MissingValueType_Throws()
    {
        var features = new FeatureCollection();

        Assert.Throws<InvalidOperationException>(() => features.Get<int>());
    }

    [Fact]
    public void Indexer_SetNull_OnMissingKey_DoesNotThrow()
    {
        var features = new FeatureCollection();

        features[typeof(ITestFeature)] = null;

        Assert.Null(features[typeof(ITestFeature)]);
    }

    [Fact]
    public void WithDefaults_InnerEntries_FallThrough()
    {
        var defaults = new FeatureCollection();
        defaults.Set<ITestFeature>(new TestFeature());
        var features = new FeatureCollection(defaults);

        Assert.NotNull(features.Get<ITestFeature>());
        // 外层自身 0 项 + 默认集合回退 1 项
        Assert.Single(features);
    }

    [Fact]
    public void WithDefaults_OuterEntry_MasksInner()
    {
        var defaults = new FeatureCollection();
        defaults.Set<ITestFeature>(new TestFeature());
        var features = new FeatureCollection(defaults);
        var another = new AnotherFeature();
        features.Set<ITestFeature>(another);

        Assert.Same(another, features.Get<ITestFeature>());
    }

    [Fact]
    public void GetRequiredFeature_ReturnsInstance()
    {
        var features = new FeatureCollection();
        var feature = new TestFeature();
        features.Set<ITestFeature>(feature);

        Assert.Same(feature, features.GetRequiredFeature<ITestFeature>());
    }

    [Fact]
    public void GetRequiredFeature_Missing_Throws()
    {
        var features = new FeatureCollection();

        Assert.Throws<InvalidOperationException>(() => features.GetRequiredFeature<ITestFeature>());
    }

    [Fact]
    public void InitialCapacity_Negative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FeatureCollection(-1));
    }
}
