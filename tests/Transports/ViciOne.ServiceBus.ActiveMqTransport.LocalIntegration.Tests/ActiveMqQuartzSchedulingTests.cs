using Quartz;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

public sealed class ActiveMqQuartzSchedulingTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0459", "quartz-scheduled-publish-reaches-consumer-exactly-once")]
    public async Task QuartzScheduledPublish_ReachesConsumerExactlyOnce(string flavor)
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
        ISchedulerFactory? schedulerFactory = null;
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseInMemoryScheduler(out schedulerFactory, schedulerQueue);
            configurator.MessageTopology.GetMessageTopology<QuartzDelivery>().SetEntityName(deliveryEntityName);
            configurator.ReceiveEndpoint(inputQueue, endpoint =>
            {
                endpoint.Handler<QuartzTrigger>(async context =>
                {
                    try
                    {
                        ScheduledMessage<QuartzDelivery> result = await context.SchedulePublish(
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
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{inputQueue}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new QuartzTrigger(flowId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ScheduledMessage<QuartzDelivery> schedule = await scheduled.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(flowId, await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(1, Volatile.Read(ref deliveryCount));

            IScheduler scheduler = await Assert.IsAssignableFrom<ISchedulerFactory>(schedulerFactory)
                .GetScheduler(cancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ITrigger? completedTrigger = await scheduler.GetTrigger(
                    new TriggerKey(schedule.TokenId.ToString("N")),
                    cancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Null(completedTrigger?.NextFireTimeUtc);

            await receives.Completed.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            Assert.Equal(1, Volatile.Read(ref deliveryCount));
            Assert.Equal(3, receives.CompletedCount);
            Assert.Equal(
                new ActiveMqBroker.BrokerQueueStatistics(1, 1, 0, 0, 0),
                await fixture.GetQueueStatistics(inputQueue, cancellationToken));
            Assert.Equal(
                new ActiveMqBroker.BrokerQueueStatistics(1, 1, 0, 0, 0),
                await fixture.GetQueueStatistics(schedulerQueue, cancellationToken));
            Assert.Equal(
                new ActiveMqBroker.BrokerQueueStatistics(1, 1, 0, 0, 0),
                await fixture.GetQueueStatistics(deliveryQueue, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public sealed record QuartzTrigger(Guid FlowId);
    public sealed record QuartzDelivery(Guid FlowId);
}
