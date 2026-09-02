namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

using System.Collections.Concurrent;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class ActiveMqSchedulingTests
{
    private static readonly TimeSpan BrokerDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan FutureDelay = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [InlineData(ActiveMqBroker.ArtemisFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0457", "scheduled-send-reaches-destination-exactly-once")]
    public Task ScheduledSend_ReachesDestinationExactlyOnce(string flavor) =>
        AssertScheduledDelivery(flavor, publish: false);

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [InlineData(ActiveMqBroker.ArtemisFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0458", "scheduled-publish-reaches-subscriber-exactly-once")]
    public Task ScheduledPublish_ReachesSubscriberExactlyOnce(string flavor) =>
        AssertScheduledDelivery(flavor, publish: true);

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0460", "completed-schedule-does-not-delay-next-message")]
    public async Task CompletedSchedule_DoesNotCarryIntoTheNextMessage(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "schedule-reset");
        string queueName = fixture.Name("input");
        Guid firstId = Guid.NewGuid();
        Guid secondId = Guid.NewGuid();
        var firstDelivered = NewObservation<Guid>();
        var delivered = NewObservation<Guid[]>();
        var identities = new ConcurrentQueue<Guid>();
        var observer = new ScheduleObserver();
        var receives = new ReceiveCompletionObserver(2);
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseDelayedMessageScheduler();
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<ScheduledDelivery>(context =>
            {
                identities.Enqueue(context.Message.FlowId);
                if (context.Message.FlowId == firstId)
                    firstDelivered.TrySetResult(context.Message.FlowId);
                if (identities.Count == 2)
                    delivered.TrySetResult(identities.ToArray());
                return Task.CompletedTask;
            }));
        });
        using ConnectHandle observerHandle = bus.ConnectSendObserver(observer);
        using ConnectHandle receiveHandle = bus.ConnectReceiveObserver(receives);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IMessageScheduler scheduler = bus.CreateDelayedMessageScheduler();
            DateTime scheduledTime = TimeProvider.System.GetUtcNow().UtcDateTime.Add(BrokerDelay);
            ScheduledMessage schedule = await scheduler.ScheduleSend(
                    new Uri($"queue:{queueName}"),
                    scheduledTime,
                    new ScheduledDelivery(firstId),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(scheduledTime, schedule.ScheduledTime);
            await observer.Scheduled.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(firstId, await firstDelivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new ScheduledDelivery(secondId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Guid[] actual = await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await receives.Completed.WaitAsync(fixture.OperationTimeout, cancellationToken);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            Assert.Equal([firstId, secondId], actual);
            AssertSinglePositiveDelay(BrokerDelay, observer.DelaysFor<ScheduledDelivery>());
            Assert.Equal(2, observer.SendCountFor<ScheduledDelivery>());
            Assert.Equal(2, receives.CompletedCount);
            Assert.Equal(
                new ActiveMqBroker.BrokerQueueStatistics(2, 2, 0, 0, 0),
                await fixture.GetQueueStatistics(queueName, cancellationToken));
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
    [InlineData(ActiveMqBroker.ArtemisFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0461", "future-schedule-is-provider-owned-before-exact-delivery")]
    public async Task FutureSchedule_IsInvisibleUntilDueThenDelivered(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "schedule-future");
        string queueName = fixture.Name("input");
        Guid flowId = Guid.NewGuid();
        var delivered = NewObservation<Guid>();
        var deliveryCount = 0;
        var observer = new ScheduleObserver();
        var receives = new ReceiveCompletionObserver(1);
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseDelayedMessageScheduler();
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<ScheduledDelivery>(context =>
            {
                Interlocked.Increment(ref deliveryCount);
                delivered.TrySetResult(context.Message.FlowId);
                return Task.CompletedTask;
            }));
        });
        using ConnectHandle observerHandle = bus.ConnectSendObserver(observer);
        using ConnectHandle receiveHandle = bus.ConnectReceiveObserver(receives);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IMessageScheduler scheduler = bus.CreateDelayedMessageScheduler();
            DateTime scheduledTime = TimeProvider.System.GetUtcNow().UtcDateTime.Add(FutureDelay);
            ScheduledMessage schedule = await scheduler.ScheduleSend(
                    new Uri($"queue:{queueName}"),
                    scheduledTime,
                    new ScheduledDelivery(flowId),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(scheduledTime, schedule.ScheduledTime);
            await observer.Scheduled.WaitAsync(fixture.OperationTimeout, cancellationToken);
            AssertSinglePositiveDelay(FutureDelay, observer.DelaysFor<ScheduledDelivery>());
            Assert.Equal(1, await fixture.GetScheduledMessageCount(queueName, cancellationToken));
            Assert.Equal(flowId, await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            await receives.Completed.WaitAsync(fixture.OperationTimeout, cancellationToken);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            Assert.Equal(1, Volatile.Read(ref deliveryCount));
            Assert.Equal(1, receives.CompletedCount);
            Assert.Equal(0, await fixture.GetScheduledMessageCount(queueName, cancellationToken));
            Assert.Equal(
                new ActiveMqBroker.BrokerQueueStatistics(1, 1, 0, 0, 0),
                await fixture.GetQueueStatistics(queueName, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task AssertScheduledDelivery(string flavor, bool publish)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, publish ? "schedule-publish" : "schedule-send");
        string queueName = fixture.Name("input");
        string entityName = fixture.Name("scheduled");
        string brokerQueueName = publish
            ? $"Consumer.{queueName}.VirtualTopic.{entityName}"
            : queueName;
        Guid flowId = Guid.NewGuid();
        var delivered = NewObservation<Guid>();
        var deliveryCount = 0;
        var observer = new ScheduleObserver();
        var receives = new ReceiveCompletionObserver(1);
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseDelayedMessageScheduler();
            configurator.MessageTopology.GetMessageTopology<ScheduledDelivery>().SetEntityName(entityName);
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<ScheduledDelivery>(context =>
            {
                Interlocked.Increment(ref deliveryCount);
                delivered.TrySetResult(context.Message.FlowId);
                return Task.CompletedTask;
            }));
        });
        using ConnectHandle observerHandle = bus.ConnectSendObserver(observer);
        using ConnectHandle receiveHandle = bus.ConnectReceiveObserver(receives);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IMessageScheduler scheduler = bus.CreateDelayedMessageScheduler();
            DateTime scheduledTime = TimeProvider.System.GetUtcNow().UtcDateTime.Add(BrokerDelay);
            ScheduledMessage schedule;
            if (publish)
                schedule = await scheduler.SchedulePublish(scheduledTime, new ScheduledDelivery(flowId), cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            else
            {
                schedule = await scheduler.ScheduleSend(
                        new Uri($"queue:{queueName}"),
                        scheduledTime,
                        new ScheduledDelivery(flowId),
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }

            Assert.Equal(scheduledTime, schedule.ScheduledTime);
            await observer.Scheduled.WaitAsync(fixture.OperationTimeout, cancellationToken);
            AssertSinglePositiveDelay(BrokerDelay, observer.DelaysFor<ScheduledDelivery>());
            Assert.Equal(flowId, await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            await receives.Completed.WaitAsync(fixture.OperationTimeout, cancellationToken);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            Assert.Equal(1, Volatile.Read(ref deliveryCount));
            Assert.Equal(1, receives.CompletedCount);
            Assert.Equal(
                new ActiveMqBroker.BrokerQueueStatistics(1, 1, 0, 0, 0),
                await fixture.GetQueueStatistics(brokerQueueName, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void AssertSinglePositiveDelay(TimeSpan requested, TimeSpan[] observed)
    {
        TimeSpan actual = Assert.Single(observed);
        Assert.True(actual > TimeSpan.Zero, $"The broker delay must be positive, but was {actual}.");
        Assert.True(
            actual <= requested,
            $"The remaining broker delay {actual} cannot exceed the requested delay {requested}.");
    }

    public sealed record ScheduledDelivery(Guid FlowId);

    private sealed class ScheduleObserver : ISendObserver
    {
        private readonly ConcurrentQueue<(Type MessageType, TimeSpan? Delay)> _sends = new();
        private readonly TaskCompletionSource _scheduled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Scheduled => _scheduled.Task;

        public TimeSpan[] DelaysFor<TMessage>() where TMessage : class =>
            [.. _sends.Where(item => item.MessageType == typeof(TMessage) && item.Delay.HasValue).Select(item => item.Delay!.Value)];

        public int SendCountFor<TMessage>() where TMessage : class =>
            _sends.Count(item => item.MessageType == typeof(TMessage));

        public Task PreSend<T>(SendContext<T> context) where T : class => Task.CompletedTask;

        public Task PostSend<T>(SendContext<T> context) where T : class
        {
            _sends.Enqueue((typeof(T), context.Delay));
            if (context.Delay.HasValue)
                _scheduled.TrySetResult();
            return Task.CompletedTask;
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception) where T : class
        {
            if (context.Delay.HasValue)
                _scheduled.TrySetException(exception);
            return Task.CompletedTask;
        }
    }

}
