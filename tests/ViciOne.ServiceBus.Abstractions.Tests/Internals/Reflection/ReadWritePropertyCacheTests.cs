namespace ViciOne.ServiceBus.Abstractions.Tests.Internals.Reflection;

using global::ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class ReadWritePropertyCacheTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-READ-WRITE-PROPERTY-CACHE", "private-setter-by-name")]
    public void PrivateSetter_IsWritableThroughItsPropertyName()
    {
        var cache = new ReadWritePropertyCache<PrivateSetterTarget>(includeNonPublic: true);
        var target = new PrivateSetterTarget();

        cache[nameof(PrivateSetterTarget.Name)].Set(target, "Chris");

        Assert.Equal("Chris", target.Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-READ-WRITE-PROPERTY-CACHE", "missing-key-try-contract")]
    public void TryGetValue_MissingPropertyReturnsFalseAndNull()
    {
        var cache = new ReadWritePropertyCache<PrivateSetterTarget>(includeNonPublic: true);

        var found = cache.TryGetValue("Missing", out var property);

        Assert.False(found);
        Assert.Null(property);
    }

    private sealed class PrivateSetterTarget
    {
        public string Name { get; private set; } = string.Empty;
    }
}
