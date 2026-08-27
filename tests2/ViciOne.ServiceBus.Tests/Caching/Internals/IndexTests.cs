using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Caching.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
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

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-LOCK-ORDER", "indexed-read-releases-index-before-node-usage")]
    public async Task ExistingValue_ReadsTheNodeOnlyAfterReleasingTheIndexLock()
    {
        var tracker = new StubNodeTracker<CacheValue>();
        var index = new Index<string, CacheValue>(tracker, value => value.Id);
        IIndex<string, CacheValue> cacheIndex = index;
        var value = new CacheValue(Key, "The key is hello");
        CallbackNode<CacheValue>? node = null;
        node = new CallbackNode<CacheValue>(value, () => index.ValueRemoved(node, value));
        index.ValueAdded(node, value);

        CacheValue actual = await cacheIndex.Get(Key).WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Same(value, actual);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await cacheIndex.Get(Key));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-EVENT-ORDER", "stale-removal-preserves-new-generation")]
    public async Task DelayedRemovalOfAnOldGeneration_PreservesTheNewValueWithTheSameKey()
    {
        var tracker = new StubNodeTracker<CacheValue>();
        var index = new Index<string, CacheValue>(tracker, value => value.Id);
        IIndex<string, CacheValue> cacheIndex = index;
        var oldValue = new CacheValue(Key, "old");
        var currentValue = new CacheValue(Key, "current");
        var oldNode = new MutableNode<CacheValue>(oldValue);
        var currentNode = new MutableNode<CacheValue>(currentValue);
        index.ValueAdded(oldNode, oldValue);
        index.ValueAdded(currentNode, currentValue);

        index.ValueRemoved(oldNode, oldValue);

        Assert.Same(currentValue, await cacheIndex.Get(Key));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-EVENT-ORDER", "stale-add-cannot-replace-live-generation")]
    public async Task DelayedAddOfAnEvictedGeneration_CannotReplaceTheCurrentValue()
    {
        var tracker = new StubNodeTracker<CacheValue>();
        var index = new Index<string, CacheValue>(tracker, value => value.Id);
        IIndex<string, CacheValue> cacheIndex = index;
        var oldValue = new CacheValue(Key, "old");
        var currentValue = new CacheValue(Key, "current");
        var oldNode = new MutableNode<CacheValue>(oldValue) { IsValid = false };
        var currentNode = new MutableNode<CacheValue>(currentValue);
        index.ValueAdded(currentNode, currentValue);

        index.ValueAdded(oldNode, oldValue);

        Assert.Same(currentValue, await cacheIndex.Get(Key));
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

    private sealed class CallbackNode<TValue>(TValue value, Action onValue) : INode<TValue>
        where TValue : class
    {
        public Task<TValue> Value
        {
            get
            {
                Task.Run(onValue)
                    .WaitAsync(OperationTimeout, TestCancellationToken)
                    .GetAwaiter()
                    .GetResult();

                return Task.FromResult(value);
            }
        }

        public bool HasValue => true;

        public bool IsValid => true;

        public Task<TValue> GetValue(IPendingValue<TValue> pendingValue) => Task.FromResult(value);
    }

    private sealed class MutableNode<TValue>(TValue value) : INode<TValue>
        where TValue : class
    {
        public Task<TValue> Value => Task.FromResult(value);

        public bool HasValue => true;

        public bool IsValid { get; set; } = true;

        public Task<TValue> GetValue(IPendingValue<TValue> pendingValue) => Task.FromResult(value);
    }

    private sealed class StubNodeTracker<TValue> : INodeTracker<TValue>
        where TValue : class
    {
        public CacheStatistics Statistics { get; } = new(
            capacity: 1,
            bucketCount: 6,
            bucketSize: 1,
            minAge: TimeSpan.Zero,
            maxAge: TimeSpan.FromMinutes(1),
            validityCheckInterval: TimeSpan.FromSeconds(1));

        public void Add(INodeValueFactory<TValue> nodeValueFactory) => throw new NotSupportedException();

        public void Add(TValue value) => throw new NotSupportedException();

        public void Rebucket(IBucketNode<TValue> node) => throw new NotSupportedException();

        public void Remove(IBucketNode<TValue> existingNode) => throw new NotSupportedException();

        public IEnumerable<INode<TValue>> GetAll() => [];

        public void Clear() => throw new NotSupportedException();

        public ConnectHandle Connect(ICacheValueObserver<TValue> observer) => new EmptyConnectHandle();
    }
}
