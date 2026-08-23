using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching.Internals;

public sealed class IndexTests
{
    private const string Key = "hello";

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "fault-removal")]
    public async Task FaultedFactory_PropagatesItsExceptionAndLeavesNoIndexedValue()
    {
        IIndex<string, CacheValue> index = CreateIndex();
        var expected = new CacheFactoryException("The cache value factory failed.");

        Task<CacheValue> creation = index.Get(Key, _ => Task.FromException<CacheValue>(expected));

        CacheFactoryException actual = await Assert.ThrowsAsync<CacheFactoryException>(() =>
            creation.WaitAsync(OperationTimeout, TestCancellationToken));
        KeyNotFoundException missing = await Assert.ThrowsAsync<KeyNotFoundException>(() => index.Get(Key));

        Assert.Same(expected, actual);
        Assert.Equal($"Key not found: {Key}", missing.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "create")]
    public async Task MissingValueFactory_CreatesAndReturnsTheExactValue()
    {
        IIndex<string, CacheValue> index = CreateIndex();
        var expected = new CacheValue(Key, "The key is hello");
        var factoryCalls = 0;
        string? requestedKey = null;

        CacheValue actual = await index.Get(Key, key =>
        {
            Interlocked.Increment(ref factoryCalls);
            requestedKey = key;
            return Task.FromResult(expected);
        }).WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Same(expected, actual);
        Assert.Equal(Key, requestedKey);
        Assert.Equal(Key, actual.Id);
        Assert.Equal("The key is hello", actual.Value);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "plain-read-identity")]
    public async Task CreatedValue_IsReturnedByALaterPlainReadAsTheSameInstance()
    {
        IIndex<string, CacheValue> index = CreateIndex();
        var expected = new CacheValue(Key, "The key is hello");
        var factoryCalls = 0;

        CacheValue created = await index.Get(Key, _ =>
        {
            Interlocked.Increment(ref factoryCalls);
            return Task.FromResult(expected);
        }).WaitAsync(OperationTimeout, TestCancellationToken);
        CacheValue read = await index.Get(Key).WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Same(expected, created);
        Assert.Same(expected, read);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "pending-fallback")]
    public async Task PendingFailure_AllowsASecondFactoryAndPlainReadToShareTheSuccessfulValue()
    {
        IIndex<string, CacheValue> index = CreateIndex();
        var controlledFactory = new ControlledFactory();
        var expectedFailure = new CacheFactoryException("The first cache value factory failed.");
        var expected = new CacheValue(Key, "The key is hello");
        var fallbackCalls = 0;
        string? fallbackKey = null;

        Task<CacheValue> first = index.Get(Key, controlledFactory.Create);
        string requestedKey = await controlledFactory.Started.WaitAsync(OperationTimeout, TestCancellationToken);

        Task<CacheValue> second = index.Get(Key, key =>
        {
            Interlocked.Increment(ref fallbackCalls);
            fallbackKey = key;
            return Task.FromResult(expected);
        });
        Task<CacheValue> read = index.Get(Key);

        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);
        Assert.False(read.IsCompleted);

        controlledFactory.Fail(expectedFailure);

        CacheFactoryException actualFailure = await Assert.ThrowsAsync<CacheFactoryException>(() =>
            first.WaitAsync(OperationTimeout, TestCancellationToken));
        CacheValue fallback = await second.WaitAsync(OperationTimeout, TestCancellationToken);
        CacheValue concurrentRead = await read.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Equal(Key, requestedKey);
        Assert.Same(expectedFailure, actualFailure);
        Assert.Same(expected, fallback);
        Assert.Same(expected, concurrentRead);
        Assert.Equal(1, fallbackCalls);
        Assert.Equal(Key, fallbackKey);
    }

    private static IIndex<string, CacheValue> CreateIndex()
    {
        var settings = new CacheSettings(
            capacity: 100,
            minAge: TimeSpan.FromMinutes(1),
            maxAge: TimeSpan.FromMinutes(30),
            nowProvider: () => DateTime.UnixEpoch);

        return new GreenCache<CacheValue>(settings).AddIndex("id", value => value.Id);
    }

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private sealed record CacheValue(string Id, string Value);

    private sealed class CacheFactoryException(string message) : Exception(message);

    private sealed class ControlledFactory
    {
        private readonly TaskCompletionSource<string> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<CacheValue> _value =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<string> Started => _started.Task;

        public Task<CacheValue> Create(string key)
        {
            _started.TrySetResult(key);
            return _value.Task;
        }

        public void Fail(Exception exception) => _value.TrySetException(exception);
    }
}
