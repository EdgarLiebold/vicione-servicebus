using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

// Observer/disposal cancellation and the no-token overloads are part of the cache contract exercised here;
// every potentially blocking assertion is independently bounded by OperationTimeout.
namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCacheObserverAndDisposalTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-OBSERVER", "add-fanout-completes-before-rethrow")]
    public async Task AddedObserverFailure_IsIsolatedAfterAtomicCommitAndDoesNotSkipLaterObserversAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var expectedFailure = new ObserverException("add failed");
        var faulting = new DelegateObserver(onAdded: (_, _) => ValueTask.FromException(expectedFailure));
        var recording = new RecordingObserver();
        using ConnectHandle firstConnection = cache.Connect(faulting);
        using ConnectHandle secondConnection = cache.Connect(recording);
        var expected = new Resource("one");

        await cache.AddAsync(expected, TestContext.Current.CancellationToken);

        Assert.Same(expected, await index.GetAsync("one", TestContext.Current.CancellationToken));
        Assert.Equal(["add:one"], recording.Events);
        Assert.Equal(1, cache.Statistics.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-OBSERVER", "remove-fanout-and-disposal-complete")]
    public async Task RemovedObserverFailure_DoesNotSkipLaterObserversOrResourceDisposalAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var expected = new Resource("one");
        await cache.AddAsync(expected, TestContext.Current.CancellationToken);
        var faulting = new DelegateObserver(onRemoved: (_, _) => ValueTask.FromException(new ObserverException("remove failed")));
        var recording = new RecordingObserver();
        using ConnectHandle firstConnection = cache.Connect(faulting);
        using ConnectHandle secondConnection = cache.Connect(recording);

        Assert.True(await index.RemoveAsync("one", TestContext.Current.CancellationToken));

        Assert.Equal(1, expected.DisposeCount);
        Assert.Equal(["remove:one"], recording.Events);
        Assert.Equal(0, cache.Statistics.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-OBSERVER", "clear-fanout-completes-before-rethrow")]
    public async Task ClearedObserverFailure_DoesNotSkipLaterObserversOrLeaveIndexedStateAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        await cache.AddAsync(new Resource("one"), TestContext.Current.CancellationToken);
        var faulting = new DelegateObserver(onCleared: _ => ValueTask.FromException(new ObserverException("clear failed")));
        var recording = new RecordingObserver();
        using ConnectHandle firstConnection = cache.Connect(faulting);
        using ConnectHandle secondConnection = cache.Connect(recording);

        await cache.ClearAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["clear"], recording.Events);
        Assert.Empty(await cache.GetValuesAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("one", TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-DISPOSAL", "clear-and-rollover-are-asynchronous")]
    public async Task ClearAsync_AwaitsAsynchronousDisposalWithoutBlockingTheCallingThreadAsync()
    {
        await using var cache = CreateCache();
        var disposal = new AsyncDisposalProbe("one");
        await cache.AddAsync(disposal, TestContext.Current.CancellationToken);

        Task clear = cache.ClearAsync(TestContext.Current.CancellationToken).AsTask();
        await disposal.Started.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.False(clear.IsCompleted);
        Assert.Equal(0, cache.Statistics.Count);
        disposal.Release();
        await clear.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(1, disposal.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "observer-failure-does-not-strand-cleanup")]
    public async Task AddedObserverFailure_DoesNotStrandCapacityEvictionAsync()
    {
        await using var cache = CreateCache(capacity: 1);
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        using ConnectHandle connection = cache.Connect(new DelegateObserver(
            onAdded: (_, _) => ValueTask.FromException(new ObserverException("add failed"))));

        await cache.AddAsync(new Resource("one"), TestContext.Current.CancellationToken);
        await cache.AddAsync(new Resource("two"), TestContext.Current.CancellationToken);

        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(1, cache.Statistics.Evictions);
        Assert.Equal("two", (await index.GetAsync("two", TestContext.Current.CancellationToken)).Id);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("one", TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "removal-observer-failure-does-not-strand-cleanup")]
    public async Task RemovedObserverFailure_DoesNotStrandCapacityEvictionOrDisposalAsync()
    {
        await using var cache = CreateCache(capacity: 1);
        var first = new Resource("one");
        await cache.AddAsync(first, TestContext.Current.CancellationToken);
        using ConnectHandle connection = cache.Connect(new DelegateObserver(
            onRemoved: (_, _) => ValueTask.FromException(new ObserverException("remove failed"))));

        await cache.AddAsync(new Resource("two"), TestContext.Current.CancellationToken);

        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(1, cache.Statistics.Evictions);
    }

    [Fact]
    public async Task GetOrAddAsync_DoesNotCompleteUntilTheAddedObserverHasFinishedAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var observer = new BlockingObserver();
        using ConnectHandle connection = cache.Connect(observer);

        Task<Resource> creation = index.GetOrAddAsync(
            "one",
            (key, _) => ValueTask.FromResult(new Resource(key)),
            TestContext.Current.CancellationToken).AsTask();
        await observer.AddedStarted.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.False(creation.IsCompleted);
        observer.ReleaseAdded();
        Resource created = await creation.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal("one", created.Id);
    }

    [Fact]
    public async Task ObserverCallbacks_AreSerializedAcrossConcurrentCommitsAsync()
    {
        await using var cache = CreateCache();
        var observer = new ConcurrencyObserver(expectedCalls: 16);
        using ConnectHandle connection = cache.Connect(observer);

        Task[] additions = Enumerable.Range(0, 16)
            .Select(index => cache.AddAsync(new Resource($"item-{index}"), TestContext.Current.CancellationToken).AsTask())
            .ToArray();
        await observer.AllEntered.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await Task.WhenAll(additions).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(1, observer.MaximumConcurrency);
        Assert.Equal(16, observer.CallCount);
    }

    [Fact]
    public async Task ObserverReentry_IsRejectedBeforeItCanMutateTheCacheAsync()
    {
        await using var cache = CreateCache();
        InvalidOperationException? observed = null;
        using ConnectHandle connection = cache.Connect(new DelegateObserver(onAdded: async (_, _) =>
        {
            observed = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await cache.AddAsync(new Resource("reentrant"), TestContext.Current.CancellationToken));
        }));

        await cache.AddAsync(new Resource("original"), TestContext.Current.CancellationToken);

        Assert.NotNull(observed);
        Assert.Contains("must not re-enter", observed.Message, StringComparison.Ordinal);
        Assert.Equal(["original"], (await cache.GetValuesAsync(TestContext.Current.CancellationToken)).Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task Disconnect_StopsFutureNotificationsWithoutChangingCommittedStateAsync()
    {
        await using var cache = CreateCache();
        var observer = new RecordingObserver();
        ConnectHandle connection = cache.Connect(observer);
        await cache.AddAsync(new Resource("one"), TestContext.Current.CancellationToken);

        connection.Disconnect();
        connection.Disconnect();
        await cache.AddAsync(new Resource("two"), TestContext.Current.CancellationToken);

        Assert.Equal(["add:one"], observer.Events);
        Assert.Equal(2, cache.Statistics.Count);
    }

    [Fact]
    public async Task Eviction_AwaitsAsynchronousDisposalAndReleasesTheResourceExactlyOnceAsync()
    {
        await using var cache = CreateCache(capacity: 1);
        var disposal = new AsyncDisposalProbe("one");
        await cache.AddAsync(disposal, TestContext.Current.CancellationToken);

        Task addReplacement = cache.AddAsync(new Resource("two"), TestContext.Current.CancellationToken).AsTask();
        await disposal.Started.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.False(addReplacement.IsCompleted);
        Assert.Equal(1, cache.Statistics.Count);
        disposal.Release();
        await addReplacement.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(1, disposal.DisposeCount);
    }

    [Fact]
    public async Task DisposeAsync_ReleasesEveryOwnedResourceExactlyOnceAndRejectsFurtherOperationsAsync()
    {
        var cache = CreateCache();
        var first = new Resource("one");
        var second = new Resource("two");
        await cache.AddAsync(first, TestContext.Current.CancellationToken);
        await cache.AddAsync(second, TestContext.Current.CancellationToken);

        await cache.DisposeAsync();
        await cache.DisposeAsync();

        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => cache.AddIndex("late", value => value.Id));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await cache.GetValuesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DisposalFailure_DoesNotPreventRemainingResourcesFromBeingReleasedAsync()
    {
        var cache = CreateCache();
        var faulting = new FaultingDisposableResource("faulting");
        var healthy = new Resource("healthy");
        await cache.AddAsync(faulting, TestContext.Current.CancellationToken);
        await cache.AddAsync(healthy, TestContext.Current.CancellationToken);

        await cache.DisposeAsync();

        Assert.Equal(1, faulting.DisposeCount);
        Assert.Equal(1, healthy.DisposeCount);
    }

    private static ResourceCache<T> CreateCache<T>(int capacity = 32)
        where T : class => new(new ResourceCacheOptions(
        capacity,
        minAge: TimeSpan.Zero,
        maxAge: TimeSpan.FromMinutes(30),
        cleanupInterval: TimeSpan.FromHours(1)));

    private static ResourceCache<Resource> CreateCache(int capacity = 32) => CreateCache<Resource>(capacity);

    private static TimeSpan OperationTimeout => TimeSpan.FromSeconds(10);

    private class Resource(string id) : IAsyncDisposable
    {
        private int _disposeCount;

        public string Id { get; } = id;
        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public virtual ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return default;
        }
    }

    private sealed class AsyncDisposalProbe(string id) : Resource(id)
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public override async ValueTask DisposeAsync()
        {
            _started.TrySetResult();
            await _release.Task;
            await base.DisposeAsync();
        }

        public void Release() => _release.TrySetResult();
    }

    private sealed class FaultingDisposableResource(string id) : Resource(id)
    {
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            throw new DisposalException("dispose failed");
        }
    }

    private sealed class RecordingObserver : IResourceCacheObserver<Resource>
    {
        private readonly List<string> _events = [];

        public IReadOnlyList<string> Events => _events;

        public ValueTask ResourceAddedAsync(Resource value, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); _events.Add($"add:{value.Id}");
            return default;
        }

        public ValueTask ResourceRemovedAsync(Resource value, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); _events.Add($"remove:{value.Id}");
            return default;
        }

        public ValueTask CacheClearedAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); _events.Add("clear");
            return default;
        }
    }

    private sealed class DelegateObserver(
        Func<Resource, CancellationToken, ValueTask>? onAdded = null,
        Func<Resource, CancellationToken, ValueTask>? onRemoved = null,
        Func<CancellationToken, ValueTask>? onCleared = null) : IResourceCacheObserver<Resource>
    {
        public ValueTask ResourceAddedAsync(Resource value, CancellationToken cancellationToken) =>
            onAdded?.Invoke(value, cancellationToken) ?? default;

        public ValueTask ResourceRemovedAsync(Resource value, CancellationToken cancellationToken) =>
            onRemoved?.Invoke(value, cancellationToken) ?? default;

        public ValueTask CacheClearedAsync(CancellationToken cancellationToken) =>
            onCleared?.Invoke(cancellationToken) ?? default;
    }

    private sealed class BlockingObserver : IResourceCacheObserver<Resource>
    {
        private readonly TaskCompletionSource _addedStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseAdded = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task AddedStarted => _addedStarted.Task;

        public async ValueTask ResourceAddedAsync(Resource value, CancellationToken cancellationToken)
        {
            _addedStarted.TrySetResult();
            await _releaseAdded.Task.WaitAsync(cancellationToken);
        }

        public ValueTask ResourceRemovedAsync(Resource value, CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); return default; }
        public ValueTask CacheClearedAsync(CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); return default; }
        public void ReleaseAdded() => _releaseAdded.TrySetResult();
    }

    private sealed class ConcurrencyObserver(int expectedCalls) : IResourceCacheObserver<Resource>
    {
        private readonly TaskCompletionSource _allEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callCount;
        private int _concurrency;
        private int _maximumConcurrency;

        public Task AllEntered => _allEntered.Task;
        public int CallCount => Volatile.Read(ref _callCount);
        public int MaximumConcurrency => Volatile.Read(ref _maximumConcurrency);

        public async ValueTask ResourceAddedAsync(Resource value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); int concurrent = Interlocked.Increment(ref _concurrency);
            UpdateMaximum(concurrent);
            await Task.Yield();
            if (Interlocked.Increment(ref _callCount) == expectedCalls)
                _allEntered.TrySetResult();
            Interlocked.Decrement(ref _concurrency);
        }

        public ValueTask ResourceRemovedAsync(Resource value, CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); return default; }
        public ValueTask CacheClearedAsync(CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); return default; }
        private void UpdateMaximum(int candidate)
        {
            int observed;
            do
            {
                observed = Volatile.Read(ref _maximumConcurrency);
                if (candidate <= observed)
                    return;
            }
            while (Interlocked.CompareExchange(ref _maximumConcurrency, candidate, observed) != observed);
        }
    }

    private sealed class ObserverException(string message) : Exception(message);
    private sealed class DisposalException(string message) : Exception(message);
}
