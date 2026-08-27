using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Caching.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Caching;
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

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "queued-cleanup-signal-is-retained")]
    public void BurstWhileCleanupIsQueued_ConvergesWithoutAnotherCacheOperation()
    {
        TestCacheSettings settings = CreateSettings(maximumAgeSeconds: 60);
        var pendingCleanup = new Queue<Action>();
        GreenCache<CacheValue> cache = GreenCacheTestFactory.Create<CacheValue>(
            settings,
            cleanup =>
            {
                pendingCleanup.Enqueue(cleanup);
                return true;
            });
        var observer = new CacheEventObserver<CacheValue>(
            expectedAdded: Capacity * 2,
            expectedRemoved: Capacity - cache.Statistics.BucketSize);
        using ConnectHandle connection = cache.Connect(observer);

        for (var indexValue = 0; indexValue < Capacity * 2; indexValue++)
        {
            if (indexValue % 2 == 0)
                settings.CurrentTime += TimeSpan.FromSeconds(1);

            cache.Add(new CacheValue($"key-{indexValue}", $"The key is key-{indexValue}"));
        }

        Assert.Single(pendingCleanup);

        Assert.True(pendingCleanup.TryDequeue(out Action? firstCleanup));
        firstCleanup();
        Assert.NotEmpty(pendingCleanup);

        DrainPendingCleanup(pendingCleanup);

        Assert.Empty(pendingCleanup);
        AssertBoundedAndNonEmpty(cache, observer, Capacity * 2);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "queued-cleanup-ring-preserves-live-values")]
    public async Task BurstBeyondBucketRingWhileCleanupIsQueued_PreservesEveryLiveValue()
    {
        var settings = new TestCacheSettings(
            capacity: 1,
            minAge: TimeSpan.FromMinutes(1),
            maxAge: TimeSpan.FromMinutes(5))
        {
            BucketCount = 1,
            CurrentTime = DateTime.UnixEpoch,
            TimeSlots = 1,
        };
        var pendingCleanup = new Queue<Action>();
        GreenCache<CacheValue> cache = GreenCacheTestFactory.Create<CacheValue>(
            settings,
            cleanup =>
            {
                pendingCleanup.Enqueue(cleanup);
                return true;
            });
        IIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        CacheValue[] values = Enumerable.Range(0, 32)
            .Select(indexValue => new CacheValue($"key-{indexValue}", $"The key is key-{indexValue}"))
            .ToArray();

        foreach (CacheValue value in values)
            cache.Add(value);

        Assert.Single(pendingCleanup);
        Assert.Equal(values.Length, cache.Statistics.Count);
        Assert.Equal(values.Length, cache.GetAll().Count());

        DrainPendingCleanup(pendingCleanup);

        Assert.Empty(pendingCleanup);
        Assert.Equal(values.Length, cache.Statistics.Count);
        Assert.Equal(cache.Statistics.Count, cache.GetAll().Count());
        foreach (CacheValue value in values)
            Assert.Same(value, await index.Get(value.Id));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "scheduler-rejection-falls-back-inline")]
    public void RejectedCleanupScheduling_DoesNotStrandTheReservation(
        int rejectedPass,
        bool throwInsteadOfRejecting)
    {
        TestCacheSettings settings = CreateSettings(maximumAgeSeconds: 60);
        var pendingCleanup = new Queue<Action>();
        var schedulingPasses = 0;
        GreenCache<CacheValue> cache = GreenCacheTestFactory.Create<CacheValue>(
            settings,
            cleanup =>
            {
                int pass = ++schedulingPasses;
                if (pass == rejectedPass)
                {
                    if (throwInsteadOfRejecting)
                        throw new InvalidOperationException("The scheduler rejected the cleanup.");

                    return false;
                }

                pendingCleanup.Enqueue(cleanup);
                return true;
            });
        var observer = new CacheEventObserver<CacheValue>(
            expectedAdded: Capacity * 2,
            expectedRemoved: Capacity - cache.Statistics.BucketSize);
        using ConnectHandle connection = cache.Connect(observer);

        for (var indexValue = 0; indexValue < Capacity * 2; indexValue++)
        {
            if (indexValue % 2 == 0)
                settings.CurrentTime += TimeSpan.FromSeconds(1);

            cache.Add(new CacheValue($"key-{indexValue}", $"The key is key-{indexValue}"));
        }

        DrainPendingCleanup(pendingCleanup);

        Assert.True(schedulingPasses >= rejectedPass);
        Assert.Empty(pendingCleanup);
        AssertBoundedAndNonEmpty(cache, observer, Capacity * 2);
        Assert.Equal(cache.Statistics.Count, cache.GetAll().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "scheduler-invoke-then-throw-is-idempotent")]
    public void SchedulerThatInvokesThenThrows_CannotExecuteTheReservationTwice()
    {
        var settings = new TestCacheSettings(
            capacity: 1,
            minAge: TimeSpan.FromMinutes(1),
            maxAge: TimeSpan.FromMinutes(5))
        {
            CurrentTime = DateTime.UnixEpoch,
        };
        GreenCache<CacheValue> cache = null!;
        var bucketIndexAfterSchedulerInvocation = -1;
        cache = GreenCacheTestFactory.Create<CacheValue>(
            settings,
            cleanup =>
            {
                cleanup();
                bucketIndexAfterSchedulerInvocation = cache.Statistics.CurrentBucketIndex;
                throw new InvalidOperationException("The scheduler threw after invoking the callback.");
            });

        cache.Add(new CacheValue("key-0", "The key is key-0"));
        cache.Add(new CacheValue("key-1", "The key is key-1"));

        Assert.True(bucketIndexAfterSchedulerInvocation >= 0);
        Assert.Equal(bucketIndexAfterSchedulerInvocation, cache.Statistics.CurrentBucketIndex);
        Assert.Equal(2, cache.Statistics.Count);
        Assert.Equal(2, cache.GetAll().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "follow-up-handoff-remains-single-flight")]
    public void ReentrantBurstDuringFollowUpHandoff_RemainsSingleFlight()
    {
        var settings = new TestCacheSettings(
            capacity: 4,
            minAge: TimeSpan.FromMinutes(1),
            maxAge: TimeSpan.FromMinutes(5))
        {
            BucketCount = 1,
            CurrentTime = DateTime.UnixEpoch,
            TimeSlots = 1,
        };
        var pendingCleanup = new Queue<Action>();
        GreenCache<CacheValue> cache = null!;
        var schedulingPasses = 0;
        cache = GreenCacheTestFactory.Create<CacheValue>(
            settings,
            cleanup =>
            {
                int pass = ++schedulingPasses;
                if (pass == 2)
                {
                    for (var indexValue = 10; indexValue < 15; indexValue++)
                        cache.Add(new CacheValue($"key-{indexValue}", $"The key is key-{indexValue}"));
                }

                pendingCleanup.Enqueue(cleanup);
                return true;
            });

        for (var indexValue = 0; indexValue < 10; indexValue++)
            cache.Add(new CacheValue($"key-{indexValue}", $"The key is key-{indexValue}"));

        Assert.Single(pendingCleanup);
        Assert.True(pendingCleanup.TryDequeue(out Action? firstCleanup));
        firstCleanup();

        Assert.Equal(2, schedulingPasses);
        Assert.Single(pendingCleanup);

        DrainPendingCleanup(pendingCleanup);

        Assert.Empty(pendingCleanup);
        Assert.Equal(15, cache.Statistics.Count);
        Assert.Equal(cache.Statistics.Count, cache.GetAll().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "observer-failure-does-not-strand-cleanup")]
    public void ObserverFailureAfterReservation_StillDispatchesTheCleanup()
    {
        var settings = new TestCacheSettings(
            capacity: 1,
            minAge: TimeSpan.FromMinutes(1),
            maxAge: TimeSpan.FromMinutes(5))
        {
            CurrentTime = DateTime.UnixEpoch,
        };
        var pendingCleanup = new Queue<Action>();
        GreenCache<CacheValue> cache = GreenCacheTestFactory.Create<CacheValue>(
            settings,
            cleanup =>
            {
                pendingCleanup.Enqueue(cleanup);
                return true;
            });
        cache.Add(new CacheValue("key-0", "The key is key-0"));
        ConnectHandle connection = cache.Connect(new ThrowingCacheObserver<CacheValue>(throwOnAdded: true));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => cache.Add(new CacheValue("key-1", "The key is key-1")));
        connection.Disconnect();

        Assert.Equal("Observer failure.", exception.Message);
        Assert.Single(pendingCleanup);
        DrainPendingCleanup(pendingCleanup);
        Assert.Empty(pendingCleanup);
        Assert.Equal(cache.Statistics.Count, cache.GetAll().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "removal-observer-failure-does-not-strand-cleanup")]
    public async Task RemovalObserverFailureAfterReservation_StillDispatchesTheCleanup()
    {
        var settings = new TestCacheSettings(
            capacity: 10,
            minAge: TimeSpan.FromMinutes(1),
            maxAge: TimeSpan.FromMinutes(5))
        {
            CurrentTime = DateTime.UnixEpoch,
        };
        Action? pendingCleanup = null;
        var cleanupScheduled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        GreenCache<CacheValue> cache = GreenCacheTestFactory.Create<CacheValue>(
            settings,
            cleanup =>
            {
                pendingCleanup = cleanup;
                cleanupScheduled.TrySetResult();
                return true;
            });
        IIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        cache.Add(new CacheValue("key-0", "The key is key-0"));
        using ConnectHandle connection = cache.Connect(
            new ThrowingCacheObserver<CacheValue>(throwOnRemoved: true));
        settings.CurrentTime += TimeSpan.FromMinutes(6);

        Assert.True(index.Remove("key-0"));
        await cleanupScheduled.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.NotNull(pendingCleanup);
        pendingCleanup();
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Empty(cache.GetAll());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-ROLLOVER", "reset-precedes-current-add")]
    public async Task ValueAddedAtTheResetBoundary_RemainsVisibleAndIndexed()
    {
        TestCacheSettings settings = CreateSettings(maximumAgeSeconds: 300);
        var cache = new GreenCache<CacheValue>(settings);
        IIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var expired = new CacheValue("expired", "The expired value");
        var current = new CacheValue("current", "The current value");
        cache.Add(expired);

        settings.CurrentTime += TimeSpan.FromHours(24);
        cache.Add(current);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.Get(expired.Id));
        Assert.Same(current, await index.Get(current.Id));
        Assert.Equal(1, cache.Statistics.Count);
        Task<CacheValue> visible = Assert.Single(cache.GetAll());
        Assert.Same(current, await visible);
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

    private static void DrainPendingCleanup(Queue<Action> pendingCleanup)
    {
        var executedCleanups = 0;
        while (pendingCleanup.TryDequeue(out Action? cleanup))
        {
            Assert.True(++executedCleanups <= 8, "The single-flight cleanup did not converge.");
            cleanup();
        }
    }

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

    private sealed class ThrowingCacheObserver<TValue>(bool throwOnAdded = false, bool throwOnRemoved = false) :
        ICacheValueObserver<TValue>
        where TValue : class
    {
        public void ValueAdded(INode<TValue> node, TValue value)
        {
            if (throwOnAdded)
                throw new InvalidOperationException("Observer failure.");
        }

        public void ValueRemoved(INode<TValue> node, TValue value)
        {
            if (throwOnRemoved)
                throw new InvalidOperationException("Observer failure.");
        }

        public void CacheCleared()
        {
        }
    }
}
