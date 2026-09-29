using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Metadata.Reflection;

public sealed class PropertyCacheTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-CACHE", "case-insensitive-read-write-lookup")]
    public void ReadWriteCache_LookupIsOrdinalCaseInsensitive()
    {
        var cache = new ReadWritePropertyCache<CacheTarget>();
        var target = new CacheTarget();

        bool found = cache.TryGetValue("PUBLICTEXT", out ReadWriteProperty<CacheTarget>? property);
        property?.Set(target, "updated");

        Assert.True(found);
        Assert.NotNull(property);
        Assert.Same(cache["publictext"], property);
        Assert.Equal("updated", target.PublicText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-CACHE", "missing-key-try-contract")]
    public void Cache_MissingPropertyReturnsFalseAndNull()
    {
        var cache = new ReadWritePropertyCache<CacheTarget>();

        bool found = cache.TryGetValue("Missing", out ReadWriteProperty<CacheTarget>? property);

        Assert.False(found);
        Assert.Null(property);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-CACHE", "public-only-excludes-private-accessors")]
    public void PublicOnly_ExcludesPropertiesWithAnyNonPublicAccessor()
    {
        var readCache = new ReadOnlyPropertyCache<CacheTarget>();
        var writeCache = new ReadWritePropertyCache<CacheTarget>();

        bool privateGetterFound = readCache.TryGetValue(nameof(CacheTarget.PrivateGetter), out _);
        bool privateGetterWritable = writeCache.TryGetValue(nameof(CacheTarget.PrivateGetter), out _);
        bool privateSetterFound = writeCache.TryGetValue(nameof(CacheTarget.PrivateSetter), out _);

        Assert.False(privateGetterFound);
        Assert.False(privateGetterWritable);
        Assert.False(privateSetterFound);
        Assert.True(readCache.TryGetValue(nameof(CacheTarget.PrivateSetter), out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-CACHE", "include-non-public-accessors")]
    public void IncludeNonPublic_IncludesAndExecutesPrivateAccessors()
    {
        var readCache = new ReadOnlyPropertyCache<CacheTarget>(PropertyAccessPolicy.IncludeNonPublic);
        var writeCache = new ReadWritePropertyCache<CacheTarget>(PropertyAccessPolicy.IncludeNonPublic);
        var target = new CacheTarget();

        bool getterFound = readCache.TryGetValue(nameof(CacheTarget.PrivateGetter), out ReadOnlyProperty<CacheTarget>? getter);
        bool setterFound = writeCache.TryGetValue(nameof(CacheTarget.PrivateSetter), out ReadWriteProperty<CacheTarget>? setter);
        setter?.Set(target, "private-updated");

        Assert.True(getterFound);
        Assert.NotNull(getter);
        Assert.Equal("private-getter", getter.Get(target));
        Assert.True(setterFound);
        Assert.NotNull(setter);
        Assert.Equal("private-updated", target.PrivateSetter);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-CACHE", "derived-property-wins")]
    public void HiddenProperty_DerivedDeclarationIsSelectedDeterministically()
    {
        var cache = new ReadWritePropertyCache<DerivedTarget>();
        var target = new DerivedTarget();

        ReadWriteProperty<DerivedTarget> property = cache[nameof(DerivedTarget.Value)];
        property.Set(target, "derived-value");

        Assert.Equal(typeof(DerivedTarget), property.Property.DeclaringType);
        Assert.Equal(typeof(string), property.Property.PropertyType);
        Assert.Equal("derived-value", target.Value);
        Assert.Null(((BaseTarget)target).Value);
    }

    [Theory]
    [InlineData(PropertyAccessPolicy.PublicOnly)]
    [InlineData(PropertyAccessPolicy.IncludeNonPublic)]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-CACHE", "indexer-does-not-prevent-scalar-property-access")]
    public void Indexer_DoesNotPreventCachingOrUsingScalarProperties(PropertyAccessPolicy policy)
    {
        var readCache = new ReadOnlyPropertyCache<IndexedTarget>(policy);
        var writeCache = new ReadWritePropertyCache<IndexedTarget>(policy);
        var target = new IndexedTarget();

        Assert.Equal(2, readCache.Count());
        Assert.Equal(2, writeCache.Count());
        Assert.Equal([nameof(IndexedBaseTarget.Item), nameof(IndexedBaseTarget.Name)],
            MessageTypeCache<IndexedTarget>.Properties.Select(property => property.Name).Order(StringComparer.Ordinal));
        Assert.Equal([nameof(IndexedBaseTarget.Item), nameof(IndexedBaseTarget.Name)],
            MessageTypeCache.GetProperties(typeof(IndexedTarget)).Select(property => property.Name).Order(StringComparer.Ordinal));

        ReadWriteProperty<IndexedTarget> property = writeCache[nameof(IndexedTarget.Name)];
        property.Set(target, "updated");
        ReadWriteProperty<IndexedTarget> baseItem = writeCache[nameof(IndexedBaseTarget.Item)];
        baseItem.Set(target, "base-updated");

        Assert.Equal("updated", target.Name);
        Assert.Equal("updated", readCache.Single(item => item.Property.Name == nameof(IndexedTarget.Name)).Get(target));
        Assert.Same(property, writeCache[nameof(IndexedTarget.Name)]);
        Assert.Equal(typeof(IndexedBaseTarget), baseItem.Property.DeclaringType);
        Assert.Equal("base-updated", ((IndexedBaseTarget)target).Item);
        Assert.Equal("base-updated", readCache.Single(item => item.Property.Name == nameof(IndexedBaseTarget.Item)).Get(target));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-CACHE", "case-insensitive-hidden-property-selection")]
    public void CaseDistinctHiddenProperty_SelectsDerivedScalarForEveryProjection()
    {
        var readCache = new ReadOnlyPropertyCache<CaseDerivedTarget>();
        var writeCache = new ReadWritePropertyCache<CaseDerivedTarget>();
        var target = new CaseDerivedTarget();

        Assert.Single(readCache);
        Assert.Single(writeCache);
        Assert.Equal(typeof(CaseDerivedTarget), readCache.Single().Property.DeclaringType);
        Assert.Equal(typeof(CaseDerivedTarget), writeCache.Single().Property.DeclaringType);
        Assert.Equal(typeof(CaseDerivedTarget), Assert.Single(MessageTypeCache<CaseDerivedTarget>.Properties).DeclaringType);
        Assert.Equal(typeof(CaseDerivedTarget), Assert.Single(MessageTypeCache.GetProperties(typeof(CaseDerivedTarget))).DeclaringType);

        writeCache["ITEM"].Set(target, "derived-updated");

        Assert.Equal("derived-updated", readCache.Single().Get(target));
        Assert.Equal("derived-updated", target.item);
        Assert.Equal("base", ((CaseBaseTarget)target).Item);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-CACHE", "expression-get-set")]
    public void ExpressionOperations_UseTheSameCachedProperty()
    {
        var cache = new ReadWritePropertyCache<CacheTarget>();
        var target = new CacheTarget();

        cache.Set(instance => instance.PublicText, target, "expression-value");
        object? value = cache.Get(instance => instance.PublicText, target);

        Assert.Equal("expression-value", value);
        Assert.Same(cache[nameof(CacheTarget.PublicText)], cache["PUBLICTEXT"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-CACHE", "invalid-policy")]
    public void Cache_RejectsAnUnknownAccessPolicy()
    {
        var policy = (PropertyAccessPolicy)(-1);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReadWritePropertyCache<CacheTarget>(policy));

        Assert.Equal("accessPolicy", exception.ParamName);
        Assert.Equal(policy, exception.ActualValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-SURFACE", "single-try-get-contract")]
    public void ReadWriteCache_ExposesOneConventionalTryGetContract()
    {
        string[] tryMethods = typeof(IReadWritePropertyCache<>)
            .GetMethods()
            .Where(method => method.Name.StartsWith("Try", StringComparison.Ordinal))
            .Select(method => method.Name)
            .ToArray();

        Assert.Equal(["TryGetValue"], tryMethods);
    }

    private sealed class CacheTarget
    {
        public string PrivateGetter { private get; set; } = "private-getter";

        public string PrivateSetter { get; private set; } = "initial";

        public string PublicText { get; set; } = "initial";
    }

    private class BaseTarget
    {
        public object? Value { get; set; }
    }

    private sealed class DerivedTarget : BaseTarget
    {
        public new string? Value { get; set; }
    }

    private class IndexedBaseTarget
    {
        public string Item { get; set; } = "base";

        public string Name { get; set; } = "initial";
    }

    private sealed class IndexedTarget : IndexedBaseTarget
    {
        public string this[int index]
        {
            get => index.ToString();
            set { }
        }

        public string this[int row, int column]
        {
            get => (row + column).ToString();
            set { }
        }
    }

    private class CaseBaseTarget
    {
        public string Item { get; set; } = "base";
    }

    private sealed class CaseDerivedTarget : CaseBaseTarget
    {
        public string item { get; set; } = "derived";
    }
}
