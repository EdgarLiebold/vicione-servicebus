using ViciOne.ServiceBus.Caching.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching.Internals;

public sealed class NodeValueFactoryTests
{
    private const string Key = "hello";

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-NODE-VALUE-FACTORY", "fault-fallback")]
    public async Task FaultedFirstValue_AllowsTheSecondPendingValueToSucceed()
    {
        var expectedFailure = new CacheFactoryException("The first cache value factory failed.");
        var expected = new CacheValue(Key, "The key is hello");
        var faulted = new PendingValue<string, CacheValue>(
            Key,
            _ => Task.FromException<CacheValue>(expectedFailure));
        var healthy = new PendingValue<string, CacheValue>(Key, _ => Task.FromResult(expected));
        var factory = new NodeValueFactory<CacheValue>(faulted, timeoutInMilliseconds: 0);
        factory.Add(healthy);

        CacheValue actual = await factory.CreateValue().WaitAsync(OperationTimeout, TestCancellationToken);
        CacheFactoryException actualFailure = await Assert.ThrowsAsync<CacheFactoryException>(() =>
            faulted.Value.WaitAsync(OperationTimeout, TestCancellationToken));
        CacheValue healthyValue = await healthy.Value.WaitAsync(OperationTimeout, TestCancellationToken);
        CacheValue publishedValue = await factory.Value.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Same(expectedFailure, actualFailure);
        Assert.Same(expected, actual);
        Assert.Same(expected, healthyValue);
        Assert.Same(expected, publishedValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-NODE-VALUE-FACTORY", "lone-fault")]
    public async Task LoneFaultedValue_PropagatesTheExactFailureToBothResults()
    {
        var expected = new CacheFactoryException("The cache value factory failed.");
        var pending = new PendingValue<string, CacheValue>(
            Key,
            _ => Task.FromException<CacheValue>(expected));
        var factory = new NodeValueFactory<CacheValue>(pending, timeoutInMilliseconds: 0);

        CacheFactoryException creationFailure = await Assert.ThrowsAsync<CacheFactoryException>(() =>
            factory.CreateValue().WaitAsync(OperationTimeout, TestCancellationToken));
        CacheFactoryException publishedFailure = await Assert.ThrowsAsync<CacheFactoryException>(() =>
            factory.Value.WaitAsync(OperationTimeout, TestCancellationToken));

        Assert.Same(expected, creationFailure);
        Assert.Same(expected, publishedFailure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-NODE-VALUE-FACTORY", "pending-value-identity")]
    public async Task HealthyValue_IsPublishedThroughThePendingValueAndFactoryAsTheSameInstance()
    {
        var expected = new CacheValue(Key, "The key is hello");
        var pending = new PendingValue<string, CacheValue>(Key, _ => Task.FromResult(expected));
        var factory = new NodeValueFactory<CacheValue>(pending, timeoutInMilliseconds: 0);

        CacheValue created = await factory.CreateValue().WaitAsync(OperationTimeout, TestCancellationToken);
        CacheValue pendingValue = await pending.Value.WaitAsync(OperationTimeout, TestCancellationToken);
        CacheValue publishedValue = await factory.Value.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Same(expected, created);
        Assert.Same(expected, pendingValue);
        Assert.Same(expected, publishedValue);
        Assert.Equal(Key, created.Id);
        Assert.Equal("The key is hello", created.Value);
    }

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private sealed record CacheValue(string Id, string Value);

    private sealed class CacheFactoryException(string message) : Exception(message);
}
