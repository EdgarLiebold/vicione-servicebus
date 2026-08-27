using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Caching.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching.Internals;

public sealed class BucketNodeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-NODE-VISIBILITY", "post-eviction-state-is-removed")]
    public async Task EvictedNode_NeverReturnsTheStoredValueAfterEvictionCompletes()
    {
        var expected = new CacheValue("value-1");
        var node = new BucketNode<CacheValue>(expected);

        Assert.True(node.TryEvict(out CacheValue removed));

        Assert.Same(expected, removed);
        Assert.False(node.IsValid);
        Assert.Null(node.Bucket);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await node.Value);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await node.GetValue(null!));
    }

    private sealed record CacheValue(string Id);
}
