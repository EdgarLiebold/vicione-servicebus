namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

using System.Collections.Concurrent;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class ActiveMqRedeliveryTests
{
    private static readonly TimeSpan BrokerDelay = TimeSpan.FromMilliseconds(100);

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [InlineData(ActiveMqBroker.ArtemisFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0396", "delayed-redelivery-routes-each-message-type-and-stops-at-configured-limit")]
    public async Task DelayedRedelivery_RoutesEachMessageTypeAndStopsAtConfiguredLimit(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "redelivery-limit");
        string queueName = fixture.Name("input");
        Guid correlationId = Guid.NewGuid();
        var firstAttempts = 0;
        var secondAttempts = 0;
        var firstRedeliveryCounts = new ConcurrentQueue<int>();
        var secondRedeliveryCounts = new ConcurrentQueue<int>();
        var firstFault = NewObservation<Fault<FirstAttemptMessage>>();
        var secondFault = NewObservation<Fault<SecondAttemptMessage>>();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.UseDelayedRedelivery(policy => policy.Intervals(BrokerDelay, BrokerDelay));
                endpoint.Handler<FirstAttemptMessage>(context =>
                {
                    Interlocked.Increment(ref firstAttempts);
                    firstRedeliveryCounts.Enqueue(context.GetRedeliveryCount());
                    throw new IntentionalFailure();
                });
                endpoint.Handler<SecondAttemptMessage>(context =>
                {
                    Interlocked.Increment(ref secondAttempts);
                    secondRedeliveryCounts.Enqueue(context.GetRedeliveryCount());
                    throw new IntentionalFailure();
                });
            });
        });
        using ConnectHandle firstHandle = bus.ConnectHandler<Fault<FirstAttemptMessage>>(context =>
        {
            if (context.Message.Message.CorrelationId == correlationId)
                firstFault.TrySetResult(context.Message);
            return Task.CompletedTask;
        });
        using ConnectHandle secondHandle = bus.ConnectHandler<Fault<SecondAttemptMessage>>(context =>
        {
            if (context.Message.Message.CorrelationId == correlationId)
                secondFault.TrySetResult(context.Message);
            return Task.CompletedTask;
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(
                    new FirstAttemptMessage(correlationId),
                    context => context.FaultAddress = bus.Address,
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(
                    new SecondAttemptMessage(correlationId),
                    context => context.FaultAddress = bus.Address,
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Fault<FirstAttemptMessage> first = await firstFault.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Fault<SecondAttemptMessage> second = await secondFault.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(correlationId, first.Message.CorrelationId);
            Assert.Equal(correlationId, second.Message.CorrelationId);
            Assert.Equal(TypeCache<IntentionalFailure>.ShortName, Assert.Single(first.Exceptions).ExceptionType);
            Assert.Equal(TypeCache<IntentionalFailure>.ShortName, Assert.Single(second.Exceptions).ExceptionType);
            Assert.Equal(3, firstAttempts);
            Assert.Equal(3, secondAttempts);
            Assert.Equal([0, 1, 2], firstRedeliveryCounts);
            Assert.Equal([0, 1, 2], secondRedeliveryCounts);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0397", "handler-delayed-redelivery-stops-at-configured-limit")]
    public async Task DelayedRetry_StopsAtTheConfiguredLimit(string flavor)
    {
        FaultRun result = await RunFaultingMessage(
            flavor,
            "handler-redelivery",
            configureEndpoint: null,
            handler => handler.UseDelayedRedelivery(policy => policy.Intervals(BrokerDelay, BrokerDelay)));

        Assert.Equal(3, result.Attempts);
        Assert.Equal([0, 1, 2], result.RedeliveryCounts);
        Assert.Equal(TypeCache<IntentionalFailure>.ShortName, Assert.Single(result.Fault.Exceptions).ExceptionType);
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0398", "no-redelivery-policy-faults-after-one-attempt")]
    public async Task NoRetryPolicy_FaultsWithoutBrokerDelay(string flavor)
    {
        var observer = new ScheduledSendObserver<AttemptMessage>(expectedCount: 1);
        FaultRun result = await RunFaultingMessage(
            flavor,
            "no-redelivery",
            configureEndpoint: null,
            handler => handler.UseDelayedRedelivery(policy => policy.None()),
            observer);

        Assert.Equal(1, result.Attempts);
        Assert.Equal([0], result.RedeliveryCounts);
        Assert.False(observer.Completion.IsCompleted);
        Assert.Empty(observer.Delays);
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0399", "explicit-defer-reaches-the-third-delivery")]
    public async Task ExplicitDefer_ReachesTheThirdDelivery(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "explicit-defer");
        string queueName = fixture.Name("input");
        Guid correlationId = Guid.NewGuid();
        var scheduled = new ScheduledSendObserver<AttemptMessage>(expectedCount: 2);
        var delivered = NewObservation<ConsumeContext<AttemptMessage>>();
        var attempts = 0;
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<AttemptMessage>(async context =>
                {
                    if (Interlocked.Increment(ref attempts) <= 2)
                    {
                        await context.Defer(BrokerDelay);
                        return;
                    }

                    delivered.TrySetResult(context);
                });
            });
        });
        using ConnectHandle observerHandle = bus.ConnectSendObserver(scheduled);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new AttemptMessage(correlationId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            await scheduled.Completion.WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<AttemptMessage> actual = await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal([BrokerDelay, BrokerDelay], scheduled.Delays);
            Assert.Equal(correlationId, actual.Message.CorrelationId);
            Assert.Equal(2, actual.GetRedeliveryCount());
            Assert.Equal(3, attempts);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0400", "interval-retry-stops-at-configured-limit")]
    public async Task IntervalRetry_StopsAtTheConfiguredLimit(string flavor)
    {
        var retryObserver = new RetryDelayObserver();
        FaultRun result = await RunFaultingMessage(
            flavor,
            "interval-retry",
            endpoint => endpoint.UseMessageRetry(policy =>
            {
                policy.Intervals(BrokerDelay, BrokerDelay);
                policy.ConnectRetryObserver(retryObserver);
            }),
            configureHandler: null);

        Assert.Equal(3, result.Attempts);
        Assert.Equal([0, 0, 0], result.RedeliveryCounts);
        Assert.Equal([BrokerDelay, BrokerDelay], retryObserver.Delays);
        Assert.Equal(1, retryObserver.TerminalFaultCount);
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0402", "retry-and-redelivery-compose-exact-attempt-counts")]
    public async Task RetryAndDelayedRedelivery_ComposeExactAttemptCounts(string flavor)
    {
        FaultRun result = await RunFaultingMessage(
            flavor,
            "retry-redelivery",
            endpoint =>
            {
                endpoint.UseDelayedRedelivery(policy => policy.Interval(1, BrokerDelay));
                endpoint.UseMessageRetry(policy => policy.Immediate(2));
            },
            configureHandler: null);

        Assert.Equal(6, result.Attempts);
        Assert.Equal([0, 0, 0, 1, 1, 1], result.RedeliveryCounts);
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0403", "each-broker-delay-follows-its-causal-schedule-signal")]
    public async Task BrokerDelay_RedeliversAfterEachCausalScheduleSignal(string flavor)
    {
        var observer = new ScheduledSendObserver<AttemptMessage>(expectedCount: 2);
        TaskCompletionSource<int> thirdDelivery = NewObservation<int>();
        var redeliveryCounts = new ConcurrentQueue<int>();
        int attempts = 0;

        await RunSuccessfulRedelivery(
            flavor,
            "causal-delay",
            observer,
            thirdDelivery.Task,
            context =>
            {
                int attempt = Interlocked.Increment(ref attempts);
                redeliveryCounts.Enqueue(context.GetRedeliveryCount());
                if (attempt <= 2)
                    throw new IntentionalFailure();

                thirdDelivery.TrySetResult(context.GetRedeliveryCount());
                return Task.CompletedTask;
            });

        int redeliveryCount = await thirdDelivery.Task.WaitAsync(observer.OperationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal([BrokerDelay, BrokerDelay], observer.Delays);
        Assert.Equal([0, 1, 2], redeliveryCounts);
        Assert.Equal(2, redeliveryCount);
        Assert.Equal(3, attempts);
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0404", "defer-callback-executes-on-each-defer-before-third-delivery")]
    public async Task DeferCallback_ExecutesOnEachDeferBeforeThirdDelivery(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "defer-callback");
        string queueName = fixture.Name("input");
        var scheduled = new ScheduledSendObserver<AttemptMessage>(expectedCount: 2);
        var callbackCount = 0;
        var attempts = 0;
        var delivered = NewObservation<(int RedeliveryCount, int CallbackCount)>();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<AttemptMessage>(async context =>
                {
                    if (Interlocked.Increment(ref attempts) <= 2)
                    {
                        await context.Defer(BrokerDelay, (_, _) => Interlocked.Increment(ref callbackCount));
                        return;
                    }

                    delivered.TrySetResult((context.GetRedeliveryCount(), Volatile.Read(ref callbackCount)));
                });
            });
        });
        using ConnectHandle observerHandle = bus.ConnectSendObserver(scheduled);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new AttemptMessage(Guid.NewGuid()), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            await scheduled.Completion.WaitAsync(fixture.OperationTimeout, cancellationToken);
            (int redeliveryCount, int observedCallbackCount) = await delivered.Task.WaitAsync(
                fixture.OperationTimeout,
                cancellationToken);

            Assert.Equal([BrokerDelay, BrokerDelay], scheduled.Delays);
            Assert.Equal(2, redeliveryCount);
            Assert.Equal(2, observedCallbackCount);
            Assert.Equal(2, callbackCount);
            Assert.Equal(3, attempts);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task<FaultRun> RunFaultingMessage(
        string flavor,
        string purpose,
        Action<IActiveMqReceiveEndpointConfigurator>? configureEndpoint,
        Action<IHandlerConfigurator<AttemptMessage>>? configureHandler,
        ScheduledSendObserver<AttemptMessage>? observer = null)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, purpose);
        string queueName = fixture.Name("input");
        Guid correlationId = Guid.NewGuid();
        var attempts = 0;
        var counts = new ConcurrentQueue<int>();
        var faulted = NewObservation<Fault<AttemptMessage>>();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                configureEndpoint?.Invoke(endpoint);
                Task Handler(ConsumeContext<AttemptMessage> context)
                {
                    Interlocked.Increment(ref attempts);
                    counts.Enqueue(context.GetRedeliveryCount());
                    throw new IntentionalFailure();
                }

                if (configureHandler is null)
                    endpoint.Handler<AttemptMessage>(Handler);
                else
                    endpoint.Handler<AttemptMessage>(Handler, configureHandler);
            });
        });
        using ConnectHandle faultHandle = bus.ConnectHandler<Fault<AttemptMessage>>(context =>
        {
            if (context.Message.Message.CorrelationId == correlationId)
                faulted.TrySetResult(context.Message);
            return Task.CompletedTask;
        });
        using ConnectHandle? observerHandle = observer is null ? null : bus.ConnectSendObserver(observer);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(
                    new AttemptMessage(correlationId),
                    context => context.FaultAddress = bus.Address,
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Fault<AttemptMessage> fault = await faulted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            return new FaultRun(attempts, counts.ToArray(), fault);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task RunSuccessfulRedelivery(
        string flavor,
        string purpose,
        ScheduledSendObserver<AttemptMessage> observer,
        Task completion,
        MessageHandler<AttemptMessage> handler)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, purpose);
        string queueName = fixture.Name("input");
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.UseDelayedRedelivery(policy => policy.Intervals(BrokerDelay, BrokerDelay));
                endpoint.Handler<AttemptMessage>(handler);
            });
        });
        using ConnectHandle observerHandle = bus.ConnectSendObserver(observer);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            observer.OperationTimeout = fixture.OperationTimeout;
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new AttemptMessage(Guid.NewGuid()), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await observer.Completion.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await completion.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record AttemptMessage(Guid CorrelationId);
    private sealed record FirstAttemptMessage(Guid CorrelationId);
    private sealed record SecondAttemptMessage(Guid CorrelationId);
    private sealed record FaultRun(int Attempts, int[] RedeliveryCounts, Fault<AttemptMessage> Fault);
    private sealed class IntentionalFailure : Exception;

    private sealed class RetryDelayObserver : IRetryObserver
    {
        private readonly ConcurrentQueue<TimeSpan?> _delays = new();
        private int _terminalFaultCount;

        public TimeSpan?[] Delays => _delays.ToArray();
        public int TerminalFaultCount => Volatile.Read(ref _terminalFaultCount);

        public Task PostCreate<T>(RetryPolicyContext<T> context)
            where T : class, PipeContext => Task.CompletedTask;

        public Task PostFault<T>(RetryContext<T> context)
            where T : class, PipeContext => Task.CompletedTask;

        public Task PreRetry<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            _delays.Enqueue(context.Delay);
            return Task.CompletedTask;
        }

        public Task RetryFault<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            Interlocked.Increment(ref _terminalFaultCount);
            return Task.CompletedTask;
        }

        public Task RetryComplete<T>(RetryContext<T> context)
            where T : class, PipeContext => Task.CompletedTask;
    }

    private sealed class ScheduledSendObserver<TMessage> : ISendObserver
        where TMessage : class
    {
        private readonly ConcurrentQueue<TimeSpan> _delays = new();
        private readonly int _expectedCount;
        private readonly TaskCompletionSource<int> _scheduled = NewObservation<int>();

        public ScheduledSendObserver(int expectedCount)
        {
            if (expectedCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedCount));

            _expectedCount = expectedCount;
        }

        public Task<int> Completion => _scheduled.Task;
        public TimeSpan[] Delays => _delays.ToArray();
        public TimeSpan OperationTimeout { get; set; }

        public Task PreSend<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PostSend<T>(SendContext<T> context)
            where T : class
        {
            if (typeof(T) == typeof(TMessage) && context.Delay is { } delay)
            {
                _delays.Enqueue(delay);
                int count = _delays.Count;
                if (count >= _expectedCount)
                    _scheduled.TrySetResult(count);
            }
            return Task.CompletedTask;
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            if (typeof(T) == typeof(TMessage) && context.Delay.HasValue)
                _scheduled.TrySetException(exception);
            return Task.CompletedTask;
        }
    }
}
