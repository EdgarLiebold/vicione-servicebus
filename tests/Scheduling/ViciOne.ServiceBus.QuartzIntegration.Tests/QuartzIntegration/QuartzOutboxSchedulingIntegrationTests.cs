using Quartz;
using ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzOutboxSchedulingIntegrationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-QUARTZ-OUTBOX", "rollback-versus-no-outbox")]
    public async Task FaultingConsumer_CancelsTheQuartzTriggerOnlyWhenTheOutboxOwnsTheScheduleAsync(bool useOutbox)
    {
        TimeSpan timeout = OperationTimeout();
        string queueName = $"quartz-outbox-{NewId.NextGuid():N}";
        string faultsQueueName = $"{queueName}-faults";
        var inputAddress = new Uri($"loopback://localhost/{queueName}");
        var scheduled = new TaskCompletionSource<ScheduledMessage<DeferredPayload>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var faulted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator =>
            {
                configurator.ReceiveEndpoint(queueName, endpoint =>
                {
                    if (useOutbox)
                        endpoint.UseInMemoryOutbox();

                    endpoint.Handler<StartDeferredSchedule>(async context =>
                    {
                        ScheduledMessage<DeferredPayload> handle = await context.Advanced().ScheduleSendAsync(
                            new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            new DeferredPayload(context.Message.CorrelationId),
                            context.CancellationToken);
                        scheduled.TrySetResult(handle);
                        throw new ExpectedOutboxException();
                    });
                });
                configurator.ReceiveEndpoint(faultsQueueName, endpoint =>
                    endpoint.Handler<Fault<StartDeferredSchedule>>(_ =>
                    {
                        faulted.TrySetResult();
                        return Task.CompletedTask;
                    }));
            });
        var scheduledCommands = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
        var cancellationCommands = new ConsumeCompletionObserver<CancelScheduledMessage>(_ => true);
        using ConnectHandle scheduleObserver = fixture.Bus.ConnectConsumeObserver(scheduledCommands);
        using ConnectHandle cancellationObserver = fixture.Bus.ConnectConsumeObserver(cancellationCommands);
        ISendEndpoint input = await fixture.Bus.GetSendEndpointAsync(inputAddress, TestContext.Current.CancellationToken).WaitAsync(timeout, TestContext.Current.CancellationToken);

        await input.SendAsync(new StartDeferredSchedule(NewId.NextGuid()), TestContext.Current.CancellationToken);
        ScheduledMessage<DeferredPayload> handle = await scheduled.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        await scheduledCommands.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await faulted.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        if (useOutbox)
            await cancellationCommands.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        var triggerKey = new TriggerKey(handle.TokenId.ToString("N"));
        bool triggerExists = await fixture.Scheduler.Exists(triggerKey, TestContext.Current.CancellationToken).AsTask()
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(1, scheduledCommands.ObservedCount);
        Assert.Equal(!useOutbox, triggerExists);
        if (useOutbox)
            Assert.Equal(1, cancellationCommands.ObservedCount);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record StartDeferredSchedule(Guid CorrelationId);

    public sealed record DeferredPayload(Guid CorrelationId);

    private sealed class ExpectedOutboxException : Exception
    {
    }
}
