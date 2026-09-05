using Quartz;
using ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests;

public sealed class AmazonSqsQuartzSchedulingTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0239", "quartz-scheduled-publish-reaches-sqs-consumer-exactly-once")]
    public async Task QuartzScheduledPublish_ReachesTheSqsConsumerOnceAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("quartzpublish");
        string inputQueue = fixture.Name("input");
        string schedulerQueue = fixture.Name("scheduler");
        Guid flowId = Guid.NewGuid();
        var scheduled = NewObservation<ScheduledMessage<QuartzDelivery>>();
        var delivered = NewObservation<Guid>();
        var deliveryCount = 0;
        ISchedulerFactory? schedulerFactory = null;
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ConfigureInMemoryScheduler(out schedulerFactory, schedulerQueue);
            configurator.ReceiveEndpoint(inputQueue, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
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
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpointAsync(new Uri($"queue:{inputQueue}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.SendAsync(new QuartzTrigger(flowId), cancellationToken)
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
            Assert.Null(completedTrigger);
            Assert.Equal(1, Volatile.Read(ref deliveryCount));
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
