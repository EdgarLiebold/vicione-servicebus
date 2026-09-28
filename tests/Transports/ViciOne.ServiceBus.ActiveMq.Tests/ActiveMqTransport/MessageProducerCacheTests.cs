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

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "t79-cached-producer-rejects-missing-owners")]
    public void CachedProducer_RejectsMissingDestinationAndNativeProducerAtConstruction()
    {
        IDestination destination = Destination();
        IMessageProducer producer = Producer((method, _) => Default(method.ReturnType));

        Assert.Equal("destination", Assert.Throws<ArgumentNullException>(() =>
            new CachedMessageProducer(null!, producer)).ParamName);
        Assert.Equal("producer", Assert.Throws<ArgumentNullException>(() =>
            new CachedMessageProducer(destination, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "t79-null-producer-factory-does-not-poison-cache-key")]
    public async Task NullProducerFactory_DoesNotPoisonTheDestinationAndHealthyRetryOwnsItsDisposalAsync()
    {
        var cache = new MessageProducerCache();
        IDestination destination = Destination();
        IMessage message = InterfaceProxy<IMessage>.Create((method, _) => Default(method.ReturnType));
        var factoryCalls = 0;
        var sends = 0;
        var disposals = 0;
        IMessageProducer producer = Producer((method, args) => method.Name switch
        {
            nameof(IMessageProducer.Send) => Record(() =>
            {
                Assert.Same(message, Assert.Single(args!));
                Interlocked.Increment(ref sends);
            }),
            nameof(IDisposable.Dispose) => Record(() => Interlocked.Increment(ref disposals)),
            _ => Default(method.ReturnType),
        });

        try
        {
            ArgumentNullException rejected = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                cache.GetMessageProducerAsync(destination, _ =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    return Task.FromResult<IMessageProducer>(null!);
                }, TestContext.Current.CancellationToken));
            Assert.Equal("producer", rejected.ParamName);

            IMessageProducer recovered = await cache.GetMessageProducerAsync(destination, _ =>
            {
                Interlocked.Increment(ref factoryCalls);
                return Task.FromResult(producer);
            }, TestContext.Current.CancellationToken);
            recovered.Send(message);
            Assert.Same(recovered, await cache.GetMessageProducerAsync(destination,
                _ => throw new InvalidOperationException("The healthy producer must remain cached."),
                TestContext.Current.CancellationToken));
            Assert.Equal(2, Volatile.Read(ref factoryCalls));
            Assert.Equal(1, Volatile.Read(ref sends));
            Assert.Equal(0, Volatile.Read(ref disposals));
        }
        finally
        {
            await cache.StopAsync("test complete", TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, Volatile.Read(ref disposals));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "t79-explicit-send-settings-and-async-failure-preserve-ownership")]
    public async Task ExplicitSendSettings_ReachTheNativeProducerAndFailedAsyncSendReportsOneUsageAsync()
    {
        IDestination owningDestination = Destination();
        IDestination explicitDestination = Destination();
        IMessage message = InterfaceProxy<IMessage>.Create((method, _) => Default(method.ReturnType));
        const MsgDeliveryMode mode = MsgDeliveryMode.NonPersistent;
        const MsgPriority priority = MsgPriority.Highest;
        TimeSpan lifetime = TimeSpan.FromMinutes(17);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expectedFailure = new InvalidOperationException("native send rejected");
        var calls = new List<(string Name, object?[] Arguments)>();
        IMessageProducer producer = Producer((method, args) =>
        {
            if (method.Name is not (nameof(IMessageProducer.Send) or nameof(IMessageProducer.SendAsync))
                || args?.Length != 5)
                throw new InvalidOperationException($"Unexpected producer call: {method.Name}.");

            calls.Add((method.Name, args));
            return method.Name == nameof(IMessageProducer.SendAsync) ? pending.Task : null;
        });
        var cached = new CachedMessageProducer(owningDestination, producer);
        var usageSignals = 0;
        ((IResourceUsageSource)cached).Used += () => usageSignals++;

        cached.Send(explicitDestination, message, mode, priority, lifetime);
        Task failedSend = cached.SendAsync(explicitDestination, message, mode, priority, lifetime);

        Assert.Same(pending.Task, failedSend);
        Assert.Equal(2, usageSignals);
        Assert.Equal([nameof(IMessageProducer.Send), nameof(IMessageProducer.SendAsync)],
            calls.Select(call => call.Name));
        Assert.All(calls, call =>
        {
            Assert.Same(explicitDestination, call.Arguments[0]);
            Assert.Same(message, call.Arguments[1]);
            Assert.Equal(mode, call.Arguments[2]);
            Assert.Equal(priority, call.Arguments[3]);
            Assert.Equal(lifetime, call.Arguments[4]);
        });

        pending.TrySetException(expectedFailure);
        Assert.Same(expectedFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => failedSend));
        Assert.Equal(2, usageSignals);
        Assert.Equal(2, calls.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "t79-synchronous-native-async-throw-reports-usage-first")]
    public void NativeAsyncSendThrow_ReportsOneUsageBeforePropagatingTheExactSynchronousFailure()
    {
        IDestination destination = Destination();
        IMessage message = InterfaceProxy<IMessage>.Create((method, _) => Default(method.ReturnType));
        var expectedFailure = new InvalidOperationException("native producer already closed");
        var nativeCalls = 0;
        var usageSignals = 0;
        IMessageProducer producer = Producer((method, args) =>
        {
            Assert.Equal(nameof(IMessageProducer.SendAsync), method.Name);
            Assert.Same(message, Assert.Single(args!));
            Assert.Equal(1, usageSignals);
            nativeCalls++;
            throw expectedFailure;
        });
        var cached = new CachedMessageProducer(destination, producer);
        ((IResourceUsageSource)cached).Used += () => usageSignals++;

        Action invoke = () => _ = cached.SendAsync(message);
        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(invoke);

        Assert.Same(expectedFailure, actual);
        Assert.Equal(1, nativeCalls);
        Assert.Equal(1, usageSignals);
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
