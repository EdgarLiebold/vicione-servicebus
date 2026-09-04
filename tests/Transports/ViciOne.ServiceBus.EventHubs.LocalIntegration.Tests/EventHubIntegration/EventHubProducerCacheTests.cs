using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubProducerCacheTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PRODUCER-CACHE", "single-flight-preserves-key-identity")]
    public async Task ConcurrentRequests_ShareOneProducerForTheSameAddressAsync()
    {
        var cache = new EventHubProducerCache<Uri>();
        var address = new Uri("sb://eventhub.local/orders");
        var factoryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factoryCalls = 0;
        var provider = new FakeEventHubProducer();

        Task<IEventHubProducer> first = cache.GetProducerAsync(address, async actualAddress =>
            {
                Assert.Equal(address, actualAddress);
                Interlocked.Increment(ref factoryCalls);
                factoryStarted.TrySetResult();
                await releaseFactory.Task.WaitAsync(TestContext.Current.CancellationToken);
                return provider;
            }, TestContext.Current.CancellationToken);
        await factoryStarted.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task<IEventHubProducer> second = cache.GetProducerAsync(address, _ => throw new InvalidOperationException("A waiter must not invoke a second factory."), TestContext.Current.CancellationToken);

        releaseFactory.TrySetResult();

        IEventHubProducer firstResult = await first.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        IEventHubProducer secondResult = await second.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.Same(firstResult, secondResult);
        Assert.NotSame(provider, firstResult);
        Assert.Equal(1, Volatile.Read(ref factoryCalls));

        await Assert.IsAssignableFrom<IAsyncDisposable>(firstResult).DisposeAsync();
        Assert.Equal(1, provider.DisposalCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PRODUCER-CACHE", "faulted-creation-is-retryable")]
    public async Task FaultedProducerCreation_DoesNotPoisonTheAddressAsync()
    {
        var cache = new EventHubProducerCache<string>();
        var expectedFailure = new InvalidOperationException("provider creation failed");
        var provider = new FakeEventHubProducer();
        var factoryCalls = 0;

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetProducerAsync("orders", _ =>
            {
                Interlocked.Increment(ref factoryCalls);
                return Task.FromException<IEventHubProducer>(expectedFailure);
            }, TestContext.Current.CancellationToken));
        IEventHubProducer recovered = await cache.GetProducerAsync("orders", _ =>
            {
                Interlocked.Increment(ref factoryCalls);
                return Task.FromResult<IEventHubProducer>(provider);
            }, TestContext.Current.CancellationToken);

        Assert.Same(expectedFailure, actual);
        Assert.Equal(2, Volatile.Read(ref factoryCalls));
        await Assert.IsAssignableFrom<IAsyncDisposable>(recovered).DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PRODUCER-CACHE", "cached-operations-refresh-usage-and-delegate")]
    public async Task CachedOperations_RefreshUsageAndDelegateTheExactPayloadAndCancellationAsync()
    {
        var provider = new FakeEventHubProducer();
        var cached = new CachedEventHubProducer<string>("orders", provider);
        var usageSignals = 0;
        ((IResourceUsageSource)cached).Used += () => Interlocked.Increment(ref usageSignals);
        var message = new Message("one");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await cached.ProduceAsync(message, cancellationToken);
        using ConnectHandle handle = cached.ConnectSendObserver(new NoopSendObserver());

        Assert.Equal(2, Volatile.Read(ref usageSignals));
        Assert.Equal(1, provider.ProduceCalls);
        Assert.Same(message, provider.LastPayload);
        Assert.Equal(cancellationToken, provider.LastCancellationToken);
        Assert.Equal(1, provider.ConnectCalls);

        await cached.DisposeAsync();
        Assert.Equal(1, provider.DisposalCount);
    }

    private sealed record Message(string Text);

    private sealed class FakeEventHubProducer : IEventHubProducer, IAsyncDisposable
    {
        private int _connectCalls;
        private int _disposalCount;
        private int _produceCalls;

        public int ConnectCalls => Volatile.Read(ref _connectCalls);
        public int DisposalCount => Volatile.Read(ref _disposalCount);
        public int ProduceCalls => Volatile.Read(ref _produceCalls);
        public object? LastPayload { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public ConnectHandle ConnectSendObserver(ISendObserver observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            Interlocked.Increment(ref _connectCalls);
            return new NoopConnectHandle();
        }

        public Task ProduceAsync<T>(T message, CancellationToken cancellationToken = default) where T : class =>
            RecordAsync(message, cancellationToken);

        public Task ProduceAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : class =>
            RecordAsync(messages, cancellationToken);

        public Task ProduceAsync<T>(T message, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default) where T : class =>
            RecordAsync(message, cancellationToken);

        public Task ProduceAsync<T>(IEnumerable<T> messages, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class => RecordAsync(messages, cancellationToken);

        public Task ProduceAsync<T>(object values, CancellationToken cancellationToken = default) where T : class =>
            RecordAsync(values, cancellationToken);

        public Task ProduceAsync<T>(IEnumerable<object> values, CancellationToken cancellationToken = default) where T : class =>
            RecordAsync(values, cancellationToken);

        public Task ProduceAsync<T>(object values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default) where T : class =>
            RecordAsync(values, cancellationToken);

        public Task ProduceAsync<T>(IEnumerable<object> values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class => RecordAsync(values, cancellationToken);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposalCount);
            return default;
        }

        private Task RecordAsync(object payload, CancellationToken cancellationToken)
        {
            LastPayload = payload;
            LastCancellationToken = cancellationToken;
            Interlocked.Increment(ref _produceCalls);
            return Task.CompletedTask;
        }
    }

    private sealed class NoopSendObserver : ISendObserver
    {
        public Task PreSendAsync<T>(SendContext<T> context) where T : class => Task.CompletedTask;

        public Task PostSendAsync<T>(SendContext<T> context) where T : class => Task.CompletedTask;

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class => Task.CompletedTask;
    }

    private sealed class NoopConnectHandle : ConnectHandle
    {
        public void Disconnect()
        {
        }

        public void Dispose()
        {
        }
    }
}
