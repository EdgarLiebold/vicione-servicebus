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

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private sealed record CacheValue(string Id);

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
