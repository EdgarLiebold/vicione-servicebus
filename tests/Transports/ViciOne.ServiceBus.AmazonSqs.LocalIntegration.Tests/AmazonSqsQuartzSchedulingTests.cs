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
        QuartzSchedulerLease? schedulerLease = null;
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            schedulerLease = configurator.ConfigureInMemoryQuartzScheduler(
                options => options.QueueName = schedulerQueue);
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

            TriggerKey finalizedKey = await finalized.Completed.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(schedule.TokenId.ToString("N"), finalizedKey.Name);
            // Finalization is notified before Quartz removes the trigger from its store.
            // Wait for the observable removal; expiration is a test failure, never success.
            using (var removalTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                removalTimeout.CancelAfter(fixture.OperationTimeout);
                while (await scheduler.Exists(finalizedKey, removalTimeout.Token))
                    await Task.Delay(TimeSpan.FromMilliseconds(10), removalTimeout.Token);
            }
            IReadOnlyCollection<TriggerKey> remainingTriggers = await scheduler
                .GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup(), cancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.DoesNotContain(remainingTriggers, key => key.Name == schedule.TokenId.ToString("N"));
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(1, Volatile.Read(ref deliveryCount));
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
