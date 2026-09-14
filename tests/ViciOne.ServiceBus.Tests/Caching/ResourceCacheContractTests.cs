using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

// The no-token overloads are part of the public contract exercised here; asynchronous
// coordination points use the xUnit cancellation token or an explicit bounded timeout.
namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCacheContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-TTL-CONFIGURATION", "invalid-inputs")]
    public void Options_RejectInvalidCapacityTimeBoundsAndExpirationModes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResourceCacheOptions(capacity: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResourceCacheOptions(minAge: TimeSpan.FromTicks(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResourceCacheOptions(maxAge: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResourceCacheOptions(
            minAge: TimeSpan.FromSeconds(2),
            maxAge: TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResourceCacheOptions(cleanupInterval: TimeSpan.Zero));
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ResourceCacheOptions(expirationMode: (ResourceCacheExpirationMode)int.MaxValue));
        Assert.Equal("expirationMode", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-METRICS", "overflow-safe-hit-ratio")]
    public void Statistics_CalculatesHitRatioWithoutOverflow()
    {
        var empty = new ResourceCacheStatistics(0, 0, 0, 0, 0, 0, 0);
        var statistics = new ResourceCacheStatistics(
            Count: 0,
            PendingCreations: 0,
            TotalCreated: 0,
            Hits: long.MaxValue,
            Misses: long.MaxValue,
            CreationFaults: 0,
            Evictions: 0);

        Assert.Equal(0, empty.HitRatio);
        Assert.Equal(0.5, statistics.HitRatio, precision: 12);
    }

    [Fact]
    public async Task IndexOperations_RejectNullKeysWithoutReadingOrMutatingCacheStateAsync()
    {
        await using var cache = new ResourceCache<CacheValue>();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);

        Assert.Throws<ArgumentNullException>(() => index.GetAsync(null!, TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentNullException>(() => index.GetOrAddAsync(null!, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentNullException>(() => index.RemoveAsync(null!, TestContext.Current.CancellationToken));

        ResourceCacheStatistics statistics = cache.Statistics;
        Assert.Equal(0, statistics.Count);
        Assert.Equal(0, statistics.PendingCreations);
        Assert.Equal(0, statistics.Hits);
        Assert.Equal(0, statistics.Misses);
    }

    [Fact]
    public async Task KeyedFacade_RejectsANullFactoryBeforeStartingCacheWorkAsync()
    {
        await using var cache = new KeyedResourceCache<string, CacheValue>(
            value => value.Id,
            new ResourceCacheOptions(cleanupInterval: TimeSpan.FromHours(1)));

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            cache.GetOrAddAsync("one", null!, TestContext.Current.CancellationToken));

        Assert.Equal("factory", exception.ParamName);
        Assert.Equal(default, cache.Statistics);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-DIRECT-ADD", "pre-canceled-mutations-are-rejected")]
    public async Task PreCanceledAddAndClear_DoNotMutateCommittedStateAsync()
    {
        await using var cache = CreateCache();
        var retained = new CacheValue("retained", "original");
        await cache.AddAsync(retained, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cache.AddAsync(new CacheValue("rejected", "new"), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cache.ClearAsync(cancellation.Token));

        Assert.Same(retained, Assert.Single(cache.GetValues(TestContext.Current.CancellationToken)));
        Assert.Equal(1, cache.Statistics.TotalCreated);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "pre-canceled-index-operations-are-rejected")]
    public async Task PreCanceledIndexOperations_DoNotReadMutateOrStartAFactoryAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var retained = new CacheValue("retained", "original");
        await cache.AddAsync(retained, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var factoryCalls = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await index.GetAsync("retained", cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await index.GetOrAddAsync(
                "missing",
                (_, _) =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    return ValueTask.FromResult(new CacheValue("missing", "new"));
                },
                cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await index.RemoveAsync("retained", cancellation.Token));

        Assert.Equal(0, factoryCalls);
        Assert.Same(retained, await index.GetAsync("retained", TestContext.Current.CancellationToken));
        Assert.Equal(1, cache.Statistics.Hits);
        Assert.Equal(0, cache.Statistics.Misses);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-DIRECT-ADD", "index-identity-and-factory-bypass")]
    public async Task AddAsync_PublishesTheExactInstanceAndBypassesTheFallbackFactoryAsync()
    {
        await using var cache = CreateCache();
        var factoryCalls = 0;
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex(
            "id",
            value => value.Id,
            (key, _) =>
            {
                Interlocked.Increment(ref factoryCalls);
                return ValueTask.FromResult(new CacheValue(key, "unexpected"));
            });
        var expected = new CacheValue("one", "first");

        await cache.AddAsync(expected, TestContext.Current.CancellationToken);

        CacheValue actual = await index.GetOrAddAsync("one", cancellationToken: TestContext.Current.CancellationToken);
        CacheValue visible = Assert.Single(cache.GetValues(TestContext.Current.CancellationToken));
        ResourceCacheStatistics statistics = cache.Statistics;

        Assert.Same(expected, actual);
        Assert.Same(expected, visible);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(1, statistics.Count);
        Assert.Equal(1, statistics.TotalCreated);
        Assert.Equal(1, statistics.Hits);
        Assert.Equal(0, statistics.Misses);
        Assert.Equal(0, statistics.CreationFaults);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "create")]
    public async Task MissingValueFactory_CommitsOneInstanceForFactoryResultAndPlainReadsAsync()
    {
        await using var cache = CreateCache();
        var expected = new CacheValue("one", "first");
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex(
            "id",
            value => value.Id,
            (key, _) => ValueTask.FromResult(expected));

        CacheValue created = await index.GetOrAddAsync("one", cancellationToken: TestContext.Current.CancellationToken);
        CacheValue read = await index.GetAsync("one", TestContext.Current.CancellationToken);
        CacheValue visible = Assert.Single(cache.GetValues(TestContext.Current.CancellationToken));

        Assert.Same(expected, created);
        Assert.Same(expected, read);
        Assert.Same(expected, visible);
        Assert.Equal(new ResourceCacheStatistics(1, 0, 1, 1, 1, 0, 0), cache.Statistics);
    }

    [Fact]
    public async Task MissingPlainRead_ThrowsAndRecordsExactlyOneMissAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);

        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await index.GetAsync("missing", TestContext.Current.CancellationToken));

        Assert.Contains("missing", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, cache.Statistics.Misses);
        Assert.Equal(0, cache.Statistics.Hits);
        Assert.Equal(0, cache.Statistics.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "propagation")]
    public async Task FactoryCommit_IsAtomicallyVisibleThroughEveryIndexAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> idIndex = cache.AddIndex("id", value => value.Id);
        IResourceCacheIndex<int, CacheValue> numberIndex = cache.AddIndex("number", value => value.Number);

        CacheValue created = await idIndex.GetOrAddAsync(
            "twenty-seven",
            (key, _) => ValueTask.FromResult(new CacheValue(key, "created", 27)),
            TestContext.Current.CancellationToken);
        CacheValue byNumber = await numberIndex.GetAsync(27, TestContext.Current.CancellationToken);

        Assert.Same(created, byNumber);
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(1, cache.Statistics.Misses);
        Assert.Equal(1, cache.Statistics.Hits);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "clear")]
    public async Task ClearAsync_RemovesEveryIndexAndAllowsTheSameKeysToBeReusedAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> idIndex = cache.AddIndex("id", value => value.Id);
        IResourceCacheIndex<int, CacheValue> numberIndex = cache.AddIndex("number", value => value.Number);
        var first = new CacheValue("one", "first", 1);
        await cache.AddAsync(first, TestContext.Current.CancellationToken);

        await cache.ClearAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await idIndex.GetAsync("one", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await numberIndex.GetAsync(1, TestContext.Current.CancellationToken));
        Assert.Empty(cache.GetValues(TestContext.Current.CancellationToken));

        var replacement = new CacheValue("one", "replacement", 1);
        await cache.AddAsync(replacement, TestContext.Current.CancellationToken);

        Assert.Same(replacement, await idIndex.GetAsync("one", TestContext.Current.CancellationToken));
        Assert.Same(replacement, await numberIndex.GetAsync(1, TestContext.Current.CancellationToken));
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(2, cache.Statistics.TotalCreated);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "cross-index-removal")]
    public async Task RemoveAsync_AtomicallyRemovesTheResourceFromEveryIndexAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> idIndex = cache.AddIndex("id", value => value.Id);
        IResourceCacheIndex<int, CacheValue> numberIndex = cache.AddIndex("number", value => value.Number);
        var value = new CacheValue("one", "first", 1);
        await cache.AddAsync(value, TestContext.Current.CancellationToken);

        bool removed = await numberIndex.RemoveAsync(1, TestContext.Current.CancellationToken);

        Assert.True(removed);
        Assert.False(await idIndex.RemoveAsync("one", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await idIndex.GetAsync("one", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await numberIndex.GetAsync(1, TestContext.Current.CancellationToken));
        Assert.Empty(cache.GetValues(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "pending-removal-truthfulness")]
    public async Task RemoveAsync_DuringCreationReportsFalseAndPreservesTheCommittedResourceAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> idIndex = cache.AddIndex("id", value => value.Id);
        IResourceCacheIndex<int, CacheValue> numberIndex = cache.AddIndex("number", value => value.Number);
        var started = NewSignal<string>();
        var release = NewSignal<CacheValue>();

        Task<CacheValue> creation = idIndex.GetOrAddAsync(
            "one",
            async (key, ownerToken) =>
            {
                started.TrySetResult(key);
                return await release.Task.WaitAsync(ownerToken);
            },
            TestContext.Current.CancellationToken).AsTask();
        Assert.Equal("one", await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));

        Assert.False(await idIndex.RemoveAsync("one", TestContext.Current.CancellationToken));
        Assert.False(creation.IsCompleted);

        var expected = new CacheValue("one", "created", 1);
        release.TrySetResult(expected);
        Assert.Same(expected, await creation.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        Assert.Same(expected, await numberIndex.GetAsync(1, TestContext.Current.CancellationToken));
        Assert.Equal(1, cache.Statistics.Count);
    }

    [Fact]
    public async Task AddIndex_ProjectsExistingResourcesBeforePublishingTheIndexAsync()
    {
        await using var cache = CreateCache();
        var first = new CacheValue("one", "first", 1);
        var second = new CacheValue("two", "second", 2);
        await cache.AddAsync(first, TestContext.Current.CancellationToken);
        await cache.AddAsync(second, TestContext.Current.CancellationToken);

        IResourceCacheIndex<int, CacheValue> index = cache.AddIndex("number", value => value.Number);

        Assert.Same(first, await index.GetAsync(1, TestContext.Current.CancellationToken));
        Assert.Same(second, await index.GetAsync(2, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DuplicateProjection_RejectsTheNewIndexWithoutPartialPublicationAsync()
    {
        await using var cache = CreateCache();
        await cache.AddAsync(new CacheValue("one", "same"), TestContext.Current.CancellationToken);
        await cache.AddAsync(new CacheValue("two", "same"), TestContext.Current.CancellationToken);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            cache.AddIndex("duplicate", value => value.Value));

        Assert.Contains("duplicate key", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<KeyNotFoundException>(() => cache.GetIndex<string>("duplicate"));
        Assert.Equal(2, cache.Statistics.Count);
    }

    [Fact]
    public async Task FactoryProjectedKeyMismatch_FaultsWithoutPublishingTheResourceAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await index.GetOrAddAsync(
                "requested",
                (_, _) => ValueTask.FromResult(new CacheValue("different", "value")),
                TestContext.Current.CancellationToken));

        Assert.Contains("requested", exception.Message, StringComparison.Ordinal);
        Assert.Contains("different", exception.Message, StringComparison.Ordinal);
        Assert.Empty(cache.GetValues(TestContext.Current.CancellationToken));
        Assert.Equal(1, cache.Statistics.CreationFaults);
        Assert.Equal(0, cache.Statistics.PendingCreations);
    }

    [Fact]
    public async Task DuplicateKey_AddAsyncPreservesTheOriginalAndRejectsTheSecondValueAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id, comparer: StringComparer.OrdinalIgnoreCase);
        var original = new CacheValue("Key", "original");
        await cache.AddAsync(original, TestContext.Current.CancellationToken);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cache.AddAsync(new CacheValue("KEY", "duplicate"), TestContext.Current.CancellationToken));

        Assert.Contains("already contains", exception.Message, StringComparison.Ordinal);
        Assert.Same(original, await index.GetAsync("key", TestContext.Current.CancellationToken));
        Assert.Equal(1, cache.Statistics.Count);
        Assert.Equal(1, cache.Statistics.TotalCreated);
    }

    [Fact]
    public async Task IndexLookup_RejectsMissingNamesAndMismatchedKeyTypesAsync()
    {
        await using var cache = CreateCache();
        IResourceCacheIndex<string, CacheValue> expected = cache.AddIndex("id", value => value.Id);

        Assert.Same(expected, cache.GetIndex<string>("id"));
        Assert.Throws<KeyNotFoundException>(() => cache.GetIndex<string>("missing"));
        Assert.Throws<InvalidOperationException>(() => cache.GetIndex<int>("id"));
        Assert.Throws<ArgumentException>(() => cache.AddIndex("id", value => value.Number));
    }

    private static ResourceCache<CacheValue> CreateCache(int capacity = 32) => new(new ResourceCacheOptions(
        capacity,
        minAge: TimeSpan.Zero,
        maxAge: TimeSpan.FromMinutes(30),
        cleanupInterval: TimeSpan.FromHours(1)));

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout => TimeSpan.FromSeconds(10);

    private sealed record CacheValue(string Id, string Value, int Number = 0);
}
