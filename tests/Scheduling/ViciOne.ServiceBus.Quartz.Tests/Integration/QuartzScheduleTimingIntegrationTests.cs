using Quartz;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzScheduleTimingIntegrationTests
{
    private static readonly Uri Destination = new("loopback://localhost/quartz-timing-destination");
    private static readonly DateTimeOffset FarFuture = new(2100, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Theory]
    [InlineData(-3600)]
    [InlineData(0)]
    [InlineData(1)]
    [RequirementCoverage("REQ-VSB-QUARTZ-DUE-TIME", "past-now-and-future-delivery")]
    public async Task DueSchedule_IsDeliveredWithoutManualSchedulerInterventionAsync(int offsetSeconds)
    {
        TimeSpan timeout = OperationTimeout();
        var delivered = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator => configurator.ReceiveEndpoint("quartz-timing-destination", endpoint =>
                endpoint.Handler<TimingPayload>(context =>
                {
                    delivered.TrySetResult(context.Message.Value);
                    return Task.CompletedTask;
                })));
        var scheduler = CreateMessageScheduler(fixture);
        DateTimeOffset dueTime = TimeProvider.System.GetUtcNow().AddSeconds(offsetSeconds);

        await scheduler.ScheduleSendAsync(
            Destination,
            dueTime,
            new TimingPayload($"offset-{offsetSeconds}"),
            TestContext.Current.CancellationToken);

        string received = await delivered.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        Assert.Equal($"offset-{offsetSeconds}", received);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-METADATA", "complete-envelope-roundtrip")]
    public async Task ScheduledDelivery_RestoresTheCompleteMessageContextAsync()
    {
        TimeSpan timeout = OperationTimeout();
        var delivered = new TaskCompletionSource<TimingMetadataObservation>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator => configurator.ReceiveEndpoint("quartz-timing-destination", endpoint =>
                endpoint.Handler<TimingPayload>(context =>
                {
                    delivered.TrySetResult(new TimingMetadataObservation(
                        context.MessageId,
                        context.RequestId,
                        context.CorrelationId,
                        context.ConversationId,
                        context.InitiatorId,
                        context.ResponseAddress,
                        context.FaultAddress,
                        context.SourceAddress,
                        context.ExpirationTime,
                        context.Headers.Get<string>("tenant")));
                    return Task.CompletedTask;
                })));
        var scheduler = CreateMessageScheduler(fixture);
        Guid messageId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f41");
        Guid requestId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f42");
        Guid correlationId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f43");
        Guid conversationId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f44");
        Guid initiatorId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f45");
        var responseAddress = new Uri("loopback://localhost/quartz-response");
        var faultAddress = new Uri("loopback://localhost/quartz-fault");

        await scheduler.ScheduleSendAsync(
            Destination,
            TimeProvider.System.GetUtcNow().AddHours(-1),
            new TimingPayload("metadata"),
            Pipe.Execute<SendContext<TimingPayload>>(context =>
            {
                context.MessageId = messageId;
                context.RequestId = requestId;
                context.CorrelationId = correlationId;
                context.ConversationId = conversationId;
                context.InitiatorId = initiatorId;
                context.ResponseAddress = responseAddress;
                context.FaultAddress = faultAddress;
                context.TimeToLive = TimeSpan.FromMinutes(30);
                context.Headers.Set("tenant", "factory-a");
            }),
            TestContext.Current.CancellationToken);

        TimingMetadataObservation received = await delivered.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        Assert.Equal(messageId, received.MessageId);
        Assert.Equal(requestId, received.RequestId);
        Assert.Equal(correlationId, received.CorrelationId);
        Assert.Equal(conversationId, received.ConversationId);
        Assert.Equal(initiatorId, received.InitiatorId);
        Assert.Equal(responseAddress, received.ResponseAddress);
        Assert.Equal(faultAddress, received.FaultAddress);
        Assert.NotNull(received.SourceAddress);
        Assert.NotNull(received.ExpirationTime);
        Assert.Equal("factory-a", received.Tenant);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-CANCEL", "future-trigger-is-removed-before-delivery")]
    public async Task CancelingAFutureSchedule_RemovesTheTriggerBeforeItCanDeliverAsync()
    {
        TimeSpan timeout = OperationTimeout();
        var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator => configurator.ReceiveEndpoint("quartz-timing-destination", endpoint =>
                endpoint.Handler<TimingPayload>(_ =>
                {
                    delivered.TrySetResult();
                    return Task.CompletedTask;
                })));
        var scheduler = CreateMessageScheduler(fixture);
        var scheduledCommand = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
        var canceledCommand = new ConsumeCompletionObserver<CancelScheduledMessage>(_ => true);
        using ConnectHandle scheduledObserver = fixture.Bus.ConnectConsumeObserver(scheduledCommand);
        using ConnectHandle canceledObserver = fixture.Bus.ConnectConsumeObserver(canceledCommand);

        ScheduledMessage<TimingPayload> scheduled = await scheduler.ScheduleSendAsync(
            Destination,
            FarFuture,
            new TimingPayload("must-not-deliver"),
            TestContext.Current.CancellationToken);
        await scheduledCommand.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        TriggerKey triggerKey = QuartzTriggerKey.ForOneTime(scheduled.TokenId, fixture.SchedulerNamespace);
        Assert.NotNull(await fixture.Scheduler.GetTrigger(triggerKey, TestContext.Current.CancellationToken));

        await scheduler.CancelScheduledSendAsync(Destination, scheduled.TokenId, TestContext.Current.CancellationToken);
        await canceledCommand.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Null(await fixture.Scheduler.GetTrigger(triggerKey, TestContext.Current.CancellationToken));
        Assert.False(delivered.Task.IsCompleted);
    }

    private static MessageScheduler CreateMessageScheduler(QuartzTestBus fixture) =>
        new(new EndpointScheduleMessageProvider(_ => Task.FromResult(fixture.SchedulerEndpoint)), fixture.Bus.Topology);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record TimingPayload(string Value);

    private sealed record TimingMetadataObservation(
        Guid? MessageId,
        Guid? RequestId,
        Guid? CorrelationId,
        Guid? ConversationId,
        Guid? InitiatorId,
        Uri? ResponseAddress,
        Uri? FaultAddress,
        Uri? SourceAddress,
        DateTimeOffset? ExpirationTime,
        string? Tenant);
}
