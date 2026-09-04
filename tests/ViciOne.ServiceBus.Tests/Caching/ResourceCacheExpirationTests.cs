using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

// The no-token overloads are part of the cache contract exercised here; asynchronous
// coordination points use the xUnit cancellation token or an explicit bounded timeout.
namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCacheExpirationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-RETENTION", "within-capacity-and-age")]
    public async Task ValuesAtCapacity_RemainWhenNoneAreExpiredAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(3, time, maxAge: TimeSpan.FromMinutes(10));
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);

        await AddAsync(cache, "one", "two", "three");
        time.Advance(TimeSpan.FromMinutes(9));
        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, cache.Statistics.Count);
        Assert.Equal(0, cache.Statistics.Evictions);
        Assert.Equal(["one", "three", "two"], cache.GetValues(TestContext.Current.CancellationToken).Select(x => x.Id).Order().ToArray());
        Assert.Equal("one", (await index.GetAsync("one", TestContext.Current.CancellationToken)).Id);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-EXPIRATION", "expired-values")]
    public async Task CleanupExpiredAsync_RemovesExpiredValuesBeforeCapacityRequiresEvictionAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(8, time, maxAge: TimeSpan.FromMinutes(1));
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var expired = new Resource("expired");
        await cache.AddAsync(expired, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(1) + TimeSpan.FromTicks(1));

        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, expired.DisposeCount);
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Equal(1, cache.Statistics.Evictions);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("expired", TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "simple-values")]
    public async Task HardCapacity_EvictsTheLeastRecentlyUsedCommittedValueAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(3, time);
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var one = new Resource("one");
        var two = new Resource("two");
        var three = new Resource("three");
        await cache.AddAsync(one, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));
        await cache.AddAsync(two, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));
        await cache.AddAsync(three, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Same(one, await index.GetAsync("one", TestContext.Current.CancellationToken));

        var four = new Resource("four");
        await cache.AddAsync(four, TestContext.Current.CancellationToken);

        Assert.Equal(3, cache.Statistics.Count);
        Assert.Equal(1, cache.Statistics.Evictions);
        Assert.Equal(1, two.DisposeCount);
        Assert.Equal(0, one.DisposeCount);
        Assert.Equal(0, three.DisposeCount);
        Assert.Equal(0, four.DisposeCount);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("two", TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "usage-aware-values")]
    public async Task UsageNotification_RefreshesSlidingRetentionWithoutACacheLookupAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(2, time);
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var first = new Resource("first");
        var second = new Resource("second");
        await cache.AddAsync(first, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));
        await cache.AddAsync(second, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));

        first.Use();
        await cache.AddAsync(new Resource("third"), TestContext.Current.CancellationToken);

        Assert.Same(first, await index.GetAsync("first", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("second", TestContext.Current.CancellationToken));
        Assert.Equal(1, second.DisposeCount);
        Assert.Equal(2, cache.Statistics.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-BUCKET", "push-links-node")]
    public async Task CacheHit_MakesTheEntryMostRecentlyUsedForTheNextEvictionAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(2, time);
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        await cache.AddAsync(new Resource("first"), TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));
        await cache.AddAsync(new Resource("second"), TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));

        await index.GetAsync("first", TestContext.Current.CancellationToken);
        await cache.AddAsync(new Resource("third"), TestContext.Current.CancellationToken);

        Assert.Equal(["first", "third"], cache.GetValues(TestContext.Current.CancellationToken).Select(x => x.Id).Order().ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-BUCKET", "used-callback-cannot-decrement-reused-generation")]
    public async Task UsageFromAnEvictedGeneration_CannotRefreshItsReplacementAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(1, time);
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var oldGeneration = new Resource("same");
        await cache.AddAsync(oldGeneration, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));
        await cache.AddAsync(new Resource("replacement"), TestContext.Current.CancellationToken);
        var newGeneration = new Resource("same");
        time.Advance(TimeSpan.FromSeconds(1));
        await cache.AddAsync(newGeneration, TestContext.Current.CancellationToken);

        oldGeneration.Use();
        time.Advance(TimeSpan.FromSeconds(1));
        await cache.AddAsync(new Resource("other"), TestContext.Current.CancellationToken);

        Assert.Equal(1, oldGeneration.DisposeCount);
        Assert.Equal(1, newGeneration.DisposeCount);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("same", TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-BUCKET", "rebucket-transfers-source-count-once")]
    public async Task HighChurn_AccountsForEachCapacityEvictionExactlyOnceAsync()
    {
        const int capacity = 8;
        const int additions = 200;
        var time = NewClock();
        await using var cache = CreateCache(capacity, time);

        for (var index = 0; index < additions; index++)
        {
            await cache.AddAsync(new Resource($"item-{index:D3}"), TestContext.Current.CancellationToken);
            time.Advance(TimeSpan.FromTicks(1));
        }

        Assert.Equal(capacity, cache.Statistics.Count);
        Assert.Equal(additions, cache.Statistics.TotalCreated);
        Assert.Equal(additions - capacity, cache.Statistics.Evictions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-NODE-VISIBILITY", "post-eviction-state-is-removed")]
    public async Task EvictedValue_IsImmediatelyAbsentFromIndexAndVisibleStateAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(1, time);
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var evicted = new Resource("old");
        await cache.AddAsync(evicted, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromTicks(1));

        await cache.AddAsync(new Resource("new"), TestContext.Current.CancellationToken);

        Assert.Equal(1, evicted.DisposeCount);
        Assert.DoesNotContain(cache.GetValues(TestContext.Current.CancellationToken), value => value.Id == "old");
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("old", TestContext.Current.CancellationToken));
        Assert.Equal("new", (await index.GetAsync("new", TestContext.Current.CancellationToken)).Id);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-RETENTION", "stalled-current-bucket-honors-minimum-age")]
    public async Task MinimumAge_PreventsTimeExpirationUntilBothAgeBoundsAreSatisfiedAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(
            2,
            time,
            minAge: TimeSpan.FromMinutes(2),
            maxAge: TimeSpan.FromMinutes(2),
            expirationMode: ResourceCacheExpirationMode.Absolute);
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        await cache.AddAsync(new Resource("one"), TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromMinutes(2));
        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);
        Assert.Equal("one", (await index.GetAsync("one", TestContext.Current.CancellationToken)).Id);

        time.Advance(TimeSpan.FromTicks(1));
        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("one", TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-ROLLOVER", "reset-precedes-current-add")]
    public async Task ExactExpirationBoundary_RemainsVisibleUntilTimeMovesBeyondTheBoundaryAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(
            2,
            time,
            maxAge: TimeSpan.FromMinutes(1),
            expirationMode: ResourceCacheExpirationMode.Absolute);
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        await cache.AddAsync(new Resource("boundary"), TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromMinutes(1));
        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);
        Assert.Equal("boundary", (await index.GetAsync("boundary", TestContext.Current.CancellationToken)).Id);

        time.Advance(TimeSpan.FromTicks(1));
        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("boundary", TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "queued-cleanup-signal-is-retained")]
    public async Task PeriodicCleanup_RemovesExpiredValuesWithoutAnotherCacheOperationAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(
            4,
            time,
            maxAge: TimeSpan.FromSeconds(1),
            cleanupInterval: TimeSpan.FromSeconds(1));
        var value = new Resource("expired");
        await cache.AddAsync(value, TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromSeconds(2));

        await value.Disposed.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(1, value.DisposeCount);
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Equal(1, cache.Statistics.Evictions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "queued-cleanup-ring-preserves-live-values")]
    public async Task RepeatedTimedCleanup_PreservesEveryLiveValueAndDoesNotDoubleDisposeExpiredValuesAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(
            4,
            time,
            maxAge: TimeSpan.FromSeconds(2),
            cleanupInterval: TimeSpan.FromSeconds(1));
        var expired = new Resource("expired");
        await cache.AddAsync(expired, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));
        var live = new Resource("live");
        await cache.AddAsync(live, TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromSeconds(2));
        await expired.Disposed.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(1, expired.DisposeCount);
        Assert.Equal(0, live.DisposeCount);
        Assert.Equal(["live"], cache.GetValues(TestContext.Current.CancellationToken).Select(x => x.Id).ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "scheduler-rejection-falls-back-inline")]
    public async Task ExplicitCleanup_RemainsAvailableWhenNoPeriodicTickHasRunAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(
            2,
            time,
            maxAge: TimeSpan.FromSeconds(1),
            cleanupInterval: TimeSpan.FromDays(1));
        var expired = new Resource("expired");
        await cache.AddAsync(expired, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(2));

        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, expired.DisposeCount);
        Assert.Equal(0, cache.Statistics.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "scheduler-invoke-then-throw-is-idempotent")]
    public async Task RepeatedCleanup_IsIdempotentForAlreadyReleasedResourcesAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(2, time, maxAge: TimeSpan.FromSeconds(1));
        var expired = new Resource("expired");
        await cache.AddAsync(expired, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(2));

        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);
        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, expired.DisposeCount);
        Assert.Equal(1, cache.Statistics.Evictions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "follow-up-handoff-remains-single-flight")]
    public async Task CapacityChurn_NeverExceedsTheConfiguredHardBoundAsync()
    {
        const int capacity = 4;
        var time = NewClock();
        await using var cache = CreateCache(capacity, time);

        for (var index = 0; index < 100; index++)
        {
            await cache.AddAsync(new Resource($"item-{index}"), TestContext.Current.CancellationToken);
            Assert.InRange(cache.Statistics.Count + cache.Statistics.PendingCreations, 0, capacity);
            time.Advance(TimeSpan.FromTicks(1));
        }

        Assert.Equal(capacity, cache.Statistics.Count);
        Assert.Equal(96, cache.Statistics.Evictions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-CAPACITY", "single-use-values")]
    public async Task SingleUseValuesAboveCapacity_RetainExactlyTheNewestCapacityAsync()
    {
        const int capacity = 10;
        var time = NewClock();
        await using var cache = CreateCache(capacity, time);

        for (var index = 0; index < 25; index++)
        {
            await cache.AddAsync(new Resource($"item-{index:D2}"), TestContext.Current.CancellationToken);
            time.Advance(TimeSpan.FromTicks(1));
        }

        string[] expected = Enumerable.Range(15, capacity).Select(index => $"item-{index:D2}").ToArray();
        Assert.Equal(expected, cache.GetValues(TestContext.Current.CancellationToken).Select(x => x.Id).Order().ToArray());
        Assert.Equal(capacity, cache.Statistics.Count);
        Assert.Equal(15, cache.Statistics.Evictions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-CAPACITY", "usage-aware-retention")]
    public async Task ReusedValues_AreRetainedAheadOfOlderUnusedValuesAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(3, time);
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        await AddAsync(cache, "one", "two", "three");
        time.Advance(TimeSpan.FromSeconds(1));
        await index.GetAsync("one", TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));
        await index.GetAsync("two", TestContext.Current.CancellationToken);

        await cache.AddAsync(new Resource("four"), TestContext.Current.CancellationToken);

        Assert.Equal(["four", "one", "two"], cache.GetValues(TestContext.Current.CancellationToken).Select(x => x.Id).Order().ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-METRICS", "deterministic-distribution")]
    public async Task RepeatedDeterministicAccess_ReportsExactCountsAndHitRatioAsync()
    {
        const int distinct = 25;
        const int accesses = 500;
        await using var cache = CreateCache(distinct, NewClock());
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);

        for (var operation = 0; operation < accesses; operation++)
        {
            string key = $"item-{operation % distinct:D2}";
            await index.GetOrAddAsync(key, (id, _) => ValueTask.FromResult(new Resource(id)), TestContext.Current.CancellationToken);
        }

        ResourceCacheStatistics statistics = cache.Statistics;
        Assert.Equal(distinct, statistics.Count);
        Assert.Equal(distinct, statistics.TotalCreated);
        Assert.Equal(distinct, statistics.Misses);
        Assert.Equal(accesses - distinct, statistics.Hits);
        Assert.Equal((accesses - distinct) / (double)accesses, statistics.HitRatio, precision: 12);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-TRACKER", "high-churn-eviction")]
    public async Task HighChurn_DisposesEveryValueBeyondTheHardCapacityAsync()
    {
        const int capacity = 7;
        const int additions = 100;
        var resources = new List<Resource>();
        var time = NewClock();
        await using var cache = CreateCache(capacity, time);

        for (var index = 0; index < additions; index++)
        {
            var resource = new Resource($"item-{index}");
            resources.Add(resource);
            await cache.AddAsync(resource, TestContext.Current.CancellationToken);
            time.Advance(TimeSpan.FromTicks(1));
        }

        Assert.Equal(additions - capacity, resources.Count(x => x.DisposeCount == 1));
        Assert.Equal(capacity, resources.Count(x => x.DisposeCount == 0));
        Assert.DoesNotContain(resources, resource => resource.DisposeCount > 1);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-TTL", "sequential-access-within-ttl")]
    public async Task SequentialAccessWithinSlidingLifetime_PreservesTheValueAndExactHitRatioAsync()
    {
        var time = NewClock();
        await using var cache = CreateCache(4, time, maxAge: TimeSpan.FromSeconds(30));
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var calls = 0;

        for (var access = 0; access < 10; access++)
        {
            await index.GetOrAddAsync("one", (key, _) =>
            {
                Interlocked.Increment(ref calls);
                return ValueTask.FromResult(new Resource(key));
            }, TestContext.Current.CancellationToken);
            time.Advance(TimeSpan.FromSeconds(2));
        }

        Assert.Equal(1, calls);
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(9, cache.Statistics.Hits);
        Assert.Equal(1, cache.Statistics.Misses);
        Assert.Equal(0.9, cache.Statistics.HitRatio, precision: 12);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-TTL-CLOCK", "timestamp-frequency")]
    public async Task Expiration_UsesTheConfiguredTimeProviderTimestampFrequencyAsync()
    {
        var time = new FrequencyTimeProvider(1_000);
        await using var cache = new ResourceCache<Resource>(new ResourceCacheOptions(
            capacity: 2,
            maxAge: TimeSpan.FromSeconds(30),
            expirationMode: ResourceCacheExpirationMode.Absolute,
            timeProvider: time,
            cleanupInterval: TimeSpan.FromDays(1)));
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        await cache.AddAsync(new Resource("one"), TestContext.Current.CancellationToken);

        time.AdvanceTimestamp(30_000);
        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);
        Assert.Equal("one", (await index.GetAsync("one", TestContext.Current.CancellationToken)).Id);

        time.AdvanceTimestamp(1);
        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("one", TestContext.Current.CancellationToken));
    }

    private static FakeTimeProvider NewClock() => new(DateTimeOffset.UnixEpoch);

    private static ResourceCache<Resource> CreateCache(
        int capacity,
        TimeProvider timeProvider,
        TimeSpan? minAge = null,
        TimeSpan? maxAge = null,
        TimeSpan? cleanupInterval = null,
        ResourceCacheExpirationMode expirationMode = ResourceCacheExpirationMode.Sliding) =>
        new(new ResourceCacheOptions(
            capacity,
            minAge ?? TimeSpan.Zero,
            maxAge ?? TimeSpan.FromHours(1),
            expirationMode,
            timeProvider,
            cleanupInterval: cleanupInterval ?? TimeSpan.FromDays(1)));

    private static async Task AddAsync(ResourceCache<Resource> cache, params string[] ids)
    {
        foreach (string id in ids)
            await cache.AddAsync(new Resource(id));
    }

    private static TimeSpan OperationTimeout => TimeSpan.FromSeconds(10);

    private sealed class Resource(string id) : IResourceUsageSource, IAsyncDisposable
    {
        private readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _disposeCount;

        public string Id { get; } = id;
        public int DisposeCount => Volatile.Read(ref _disposeCount);
        public Task Disposed => _disposed.Task;
        public event Action? Used;

        public void Use() => Used?.Invoke();

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            _disposed.TrySetResult();
            return default;
        }
    }

    private sealed class FrequencyTimeProvider(long timestampFrequency) : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency { get; } = timestampFrequency;

        public override long GetTimestamp() => Volatile.Read(ref _timestamp);

        public void AdvanceTimestamp(long timestampDelta) => Interlocked.Add(ref _timestamp, timestampDelta);
    }
}
