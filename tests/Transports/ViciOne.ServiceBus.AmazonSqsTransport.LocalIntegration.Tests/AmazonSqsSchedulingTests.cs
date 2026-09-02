namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

using System.Diagnostics;
using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AmazonSqsSchedulingTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0236", "zero-delay-scheduled-send-reaches-source-queue")]
    public Task ImmediateScheduledSend_ReachesSourceQueue() => AssertImmediateSchedule(publish: false);

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0237", "zero-delay-scheduled-publish-reaches-subscriber")]
    public Task ImmediateScheduledPublish_ReachesSubscriber() => AssertImmediateSchedule(publish: true);

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0240", "two-second-native-delay-is-invisible-before-due-and-then-delivers")]
    public async Task FutureScheduledSend_IsInvisibleUntilDue()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("futureschedule");
        string queueName = fixture.Name("input");
        Guid flowId = Guid.NewGuid();
        var scheduleStarted = NewObservation<long>();
        var delivered = NewObservation<Guid>();
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseDelayedMessageScheduler();
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<ScheduleTrigger>(async context =>
                {
                    try
                    {
                        long startedAt = Stopwatch.GetTimestamp();
                        scheduleStarted.TrySetResult(startedAt);
                        await context.ScheduleSend(TimeSpan.FromSeconds(2), new ScheduledDelivery(context.Message.FlowId),
                            context.CancellationToken);
                    }
                    catch (Exception exception)
                    {
                        scheduleStarted.TrySetException(exception);
                        delivered.TrySetException(exception);
                        throw;
                    }
                });
                endpoint.Handler<ScheduledDelivery>(context =>
                {
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
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new ScheduleTrigger(flowId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            long startedAt = await scheduleStarted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.False(delivered.Task.IsCompleted, delivered.Task.Exception?.ToString());
            Assert.Equal(flowId, await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.InRange(Stopwatch.GetElapsedTime(startedAt), TimeSpan.FromSeconds(1.8), fixture.OperationTimeout);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private static async Task AssertImmediateSchedule(bool publish)
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create(publish ? "schedulepublish" : "schedulesend");
        string queueName = fixture.Name("input");
        Guid flowId = Guid.NewGuid();
        var triggerHandled = NewObservation<Guid>();
        var delivered = NewObservation<Guid>();
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseDelayedMessageScheduler();
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.ConfigureConsumeTopology = publish;
                endpoint.Handler<ScheduleTrigger>(async context =>
                {
                    try
                    {
                        if (publish)
                            await context.SchedulePublish(TimeSpan.Zero, new ScheduledDelivery(context.Message.FlowId), context.CancellationToken);
                        else
                            await context.ScheduleSend(TimeSpan.Zero, new ScheduledDelivery(context.Message.FlowId), context.CancellationToken);

                        triggerHandled.TrySetResult(context.Message.FlowId);
                    }
                    catch (Exception exception)
                    {
                        triggerHandled.TrySetException(exception);
                        delivered.TrySetException(exception);
                        throw;
                    }
                });
                endpoint.Handler<ScheduledDelivery>(context =>
                {
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
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new ScheduleTrigger(flowId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(flowId, await triggerHandled.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(flowId, await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public sealed record ScheduleTrigger(Guid FlowId);
    public sealed record ScheduledDelivery(Guid FlowId);
}
