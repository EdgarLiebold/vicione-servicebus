using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

// Caller cancellation and the no-token overloads are part of the cache contract exercised here;
// every potentially blocking assertion is independently bounded by OperationTimeout.
#pragma warning disable xUnit1051

namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCacheConcurrencyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-SINGLE-FLIGHT", "concurrent-get-or-add")]
    public async Task ConcurrentGetOrAdd_ExecutesOnlyTheWinningFactory()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var started = NewSignal();
        var release = NewSignal();
        var winnerCalls = 0;
        var loserCalls = 0;
        var expected = new CacheValue("one", "winner");

        Task<CacheValue> first = index.GetOrAddAsync(
            "one",
            async (_, ownerToken) =>
            {
                Interlocked.Increment(ref winnerCalls);
                started.TrySetResult();
                await release.Task.WaitAsync(ownerToken);
                return expected;
            },
            TestContext.Current.CancellationToken).AsTask();
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Task<CacheValue>[] followers = Enumerable.Range(0, 31)
            .Select(_ => index.GetOrAddAsync(
                "one",
                (_, _) =>
                {
                    Interlocked.Increment(ref loserCalls);
                    return ValueTask.FromResult(new CacheValue("one", "loser"));
                },
                TestContext.Current.CancellationToken).AsTask())
            .ToArray();

        release.TrySetResult();
        CacheValue[] results = await Task.WhenAll(followers.Prepend(first))
            .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.All(results, result => Assert.Same(expected, result));
        Assert.Equal(1, winnerCalls);
        Assert.Equal(0, loserCalls);
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(0, cache.Statistics.PendingCreations);
        Assert.Equal(31, cache.Statistics.Hits);
        Assert.Equal(1, cache.Statistics.Misses);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-NODE-VALUE-FACTORY", "pending-value-identity")]
    public async Task PendingPlainReadAndFactoryWaiter_ReceiveTheSameCommittedInstance()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var started = NewSignal();
        var release = NewSignal<CacheValue>();

        Task<CacheValue> owner = index.GetOrAddAsync(
            "one",
            async (_, ownerToken) =>
            {
                started.TrySetResult();
                return await release.Task.WaitAsync(ownerToken);
            },
            TestContext.Current.CancellationToken).AsTask();
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task<CacheValue> plainRead = index.GetAsync("one", TestContext.Current.CancellationToken).AsTask();
        Task<CacheValue> factoryWaiter = index.GetOrAddAsync(
            "one",
            (_, _) => ValueTask.FromResult(new CacheValue("one", "loser")),
            TestContext.Current.CancellationToken).AsTask();
        var expected = new CacheValue("one", "winner");

        release.TrySetResult(expected);

        Assert.Same(expected, await owner.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        Assert.Same(expected, await plainRead.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        Assert.Same(expected, await factoryWaiter.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-NODE-VALUE-FACTORY", "lone-fault")]
    public async Task FaultedFactory_PropagatesTheExactFailureToEveryCurrentWaiter()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var started = NewSignal();
        var release = NewSignal();
        var expected = new CacheFactoryException("factory failed");

        Task<CacheValue> owner = index.GetOrAddAsync(
            "one",
            async (_, ownerToken) =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(ownerToken);
                throw expected;
            },
            TestContext.Current.CancellationToken).AsTask();
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task<CacheValue> waiter = index.GetAsync("one", TestContext.Current.CancellationToken).AsTask();

        release.TrySetResult();

        CacheFactoryException ownerFailure = await Assert.ThrowsAsync<CacheFactoryException>(() => owner);
        CacheFactoryException waiterFailure = await Assert.ThrowsAsync<CacheFactoryException>(() => waiter);
        Assert.Same(expected, ownerFailure);
        Assert.Same(expected, waiterFailure);
        Assert.Equal(1, cache.Statistics.CreationFaults);
        Assert.Equal(0, cache.Statistics.PendingCreations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "fault-removal")]
    public async Task FaultedFactory_RemovesItsReservationAndLeavesNoIndexedValue()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var expected = new CacheFactoryException("factory failed");

        CacheFactoryException observed = await Assert.ThrowsAsync<CacheFactoryException>(async () =>
            await index.GetOrAddAsync(
                "one",
                (_, _) => ValueTask.FromException<CacheValue>(expected),
                TestContext.Current.CancellationToken));

        Assert.Same(expected, observed);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await index.GetAsync("one", TestContext.Current.CancellationToken));
        Assert.Empty(await cache.GetValuesAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Equal(0, cache.Statistics.PendingCreations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-NODE-VALUE-FACTORY", "fault-fallback")]
    public async Task FailedCreation_DoesNotPoisonTheNextHealthyCreation()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var failure = new CacheFactoryException("first failed");
        await Assert.ThrowsAsync<CacheFactoryException>(async () =>
            await index.GetOrAddAsync("one", (_, _) => ValueTask.FromException<CacheValue>(failure)));
        var expected = new CacheValue("one", "healthy");

        CacheValue recovered = await index.GetOrAddAsync(
            "one",
            (_, _) => ValueTask.FromResult(expected),
            TestContext.Current.CancellationToken);

        Assert.Same(expected, recovered);
        Assert.Same(expected, await index.GetAsync("one", TestContext.Current.CancellationToken));
        Assert.Equal(1, cache.Statistics.CreationFaults);
        Assert.Equal(1, cache.Statistics.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "pending-fallback")]
    public async Task WaiterOnFailedPendingCreation_CanRetryWithAHealthyFactory()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var started = NewSignal();
        var release = NewSignal();
        var failure = new CacheFactoryException("pending failed");

        Task<CacheValue> owner = index.GetOrAddAsync(
            "one",
            async (_, ownerToken) =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(ownerToken);
                throw failure;
            }).AsTask();
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task<CacheValue> waiter = index.GetOrAddAsync(
            "one",
            (_, _) => ValueTask.FromResult(new CacheValue("one", "not-run"))).AsTask();
        release.TrySetResult();

        Assert.Same(failure, await Assert.ThrowsAsync<CacheFactoryException>(() => owner));
        Assert.Same(failure, await Assert.ThrowsAsync<CacheFactoryException>(() => waiter));
        var expected = new CacheValue("one", "recovered");
        CacheValue recovered = await index.GetOrAddAsync("one", (_, _) => ValueTask.FromResult(expected));

        Assert.Same(expected, recovered);
        Assert.Same(expected, await index.GetAsync("one"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-FAULT-RECOVERY", "faulted-entry-removed")]
    public async Task FactoryFailure_LeavesTheKeyMissingAndTheCacheEmpty()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);

        await Assert.ThrowsAsync<CacheFactoryException>(async () =>
            await index.GetOrAddAsync(
                "one",
                (_, _) => ValueTask.FromException<CacheValue>(new CacheFactoryException("failed"))));

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync("one"));
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Equal(0, cache.Statistics.PendingCreations);
        Assert.Empty(await cache.GetValuesAsync());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-FAULT-RECOVERY", "pending-fallback")]
    public async Task FailedPendingGeneration_IsReplacedByOneSharedHealthyGeneration()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        await Assert.ThrowsAsync<CacheFactoryException>(async () =>
            await index.GetOrAddAsync(
                "one",
                (_, _) => ValueTask.FromException<CacheValue>(new CacheFactoryException("failed"))));
        var started = NewSignal();
        var release = NewSignal<CacheValue>();
        var calls = 0;

        Task<CacheValue> recovery = index.GetOrAddAsync(
            "one",
            async (_, ownerToken) =>
            {
                Interlocked.Increment(ref calls);
                started.TrySetResult();
                return await release.Task.WaitAsync(ownerToken);
            }).AsTask();
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task<CacheValue> reader = index.GetAsync("one").AsTask();
        var expected = new CacheValue("one", "healthy");
        release.TrySetResult(expected);

        Assert.Same(expected, await recovery.WaitAsync(OperationTimeout));
        Assert.Same(expected, await reader.WaitAsync(OperationTimeout));
        Assert.Equal(1, calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-FAULT-RECOVERY", "consecutive-faults")]
    public async Task ConsecutiveFailures_DoNotPoisonALaterHealthyCreation()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var expected = new CacheFactoryException($"failure-{attempt}");
            CacheFactoryException actual = await Assert.ThrowsAsync<CacheFactoryException>(async () =>
                await index.GetOrAddAsync("one", (_, _) => ValueTask.FromException<CacheValue>(expected)));
            Assert.Same(expected, actual);
        }

        var healthy = new CacheValue("one", "healthy");
        Assert.Same(healthy, await index.GetOrAddAsync("one", (_, _) => ValueTask.FromResult(healthy)));
        Assert.Same(healthy, await index.GetAsync("one"));
        Assert.Equal(3, cache.Statistics.CreationFaults);
        Assert.Equal(1, cache.Statistics.Count);
    }

    [Fact]
    public async Task CallerCancellation_CancelsOnlyThatWaiterAndNotSharedCreation()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var started = NewSignal();
        var release = NewSignal<CacheValue>();
        var ownerTokenCanceled = false;
        using var waiterCancellation = new CancellationTokenSource();

        Task<CacheValue> canceledWaiter = index.GetOrAddAsync(
            "one",
            async (_, ownerToken) =>
            {
                started.TrySetResult();
                return await release.Task.WaitAsync(ownerToken);
            },
            waiterCancellation.Token).AsTask();
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task<CacheValue> survivingWaiter = index.GetOrAddAsync(
            "one",
            (_, ownerToken) =>
            {
                ownerTokenCanceled = ownerToken.IsCancellationRequested;
                return ValueTask.FromResult(new CacheValue("one", "loser"));
            }).AsTask();

        waiterCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledWaiter);
        var expected = new CacheValue("one", "healthy");
        release.TrySetResult(expected);

        Assert.Same(expected, await survivingWaiter.WaitAsync(OperationTimeout));
        Assert.False(ownerTokenCanceled);
        Assert.Same(expected, await index.GetAsync("one"));
    }

    [Fact]
    public async Task CapacityOwnedByPendingCreation_BackpressuresAnotherKeyWithoutExceedingTheBound()
    {
        await using var cache = CreateCache(capacity: 1);
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal<CacheValue>();
        var secondStarted = NewSignal();

        Task<CacheValue> first = index.GetOrAddAsync(
            "one",
            async (_, ownerToken) =>
            {
                firstStarted.TrySetResult();
                return await releaseFirst.Task.WaitAsync(ownerToken);
            }).AsTask();
        await firstStarted.Task.WaitAsync(OperationTimeout);

        Task<CacheValue> second = index.GetOrAddAsync(
            "two",
            (_, _) =>
            {
                secondStarted.TrySetResult();
                return ValueTask.FromResult(new CacheValue("two", "second"));
            }).AsTask();

        Assert.False(secondStarted.Task.IsCompleted);
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Equal(1, cache.Statistics.PendingCreations);
        releaseFirst.TrySetResult(new CacheValue("one", "first"));

        await first.WaitAsync(OperationTimeout);
        await secondStarted.Task.WaitAsync(OperationTimeout);
        CacheValue secondValue = await second.WaitAsync(OperationTimeout);

        Assert.Equal("two", secondValue.Id);
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(0, cache.Statistics.PendingCreations);
        Assert.Equal(1, cache.Statistics.Evictions);
    }

    [Fact]
    public async Task ClearAsync_CancelsPendingOwnershipAndAllowsImmediateKeyReuse()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var started = NewSignal();

        Task<CacheValue> pending = index.GetOrAddAsync(
            "one",
            async (_, ownerToken) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ownerToken);
                return new CacheValue("one", "unreachable");
            }).AsTask();
        await started.Task.WaitAsync(OperationTimeout);

        await cache.ClearAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(OperationTimeout);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        var replacement = new CacheValue("one", "replacement");
        Assert.Same(replacement, await index.GetOrAddAsync("one", (_, _) => ValueTask.FromResult(replacement)));
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(0, cache.Statistics.PendingCreations);
    }

    [Fact]
    public async Task IndexAddedDuringCreation_IsPopulatedBeforeTheResourceBecomesObservable()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> idIndex = cache.AddIndex("id", value => value.Id);
        var started = NewSignal();
        var release = NewSignal<CacheValue>();

        Task<CacheValue> creation = idIndex.GetOrAddAsync(
            "one",
            async (_, ownerToken) =>
            {
                started.TrySetResult();
                return await release.Task.WaitAsync(ownerToken);
            }).AsTask();
        await started.Task.WaitAsync(OperationTimeout);
        IResourceCacheIndex<int, CacheValue> numberIndex = cache.AddIndex("number", value => value.Number);
        var expected = new CacheValue("one", "value", 17);

        release.TrySetResult(expected);

        Assert.Same(expected, await creation.WaitAsync(OperationTimeout));
        Assert.Same(expected, await numberIndex.GetAsync(17));
        Assert.Equal(1, cache.Statistics.Count);
    }

    private static ResourceCache<CacheValue> CreateCache(int capacity = 32) => new(new ResourceCacheOptions(
        capacity,
        minAge: TimeSpan.Zero,
        maxAge: TimeSpan.FromMinutes(30),
        cleanupInterval: TimeSpan.FromHours(1)));

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout => TimeSpan.FromSeconds(10);

    private sealed record CacheValue(string Id, string Value, int Number = 0);

    private sealed class CacheFactoryException(string message) : Exception(message);
}
