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

        bool found = cache.TryGetProperty("PUBLICTEXT", out ReadWriteProperty<CacheTarget>? property);
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
}
