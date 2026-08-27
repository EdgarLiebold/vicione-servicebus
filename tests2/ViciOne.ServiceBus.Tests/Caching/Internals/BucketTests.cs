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

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-BUCKET", "used-callback-cannot-decrement-reused-generation")]
    public async Task UsedCallback_CannotDecrementAReusedBucketGenerationAfterRebucketReturns()
    {
        var tracker = new ReusingTracker();
        var bucket = new Bucket<CacheValue>(tracker);
        var oldNode = new BucketNode<CacheValue>(new CacheValue("old"));
        var currentNode = new BucketNode<CacheValue>(new CacheValue("current"));
        tracker.Initialize(bucket, currentNode);
        bucket.Start(DateTime.UnixEpoch);
        bucket.Push(oldNode);
        bucket.Stop(DateTime.UnixEpoch.AddSeconds(1));

        _ = await oldNode.Value;

        Assert.Equal(1, bucket.Count);
        Assert.Same(currentNode, bucket.Head);
        Assert.Same(bucket, currentNode.Bucket);
    }

    private sealed record CacheValue(string Id);

    private sealed class ReusingTracker : INodeTracker<CacheValue>
    {
        private Bucket<CacheValue> _bucket = null!;
        private BucketNode<CacheValue> _currentNode = null!;

        public CacheStatistics Statistics { get; } = new(
            capacity: 1,
            bucketCount: 6,
            bucketSize: 1,
            minAge: TimeSpan.Zero,
            maxAge: TimeSpan.FromMinutes(1),
            validityCheckInterval: TimeSpan.FromSeconds(1));

        public void Initialize(Bucket<CacheValue> bucket, BucketNode<CacheValue> currentNode)
        {
            _bucket = bucket;
            _currentNode = currentNode;
        }

        public void Rebucket(IBucketNode<CacheValue> node)
        {
            _bucket.Clear();
            _bucket.Start(DateTime.UnixEpoch.AddSeconds(2));
            _bucket.Push(_currentNode);
        }

        public void Add(INodeValueFactory<CacheValue> nodeValueFactory) => throw new NotSupportedException();

        public void Add(CacheValue value) => throw new NotSupportedException();

        public void Remove(IBucketNode<CacheValue> existingNode) => throw new NotSupportedException();

        public IEnumerable<INode<CacheValue>> GetAll() => [];

        public void Clear() => throw new NotSupportedException();

        public ConnectHandle Connect(ICacheValueObserver<CacheValue> observer) =>
            throw new NotSupportedException();
    }
}
