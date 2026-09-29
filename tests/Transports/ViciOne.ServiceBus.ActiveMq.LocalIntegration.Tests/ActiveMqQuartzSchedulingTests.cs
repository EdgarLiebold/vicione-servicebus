using Quartz;
using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

public sealed class ActiveMqQuartzSchedulingTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0459", "quartz-scheduled-publish-reaches-consumer-exactly-once")]
    public async Task QuartzScheduledPublish_ReachesConsumerExactlyOnceAsync(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "quartz-publish");
        string inputQueue = fixture.Name("input");
        string schedulerQueue = fixture.Name("scheduler");
        string deliveryEntityName = fixture.Name("delivery");
        string deliveryQueue = $"Consumer.{inputQueue}.VirtualTopic.{deliveryEntityName}";
        Guid flowId = Guid.NewGuid();
        var scheduled = NewObservation<ScheduledMessage<QuartzDelivery>>();
        var delivered = NewObservation<Guid>();
        var deliveryCount = 0;
        // Trigger consumption, scheduler-command consumption, and final delivery must all complete
        // before the terminal broker-state assertion is meaningful.
        var receives = new ReceiveCompletionObserver(3);
        QuartzSchedulerLease? schedulerLease = null;
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            schedulerLease = configurator.ConfigureInMemoryQuartzScheduler(
                options => options.QueueName = schedulerQueue);
            configurator.MessageTopology.GetMessageTopology<QuartzDelivery>().SetEntityName(deliveryEntityName);
            configurator.ReceiveEndpoint(inputQueue, endpoint =>
            {
                endpoint.Handler<QuartzTrigger>(async context =>
                {
                    try
                    {
                        ScheduledMessage<QuartzDelivery> result = await context.Advanced().SchedulePublishAsync(
                            TimeSpan.FromSeconds(1),
                            new QuartzDelivery(context.Message.FlowId),
                            context.CancellationToken);
                        scheduled.TrySetResult(result);
                    }
                    catch (Exception exception)
                    {
                        scheduled.TrySetException(exception);
                        delivered.TrySetException(exception);
                        throw;
                    }
                });
                endpoint.Handler<QuartzDelivery>(context =>
                {
                    Interlocked.Increment(ref deliveryCount);
                    delivered.TrySetResult(context.Message.FlowId);
                    return Task.CompletedTask;
                });
            });
        });
        using ConnectHandle receiveHandle = bus.ConnectReceiveObserver(receives);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IScheduler scheduler = await Assert.IsType<QuartzSchedulerLease>(schedulerLease).SchedulerFactory
                .GetScheduler(cancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            var finalized = new TriggerFinalizationObserver();
            scheduler.ListenerManager.AddSchedulerListener(finalized);
            ISendEndpoint input = await bus.GetSendEndpointAsync(new Uri($"queue:{inputQueue}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.SendAsync(new QuartzTrigger(flowId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ScheduledMessage<QuartzDelivery> schedule = await scheduled.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(flowId, await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(1, Volatile.Read(ref deliveryCount));

            await receives.Completed.WaitAsync(fixture.OperationTimeout, cancellationToken);
            TriggerKey finalizedKey = await finalized.Completed.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(schedule.TokenId.ToString("N"), finalizedKey.Name);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            Assert.Equal(1, Volatile.Read(ref deliveryCount));
            Assert.Equal(3, receives.CompletedCount);
            Assert.Equal(
                new ActiveMqBroker.BrokerQueueStatistics(1, 1, 0, 0, 0),
                await fixture.GetQueueStatisticsAsync(inputQueue, cancellationToken));
            Assert.Equal(
                new ActiveMqBroker.BrokerQueueStatistics(1, 1, 0, 0, 0),
                await fixture.GetQueueStatisticsAsync(schedulerQueue, cancellationToken));
            Assert.Equal(
                new ActiveMqBroker.BrokerQueueStatistics(1, 1, 0, 0, 0),
                await fixture.GetQueueStatisticsAsync(deliveryQueue, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            if (schedulerLease is not null)
                await schedulerLease.DisposeAsync();
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class TriggerFinalizationObserver : ISchedulerListener
    {
        private readonly TaskCompletionSource<TriggerKey> _completed = NewObservation<TriggerKey>();

        public Task<TriggerKey> Completed => _completed.Task;

        public ValueTask TriggerFinalized(IScheduler scheduler, ITrigger trigger, CancellationToken cancellationToken)
        {
            _completed.TrySetResult(trigger.Key);
            return ValueTask.CompletedTask;
        }
    }

    public sealed record QuartzTrigger(Guid FlowId);
    public sealed record QuartzDelivery(Guid FlowId);
}
