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
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "canceled-producer-waiter-preserves-shared-creation")]
    public async Task CanceledWaiter_DoesNotCancelAnotherSendersProducerCreationAsync()
    {
        var cache = new MessageProducerCache();
        IDestination destination = Destination();
        var factoryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var canceledWaiter = new CancellationTokenSource();
        var factoryCalls = 0;
        var disposalCalls = 0;
        IMessageProducer producer = Producer((method, _) => method.Name switch
        {
            nameof(IDisposable.Dispose) => Record(() => Interlocked.Increment(ref disposalCalls)),
            _ => Default(method.ReturnType),
        });

        try
        {
            Task<IMessageProducer> first = cache.GetMessageProducerWithCancellationAsync(destination, async (_, creationToken) =>
            {
                Interlocked.Increment(ref factoryCalls);
                factoryStarted.TrySetResult();
                await releaseFactory.Task.WaitAsync(creationToken);
                return producer;
            }, canceledWaiter.Token);
            await factoryStarted.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            Task<IMessageProducer> second = cache.GetMessageProducerWithCancellationAsync(destination,
                (_, _) => throw new InvalidOperationException("A shared waiter must not create another producer."),
                TestContext.Current.CancellationToken);

            canceledWaiter.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
            releaseFactory.TrySetResult();

            IMessageProducer survivor = await second.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            IMessageProducer cached = await cache.GetMessageProducerWithCancellationAsync(destination,
                (_, _) => throw new InvalidOperationException("A committed producer must be reused."),
                TestContext.Current.CancellationToken);
            Assert.Same(survivor, cached);
            Assert.Equal(1, Volatile.Read(ref factoryCalls));
            Assert.Equal(0, Volatile.Read(ref disposalCalls));
        }
        finally
        {
            releaseFactory.TrySetResult();
            await cache.StopAsync("test complete", TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, Volatile.Read(ref disposalCalls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "independent-destinations-create-and-release-independent-producers")]
    public async Task DifferentDestinations_CreateConcurrentlyAndReleaseTheirOwnProducersAsync()
    {
        var cache = new MessageProducerCache();
        IDestination firstDestination = Destination();
        IDestination secondDestination = Destination();
        var bothFactoriesStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactories = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factoryCalls = 0;
        var firstDisposals = 0;
        var secondDisposals = 0;
        IMessageProducer firstProducer = Producer((method, _) => method.Name switch
        {
            nameof(IDisposable.Dispose) => Record(() => Interlocked.Increment(ref firstDisposals)),
            _ => Default(method.ReturnType),
        });
        IMessageProducer secondProducer = Producer((method, _) => method.Name switch
        {
            nameof(IDisposable.Dispose) => Record(() => Interlocked.Increment(ref secondDisposals)),
            _ => Default(method.ReturnType),
        });

        async Task<IMessageProducer> CreateAsync(IDestination destination)
        {
            if (Interlocked.Increment(ref factoryCalls) == 2)
                bothFactoriesStarted.TrySetResult();
            await releaseFactories.Task.WaitAsync(TestContext.Current.CancellationToken);
            return ReferenceEquals(destination, firstDestination) ? firstProducer : secondProducer;
        }

        try
        {
            Task<IMessageProducer> first = cache.GetMessageProducerAsync(firstDestination, CreateAsync, TestContext.Current.CancellationToken);
            Task<IMessageProducer> second = cache.GetMessageProducerAsync(secondDestination, CreateAsync, TestContext.Current.CancellationToken);
            await bothFactoriesStarted.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            releaseFactories.TrySetResult();

            IMessageProducer firstResult = await first.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            IMessageProducer secondResult = await second.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            Assert.NotSame(firstResult, secondResult);
            Assert.Equal(2, Volatile.Read(ref factoryCalls));
            Assert.Same(firstResult, await cache.GetMessageProducerAsync(firstDestination,
                _ => throw new InvalidOperationException("The first producer must remain cached."), TestContext.Current.CancellationToken));
            Assert.Same(secondResult, await cache.GetMessageProducerAsync(secondDestination,
                _ => throw new InvalidOperationException("The second producer must remain cached."), TestContext.Current.CancellationToken));
        }
        finally
        {
            releaseFactories.TrySetResult();
            await cache.StopAsync("test complete", TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, Volatile.Read(ref firstDisposals));
        Assert.Equal(1, Volatile.Read(ref secondDisposals));
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
