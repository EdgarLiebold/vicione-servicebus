using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Tests.TestDoubles;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class MessageProducerCacheTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "producer-cache-single-flight-and-stop-release")]
    public async Task ConcurrentRequests_ShareOneProducerAndStoppingTheCacheReleasesItExactlyOnceAsync()
    {
        var cache = new MessageProducerCache();
        IDestination destination = Destination();
        var factoryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factoryCalls = 0;
        var disposalCalls = 0;
        IMessageProducer producer = Producer((method, _) => method.Name switch
        {
            nameof(IDisposable.Dispose) => Record(() => Interlocked.Increment(ref disposalCalls)),
            _ => Default(method.ReturnType),
        });

        try
        {
            Task<IMessageProducer> first = cache.GetMessageProducerAsync(destination, async _ =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    factoryStarted.TrySetResult();
                    await releaseFactory.Task.WaitAsync(TestContext.Current.CancellationToken);
                    return producer;
                }, TestContext.Current.CancellationToken);
            await factoryStarted.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            Task<IMessageProducer> second = cache.GetMessageProducerAsync(destination, _ => throw new InvalidOperationException("A waiter must not invoke a second factory."), TestContext.Current.CancellationToken);

            releaseFactory.TrySetResult();

            IMessageProducer firstResult = await first.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            IMessageProducer secondResult = await second.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            Assert.Same(firstResult, secondResult);
            Assert.Equal(1, Volatile.Read(ref factoryCalls));
            Assert.Equal(0, Volatile.Read(ref disposalCalls));
        }
        finally
        {
            await cache.StopAsync("test complete", TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, Volatile.Read(ref disposalCalls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "producer-cache-faulted-creation-is-retryable")]
    public async Task FaultedProducerCreation_DoesNotPoisonTheDestinationKeyAsync()
    {
        var cache = new MessageProducerCache();
        IDestination destination = Destination();
        var expectedFailure = new InvalidOperationException("provider creation failed");
        IMessageProducer producer = Producer((method, _) => Default(method.ReturnType));
        var factoryCalls = 0;

        try
        {
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetMessageProducerAsync(destination, _ =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    return Task.FromException<IMessageProducer>(expectedFailure);
                }, TestContext.Current.CancellationToken));
            IMessageProducer recovered = await cache.GetMessageProducerAsync(destination, _ =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    return Task.FromResult(producer);
                }, TestContext.Current.CancellationToken);

            Assert.Same(expectedFailure, actual);
            Assert.NotSame(producer, recovered);
            Assert.Equal(2, Volatile.Read(ref factoryCalls));
        }
        finally
        {
            await cache.StopAsync("test complete", TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "cached-producer-operations-refresh-resource-usage")]
    public async Task ProducerOperations_RefreshUsageAndDelegateToTheOwnedProducerAsync()
    {
        var delegatedCalls = 0;
        IMessage message = InterfaceProxy<IMessage>.Create((method, _) => Default(method.ReturnType));
        IMessageProducer producer = Producer((method, args) => method.Name switch
        {
            nameof(IMessageProducer.Send) => Record(() =>
            {
                Assert.Same(message, args![0]);
                Interlocked.Increment(ref delegatedCalls);
            }),
            nameof(IMessageProducer.SendAsync) => Record(
                () =>
                {
                    Assert.Same(message, args![0]);
                    Interlocked.Increment(ref delegatedCalls);
                },
                Task.CompletedTask),
            nameof(IMessageProducer.CreateMessage) => Record(
                () => Interlocked.Increment(ref delegatedCalls),
                message),
            _ => Default(method.ReturnType),
        });
        var cached = new CachedMessageProducer(Destination(), producer);
        var usageSignals = 0;
        ((IResourceUsageSource)cached).Used += () => Interlocked.Increment(ref usageSignals);

        cached.Send(message);
        await cached.SendAsync(message);
        Assert.Same(message, cached.CreateMessage());

        Assert.Equal(3, Volatile.Read(ref delegatedCalls));
        Assert.Equal(3, Volatile.Read(ref usageSignals));
    }

    private static IDestination Destination() =>
        InterfaceProxy<IDestination>.Create((method, _) => Default(method.ReturnType));

    private static IMessageProducer Producer(Func<System.Reflection.MethodInfo, object?[]?, object?> handler) =>
        InterfaceProxy<IMessageProducer>.Create(handler);

    private static object? Record(Action action, object? result = null)
    {
        action();
        return result;
    }

    private static object? Default(Type returnType) =>
        returnType == typeof(void)
            ? null
            : returnType.IsValueType
                ? Activator.CreateInstance(returnType)
                : null;
}
