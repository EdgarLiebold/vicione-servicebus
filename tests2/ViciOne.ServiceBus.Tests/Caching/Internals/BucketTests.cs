using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Caching.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching.Internals;

public sealed class BucketTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-BUCKET", "push-links-node")]
    public void Push_MakesTheNodeHeadAndLinksItBackToTheBucket()
    {
        var settings = new CacheSettings(
            capacity: 1_000,
            minAge: TimeSpan.FromSeconds(30),
            maxAge: TimeSpan.FromSeconds(60),
            nowProvider: () => DateTime.UnixEpoch);
        var tracker = new NodeTracker<CacheValue>(settings);
        var bucket = new Bucket<CacheValue>(tracker);
        var node = new BucketNode<CacheValue>(new CacheValue("value-1"));

        bucket.Push(node);

        Assert.Equal(1, bucket.Count);
        Assert.Same(node, bucket.Head);
        Assert.Same(bucket, node.Bucket);
        Assert.Null(node.Next);
    }

    private sealed record CacheValue(string Id);
}
