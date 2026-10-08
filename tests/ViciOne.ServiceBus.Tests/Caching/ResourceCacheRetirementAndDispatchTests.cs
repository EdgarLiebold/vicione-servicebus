using System.Collections.Concurrent;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCacheRetirementAndDispatchTests
{
    static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-CAPACITY", "retirement-charged-through-observer-and-async-disposal")]
    public async Task Retirement_BlocksAdmissionThroughRemovedObserverAndAsyncDisposalAsync()
    {
        await using var cache = CreateCache(capacity: 1);
        var index = cache.AddIndex("id", value => value.Id);
        var retired = new Resource("retired", blockDisposal: true);
        await using var candidate = new Resource("candidate");
        await cache.AddAsync(retired, TestContext.Current.CancellationToken);
        var removalEntered = Gate();
        var releaseRemoval = Gate();
        var removalObserver = new Observer(onRemoved: async value =>
        {
            if (ReferenceEquals(value, retired))
            {
                removalEntered.TrySetResult();
                await releaseRemoval.Task.WaitAsync(OperationTimeout, CancellationToken.None);
            }
        });
        using var observer = cache.Connect(removalObserver);
        using var callers = new CancellationTokenSource();
        Task<bool> removal = index.RemoveAsync("retired", TestContext.Current.CancellationToken).AsTask();
        Task? addition = null;
        Task<Resource>? creation = null;
        Exception? addFailure = null;
        Exception? creationFailure = null;
        int factoryCalls = 0;
        ResourceCacheStatistics beforeDisposal = default;
        ResourceCacheStatistics duringDisposal = default;
        bool additionBlocked = false;
        bool creationBlocked = false;
        int factoryCallsDuringDisposal = -1;
        int candidateDisposalsDuringDisposal = -1;
        bool disposalStartedWhileRemovalHeld = false;
        int disposalCallsWhileRemovalHeld = -1;
        try
        {
            await removalEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            // These calls enter the actual admission paths before returning their awaiters.
            addition = cache.AddAsync(candidate, callers.Token).AsTask();
            creation = index.GetOrAddAsync("factory", (key, _) =>
            {
                Interlocked.Increment(ref factoryCalls);
                return ValueTask.FromResult(new Resource(key));
            }, callers.Token).AsTask();
            beforeDisposal = cache.Statistics;
            additionBlocked = !addition.IsCompleted;
            creationBlocked = !creation.IsCompleted;
            disposalStartedWhileRemovalHeld = retired.DisposalStarted.Task.IsCompleted;
            disposalCallsWhileRemovalHeld = retired.DisposeCount;
            releaseRemoval.TrySetResult();
            await retired.DisposalStarted.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            duringDisposal = cache.Statistics;
            factoryCallsDuringDisposal = Volatile.Read(ref factoryCalls);
            candidateDisposalsDuringDisposal = candidate.DisposeCount;
        }
        finally
        {
            // Cancel waiters before releasing retirement. A committed mutant add must be joined after release.
            callers.Cancel();
            releaseRemoval.TrySetResult();
            retired.ReleaseDisposal();
            await removal.WaitAsync(OperationTimeout, CancellationToken.None);
            await Task.WhenAll(removalObserver.Callbacks).WaitAsync(OperationTimeout, CancellationToken.None);
            if (addition is not null)
                addFailure = await Record.ExceptionAsync(() => addition.WaitAsync(OperationTimeout, CancellationToken.None));
            if (creation is not null)
                creationFailure = await Record.ExceptionAsync(() => creation.WaitAsync(OperationTimeout, CancellationToken.None));
        }

        Assert.Equal(1, beforeDisposal.TotalCreated);
        Assert.Equal(0, beforeDisposal.Count);
        Assert.Equal(0, beforeDisposal.PendingCreations);
        Assert.True(additionBlocked);
        Assert.True(creationBlocked);
        Assert.False(disposalStartedWhileRemovalHeld);
        Assert.Equal(0, disposalCallsWhileRemovalHeld);
        Assert.Equal(1, duringDisposal.TotalCreated);
        Assert.Equal(0, duringDisposal.Count);
        Assert.Equal(0, duringDisposal.PendingCreations);
        Assert.Equal(0, factoryCallsDuringDisposal);
        Assert.Equal(0, candidateDisposalsDuringDisposal);
        Assert.Equal(callers.Token, Assert.IsAssignableFrom<OperationCanceledException>(addFailure).CancellationToken);
        Assert.Equal(callers.Token, Assert.IsAssignableFrom<OperationCanceledException>(creationFailure).CancellationToken);
        Assert.True(addition!.IsCanceled);
        Assert.True(creation!.IsCanceled);
        Assert.True(await removal);
        Assert.Equal(1, retired.DisposeCount);
        Assert.Equal(0, candidate.DisposeCount);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Equal(0, cache.Statistics.PendingCreations);
        Assert.Equal(1, cache.Statistics.TotalCreated);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-OBSERVER", "dispatch-time-snapshot-and-active-snapshot-retention")]
    public async Task ObserverSnapshot_IsChosenAfterDispatchAdmissionAndRetainsActiveObserversAsync()
    {
        await using var cache = CreateCache();
        var entered = Gate();
        var release = Gate();
        var events = new ConcurrentQueue<string>();
        var first = new Resource("one");
        var second = new Resource("two");
        var observerA = new Observer(onAdded: async value =>
        {
            events.Enqueue("A:" + value.Id);
            if (ReferenceEquals(value, first))
            {
                entered.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, CancellationToken.None);
            }
        });
        var observerB = new Observer(onAdded: value =>
        {
            events.Enqueue("B:" + value.Id);
            return default;
        });
        var observerC = new Observer(onAdded: value =>
        {
            events.Enqueue("C:" + value.Id);
            return default;
        });
        using var connectionA = cache.Connect(observerA);
        using var connectionB = cache.Connect(observerB);
        IDisposable? connectionC = null;
        Task initial = cache.AddAsync(first, TestContext.Current.CancellationToken).AsTask();
        Task? subsequent = null;
        try
        {
            await entered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            subsequent = cache.AddAsync(second, TestContext.Current.CancellationToken).AsTask();
            Assert.Equal(2, cache.Statistics.Count);
            Assert.False(subsequent.IsCompleted);
            connectionB.Dispose();
            connectionC = cache.Connect(observerC);
        }
        finally
        {
            release.TrySetResult();
            await initial.WaitAsync(OperationTimeout, CancellationToken.None);
            if (subsequent is not null)
                await subsequent.WaitAsync(OperationTimeout, CancellationToken.None);
            await Task.WhenAll(observerA.Callbacks.Concat(observerB.Callbacks).Concat(observerC.Callbacks))
                .WaitAsync(OperationTimeout, CancellationToken.None);
            connectionC?.Dispose();
        }

        Assert.Equal(["A:one", "B:one", "A:two", "C:two"], events.ToArray());
        Assert.Equal([first, second], cache.GetValues(TestContext.Current.CancellationToken).OrderBy(value => value.Id).ToArray());
        Assert.Equal(2, cache.Statistics.TotalCreated);
        Assert.Equal(0, first.DisposeCount);
        Assert.Equal(0, second.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-OBSERVER", "entire-awaited-observer-batch-is-serialized")]
    public async Task ObserverDispatch_SerializesTheEntireAwaitedBatchBeforeTheNextCommittedBatchAsync()
    {
        await using var cache = CreateCache();
        var enteredA = Gate();
        var enteredB = Gate();
        var releaseA = Gate();
        var releaseB = Gate();
        var events = new ConcurrentQueue<string>();
        var first = new Resource("one");
        var second = new Resource("two");
        var observerA = new Observer(onAdded: async value =>
        {
            events.Enqueue("A:" + value.Id);
            if (ReferenceEquals(value, first))
            {
                enteredA.TrySetResult();
                await releaseA.Task.WaitAsync(OperationTimeout, CancellationToken.None);
            }
        });
        var observerB = new Observer(onAdded: async value =>
        {
            events.Enqueue("B:" + value.Id);
            if (ReferenceEquals(value, first))
            {
                enteredB.TrySetResult();
                await releaseB.Task.WaitAsync(OperationTimeout, CancellationToken.None);
            }
        });
        using var connectionA = cache.Connect(observerA);
        using var connectionB = cache.Connect(observerB);
        Task initial = cache.AddAsync(first, TestContext.Current.CancellationToken).AsTask();
        Task? subsequent = null;
        string[] whileSecondObserverBlocked = [];
        bool initialBlocked = false;
        bool subsequentBlocked = false;
        try
        {
            await enteredA.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            subsequent = cache.AddAsync(second, TestContext.Current.CancellationToken).AsTask();
            Assert.Equal(2, cache.Statistics.Count);
            releaseA.TrySetResult();
            await enteredB.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            whileSecondObserverBlocked = events.ToArray();
            initialBlocked = !initial.IsCompleted;
            subsequentBlocked = !subsequent.IsCompleted;
        }
        finally
        {
            releaseA.TrySetResult();
            releaseB.TrySetResult();
            await initial.WaitAsync(OperationTimeout, CancellationToken.None);
            if (subsequent is not null)
                await subsequent.WaitAsync(OperationTimeout, CancellationToken.None);
            await Task.WhenAll(observerA.Callbacks.Concat(observerB.Callbacks)).WaitAsync(OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(["A:one", "B:one"], whileSecondObserverBlocked);
        Assert.True(initialBlocked);
        Assert.True(subsequentBlocked);
        Assert.Equal(["A:one", "B:one", "A:two", "B:two"], events.ToArray());
        Assert.Equal(2, cache.Statistics.Count);
        Assert.Equal(2, cache.Statistics.TotalCreated);
        Assert.Equal(0, first.DisposeCount);
        Assert.Equal(0, second.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-OBSERVER", "active-inherited-scope-rejects-same-cache-allows-other-cache")]
    public async Task ActiveInheritedObserverScope_RejectsSameCacheMutationAndAllowsAnotherCacheAsync()
    {
        await using var cache = CreateCache();
        await using var otherCache = CreateCache();
        var original = new Resource("original");
        await using var rejected = new Resource("reentrant");
        var admittedElsewhere = new Resource("other");
        var attempted = Gate();
        Task? child = null;
        Exception? childFailure = null;
        var childObserver = new Observer(onAdded: async value =>
        {
            if (!ReferenceEquals(value, original))
                return;
            child = Task.Run(async () =>
            {
                try
                {
                    await otherCache.AddAsync(admittedElsewhere, TestContext.Current.CancellationToken);
                    Task mutation = cache.AddAsync(rejected, TestContext.Current.CancellationToken).AsTask();
                    attempted.TrySetResult();
                    await mutation;
                }
                finally
                {
                    attempted.TrySetResult();
                }
            }, TestContext.Current.CancellationToken);
            // The callback waits only for entry; waiting for the nested mutation would deadlock a guard mutant.
            await attempted.Task.WaitAsync(OperationTimeout, CancellationToken.None);
        });
        using var connection = cache.Connect(childObserver);
        Task addition = cache.AddAsync(original, TestContext.Current.CancellationToken).AsTask();
        try
        {
            await addition.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        }
        finally
        {
            attempted.TrySetResult();
            await addition.WaitAsync(OperationTimeout, CancellationToken.None);
            if (child is not null)
                childFailure = await Record.ExceptionAsync(() => child.WaitAsync(OperationTimeout, CancellationToken.None));
            await Task.WhenAll(childObserver.Callbacks).WaitAsync(OperationTimeout, CancellationToken.None);
        }

        var rejection = Assert.IsType<InvalidOperationException>(childFailure);
        Assert.Contains("must not mutate", rejection.Message, StringComparison.Ordinal);
        Assert.NotNull(child);
        Assert.True(child.IsFaulted);
        Assert.Equal([original], cache.GetValues(TestContext.Current.CancellationToken));
        Assert.Equal([admittedElsewhere], otherCache.GetValues(TestContext.Current.CancellationToken));
        Assert.Equal(1, cache.Statistics.TotalCreated);
        Assert.Equal(1, otherCache.Statistics.TotalCreated);
        Assert.Equal(0, rejected.DisposeCount);
        Assert.Equal(0, original.DisposeCount);
        Assert.Equal(0, admittedElsewhere.DisposeCount);
    }

    static ResourceCache<Resource> CreateCache(int capacity = 4) => new(new ResourceCacheOptions(
        capacity: capacity, timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch)));

    static TaskCompletionSource Gate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    sealed class Observer : IResourceCacheObserver<Resource>
    {
        readonly Func<Resource, ValueTask> _added;
        readonly Func<Resource, ValueTask> _removed;
        readonly ConcurrentBag<Task> _callbacks = [];

        public Observer(Func<Resource, ValueTask>? onAdded = null, Func<Resource, ValueTask>? onRemoved = null)
        {
            _added = onAdded ?? (_ => default);
            _removed = onRemoved ?? (_ => default);
        }

        public IEnumerable<Task> Callbacks => _callbacks.ToArray();

        public ValueTask ResourceAddedAsync(Resource value, CancellationToken cancellationToken) => Track(_added(value));
        public ValueTask ResourceRemovedAsync(Resource value, CancellationToken cancellationToken) => Track(_removed(value));
        public ValueTask CacheClearedAsync(CancellationToken cancellationToken) => default;

        ValueTask Track(ValueTask callback)
        {
            Task task = callback.AsTask();
            _callbacks.Add(task);
            return new ValueTask(task);
        }
    }

    sealed class Resource : IAsyncDisposable
    {
        readonly TaskCompletionSource _release = Gate();
        int _disposals;

        public Resource(string id, bool blockDisposal = false)
        {
            Id = id;
            if (!blockDisposal)
                _release.TrySetResult();
        }

        public string Id { get; }
        public int DisposeCount => Volatile.Read(ref _disposals);
        public TaskCompletionSource DisposalStarted { get; } = Gate();
        public void ReleaseDisposal() => _release.TrySetResult();

        public async ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposals);
            DisposalStarted.TrySetResult();
            await _release.Task.WaitAsync(OperationTimeout, CancellationToken.None);
        }
    }
}
