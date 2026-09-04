using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

// Caller cancellation and the no-token overloads are part of the cache contract exercised here;
// every potentially blocking assertion is independently bounded by OperationTimeout.
namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCacheGenerationAndLockingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-IDENTITY", "add-and-read-same-instance")]
    public async Task KeyedFacade_ReturnsTheExactCreatedInstanceForLaterReadsAsync()
    {
        await using var cache = new KeyedResourceCache<string, Resource>(
            value => value.Id,
            NewOptions());
        var expected = new Resource("one");

        Resource created = await cache.GetOrAddAsync("one", (_, _) => ValueTask.FromResult(expected), TestContext.Current.CancellationToken);
        Resource read = await cache.GetAsync("one", TestContext.Current.CancellationToken);

        Assert.Same(expected, created);
        Assert.Same(expected, read);
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(1, cache.Statistics.TotalCreated);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-NODE-PROMOTION", "completed-factory-to-bucket-node")]
    public async Task CompletedFactory_IsCommittedBeforeTheAddedObserverReceivesTheExactResourceAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var observer = new CommitObservingObserver(cache);
        using ConnectHandle connection = cache.Connect(observer);
        var expected = new Resource("one");

        Resource actual = await index.GetOrAddAsync("one", (_, _) => ValueTask.FromResult(expected), TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        Assert.Same(expected, observer.ObservedValue);
        Assert.True(observer.WasCommittedWhenObserved);
        Assert.Same(expected, await index.GetAsync("one", TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "plain-read-identity")]
    public async Task CreatedValue_IsReturnedByLaterPlainReadAsTheSameInstanceAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var expected = new Resource("one");

        Resource created = await index.GetOrAddAsync("one", (_, _) => ValueTask.FromResult(expected), TestContext.Current.CancellationToken);
        Resource read = await index.GetAsync("one", TestContext.Current.CancellationToken);

        Assert.Same(expected, created);
        Assert.Same(expected, read);
        Assert.Equal(1, cache.Statistics.Hits);
        Assert.Equal(1, cache.Statistics.Misses);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "post-clear-reuse")]
    public async Task ClearAsync_AllowsEveryFormerKeyToBeCommittedAgainAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, Resource> idIndex = cache.AddIndex("id", value => value.Id);
        IResourceCacheIndex<int, Resource> numberIndex = cache.AddIndex("number", value => value.Number);
        await cache.AddAsync(new Resource("one", 1), TestContext.Current.CancellationToken);
        await cache.ClearAsync(TestContext.Current.CancellationToken);
        var replacement = new Resource("one", 1);

        await cache.AddAsync(replacement, TestContext.Current.CancellationToken);

        Assert.Same(replacement, await idIndex.GetAsync("one", TestContext.Current.CancellationToken));
        Assert.Same(replacement, await numberIndex.GetAsync(1, TestContext.Current.CancellationToken));
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(2, cache.Statistics.TotalCreated);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-LOCK-ORDER", "indexed-read-releases-index-before-node-usage")]
    public async Task IndexedRead_ObtainsTimeOutsideTheStateLockAsync()
    {
        var timeProvider = new CoordinatingTimeProvider();
        await using var cache = CreateCache(timeProvider);
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        await cache.AddAsync(new Resource("one"), TestContext.Current.CancellationToken);
        timeProvider.Arm(() => CompleteConcurrentRead(cache));

        Resource value = await index.GetAsync("one", TestContext.Current.CancellationToken);

        Assert.Equal("one", value.Id);
        Assert.True(timeProvider.CallbackCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-LOCK-ORDER", "time-provider-runs-before-tracker-lock")]
    public async Task CapacityMutation_ObtainsTimeOutsideTheStateLockAsync()
    {
        var timeProvider = new CoordinatingTimeProvider();
        await using var cache = CreateCache(timeProvider);
        cache.AddIndex("id", value => value.Id);
        timeProvider.Arm(() => CompleteConcurrentRead(cache));

        await cache.AddAsync(new Resource("one"), TestContext.Current.CancellationToken);

        Assert.True(timeProvider.CallbackCompleted);
        Assert.Equal(1, cache.Statistics.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-LOCK-ORDER", "usage-detach-runs-after-tracker-unlock")]
    public async Task UsageHandlerDetach_CanCompleteAConcurrentCacheReadWithoutLockInversionAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var value = new CoordinatingUsageResource("one", () => CompleteConcurrentRead(cache));
        await cache.AddAsync(value, TestContext.Current.CancellationToken);

        Assert.True(await index.RemoveAsync("one", TestContext.Current.CancellationToken));

        Assert.True(value.DetachCallbackCompleted);
        Assert.Equal(1, value.DisposeCount);
        Assert.Equal(0, cache.Statistics.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-LOCK-ORDER", "timed-cleanup-obtains-time-before-state-lock")]
    public async Task TimedCleanup_ObtainsTimeOutsideTheStateLockAsync()
    {
        var timeProvider = new CoordinatingTimeProvider();
        await using var cache = CreateCache(timeProvider);
        cache.AddIndex("id", value => value.Id);
        await cache.AddAsync(new Resource("one"), TestContext.Current.CancellationToken);
        timeProvider.Arm(() => CompleteConcurrentRead(cache));

        timeProvider.FireTimer();

        Assert.True(timeProvider.CallbackCompleted);
        Assert.Equal(1, cache.Statistics.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CACHE-LOCK-ORDER", "lifetime-cancellation-callbacks-run-after-state-lock")]
    public async Task LifetimeCancellationCallbacks_CanReadCacheStateWithoutLockInversionAsync(bool disposeCache)
    {
        var cache = CreateCache();
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var started = NewSignal();
        var callbackReadCompleted = 0;

        Task<Resource> pending = index.GetOrAddAsync("one", async (_, ownerToken) =>
            {
                using CancellationTokenRegistration registration = ownerToken.Register(() =>
                {
                    Task read = Task.Run(() => cache.Statistics.Count);
                    if (read.Wait(TimeSpan.FromSeconds(2)))
                        Volatile.Write(ref callbackReadCompleted, 1);
                });
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ownerToken);
                return new Resource("unreachable");
            }, TestContext.Current.CancellationToken).AsTask();
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Task stop = disposeCache
            ? cache.DisposeAsync().AsTask()
            : cache.ClearAsync(TestContext.Current.CancellationToken).AsTask();

        await stop.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(1, Volatile.Read(ref callbackReadCompleted));

        await cache.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-LOCK-ORDER", "concurrent-removal-cannot-leak-usage-subscription")]
    public async Task UsageSubscription_RacingRemovalIsDetachedFromTheReleasedResourceAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var value = new BlockingSubscriptionResource("one");

        Task addition = Task.Run(async () => await cache.AddAsync(value), TestContext.Current.CancellationToken);
        await value.SubscriptionStarted.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.True(await index.RemoveAsync("one", TestContext.Current.CancellationToken));
        Assert.Equal(1, value.DisposeCount);

        value.ReleaseSubscription();
        await addition.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(0, value.SubscriberCount);
        Assert.Equal(0, cache.Statistics.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-EVENT-ORDER", "stale-removal-preserves-new-generation")]
    public async Task ActivityFromRemovedGeneration_CannotAffectReplacementWithTheSameKeyAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var removed = new Resource("same");
        await cache.AddAsync(removed, TestContext.Current.CancellationToken);
        Assert.True(await index.RemoveAsync("same", TestContext.Current.CancellationToken));
        var replacement = new Resource("same");
        await cache.AddAsync(replacement, TestContext.Current.CancellationToken);

        removed.Use();

        Assert.Same(replacement, await index.GetAsync("same", TestContext.Current.CancellationToken));
        Assert.Equal(1, removed.DisposeCount);
        Assert.Equal(0, replacement.DisposeCount);
        Assert.Equal(1, cache.Statistics.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-EVENT-ORDER", "stale-add-cannot-replace-live-generation")]
    public async Task InvalidatedFactoryCompletion_CannotReplaceANewerGenerationWithTheSameKeyAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var started = NewSignal();
        var releaseOld = NewSignal<Resource>();
        var oldValue = new Resource("same");

        Task<Resource> oldCreation = index.GetOrAddAsync("same", async (_, _) =>
            {
                started.TrySetResult();
                return await releaseOld.Task;
            }, TestContext.Current.CancellationToken).AsTask();
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task clear = cache.ClearAsync(TestContext.Current.CancellationToken).AsTask();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => oldCreation);
        var replacement = new Resource("same");
        Resource committed = await index.GetOrAddAsync("same", (_, _) => ValueTask.FromResult(replacement), TestContext.Current.CancellationToken);

        releaseOld.TrySetResult(oldValue);
        await clear.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Same(replacement, committed);
        Assert.Same(replacement, await index.GetAsync("same", TestContext.Current.CancellationToken));
        Assert.Equal(1, oldValue.DisposeCount);
        Assert.Equal(0, replacement.DisposeCount);
        Assert.Equal(1, cache.Statistics.Count);
    }

    private static void CompleteConcurrentRead<T>(ResourceCache<T> cache)
        where T : class
    {
        Task read = Task.Run(async () => await cache.GetValuesAsync());
        read.WaitAsync(OperationTimeout).GetAwaiter().GetResult();
    }

    private static ResourceCache<Resource> CreateCache(TimeProvider? timeProvider = null) =>
        new(NewOptions(timeProvider));

    private static ResourceCacheOptions NewOptions(TimeProvider? timeProvider = null) => new(
        capacity: 8,
        minAge: TimeSpan.Zero,
        maxAge: TimeSpan.FromMinutes(30),
        timeProvider: timeProvider,
        cleanupInterval: TimeSpan.FromHours(1));

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout => TimeSpan.FromSeconds(10);

    private class Resource(string id, int number = 0) : IResourceUsageSource, IAsyncDisposable
    {
        private int _disposeCount;

        public string Id { get; } = id;
        public int Number { get; } = number;
        public int DisposeCount => Volatile.Read(ref _disposeCount);
        public virtual event Action? Used;

        public void Use() => Used?.Invoke();

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return default;
        }
    }

    private sealed class CoordinatingUsageResource(string id, Action onDetach) : Resource(id)
    {
        private Action? _used;

        public bool DetachCallbackCompleted { get; private set; }

        public override event Action? Used
        {
            add => _used += value;
            remove
            {
                onDetach();
                DetachCallbackCompleted = true;
                _used -= value;
            }
        }
    }

    private sealed class BlockingSubscriptionResource(string id) : Resource(id)
    {
        private readonly TaskCompletionSource _releaseSubscription = NewSignal();
        private int _subscriberCount;

        public Task SubscriptionStarted => _subscriptionStarted.Task;
        public int SubscriberCount => Volatile.Read(ref _subscriberCount);

        private readonly TaskCompletionSource _subscriptionStarted = NewSignal();

        public override event Action? Used
        {
            add
            {
                _subscriptionStarted.TrySetResult();
                _releaseSubscription.Task.WaitAsync(OperationTimeout).GetAwaiter().GetResult();
                Interlocked.Increment(ref _subscriberCount);
            }
            remove => Interlocked.Decrement(ref _subscriberCount);
        }

        public void ReleaseSubscription() => _releaseSubscription.TrySetResult();
    }

    private sealed class CommitObservingObserver(ResourceCache<Resource> cache) : IResourceCacheObserver<Resource>
    {
        public Resource? ObservedValue { get; private set; }
        public bool WasCommittedWhenObserved { get; private set; }

        public ValueTask ResourceAddedAsync(Resource value, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); ObservedValue = value;
            WasCommittedWhenObserved = cache.Statistics is { Count: 1, PendingCreations: 0 };
            return default;
        }

        public ValueTask ResourceRemovedAsync(Resource value, CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); return default; }
        public ValueTask CacheClearedAsync(CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); return default; }
    }

    private sealed class CoordinatingTimeProvider : TimeProvider
    {
        private Action? _callback;
        private ManualTimer? _timer;
        private long _timestamp;

        public bool CallbackCompleted { get; private set; }

        public override long GetTimestamp()
        {
            Action? callback = Interlocked.Exchange(ref _callback, null);
            if (callback is not null)
            {
                callback();
                CallbackCompleted = true;
            }

            return Interlocked.Increment(ref _timestamp);
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _timer = new ManualTimer(callback, state);
            return _timer;
        }

        public void Arm(Action callback)
        {
            CallbackCompleted = false;
            Volatile.Write(ref _callback, callback);
        }

        public void FireTimer() => (_timer ?? throw new InvalidOperationException("The timer has not been created.")).Fire();

        private sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Fire() => callback(state);

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => default;
        }
    }
}
