using System.Collections.Concurrent;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

public sealed class ActiveMqOutboxRedeliveryTests
{
    private static readonly TimeSpan BrokerDelay = TimeSpan.FromMilliseconds(100);

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor, OutboxMode.MessageScopedPublish)]
    [InlineData(ActiveMqBroker.AmqpFlavor, OutboxMode.MessageScopedPublish)]
    [InlineData(ActiveMqBroker.OpenWireFlavor, OutboxMode.MessageScopedSend)]
    [InlineData(ActiveMqBroker.AmqpFlavor, OutboxMode.MessageScopedSend)]
    [InlineData(ActiveMqBroker.OpenWireFlavor, OutboxMode.EndpointScopedPublish)]
    [InlineData(ActiveMqBroker.AmqpFlavor, OutboxMode.EndpointScopedPublish)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-OUTBOX-REDELIVERY", "failed-attempts-discard-and-success-releases-exactly-once")]
    public async Task DelayedRedelivery_PublishesAndSendsExactlyOnceAsync(string flavor, OutboxMode mode)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "outbox-redelivery");
        string queueName = fixture.Name("input");
        Guid correlationId = Guid.NewGuid();
        var attempts = new ConcurrentQueue<AttemptObservation>();
        int attemptCount = 0;
        var published = new DeliveryRecorder<OutboxPublished>();
        var sent = new DeliveryRecorder<OutboxSent>();
        var observer = new OutboxTransportObserver();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                if (mode == OutboxMode.EndpointScopedPublish)
                    ConfigureOutbox(endpoint);

                endpoint.Handler<OutboxCommand>(async context =>
                {
                    int attempt = Interlocked.Increment(ref attemptCount) - 1;
                    attempts.Enqueue(new AttemptObservation(context.Advanced().GetRedeliveryCount(), context.Advanced().GetRetryAttempt()));
                    if (mode == OutboxMode.MessageScopedSend)
                    {
                        await context.Advanced().SendAsync(
                            context.Advanced().ReceiveContext.InputAddress,
                            new OutboxSent(context.Message.CorrelationId));
                    }
                    else
                        await context.Advanced().PublishAsync(new OutboxPublished(context.Message.CorrelationId), context.CancellationToken);

                    if (attempt < 2)
                        throw new IntentionalOutboxFailureException(attempt);
                }, handler =>
                {
                    if (mode != OutboxMode.EndpointScopedPublish)
                        ConfigureOutbox(handler);
                });
                endpoint.Handler<OutboxPublished>(published.ObserveAsync);
                endpoint.Handler<OutboxSent>(sent.ObserveAsync);
            });
        });
        using ConnectHandle sendObserver = bus.ConnectSendObserver(observer);
        using ConnectHandle publishObserver = bus.ConnectPublishObserver(observer);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await bus.PublishAsync(new OutboxCommand(correlationId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Guid actual = mode == OutboxMode.MessageScopedSend
                ? await sent.Received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken)
                : await published.Received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            // Stopping is the positive completion barrier for the receive pipeline and broker
            // delivery. Counts are asserted only after no buffered outbox action can still arrive.
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            Assert.Equal(correlationId, actual);
            Assert.Equal(
                [new AttemptObservation(0, 0), new AttemptObservation(0, 1), new AttemptObservation(1, 0)],
                attempts);
            Assert.Equal(mode == OutboxMode.MessageScopedSend ? 1 : 0, sent.DeliveryCount);
            Assert.Equal(mode == OutboxMode.MessageScopedSend ? 0 : 1, published.DeliveryCount);
            Assert.Equal(mode == OutboxMode.MessageScopedSend ? 1 : 0, observer.SendCount);
            Assert.Equal(mode == OutboxMode.MessageScopedSend ? 0 : 1, observer.PublishCount);
            Assert.Equal(0, observer.FaultCount);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static void ConfigureOutbox(IConsumePipeConfigurator configurator)
    {
        configurator.UseDelayedRedelivery(redelivery => redelivery.Interval(1, BrokerDelay));
        configurator.UseMessageRetry(retry => retry.Immediate(1));
        configurator.UseInMemoryOutbox();
    }

    private static void ConfigureOutbox<T>(IHandlerConfigurator<T> configurator)
        where T : class
    {
        configurator.UseDelayedRedelivery(redelivery => redelivery.Interval(1, BrokerDelay));
        configurator.UseMessageRetry(retry => retry.Immediate(1));
        configurator.UseInMemoryOutbox();
    }

    public enum OutboxMode
    {
        MessageScopedPublish,
        MessageScopedSend,
        EndpointScopedPublish,
    }

    private sealed record OutboxCommand(Guid CorrelationId);
    private sealed record OutboxPublished(Guid CorrelationId);
    private sealed record OutboxSent(Guid CorrelationId);
    private sealed record AttemptObservation(int RedeliveryCount, int RetryAttempt);

    private sealed class DeliveryRecorder<T>
        where T : class
    {
        private int _deliveryCount;

        public int DeliveryCount => Volatile.Read(ref _deliveryCount);
        public TaskCompletionSource<Guid> Received { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ObserveAsync(ConsumeContext<T> context)
        {
            Guid correlationId = context.Message switch
            {
                OutboxPublished message => message.CorrelationId,
                OutboxSent message => message.CorrelationId,
                _ => throw new InvalidDataException($"Unexpected outbox message type '{typeof(T)}'."),
            };
            Interlocked.Increment(ref _deliveryCount);
            Received.TrySetResult(correlationId);
            return Task.CompletedTask;
        }
    }

    private sealed class OutboxTransportObserver : ISendObserver, IPublishObserver
    {
        private int _faultCount;
        private int _publishCount;
        private int _sendCount;

        public int FaultCount => Volatile.Read(ref _faultCount);
        public int PublishCount => Volatile.Read(ref _publishCount);
        public int SendCount => Volatile.Read(ref _sendCount);

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (context.Message is OutboxSent)
                Interlocked.Increment(ref _sendCount);
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            if (context.Message is OutboxSent)
                Interlocked.Increment(ref _faultCount);
            return Task.CompletedTask;
        }

        public Task PrePublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            if (context.Message is OutboxPublished)
                Interlocked.Increment(ref _publishCount);
            return Task.CompletedTask;
        }

        public Task PostPublishAsync<T>(PublishContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
            where T : class
        {
            if (context.Message is OutboxPublished)
                Interlocked.Increment(ref _faultCount);
            return Task.CompletedTask;
        }
    }

    private sealed class IntentionalOutboxFailureException(int attempt)
        : Exception($"Intentional outbox failure on attempt {attempt}.");
}
