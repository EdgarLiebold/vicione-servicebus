using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class MultipleIndexTests
{
    private const int ValueCount = 100;

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "propagation")]
    public async Task ValueCreatedThroughOneIndex_IsAvailableThroughTheOtherIndex()
    {
        using CacheFixture fixture = CreateFixture(expectedAdditions: ValueCount);

        await AddValues(fixture.IdIndex, 0);
        await fixture.Observer.WaitForAddedCount(ValueCount, OperationTimeout, TestCancellationToken);

        CacheValue actual = await fixture.ValueIndex.Get("The key is key-27");

        Assert.Equal("key-27", actual.Id);
        Assert.Equal("The key is key-27", actual.Value);
        Assert.Equal(ValueCount, fixture.Cache.Statistics.Count);
        Assert.Equal(ValueCount, fixture.Cache.Statistics.TotalCount);
        Assert.Equal(ValueCount, fixture.Cache.Statistics.Misses);
        Assert.Equal(1, fixture.Cache.Statistics.Hits);
        Assert.Equal(ValueCount, fixture.Cache.GetAll().Count());
        Assert.Equal(ValueCount, fixture.Observer.AddedCount);
        Assert.Equal(0, fixture.Observer.RemovedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "clear")]
    public async Task Clear_EmptiesEveryIndexAndResetsTheVisibleCacheState()
    {
        using CacheFixture fixture = CreateFixture(expectedAdditions: ValueCount);

        await AddValues(fixture.IdIndex, 0);
        await fixture.Observer.WaitForAddedCount(ValueCount, OperationTimeout, TestCancellationToken);
        CacheValue indexed = await fixture.ValueIndex.Get("The key is key-27");

        fixture.Cache.Clear();

        Assert.Equal("key-27", indexed.Id);
        Assert.Equal(1, fixture.Observer.ClearedCount);
        Assert.Equal(0, fixture.Cache.Statistics.Count);
        Assert.Equal(0, fixture.Cache.Statistics.TotalCount);
        Assert.Equal(0, fixture.Cache.Statistics.Hits);
        Assert.Equal(0, fixture.Cache.Statistics.Misses);
        Assert.Empty(fixture.Cache.GetAll());

        KeyNotFoundException missingId = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fixture.IdIndex.Get("key-27"));
        KeyNotFoundException missingValue = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fixture.ValueIndex.Get("The key is key-27"));

        Assert.Equal("Key not found: key-27", missingId.Message);
        Assert.Equal("Key not found: The key is key-27", missingValue.Message);
        Assert.Equal(2, fixture.Cache.Statistics.Misses);
        Assert.Equal(ValueCount, fixture.Observer.AddedCount);
        Assert.Equal(0, fixture.Observer.RemovedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "post-clear-reuse")]
    public async Task Clear_AllowsTheSameKeysToBeCreatedAndIndexedAgain()
    {
        using CacheFixture fixture = CreateFixture(expectedAdditions: ValueCount * 2);

        await AddValues(fixture.IdIndex, 0);
        await fixture.Observer.WaitForAddedCount(ValueCount, OperationTimeout, TestCancellationToken);

        fixture.Cache.Clear();

        await AddValues(fixture.IdIndex, 0);
        await fixture.Observer.WaitForAddedCount(ValueCount * 2, OperationTimeout, TestCancellationToken);

        CacheValue byId = await fixture.IdIndex.Get("key-27");
        CacheValue byValue = await fixture.ValueIndex.Get("The key is key-27");

        Assert.Same(byId, byValue);
        Assert.Equal("key-27", byId.Id);
        Assert.Equal(ValueCount, fixture.Cache.Statistics.Count);
        Assert.Equal(ValueCount, fixture.Cache.Statistics.TotalCount);
        Assert.Equal(ValueCount, fixture.Cache.Statistics.Misses);
        Assert.Equal(2, fixture.Cache.Statistics.Hits);
        Assert.Equal(ValueCount, fixture.Cache.GetAll().Count());
        Assert.Equal(1, fixture.Observer.ClearedCount);
        Assert.Equal(ValueCount * 2, fixture.Observer.AddedCount);
        Assert.Equal(0, fixture.Observer.RemovedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "cross-index-removal")]
    public async Task RemoveThroughOneIndex_RemovesTheValueFromAllIndexesAndVisibleCacheState()
    {
        using CacheFixture fixture = CreateFixture(expectedAdditions: ValueCount, expectedRemovals: 1);

        await AddValues(fixture.IdIndex, 0);
        await fixture.Observer.WaitForAddedCount(ValueCount, OperationTimeout, TestCancellationToken);
        CacheValue indexed = await fixture.ValueIndex.Get("The key is key-27");

        bool removed = fixture.ValueIndex.Remove("The key is key-29");

        (INode<CacheValue> removedNode, CacheValue removedValue) =
            await fixture.Observer.Removed.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.True(removed);
        Assert.Equal("key-27", indexed.Id);
        Assert.Equal("key-29", removedValue.Id);
        Assert.False(removedNode.IsValid);
        CacheValue[] visibleValues = await Task.WhenAll(fixture.Cache.GetAll());
        Assert.DoesNotContain(visibleValues, value => value.Id == "key-29");
        Assert.Equal(ValueCount - 1, visibleValues.Length);

        KeyNotFoundException missingId = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fixture.IdIndex.Get("key-29"));
        KeyNotFoundException missingValue = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fixture.ValueIndex.Get("The key is key-29"));

        Assert.Equal("Key not found: key-29", missingId.Message);
        Assert.Equal("Key not found: The key is key-29", missingValue.Message);
        Assert.Equal(ValueCount - 1, fixture.Cache.Statistics.Count);
        Assert.Equal(ValueCount, fixture.Cache.Statistics.TotalCount);
        Assert.Equal(1, fixture.Cache.Statistics.Hits);
        Assert.Equal(ValueCount + 2, fixture.Cache.Statistics.Misses);
        Assert.Equal(1, fixture.Observer.RemovedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "pending-removal-truthfulness")]
    public async Task RemoveWhileCreationIsPending_ReportsFalseAndPreservesTheCreatedValue()
    {
        using CacheFixture fixture = CreateFixture(expectedAdditions: 1, expectedRemovals: 1);
        var controlledFactory = new ControlledFactory();
        var expected = new CacheValue("key-pending", "The key is key-pending");

        Task<CacheValue> creation = fixture.IdIndex.Get("key-pending", controlledFactory.Create);
        string requestedKey = await controlledFactory.Started.WaitAsync(OperationTimeout, TestCancellationToken);

        bool removedWhilePending = fixture.IdIndex.Remove("key-pending");

        Assert.False(removedWhilePending);
        Assert.False(creation.IsCompleted);

        controlledFactory.Complete(expected);

        CacheValue created = await creation.WaitAsync(OperationTimeout, TestCancellationToken);
        await fixture.Observer.WaitForAddedCount(1, OperationTimeout, TestCancellationToken);
        CacheValue indexedByValue = await fixture.ValueIndex.Get(expected.Value)
            .WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Equal("key-pending", requestedKey);
        Assert.Same(expected, created);
        Assert.Same(expected, indexedByValue);
        CacheValue visible = await Assert.Single(fixture.Cache.GetAll())
            .WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.Same(expected, visible);
        Assert.Equal(1, fixture.Cache.Statistics.Count);
        Assert.Equal(1, fixture.Cache.Statistics.TotalCount);
        Assert.Equal(1, fixture.Cache.Statistics.Misses);
        Assert.Equal(1, fixture.Cache.Statistics.Hits);

        Assert.True(fixture.IdIndex.Remove(expected.Id));
        await fixture.Observer.Removed.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Empty(fixture.Cache.GetAll());
        Assert.Equal(0, fixture.Cache.Statistics.Count);
        Assert.Equal(1, fixture.Cache.Statistics.TotalCount);
        Assert.Equal(1, fixture.Observer.RemovedCount);
    }

    private static CacheFixture CreateFixture(int expectedAdditions, int expectedRemovals = 0)
    {
        var settings = new TestCacheSettings(
            capacity: ValueCount,
            minAge: TimeSpan.FromSeconds(1),
            maxAge: TimeSpan.FromMinutes(1))
        {
            CurrentTime = DateTime.UnixEpoch,
            BucketCount = 1,
        };
        var cache = new GreenCache<CacheValue>(settings);
        IIndex<string, CacheValue> idIndex = cache.AddIndex("id", value => value.Id);
        IIndex<string, CacheValue> valueIndex = cache.AddIndex("value", value => value.Value);
        var observer = new CacheObserver(expectedAdditions, expectedRemovals);
        ConnectHandle connection = cache.Connect(observer);

        return new CacheFixture(cache, idIndex, valueIndex, observer, connection);
    }

    private static async Task AddValues(IIndex<string, CacheValue> index, int start)
    {
        for (var offset = 0; offset < ValueCount; offset++)
        {
            string key = $"key-{start + offset}";
            CacheValue actual = await index.Get(key, requestedKey =>
                Task.FromResult(new CacheValue(requestedKey, $"The key is {requestedKey}")));

            Assert.Equal(key, actual.Id);
            Assert.Equal($"The key is {key}", actual.Value);
        }
    }

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private sealed record CacheValue(string Id, string Value);

    private sealed class ControlledFactory
    {
        private readonly TaskCompletionSource<string> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<CacheValue> _value =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<string> Started => _started.Task;

        public Task<CacheValue> Create(string key)
        {
            _started.TrySetResult(key);
            return _value.Task;
        }

        public void Complete(CacheValue value) => _value.TrySetResult(value);
    }

    private sealed class CacheFixture(
        GreenCache<CacheValue> cache,
        IIndex<string, CacheValue> idIndex,
        IIndex<string, CacheValue> valueIndex,
        CacheObserver observer,
        ConnectHandle connection) : IDisposable
    {
        public GreenCache<CacheValue> Cache { get; } = cache;

        public IIndex<string, CacheValue> IdIndex { get; } = idIndex;

        public IIndex<string, CacheValue> ValueIndex { get; } = valueIndex;

        public CacheObserver Observer { get; } = observer;

        public void Dispose()
        {
            connection.Dispose();
            Observer.Dispose();
        }
    }

    private sealed class CacheObserver(int expectedAdditions, int expectedRemovals) :
        ICacheValueObserver<CacheValue>,
        IDisposable
    {
        private readonly SemaphoreSlim _addedSignal = new(initialCount: 0);
        private readonly TaskCompletionSource<(INode<CacheValue> Node, CacheValue Value)> _removed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _addedCount;
        private int _clearedCount;
        private int _removedCount;

        public int AddedCount => Volatile.Read(ref _addedCount);

        public int ClearedCount => Volatile.Read(ref _clearedCount);

        public Task<(INode<CacheValue> Node, CacheValue Value)> Removed => _removed.Task;

        public int RemovedCount => Volatile.Read(ref _removedCount);

        public void ValueAdded(INode<CacheValue> node, CacheValue value)
        {
            Interlocked.Increment(ref _addedCount);
            _addedSignal.Release();
        }

        public void ValueRemoved(INode<CacheValue> node, CacheValue value)
        {
            int removedCount = Interlocked.Increment(ref _removedCount);
            if (removedCount >= expectedRemovals)
                _removed.TrySetResult((node, value));
        }

        public void CacheCleared() => Interlocked.Increment(ref _clearedCount);

        public void Dispose() => _addedSignal.Dispose();

        public async Task WaitForAddedCount(
            int expectedCount,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            Assert.InRange(expectedCount, 0, expectedAdditions);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            while (Volatile.Read(ref _addedCount) < expectedCount)
                await _addedSignal.WaitAsync(timeoutSource.Token);
        }
    }
}
