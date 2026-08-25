using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Caching.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class GreenCacheCapacityTests
{
    private const int Capacity = 100;

    [Theory]
    [InlineData(false, 60)]
    [InlineData(true, 300)]
    [RequirementCoverage("REQ-VSB-CACHE-RETENTION", "within-capacity-and-age")]
    public async Task ValuesAtCapacity_RemainWhenNoneAreExpired(bool advanceClock, int maximumAgeSeconds)
    {
        TestCacheSettings settings = CreateSettings(maximumAgeSeconds);
        var cache = new GreenCache<CacheValue>(settings);
        IIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var observer = new CacheEventObserver<CacheValue>(expectedAdded: Capacity);
        using ConnectHandle connection = cache.Connect(observer);

        for (var indexValue = 0; indexValue < Capacity; indexValue++)
        {
            if (advanceClock)
                settings.CurrentTime += TimeSpan.FromSeconds(1);

            await index.Get($"key-{indexValue}", CreateValue);
        }

        await observer.Added.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Equal(Capacity, observer.AddedCount);
        Assert.Equal(0, observer.RemovedCount);
        Assert.Equal(Capacity, cache.Statistics.Count);
        Assert.Equal(Capacity, cache.GetAll().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-EXPIRATION", "expired-values")]
    public async Task ExpiredValues_AreRemovedEvenBeforeCapacityRequiresIt()
    {
        TestCacheSettings settings = CreateSettings(maximumAgeSeconds: 60);
        var cache = new GreenCache<CacheValue>(settings);
        IIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var observer = new CacheEventObserver<CacheValue>(expectedAdded: Capacity, expectedRemoved: 35);
        using ConnectHandle connection = cache.Connect(observer);

        for (var indexValue = 0; indexValue < Capacity; indexValue++)
        {
            settings.CurrentTime += TimeSpan.FromSeconds(1);
            await index.Get($"key-{indexValue}", CreateValue);
        }

        await observer.Added.WaitAsync(OperationTimeout, TestCancellationToken);
        await observer.Removed.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Equal(Capacity, observer.AddedCount);
        Assert.True(observer.RemovedCount >= 35);
        Assert.InRange(cache.Statistics.Count, 1, Capacity - 1);
        Assert.InRange(cache.GetAll().Count(), 1, Capacity - 1);
    }

    [Theory]
    [InlineData(60, false)]
    [InlineData(300, true)]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "simple-values")]
    public async Task SimpleValuesAboveCapacity_AreReducedToANonEmptyBoundedSet(
        int maximumAgeSeconds,
        bool advanceEveryAddition)
    {
        TestCacheSettings settings = CreateSettings(maximumAgeSeconds);
        var cache = new GreenCache<CacheValue>(settings);
        IIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        const int targetCount = Capacity * 2;
        int maximumBoundedCount = Capacity + cache.Statistics.BucketSize;
        var observer = new CacheEventObserver<CacheValue>(
            expectedAdded: targetCount,
            expectedRemoved: targetCount - maximumBoundedCount);
        using ConnectHandle connection = cache.Connect(observer);

        int added = await FillToCount(
            index,
            CreateValue,
            settings,
            indexValue => advanceEveryAddition || indexValue % 2 == 0,
            startingIndex: 0,
            targetCount: targetCount);

        await observer.WaitForAddedCount(added, OperationTimeout, TestCancellationToken);
        try
        {
            await observer.Removed.WaitAsync(OperationTimeout, TestCancellationToken);
        }
        catch (TimeoutException exception)
        {
            throw new InvalidOperationException(
                $"Cache did not converge: added={observer.AddedCount}, removed={observer.RemovedCount}, "
                + $"count={cache.Statistics.Count}, oldestBucket={cache.Statistics.OldestBucketIndex}, "
                + $"currentBucket={cache.Statistics.CurrentBucketIndex}.", exception);
        }

        AssertBoundedAndNonEmpty(cache, observer, added);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "usage-aware-values")]
    public async Task UsageAwareValuesAboveCapacity_AreReducedToANonEmptyBoundedSet()
    {
        TestCacheSettings settings = CreateSettings(maximumAgeSeconds: 300);
        var cache = new GreenCache<UsageAwareCacheValue>(settings);
        IIndex<string, UsageAwareCacheValue> index = cache.AddIndex("id", value => value.Id);
        int maximumBoundedCount = Capacity + cache.Statistics.BucketSize;
        var observer = new CacheEventObserver<UsageAwareCacheValue>(
            expectedAdded: 200,
            expectedRemoved: 200 - maximumBoundedCount);
        using ConnectHandle connection = cache.Connect(observer);

        UsageAwareCacheValue first = await index.Get("key-0", CreateUsageAwareValue);
        settings.CurrentTime += TimeSpan.FromSeconds(1);

        for (var indexValue = 1; indexValue < Capacity; indexValue++)
        {
            settings.CurrentTime += TimeSpan.FromSeconds(1);
            await index.Get($"key-{indexValue}", CreateUsageAwareValue);
        }

        await observer.WaitForAddedCount(Capacity, OperationTimeout, TestCancellationToken);

        int added = await FillToCount(
            index,
            CreateUsageAwareValue,
            settings,
            ignoredIndex =>
            {
                _ = first.Value;
                return true;
            },
            startingIndex: Capacity,
            targetCount: 200);

        await observer.WaitForAddedCount(added, OperationTimeout, TestCancellationToken);
        await observer.Removed.WaitAsync(OperationTimeout, TestCancellationToken);

        AssertBoundedAndNonEmpty(cache, observer, added);
        Assert.Same(first, await index.Get(first.Id));
    }

    private static TestCacheSettings CreateSettings(int maximumAgeSeconds)
    {
        var settings = new TestCacheSettings(
            Capacity,
            minAge: TimeSpan.FromSeconds(1),
            maxAge: TimeSpan.FromSeconds(maximumAgeSeconds))
        {
            CurrentTime = DateTime.UnixEpoch,
        };

        return settings;
    }

    private static Task<CacheValue> CreateValue(string key) =>
        Task.FromResult(new CacheValue(key, $"The key is {key}"));

    private static Task<UsageAwareCacheValue> CreateUsageAwareValue(string key) =>
        Task.FromResult(new UsageAwareCacheValue(key, $"The key is {key}"));

    private static async Task<int> FillToCount<TValue>(
        IIndex<string, TValue> index,
        MissingValueFactory<string, TValue> factory,
        TestCacheSettings settings,
        Func<int, bool> advanceClock,
        int startingIndex,
        int targetCount)
        where TValue : class
    {
        var added = startingIndex;
        while (added < targetCount)
        {
            if (advanceClock(added))
                settings.CurrentTime += TimeSpan.FromSeconds(1);

            await index.Get($"key-{added}", factory);

            added++;
        }

        return added;
    }

    private static void AssertBoundedAndNonEmpty<TValue>(
        GreenCache<TValue> cache,
        CacheEventObserver<TValue> observer,
        int added)
        where TValue : class
    {
        int count = cache.Statistics.Count;
        int visibleCount = cache.GetAll().Count();
        int maximumBoundedCount = Capacity + cache.Statistics.BucketSize;

        Assert.True(observer.AddedCount >= added);
        Assert.True(observer.RemovedCount >= added - maximumBoundedCount);
        Assert.Equal(0, observer.RemovedWhileValidCount);
        Assert.InRange(count, 1, maximumBoundedCount);
        Assert.InRange(visibleCount, 1, maximumBoundedCount);
    }

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private sealed record CacheValue(string Id, string Value);

    private sealed class UsageAwareCacheValue(string id, string value) : INotifyValueUsed
    {
        public string Id { get; } = id;

        public string Value
        {
            get
            {
                Used?.Invoke();
                return value;
            }
        }

        public event Action? Used;
    }

    private sealed class CacheEventObserver<TValue>(int expectedAdded, int expectedRemoved = 0) :
        ICacheValueObserver<TValue>
        where TValue : class
    {
        private readonly TaskCompletionSource _added = NewCompletionSource(expectedAdded == 0);
        private readonly SemaphoreSlim _addedSignal = new(initialCount: 0);
        private readonly TaskCompletionSource _removed = NewCompletionSource(expectedRemoved == 0);
        private int _addedCount;
        private int _removedCount;
        private int _removedWhileValidCount;

        public Task Added => _added.Task;

        public Task Removed => _removed.Task;

        public int AddedCount => Volatile.Read(ref _addedCount);

        public int RemovedCount => Volatile.Read(ref _removedCount);

        public int RemovedWhileValidCount => Volatile.Read(ref _removedWhileValidCount);

        public void ValueAdded(INode<TValue> node, TValue value)
        {
            int addedCount = Interlocked.Increment(ref _addedCount);
            _addedSignal.Release();

            if (addedCount >= expectedAdded)
                _added.TrySetResult();
        }

        public void ValueRemoved(INode<TValue> node, TValue value)
        {
            if (node.IsValid)
                Interlocked.Increment(ref _removedWhileValidCount);

            if (Interlocked.Increment(ref _removedCount) >= expectedRemoved)
                _removed.TrySetResult();
        }

        public void CacheCleared()
        {
        }

        public async Task WaitForAddedCount(
            int expectedCount,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            while (AddedCount < expectedCount)
                await _addedSignal.WaitAsync(timeoutSource.Token);
        }

        private static TaskCompletionSource NewCompletionSource(bool completed)
        {
            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (completed)
                source.SetResult();

            return source;
        }
    }
}
