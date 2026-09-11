using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

// Disposal cancellation and the no-token overloads are part of the cache contract exercised here;
// every potentially blocking assertion is independently bounded by OperationTimeout.
namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCacheLifecycleTests
{
    [Fact]
    public async Task AbsoluteExpiration_IgnoresCacheHitsAndResourceUsageAsync()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        await using var cache = CreateCache(
            timeProvider: time,
            maxAge: TimeSpan.FromMinutes(1),
            expirationMode: ResourceCacheExpirationMode.Absolute);
        IResourceCacheIndex<string, TrackedResource> index = cache.AddIndex("id", value => value.Id);
        var value = new TrackedResource("one");
        await cache.AddAsync(value, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(40));
        Assert.Same(value, await index.GetAsync("one", TestContext.Current.CancellationToken));
        value.Use();
        time.Advance(TimeSpan.FromSeconds(21));

        await cache.CleanupExpiredAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, value.AsyncDisposeCount);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("one", TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-EXPIRATION", "absolute-mode-does-not-subscribe-to-sliding-usage")]
    public async Task AbsoluteExpiration_DoesNotSubscribeToResourceUsageAsync()
    {
        await using var cache = CreateCache(expirationMode: ResourceCacheExpirationMode.Absolute);
        var value = new TrackedResource("one");

        await cache.AddAsync(value, TestContext.Current.CancellationToken);

        Assert.Equal(0, value.SubscriberCount);
    }

    [Fact]
    public async Task NullFactoryResult_FaultsTheGenerationAndAllowsAHealthyRetryAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, TrackedResource> index = cache.AddIndex("id", value => value.Id);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await index.GetOrAddAsync("one", (_, _) => ValueTask.FromResult<TrackedResource>(null!), TestContext.Current.CancellationToken));

        Assert.Contains("returned null", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, cache.Statistics.CreationFaults);
        Assert.Equal(0, cache.Statistics.PendingCreations);
        var expected = new TrackedResource("one");
        Assert.Same(expected, await index.GetOrAddAsync("one", (_, _) => ValueTask.FromResult(expected), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SecondaryIndexCollision_DisposesOnlyTheRejectedFactoryResourceAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, TrackedResource> idIndex = cache.AddIndex("id", value => value.Id);
        IResourceCacheIndex<string, TrackedResource> groupIndex = cache.AddIndex("group", value => value.Group);
        var existing = new TrackedResource("existing", "shared");
        await cache.AddAsync(existing, TestContext.Current.CancellationToken);
        var rejected = new TrackedResource("new", "shared");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await idIndex.GetOrAddAsync("new", (_, _) => ValueTask.FromResult(rejected), TestContext.Current.CancellationToken));

        Assert.Contains("group", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, rejected.AsyncDisposeCount);
        Assert.Equal(0, existing.AsyncDisposeCount);
        Assert.Same(existing, await idIndex.GetAsync("existing", TestContext.Current.CancellationToken));
        Assert.Same(existing, await groupIndex.GetAsync("shared", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await idIndex.GetAsync("new", TestContext.Current.CancellationToken));
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(1, cache.Statistics.CreationFaults);
    }

    [Fact]
    public async Task ProjectedKeyMismatch_DisposesTheUncommittedFactoryResourceExactlyOnceAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, TrackedResource> index = cache.AddIndex("id", value => value.Id);
        var rejected = new TrackedResource("different");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await index.GetOrAddAsync("requested", (_, _) => ValueTask.FromResult(rejected), TestContext.Current.CancellationToken));

        Assert.Equal(1, rejected.AsyncDisposeCount);
        Assert.Equal(0, rejected.SyncDisposeCount);
        Assert.Equal(0, cache.Statistics.Count);
    }

    [Fact]
    public async Task ExpiredResources_AreReleasedEvenWhenTheIncomingAddIsRejectedByAnotherIndexAsync()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        await using var cache = CreateCache(timeProvider: time, maxAge: TimeSpan.FromMinutes(1));
        IResourceCacheIndex<string, TrackedResource> idIndex = cache.AddIndex("id", value => value.Id);
        cache.AddIndex("group", value => value.Group);
        var expired = new TrackedResource("expired", "expired-group");
        var retained = new TrackedResource("retained", "retained-group");
        await cache.AddAsync(expired, TestContext.Current.CancellationToken);
        await cache.AddAsync(retained, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(30));
        await idIndex.GetAsync("retained", TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(31));
        var rejected = new TrackedResource("retained", "new-group");

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await cache.AddAsync(rejected, TestContext.Current.CancellationToken));

        Assert.Equal(1, expired.AsyncDisposeCount);
        Assert.Equal(0, retained.AsyncDisposeCount);
        Assert.Equal(0, rejected.AsyncDisposeCount);
        Assert.Same(retained, await idIndex.GetAsync("retained", TestContext.Current.CancellationToken));
        Assert.Equal(1, cache.Statistics.Count);
    }

    [Fact]
    public async Task DisposeAsync_CancelsPendingFactoryAndWaitsForOwnershipReleaseAsync()
    {
        var cache = CreateCache();
        IResourceCacheIndex<string, TrackedResource> index = cache.AddIndex("id", value => value.Id);
        var started = NewSignal();
        var factoryExited = NewSignal();

        Task<TrackedResource> pending = index.GetOrAddAsync("one", async (_, ownerToken) =>
            {
                started.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ownerToken);
                    return new TrackedResource("unreachable");
                }
                finally
                {
                    factoryExited.TrySetResult();
                }
            }, TestContext.Current.CancellationToken).AsTask();
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Task dispose = cache.DisposeAsync().AsTask();

        await factoryExited.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await dispose.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.Throws<ObjectDisposedException>(() => cache.AddIndex("late", value => value.Id));
    }

    [Fact]
    public async Task ClearAsync_DisposesAFactoryResultProducedAfterInvalidationAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, TrackedResource> index = cache.AddIndex("id", value => value.Id);
        var started = NewSignal();
        var release = NewSignal<TrackedResource>();
        var produced = new TrackedResource("one");

        Task<TrackedResource> pending = index.GetOrAddAsync("one", async (_, _) =>
            {
                started.TrySetResult();
                return await release.Task;
            }, TestContext.Current.CancellationToken).AsTask();
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task clear = cache.ClearAsync(TestContext.Current.CancellationToken).AsTask();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);

        release.TrySetResult(produced);
        await clear.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(1, produced.AsyncDisposeCount);
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Equal(0, cache.Statistics.PendingCreations);
    }

    [Fact]
    public async Task ResourceImplementingBothDisposalContracts_UsesOnlyAsyncDisposalAsync()
    {
        var cache = CreateCache();
        var value = new TrackedResource("one");
        await cache.AddAsync(value, TestContext.Current.CancellationToken);

        await cache.DisposeAsync();

        Assert.Equal(1, value.AsyncDisposeCount);
        Assert.Equal(0, value.SyncDisposeCount);
    }

    [Fact]
    public async Task AddProjectionFailure_DoesNotTransferOwnershipToTheCacheAsync()
    {
        await using var cache = CreateCache();
        cache.AddIndex<string>("id", value => throw new ProjectionException(value.Id));
        var value = new TrackedResource("one");

        ProjectionException exception = await Assert.ThrowsAsync<ProjectionException>(async () => await cache.AddAsync(value, TestContext.Current.CancellationToken));

        Assert.Equal("one", exception.Message);
        Assert.Equal(0, value.AsyncDisposeCount);
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Equal(0, cache.Statistics.TotalCreated);
    }

    [Fact]
    public async Task KeySelectorReturningNull_FaultsWithoutPublishingPartialStateAsync()
    {
        await using var cache = CreateCache();
        cache.AddIndex<string>("nullable", _ => null!);
        var value = new TrackedResource("one");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await cache.AddAsync(value, TestContext.Current.CancellationToken));

        Assert.Contains("null key", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Equal(0, value.AsyncDisposeCount);
    }

    private static ResourceCache<TrackedResource> CreateCache(
        int capacity = 8,
        TimeProvider? timeProvider = null,
        TimeSpan? maxAge = null,
        ResourceCacheExpirationMode expirationMode = ResourceCacheExpirationMode.Sliding) =>
        new(new ResourceCacheOptions(
            capacity,
            minAge: TimeSpan.Zero,
            maxAge: maxAge ?? TimeSpan.FromMinutes(30),
            expirationMode,
            timeProvider,
            cleanupInterval: TimeSpan.FromHours(1)));

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout => TimeSpan.FromSeconds(10);

    private sealed class TrackedResource(string id, string group = "default") : IResourceUsageSource, IAsyncDisposable, IDisposable
    {
        private int _asyncDisposeCount;
        private int _subscriberCount;
        private int _syncDisposeCount;
        private Action? _used;

        public string Id { get; } = id;
        public string Group { get; } = group;
        public int AsyncDisposeCount => Volatile.Read(ref _asyncDisposeCount);
        public int SubscriberCount => Volatile.Read(ref _subscriberCount);
        public int SyncDisposeCount => Volatile.Read(ref _syncDisposeCount);
        public event Action? Used
        {
            add
            {
                _used += value;
                Interlocked.Increment(ref _subscriberCount);
            }
            remove
            {
                _used -= value;
                Interlocked.Decrement(ref _subscriberCount);
            }
        }

        public void Use() => _used?.Invoke();

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _asyncDisposeCount);
            return default;
        }

        public void Dispose() => Interlocked.Increment(ref _syncDisposeCount);
    }

    private sealed class ProjectionException(string message) : Exception(message);
}
