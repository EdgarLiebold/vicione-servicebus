using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Caching.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching.Internals;

public sealed class NodeTrackerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-NODE-PROMOTION", "completed-factory-to-bucket-node")]
    public async Task CompletedFactory_IsPromotedToAStoredBucketNodeAndReportedToObservers()
    {
        var settings = new CacheSettings(
            capacity: 1_000,
            minAge: TimeSpan.FromSeconds(30),
            maxAge: TimeSpan.FromSeconds(60),
            nowProvider: () => DateTime.UnixEpoch);
        var tracker = new NodeTracker<CacheValue>(settings);
        var observer = new CapturingObserver<CacheValue>();
        using ConnectHandle connection = tracker.Connect(observer);
        var expected = new CacheValue("value-1");
        var pendingValue = new PendingValue<string, CacheValue>(
            "key-1",
            _ => Task.FromResult(expected));
        var nodeValueFactory = new NodeValueFactory<CacheValue>(pendingValue, timeoutInMilliseconds: 0);
        var temporaryNode = new FactoryNode<CacheValue>(nodeValueFactory);

        tracker.Add(nodeValueFactory);

        (INode<CacheValue> storedNode, CacheValue observedValue) =
            await observer.Added.WaitAsync(OperationTimeout, TestCancellationToken);
        CacheValue temporaryValue = await temporaryNode.Value.WaitAsync(OperationTimeout, TestCancellationToken);
        CacheValue storedValue = await storedNode.Value.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.IsType<BucketNode<CacheValue>>(storedNode);
        Assert.NotSame(temporaryNode, storedNode);
        Assert.Same(expected, temporaryValue);
        Assert.Same(expected, storedValue);
        Assert.Same(expected, observedValue);
        Assert.Equal(1, tracker.Statistics.Count);
        Assert.Equal(1, tracker.Statistics.TotalCount);
        Assert.Equal(1, tracker.Statistics.Misses);
        Assert.Equal(0, tracker.Statistics.Hits);
        Assert.Equal(0, tracker.Statistics.CreateFaults);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-LOCK-ORDER", "time-provider-runs-before-tracker-lock")]
    public async Task TimeProvider_CanCoordinateWithTheTrackerWithoutRunningUnderItsLock()
    {
        NodeTracker<CacheValue>? tracker = null;
        IBucketNode<CacheValue>? existingNode = null;
        var probeEnabled = false;
        var probeCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        DateTime Now()
        {
            if (probeEnabled)
            {
                probeEnabled = false;
                try
                {
                    Task.Run(() => tracker!.Rebucket(existingNode!))
                        .WaitAsync(OperationTimeout, TestCancellationToken)
                        .GetAwaiter()
                        .GetResult();
                    probeCompleted.TrySetResult(true);
                }
                catch
                {
                    probeCompleted.TrySetResult(false);
                }
            }

            return DateTime.UnixEpoch;
        }

        tracker = new NodeTracker<CacheValue>(new CacheSettings(nowProvider: Now));
        var observer = new CapturingObserver<CacheValue>();
        using ConnectHandle connection = tracker.Connect(observer);
        tracker.Add(new CacheValue("value-1"));
        (INode<CacheValue> node, _) = await observer.Added.WaitAsync(OperationTimeout, TestCancellationToken);
        existingNode = Assert.IsAssignableFrom<IBucketNode<CacheValue>>(node);

        probeEnabled = true;
        tracker.Add(new CacheValue("value-2"));

        Assert.True(await probeCompleted.Task.WaitAsync(OperationTimeout, TestCancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-LOCK-ORDER", "usage-detach-runs-after-tracker-unlock")]
    public async Task UsageEventDetach_CanCoordinateWithClearWithoutRunningUnderTheTrackerLock()
    {
        var tracker = new NodeTracker<UsageAwareValue>(new CacheSettings(nowProvider: () => DateTime.UnixEpoch));
        var observer = new CapturingObserver<UsageAwareValue>();
        using ConnectHandle connection = tracker.Connect(observer);
        var detachCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var value = new UsageAwareValue(() =>
        {
            try
            {
                Task.Run(tracker.Clear)
                    .WaitAsync(OperationTimeout, TestCancellationToken)
                    .GetAwaiter()
                    .GetResult();
                detachCompleted.TrySetResult(true);
            }
            catch
            {
                detachCompleted.TrySetResult(false);
                throw;
            }
        });
        tracker.Add(value);
        (INode<UsageAwareValue> node, _) = await observer.Added.WaitAsync(OperationTimeout, TestCancellationToken);

        tracker.Remove(Assert.IsAssignableFrom<IBucketNode<UsageAwareValue>>(node));

        Assert.True(await detachCompleted.Task.WaitAsync(OperationTimeout, TestCancellationToken));
        Assert.Equal(0, tracker.Statistics.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-BUCKET", "rebucket-transfers-source-count-once")]
    public void Rebucket_TransfersTheSourceBucketCountExactlyOnce()
    {
        var tracker = new NodeTracker<CacheValue>(new CacheSettings(nowProvider: () => DateTime.UnixEpoch));
        var sourceBucket = new Bucket<CacheValue>(tracker);
        var node = new BucketNode<CacheValue>(new CacheValue("value-1"));
        sourceBucket.Start(DateTime.UnixEpoch);
        sourceBucket.Push(node);
        sourceBucket.Stop(DateTime.UnixEpoch.AddSeconds(1));

        tracker.Rebucket(node);

        Bucket<CacheValue> currentBucket = node.Bucket;
        Assert.Equal(0, sourceBucket.Count);
        Assert.NotSame(sourceBucket, currentBucket);

        tracker.Rebucket(node);

        Assert.Equal(0, sourceBucket.Count);
        Assert.Same(currentBucket, node.Bucket);
    }

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private sealed record CacheValue(string Id);

    private sealed class UsageAwareValue(Action onDetached) : INotifyValueUsed
    {
        private Action? _used;

        public event Action? Used
        {
            add => _used += value;
            remove
            {
                onDetached();
                _used -= value;
            }
        }
    }

    private sealed class CapturingObserver<TValue> : ICacheValueObserver<TValue>
        where TValue : class
    {
        private readonly TaskCompletionSource<(INode<TValue> Node, TValue Value)> _added =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<(INode<TValue> Node, TValue Value)> Added => _added.Task;

        public void ValueAdded(INode<TValue> node, TValue value) => _added.TrySetResult((node, value));

        public void ValueRemoved(INode<TValue> node, TValue value)
        {
        }

        public void CacheCleared()
        {
        }
    }
}
