using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests;

public sealed class TypeCacheTests
{
    [Theory]
    [InlineData(typeof(List<>), "System.Collections.Generic.List<>")]
    [InlineData(typeof(Dictionary<,>), "System.Collections.Generic.Dictionary<,>")]
    [RequirementCoverage("REQ-VSB-TYPE-NAME", "open-generic-definition")]
    public void GetShortName_FormatsOpenGenericDefinitionsWithoutActivation(Type type, string expected)
    {
        Assert.Equal(expected, TypeCache.GetShortName(type));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-OWNER", "type-cache-public-only-default")]
    public void PropertyCaches_DefaultToPublicAccessorOwnership()
    {
        IReadOnlyPropertyCache<PrivateAccessorTarget> readCache = TypeCache<PrivateAccessorTarget>.ReadOnlyPropertyCache;
        IReadWritePropertyCache<PrivateAccessorTarget> writeCache = TypeCache<PrivateAccessorTarget>.ReadWritePropertyCache;

        bool privateGetterFound = readCache.TryGetValue(nameof(PrivateAccessorTarget.PrivateGetter), out _);
        bool privateSetterFound = writeCache.TryGetValue(nameof(PrivateAccessorTarget.PrivateSetter), out _);
        bool publicFound = writeCache.TryGetValue(nameof(PrivateAccessorTarget.PublicValue), out _);

        Assert.False(privateGetterFound);
        Assert.False(privateSetterFound);
        Assert.True(publicFound);
    }

    private sealed class PrivateAccessorTarget
    {
        public string PrivateGetter { private get; set; } = string.Empty;

        public string PrivateSetter { get; private set; } = string.Empty;

        public string PublicValue { get; set; } = string.Empty;
    }
}
