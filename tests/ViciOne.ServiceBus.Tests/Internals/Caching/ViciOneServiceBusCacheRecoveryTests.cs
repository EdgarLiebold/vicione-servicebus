using ViciOne.ServiceBus.Internals.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Internals.Caching;

public sealed class ViciOneServiceBusCacheRecoveryTests
{
    private const string Key = "hello";

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-FAULT-RECOVERY", "faulted-entry-removed")]
    public async Task FaultedFactory_PropagatesItsExceptionAndLeavesNoCachedValue()
    {
        var cache = CreateCache();
        var expectedFailure = new CacheFactoryException("factory failed");

        Task<CacheEntry> creation = cache.GetOrAdd(
            Key,
            _ => Task.FromException<CacheEntry>(expectedFailure));

        CacheFactoryException observedFailure = await Assert.ThrowsAsync<CacheFactoryException>(
            () => creation.WaitAsync(OperationTimeout, TestCancellationToken));
        KeyNotFoundException missing = await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await cache.Get(Key));

        Assert.Same(expectedFailure, observedFailure);
        Assert.Contains(Key, missing.Message, StringComparison.Ordinal);
        Assert.Equal(0, cache.Count);
        Assert.Empty(await cache.Values.WaitAsync(OperationTimeout, TestCancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-IDENTITY", "add-and-read-same-instance")]
    public async Task AddedValue_IsReturnedByLaterReadsAsTheSameInstance()
    {
        var cache = CreateCache();
        var expected = Entry(Key);
        var factoryCalls = 0;

        CacheEntry added = await cache.GetOrAdd(Key, _ =>
            {
                Interlocked.Increment(ref factoryCalls);
                return Task.FromResult(expected);
            })
            .WaitAsync(OperationTimeout, TestCancellationToken);
        CacheEntry read = await cache.Get(Key).WaitAsync(OperationTimeout, TestCancellationToken);
        CacheEntry visible = Assert.Single(await cache.Values.WaitAsync(OperationTimeout, TestCancellationToken));

        Assert.Same(expected, added);
        Assert.Same(expected, read);
        Assert.Same(expected, visible);
        Assert.Equal(1, factoryCalls);
        Assert.Equal(1, cache.Count);
        Assert.Equal(0.5, cache.HitRatio);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-FAULT-RECOVERY", "pending-fallback")]
    public async Task PendingFailure_AllowsASecondFactoryAndReaderToShareTheHealthyValue()
    {
        var cache = CreateCache();
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal();
        var expectedFailure = new CacheFactoryException("first factory failed");
        var expected = Entry(Key);
        var healthyFactoryCalls = 0;

        Task<CacheEntry> first = cache.GetOrAdd(Key, async _ =>
        {
            firstStarted.TrySetResult();
            await releaseFirst.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            throw expectedFailure;
        });
        await firstStarted.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        Task<CacheEntry> second = cache.GetOrAdd(Key, _ =>
        {
            Interlocked.Increment(ref healthyFactoryCalls);
            return Task.FromResult(expected);
        });
        Task<CacheEntry> pendingRead = cache.Get(Key);

        releaseFirst.TrySetResult();

        CacheFactoryException observedFailure = await Assert.ThrowsAsync<CacheFactoryException>(
            () => first.WaitAsync(OperationTimeout, TestCancellationToken));
        CacheEntry recovered = await second.WaitAsync(OperationTimeout, TestCancellationToken);
        CacheEntry read = await pendingRead.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Same(expectedFailure, observedFailure);
        Assert.Same(expected, recovered);
        Assert.Same(expected, read);
        Assert.Equal(1, healthyFactoryCalls);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-FAULT-RECOVERY", "consecutive-faults")]
    public async Task ConsecutiveFailures_DoNotPoisonALaterHealthyCreation()
    {
        var cache = CreateCache();
        var firstFailure = new CacheFactoryException("first failure");
        var secondFailure = new CacheFactoryException("second failure");
        var expected = Entry(Key);

        CacheFactoryException observedFirst = await Assert.ThrowsAsync<CacheFactoryException>(() =>
            cache.GetOrAdd(Key, _ => Task.FromException<CacheEntry>(firstFailure))
                .WaitAsync(OperationTimeout, TestCancellationToken));
        CacheFactoryException observedSecond = await Assert.ThrowsAsync<CacheFactoryException>(() =>
            cache.GetOrAdd(Key, _ => Task.FromException<CacheEntry>(secondFailure))
                .WaitAsync(OperationTimeout, TestCancellationToken));
        CacheEntry recovered = await cache.GetOrAdd(Key, _ => Task.FromResult(expected))
            .WaitAsync(OperationTimeout, TestCancellationToken);
        CacheEntry read = await cache.Get(Key).WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Same(firstFailure, observedFirst);
        Assert.Same(secondFailure, observedSecond);
        Assert.Same(expected, recovered);
        Assert.Same(expected, read);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-SINGLE-FLIGHT", "concurrent-get-or-add")]
    public async Task ConcurrentGetOrAdd_ExecutesOnlyTheWinningFactory()
    {
        var cache = CreateCache();
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal();
        var expected = Entry(Key);
        var firstFactoryCalls = 0;
        var losingFactoryCalls = 0;

        Task<CacheEntry> first = cache.GetOrAdd(Key, async _ =>
        {
            Interlocked.Increment(ref firstFactoryCalls);
            firstStarted.TrySetResult();
            await releaseFirst.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            return expected;
        });
        await firstStarted.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        Task<CacheEntry> second = cache.GetOrAdd(Key, _ =>
        {
            Interlocked.Increment(ref losingFactoryCalls);
            return Task.FromResult(Entry("loser"));
        });
        Task<CacheEntry> pendingRead = cache.Get(Key);

        releaseFirst.TrySetResult();

        CacheEntry firstResult = await first.WaitAsync(OperationTimeout, TestCancellationToken);
        CacheEntry secondResult = await second.WaitAsync(OperationTimeout, TestCancellationToken);
        CacheEntry readResult = await pendingRead.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Same(expected, firstResult);
        Assert.Same(expected, secondResult);
        Assert.Same(expected, readResult);
        Assert.Equal(1, firstFactoryCalls);
        Assert.Equal(0, losingFactoryCalls);
        Assert.Equal(1, cache.Count);
    }

    private static ViciOneServiceBusCache<string, CacheEntry, CacheValue<CacheEntry>> CreateCache() =>
        new(new UsageCachePolicy<CacheEntry>());

    private static CacheEntry Entry(string key) => new(key, $"The key is {key}");

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private sealed class CacheFactoryException(string message) : Exception(message);
}
