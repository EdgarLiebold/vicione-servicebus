using ViciOne.ServiceBus.Internals.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Internals.Caching;

public sealed class ViciOneServiceBusCacheCapacityTests
{
    private const int Capacity = 1_000;

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-CAPACITY", "single-use-values")]
    public async Task SingleUseValuesAboveCapacity_RetainTheNewestHalf()
    {
        var cache = CreateUsageCache();

        await AddDistinct(cache, Capacity + 10).WaitAsync(OperationTimeout, TestCancellationToken);
        CacheEntry[] visible = (await cache.Values.WaitAsync(OperationTimeout, TestCancellationToken)).ToArray();
        string[] expectedKeys = Enumerable.Range(Capacity + 10 - Capacity / 2, Capacity / 2)
            .Select(Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Capacity / 2, cache.Count);
        Assert.Equal(Capacity / 2, visible.Length);
        Assert.Equal(0, cache.HitRatio);
        Assert.Equal(expectedKeys, visible.Select(entry => entry.Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-CAPACITY", "usage-aware-retention")]
    public async Task ReusedValues_AreRetainedAlongsideTheNewValuesAtCapacity()
    {
        var cache = CreateUsageCache();
        CacheEntry? firstAdded = null;
        CacheEntry? firstReused = null;
        var losingFactoryCalls = 0;

        async Task Exercise()
        {
            for (var index = 0; index < Capacity / 2; index++)
            {
                string key = Key(index);
                CacheEntry first = await cache.GetOrAdd(key, CreateEntry);
                CacheEntry second = await cache.GetOrAdd(key, _ =>
                {
                    Interlocked.Increment(ref losingFactoryCalls);
                    return Task.FromResult(Entry("unexpected"));
                });

                if (index == 0)
                {
                    firstAdded = first;
                    firstReused = second;
                }
            }

            for (var index = Capacity / 2; index < Capacity; index++)
                await cache.GetOrAdd(Key(index), CreateEntry);
        }

        await Exercise().WaitAsync(OperationTimeout, TestCancellationToken);
        CacheEntry[] visible = (await cache.Values.WaitAsync(OperationTimeout, TestCancellationToken)).ToArray();

        Assert.Equal(Capacity, cache.Count);
        Assert.Equal(Capacity, visible.Length);
        Assert.Equal(1d / 3d, cache.HitRatio, precision: 12);
        Assert.Same(firstAdded, firstReused);
        Assert.Equal(0, losingFactoryCalls);
        Assert.Equal(
            Enumerable.Range(0, Capacity).Select(Key).Order(StringComparer.Ordinal),
            visible.Select(entry => entry.Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-METRICS", "deterministic-distribution")]
    public async Task RepeatedDeterministicAccess_ReportsExactCountAndHitRatio()
    {
        const int distinctKeys = 500;
        const int accessCount = 10_000;
        var cache = CreateUsageCache();

        async Task Exercise()
        {
            for (var index = 0; index < accessCount; index++)
                await cache.GetOrAdd(Key(index % distinctKeys), CreateEntry);
        }

        await Exercise().WaitAsync(OperationTimeout, TestCancellationToken);
        CacheEntry[] visible = (await cache.Values.WaitAsync(OperationTimeout, TestCancellationToken)).ToArray();
        double expectedHitRatio = (accessCount - distinctKeys) / (double)accessCount;

        Assert.Equal(distinctKeys, cache.Count);
        Assert.Equal(distinctKeys, visible.Length);
        Assert.Equal(expectedHitRatio, cache.HitRatio);
        Assert.Equal(distinctKeys, visible.Select(entry => entry.Key).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-TRACKER", "high-churn-eviction")]
    public async Task ValueTracker_HighChurnEvictsEveryValueBeyondItsNewBucketCapacity()
    {
        const int additions = 10_000;
        var removed = 0;
        var tracker = new ValueTracker<CacheEntry, CacheValue<CacheEntry>>(
            new UsageCachePolicy<CacheEntry>(),
            Capacity);

        async Task Exercise()
        {
            for (var index = 0; index < additions; index++)
                await tracker.Add(new CacheValue<CacheEntry>(() => Interlocked.Increment(ref removed)));
        }

        await Exercise().WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Equal(Capacity, tracker.Capacity);
        Assert.Equal(additions - Capacity / 4, removed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-TTL", "sequential-access-within-ttl")]
    public async Task SequentialAccessWithinTimeToLive_PreservesExactCountAndHitRatio()
    {
        const int distinctKeys = 500;
        const int accessCount = 10_000;
        var timeProvider = new ManualTimeProvider(timestampFrequency: 1_000);
        var policy = new TimeToLiveCachePolicy<CacheEntry>(TimeSpan.FromSeconds(30), timeProvider);
        var cache = new ViciOneServiceBusCache<string, CacheEntry, ITimeToLiveCacheValue<CacheEntry>>(
            policy,
            new CacheOptions { Capacity = Capacity });

        async Task Exercise()
        {
            for (var index = 0; index < accessCount; index++)
                await cache.GetOrAdd(Key(index % distinctKeys), CreateEntry);
        }

        await Exercise().WaitAsync(OperationTimeout, TestCancellationToken);
        CacheEntry[] visible = (await cache.Values.WaitAsync(OperationTimeout, TestCancellationToken)).ToArray();

        Assert.Equal(distinctKeys, cache.Count);
        Assert.Equal(distinctKeys, visible.Length);
        Assert.Equal((accessCount - distinctKeys) / (double)accessCount, cache.HitRatio);
    }

    private static ViciOneServiceBusCache<string, CacheEntry, CacheValue<CacheEntry>> CreateUsageCache() =>
        new(new UsageCachePolicy<CacheEntry>(), new CacheOptions { Capacity = Capacity });

    private static async Task AddDistinct(
        ViciOneServiceBusCache<string, CacheEntry, CacheValue<CacheEntry>> cache,
        int count)
    {
        for (var index = 0; index < count; index++)
            await cache.GetOrAdd(Key(index), CreateEntry);
    }

    private static Task<CacheEntry> CreateEntry(string key) => Task.FromResult(Entry(key));

    private static CacheEntry Entry(string key) => new(key, $"The key is {key}");

    private static string Key(int index) => $"key-{index:D4}";

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;
}
