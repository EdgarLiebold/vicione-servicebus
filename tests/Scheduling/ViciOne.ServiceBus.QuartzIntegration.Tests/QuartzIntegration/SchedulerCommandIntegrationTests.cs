using Quartz;
using Quartz.Impl.Triggers;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class SchedulerCommandIntegrationTests
{
    private static readonly DateTime ScheduledTime = new(2100, 2, 3, 4, 5, 6, DateTimeKind.Utc);
    private static readonly Uri Destination = new("loopback://localhost/quartz-test-destination");

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-ONE-TIME", "command-creates-exact-recoverable-trigger")]
    public async Task OneTimeCommand_CreatesAnExactRecoverableTrigger()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.Start(timeout);
        Guid tokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f27");
        Guid messageId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f28");
        var consumed = new ConsumeCompletionObserver<ScheduleMessage>(message => message.CorrelationId == tokenId);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(consumed);
        var command = new ScheduleMessageCommand<ScheduledPayload>(ScheduledTime, Destination, new ScheduledPayload("alpha"), tokenId);

        await fixture.SchedulerEndpoint.Send<ScheduleMessage>(command, context =>
        {
            context.MessageId = messageId;
            context.CorrelationId = tokenId;
            context.TimeToLive = TimeSpan.FromMinutes(30);
            context.Headers.Set("tenant", "factory-a");
        }, TestContext.Current.CancellationToken);
        await consumed.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        TriggerKey triggerKey = new(tokenId.ToString("N"));
        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(
            await fixture.Scheduler.GetTrigger(triggerKey, TestContext.Current.CancellationToken));
        IJobDetail job = Assert.IsAssignableFrom<IJobDetail>(
            await fixture.Scheduler.GetJobDetail(trigger.JobKey, TestContext.Current.CancellationToken));

        Assert.Equal(ScheduledTime, trigger.StartTimeUtc.UtcDateTime);
        Assert.IsType<SimpleTriggerImpl>(trigger);
        Assert.Equal(messageId.ToString(), trigger.JobDataMap.GetString("MessageId"));
        Assert.Equal(tokenId.ToString(), trigger.JobDataMap.GetString("CorrelationId"));
        Assert.Equal(tokenId.ToString("N"), trigger.JobDataMap.GetString("TokenId"));
        Assert.Equal(Destination.ToString(), trigger.JobDataMap.GetString("Destination"));
        Assert.Contains("tenant", trigger.JobDataMap.GetString("HeadersAsJson"), StringComparison.Ordinal);
        Assert.True(job.Durable);
        Assert.True(job.RequestsRecovery);
        Assert.Equal(typeof(ScheduledMessageJob), job.JobType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-ONE-TIME", "same-token-replaces-trigger")]
    public async Task ReusingTheToken_ReplacesRatherThanDuplicatesTheTrigger()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.Start(timeout);
        Guid tokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f29");
        TriggerKey triggerKey = new(tokenId.ToString("N"));

        await SendOneTimeCommand(fixture, tokenId, ScheduledTime, timeout);
        DateTime replacementTime = ScheduledTime.AddHours(1);
        await SendOneTimeCommand(fixture, tokenId, replacementTime, timeout);

        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(
            await fixture.Scheduler.GetTrigger(triggerKey, TestContext.Current.CancellationToken));
        IReadOnlyCollection<TriggerKey> keys = await fixture.Scheduler.GetTriggerKeys(
            Quartz.Impl.Matchers.GroupMatcher<TriggerKey>.GroupEquals(triggerKey.Group),
            TestContext.Current.CancellationToken);

        Assert.Equal(replacementTime, trigger.StartTimeUtc.UtcDateTime);
        Assert.Equal(1, keys.Count(key => key.Equals(triggerKey)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-ONE-TIME", "deleted-durable-job-is-recreated")]
    public async Task DeletedDurableJob_IsRecreatedBeforeTheNextMessageIsScheduled()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.Start(timeout);
        Guid firstTokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f2a");
        Guid secondTokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f2b");

        await SendOneTimeCommand(fixture, firstTokenId, ScheduledTime, timeout);
        ITrigger firstTrigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            new TriggerKey(firstTokenId.ToString("N")),
            TestContext.Current.CancellationToken));
        JobKey durableJobKey = firstTrigger.JobKey;
        Assert.True(await fixture.Scheduler.DeleteJob(durableJobKey, TestContext.Current.CancellationToken));
        Assert.False(await fixture.Scheduler.CheckExists(durableJobKey, TestContext.Current.CancellationToken));

        await SendOneTimeCommand(fixture, secondTokenId, ScheduledTime.AddHours(1), timeout);

        ITrigger replacementTrigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            new TriggerKey(secondTokenId.ToString("N")),
            TestContext.Current.CancellationToken));
        IJobDetail replacementJob = Assert.IsAssignableFrom<IJobDetail>(await fixture.Scheduler.GetJobDetail(
            replacementTrigger.JobKey,
            TestContext.Current.CancellationToken));
        Assert.Equal(durableJobKey, replacementTrigger.JobKey);
        Assert.True(replacementJob.Durable);
        Assert.True(replacementJob.RequestsRecovery);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-RECURRING-CONTROL", "schedule-pause-resume-cancel")]
    public async Task RecurringControlCommands_OperateOnOneCanonicalTrigger()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.Start(timeout);
        const string scheduleId = "nightly-orders";
        const string scheduleGroup = "operations";
        TriggerKey triggerKey = QuartzTriggerKey.ForRecurring(scheduleId, scheduleGroup);
        var recurring = new ScheduleRecurringMessageCommand<ScheduledPayload>(
            new TestRecurringSchedule(scheduleId, scheduleGroup),
            Destination,
            new ScheduledPayload("recurring"));

        var scheduled = new ConsumeCompletionObserver<ScheduleRecurringMessage>(message => message.Schedule.ScheduleId == scheduleId);
        using (fixture.Bus.ConnectConsumeObserver(scheduled))
        {
            await fixture.SchedulerEndpoint.Send<ScheduleRecurringMessage>(recurring, TestContext.Current.CancellationToken);
            await scheduled.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        }

        Assert.NotNull(await fixture.Scheduler.GetTrigger(triggerKey, TestContext.Current.CancellationToken));
        Assert.Equal(TriggerState.Normal,
            await fixture.Scheduler.GetTriggerState(triggerKey, TestContext.Current.CancellationToken));

        await SendControl<PauseScheduledRecurringMessage>(fixture, scheduleId, scheduleGroup, timeout);
        Assert.Equal(TriggerState.Paused,
            await fixture.Scheduler.GetTriggerState(triggerKey, TestContext.Current.CancellationToken));

        await SendControl<ResumeScheduledRecurringMessage>(fixture, scheduleId, scheduleGroup, timeout);
        Assert.Equal(TriggerState.Normal,
            await fixture.Scheduler.GetTriggerState(triggerKey, TestContext.Current.CancellationToken));

        await SendControl<CancelScheduledRecurringMessage>(fixture, scheduleId, scheduleGroup, timeout);
        Assert.Null(await fixture.Scheduler.GetTrigger(triggerKey, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-QUARTZ-DELIVERY", "serialized-payload-and-context-roundtrip")]
    public async Task TriggeredJob_DeliversTheSerializedPayloadAndUserHeaders(bool useRawJson)
    {
        TimeSpan timeout = OperationTimeout();
        var delivered = new TaskCompletionSource<ScheduledDeliveryObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.Start(
            timeout,
            null,
            configurator =>
            {
                if (useRawJson)
                    configurator.UseRawJsonSerializer();

                configurator.ReceiveEndpoint("quartz-test-destination", endpoint =>
                    endpoint.Handler<ScheduledPayload>(context =>
                    {
                        delivered.TrySetResult(new ScheduledDeliveryObservation(
                            context.Message.Value,
                            context.MessageId,
                            context.CorrelationId,
                            context.Headers.Get<string>("tenant"),
                            context.SentTime));
                        return Task.CompletedTask;
                    }));
            });
        Guid messageId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f31");
        Guid correlationId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f32");
        var consumed = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(consumed);
        var scheduler = new MessageScheduler(
            new EndpointScheduleMessageProvider(() => Task.FromResult(fixture.SchedulerEndpoint)),
            fixture.Bus.Topology);

        ScheduledMessage<ScheduledPayload> scheduled = await scheduler.ScheduleSend(
            Destination,
            ScheduledTime,
            new ScheduledPayload("delivered"),
            Pipe.Execute<SendContext<ScheduledPayload>>(context =>
            {
                context.MessageId = messageId;
                context.CorrelationId = correlationId;
                context.Headers.Set("tenant", "factory-a");
            }),
            TestContext.Current.CancellationToken);
        await consumed.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            new TriggerKey(scheduled.TokenId.ToString("N")),
            TestContext.Current.CancellationToken));
        await fixture.Scheduler.TriggerJob(trigger.JobKey, trigger.JobDataMap, TestContext.Current.CancellationToken);
        ScheduledDeliveryObservation received = await delivered.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal("delivered", received.Value);
        Assert.Equal(messageId, received.MessageId);
        Assert.Equal(correlationId, received.CorrelationId);
        Assert.Equal("factory-a", received.Tenant);
        if (useRawJson)
            Assert.Null(received.SentTime);
        else
            Assert.NotNull(received.SentTime);
    }

    private static async Task SendOneTimeCommand(QuartzTestBus fixture, Guid tokenId, DateTime scheduledTime, TimeSpan timeout)
    {
        var consumed = new ConsumeCompletionObserver<ScheduleMessage>(message => message.CorrelationId == tokenId);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(consumed);
        var command = new ScheduleMessageCommand<ScheduledPayload>(scheduledTime, Destination, new ScheduledPayload("replacement"), tokenId);

        await fixture.SchedulerEndpoint.Send<ScheduleMessage>(command, TestContext.Current.CancellationToken);
        await consumed.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
    }

    private static async Task SendControl<T>(QuartzTestBus fixture, string scheduleId, string scheduleGroup, TimeSpan timeout)
        where T : class
    {
        var consumed = new ConsumeCompletionObserver<T>(_ => true);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(consumed);

        await fixture.SchedulerEndpoint.Send<T>(new
        {
            CorrelationId = NewId.NextGuid(),
            Timestamp = ScheduledTime,
            ScheduleId = scheduleId,
            ScheduleGroup = scheduleGroup,
        }, TestContext.Current.CancellationToken);
        await consumed.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record ScheduledPayload(string Value);

    private sealed record ScheduledDeliveryObservation(
        string Value,
        Guid? MessageId,
        Guid? CorrelationId,
        string? Tenant,
        DateTime? SentTime);

    private sealed class TestRecurringSchedule(string scheduleId, string scheduleGroup) : RecurringSchedule
    {
        public string TimeZoneId => TimeZoneInfo.Utc.Id;
        public DateTimeOffset StartTime => ScheduledTime;
        public DateTimeOffset? EndTime => ScheduledTime.AddDays(2);
        public string ScheduleId => scheduleId;
        public string ScheduleGroup => scheduleGroup;
        public string CronExpression => "0 0 0 ? * *";
        public string Description => "Nightly order processing";
        public MissedEventPolicy MisfirePolicy => MissedEventPolicy.Skip;
    }

}
