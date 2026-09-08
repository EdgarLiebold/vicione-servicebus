using Quartz;
using Quartz.Impl.Triggers;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class SchedulerCommandIntegrationTests
{
    private static readonly DateTimeOffset DueAt = new(2100, 2, 3, 4, 5, 6, TimeSpan.Zero);
    private static readonly Uri Destination = new("loopback://localhost/quartz-test-destination");

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-ONE-TIME", "command-creates-exact-recoverable-trigger")]
    public async Task OneTimeCommand_CreatesAnExactRecoverableTriggerAsync()
    {
        TimeSpan timeout = OperationTimeout();
        RetryPolicy retryPolicy = RetryPolicy.Explicit([
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(13),
        ]);
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configureScheduler: options => options.DeliveryRetryPolicy = retryPolicy);
        Guid tokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f27");
        Guid messageId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f28");
        Guid businessCorrelationId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f26");
        var consumed = new ConsumeCompletionObserver<ScheduleMessage>(message => message.TokenId == tokenId);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(consumed);
        var command = new ScheduleMessageCommand<ScheduledPayload>(DueAt, Destination, new ScheduledPayload("alpha"), tokenId);

        await fixture.SchedulerEndpoint.SendAsync<ScheduleMessage>(command, context =>
        {
            context.MessageId = messageId;
            context.CorrelationId = businessCorrelationId;
            context.TimeToLive = TimeSpan.FromMinutes(30);
            context.Headers.Set("tenant", "factory-a");
        }, TestContext.Current.CancellationToken);
        await consumed.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        TriggerKey triggerKey = QuartzTriggerKey.ForOneTime(tokenId, fixture.SchedulerNamespace);
        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(
            await fixture.Scheduler.GetTrigger(triggerKey, TestContext.Current.CancellationToken));
        IJobDetail job = Assert.IsAssignableFrom<IJobDetail>(
            await fixture.Scheduler.GetJobDetail(trigger.JobKey, TestContext.Current.CancellationToken));

        Assert.Equal(DueAt, trigger.StartTimeUtc);
        Assert.IsType<SimpleTriggerImpl>(trigger);
        Assert.Equal(retryPolicy, trigger.RetryPolicy);
        Assert.Equal(messageId.ToString(), trigger.JobDataMap.GetString("MessageId"));
        Assert.True(Guid.TryParseExact(trigger.JobDataMap.GetString(QuartzJobDataKeys.MessageIdSeed), "D", out _));
        Assert.Equal(businessCorrelationId.ToString(), trigger.JobDataMap.GetString("CorrelationId"));
        Assert.Equal(tokenId.ToString("N"), trigger.JobDataMap.GetString("SchedulingTokenId"));
        Assert.Equal(Destination.ToString(), trigger.JobDataMap.GetString("DestinationAddress"));
        Assert.Contains("tenant", trigger.JobDataMap.GetString("Headers"), StringComparison.Ordinal);
        Assert.True(job.Durable);
        Assert.True(job.RequestsRecovery);
        Assert.Equal(typeof(QuartzScheduledMessageJob<IBus>), job.JobType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULING-TECHNICAL-IDENTITY", "token-and-recurring-keys-do-not-overload-correlation")]
    public void TechnicalSchedulingContracts_DoNotExposeSyntheticBusinessCorrelation()
    {
        Assert.NotNull(typeof(ScheduleMessage).GetProperty(nameof(ScheduleMessage.TokenId)));
        Assert.Null(typeof(ScheduleMessage).GetProperty("CorrelationId"));
        Assert.Null(typeof(ScheduleRecurringMessage).GetProperty("CorrelationId"));
        Assert.Null(typeof(CancelScheduledMessage).GetProperty("CorrelationId"));
        Assert.Null(typeof(CancelScheduledRecurringMessage).GetProperty("CorrelationId"));
        Assert.Null(typeof(PauseScheduledRecurringMessage).GetProperty("CorrelationId"));
        Assert.Null(typeof(ResumeScheduledRecurringMessage).GetProperty("CorrelationId"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-ONE-TIME", "same-token-replaces-trigger")]
    public async Task ReusingTheToken_ReplacesRatherThanDuplicatesTheTriggerAsync()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(timeout);
        Guid tokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f29");
        TriggerKey triggerKey = QuartzTriggerKey.ForOneTime(tokenId, fixture.SchedulerNamespace);

        await SendOneTimeCommandAsync(fixture, tokenId, DueAt, timeout);
        DateTimeOffset replacementTime = DueAt.AddHours(1);
        await SendOneTimeCommandAsync(fixture, tokenId, replacementTime, timeout);

        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(
            await fixture.Scheduler.GetTrigger(triggerKey, TestContext.Current.CancellationToken));
        PagedResult<TriggerHeader> triggers = await fixture.Scheduler.QueryTriggers(
            new TriggerQuery { Group = GroupMatcher<TriggerKey>.GroupEquals(triggerKey.Group) },
            TestContext.Current.CancellationToken);

        Assert.Equal(replacementTime, trigger.StartTimeUtc);
        Assert.Equal(1, triggers.Items.Count(header => header.Key.Equals(triggerKey)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-ONE-TIME", "deleted-durable-job-is-recreated")]
    public async Task DeletedDurableJob_IsRecreatedBeforeTheNextMessageIsScheduledAsync()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(timeout);
        Guid firstTokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f2a");
        Guid secondTokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f2b");

        await SendOneTimeCommandAsync(fixture, firstTokenId, DueAt, timeout);
        ITrigger firstTrigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            QuartzTriggerKey.ForOneTime(firstTokenId, fixture.SchedulerNamespace),
            TestContext.Current.CancellationToken));
        JobKey durableJobKey = firstTrigger.JobKey;
        Assert.True(await fixture.Scheduler.DeleteJob(durableJobKey, TestContext.Current.CancellationToken));
        Assert.False(await fixture.Scheduler.Exists(durableJobKey, TestContext.Current.CancellationToken));

        await SendOneTimeCommandAsync(fixture, secondTokenId, DueAt.AddHours(1), timeout);

        ITrigger replacementTrigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            QuartzTriggerKey.ForOneTime(secondTokenId, fixture.SchedulerNamespace),
            TestContext.Current.CancellationToken));
        IJobDetail replacementJob = Assert.IsAssignableFrom<IJobDetail>(await fixture.Scheduler.GetJobDetail(
            replacementTrigger.JobKey,
            TestContext.Current.CancellationToken));
        Assert.Equal(durableJobKey, replacementTrigger.JobKey);
        Assert.True(replacementJob.Durable);
        Assert.True(replacementJob.RequestsRecovery);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-OWNERSHIP", "foreign-job-collision-fails-closed")]
    public async Task ForeignJobAtTheAdapterIdentity_IsPreservedAndRejectsSchedulingAsync()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(timeout);
        Guid firstTokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f62");
        Guid rejectedTokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f63");
        await SendOneTimeCommandAsync(fixture, firstTokenId, DueAt, timeout);
        ITrigger firstTrigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            QuartzTriggerKey.ForOneTime(firstTokenId, fixture.SchedulerNamespace),
            TestContext.Current.CancellationToken));
        JobKey reservedJobKey = firstTrigger.JobKey;
        Assert.True(await fixture.Scheduler.DeleteJob(reservedJobKey, TestContext.Current.CancellationToken));
        IJobDetail foreignJob = JobBuilder.Create<ForeignQuartzJob>()
            .WithIdentity(reservedJobKey)
            .StoreDurably()
            .RequestRecovery()
            .Build();
        await fixture.Scheduler.AddJob(foreignJob, default, TestContext.Current.CancellationToken);
        var faulted = new ConsumeFaultObserver<ScheduleMessage>();
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(faulted);

        await fixture.SchedulerEndpoint.SendAsync<ScheduleMessage>(
            new ScheduleMessageCommand<ScheduledPayload>(
                DueAt,
                Destination,
                new ScheduledPayload("must-not-schedule"),
                rejectedTokenId),
            TestContext.Current.CancellationToken);
        Exception failure = await faulted.Faulted.WaitAsync(timeout, TestContext.Current.CancellationToken);

        IJobDetail preservedJob = Assert.IsAssignableFrom<IJobDetail>(await fixture.Scheduler.GetJobDetail(
            reservedJobKey,
            TestContext.Current.CancellationToken));
        Assert.Equal(typeof(ForeignQuartzJob), preservedJob.JobType);
        Assert.Contains(nameof(QuartzScheduledMessageJob<IBus>), failure.ToString(), StringComparison.Ordinal);
        Assert.Null(await fixture.Scheduler.GetTrigger(
            QuartzTriggerKey.ForOneTime(rejectedTokenId, fixture.SchedulerNamespace),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-RECURRING-CONTROL", "schedule-pause-resume-cancel")]
    public async Task RecurringControlCommands_OperateOnOneCanonicalTriggerAsync()
    {
        TimeSpan timeout = OperationTimeout();
        RetryPolicy retryPolicy = RetryPolicy.Fixed(4, TimeSpan.FromSeconds(7));
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configureScheduler: options => options.DeliveryRetryPolicy = retryPolicy);
        const string scheduleId = "nightly-orders";
        const string scheduleGroup = "operations";
        TriggerKey triggerKey = QuartzTriggerKey.ForRecurring(scheduleId, scheduleGroup, fixture.SchedulerNamespace);
        var recurring = new ScheduleRecurringMessageCommand<ScheduledPayload>(
            new TestRecurringSchedule(scheduleId, scheduleGroup),
            Destination,
            new ScheduledPayload("recurring"));

        var scheduled = new ConsumeCompletionObserver<ScheduleRecurringMessage>(message => message.Schedule.ScheduleId == scheduleId);
        using (fixture.Bus.ConnectConsumeObserver(scheduled))
        {
            await fixture.SchedulerEndpoint.SendAsync<ScheduleRecurringMessage>(recurring, TestContext.Current.CancellationToken);
            await scheduled.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        }

        ITrigger scheduledTrigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            triggerKey,
            TestContext.Current.CancellationToken));
        Assert.Equal(retryPolicy, scheduledTrigger.RetryPolicy);
        Assert.False(scheduledTrigger.JobDataMap.ContainsKey(QuartzJobDataKeys.MessageId));
        Assert.True(Guid.TryParseExact(scheduledTrigger.JobDataMap.GetString(QuartzJobDataKeys.MessageIdSeed), "D", out _));
        Assert.Equal(TriggerState.Normal,
            await fixture.Scheduler.GetTriggerState(triggerKey, TestContext.Current.CancellationToken));

        await SendControlAsync<PauseScheduledRecurringMessage>(fixture, scheduleId, scheduleGroup, timeout);
        Assert.Equal(TriggerState.Paused,
            await fixture.Scheduler.GetTriggerState(triggerKey, TestContext.Current.CancellationToken));

        await SendControlAsync<ResumeScheduledRecurringMessage>(fixture, scheduleId, scheduleGroup, timeout);
        Assert.Equal(TriggerState.Normal,
            await fixture.Scheduler.GetTriggerState(triggerKey, TestContext.Current.CancellationToken));

        await SendControlAsync<CancelScheduledRecurringMessage>(fixture, scheduleId, scheduleGroup, timeout);
        Assert.Null(await fixture.Scheduler.GetTrigger(triggerKey, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-RECURRING-CONTROL", "same-key-replaces-trigger-with-send-misfire")]
    public async Task RecurringCommand_ReplacesTheTriggerAndAppliesSendMisfirePolicyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(timeout);
        const string scheduleId = "replace-recurring-schedule";
        const string scheduleGroup = "operations";
        TriggerKey triggerKey = QuartzTriggerKey.ForRecurring(scheduleId, scheduleGroup, fixture.SchedulerNamespace);

        await SendRecurringCommandAsync(
            fixture,
            new TestRecurringSchedule(scheduleId, scheduleGroup, MissedEventPolicy.Skip, DueAt),
            timeout);
        DateTimeOffset replacementStart = DueAt.AddHours(1);
        await SendRecurringCommandAsync(
            fixture,
            new TestRecurringSchedule(scheduleId, scheduleGroup, MissedEventPolicy.Send, replacementStart),
            timeout);

        ICronTrigger trigger = Assert.IsAssignableFrom<ICronTrigger>(
            await fixture.Scheduler.GetTrigger(triggerKey, TestContext.Current.CancellationToken));
        PagedResult<TriggerHeader> triggers = await fixture.Scheduler.QueryTriggers(
            new TriggerQuery { Group = GroupMatcher<TriggerKey>.GroupEquals(fixture.SchedulerNamespace) },
            TestContext.Current.CancellationToken);

        Assert.Equal(replacementStart, trigger.StartTimeUtc);
        Assert.Equal(CronTriggerMisfireInstruction.FireAndProceed, trigger.MisfireInstruction);
        Assert.Equal(1, triggers.Items.Count(header => header.Key.Equals(triggerKey)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-METADATA", "transport-properties-persisted")]
    public async Task OneTimeCommand_PersistsInboundTransportPropertiesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(timeout);
        Guid tokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f61");
        var consumed = new ConsumeCompletionObserver<ScheduleMessage>(message => message.TokenId == tokenId);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(consumed);
        var command = new ScheduleMessageCommand<ScheduledPayload>(
            DueAt,
            Destination,
            new ScheduledPayload("transport-properties"),
            tokenId);

        await fixture.SchedulerEndpoint.SendAsync<ScheduleMessage>(command, context =>
        {
            context.SetRoutingKey("north");
        }, TestContext.Current.CancellationToken);
        await consumed.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            QuartzTriggerKey.ForOneTime(tokenId, fixture.SchedulerNamespace),
            TestContext.Current.CancellationToken));
        string serializedProperties = Assert.IsType<string>(trigger.JobDataMap[QuartzJobDataKeys.TransportProperties]);
        IReadOnlyDictionary<string, object> properties = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            ServiceBusMetadataJson.ObjectDeserializer.DeserializeObject<Dictionary<string, object>>(serializedProperties));

        Assert.Equal("north", Assert.IsType<string>(properties["RoutingKey"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-RECURRING-CONTROL", "missing-targets-are-idempotent")]
    public async Task ControlCommands_AreIdempotentWhenTheirTargetsDoNotExistAsync()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(timeout);
        Guid tokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f49");
        const string scheduleId = "missing-recurring-schedule";
        const string scheduleGroup = "operations";
        var canceled = new ConsumeCompletionObserver<CancelScheduledMessage>(message => message.TokenId == tokenId);
        using (fixture.Bus.ConnectConsumeObserver(canceled))
        {
            await fixture.SchedulerEndpoint.SendAsync<CancelScheduledMessage>(new
            {
                Timestamp = DueAt,
                TokenId = tokenId,
            }, TestContext.Current.CancellationToken);
            await canceled.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        }

        await SendControlAsync<PauseScheduledRecurringMessage>(fixture, scheduleId, scheduleGroup, timeout);
        await SendControlAsync<ResumeScheduledRecurringMessage>(fixture, scheduleId, scheduleGroup, timeout);
        await SendControlAsync<CancelScheduledRecurringMessage>(fixture, scheduleId, scheduleGroup, timeout);

        Assert.Null(await fixture.Scheduler.GetTrigger(
            QuartzTriggerKey.ForOneTime(tokenId, fixture.SchedulerNamespace),
            TestContext.Current.CancellationToken));
        Assert.Null(await fixture.Scheduler.GetTrigger(
            QuartzTriggerKey.ForRecurring(scheduleId, scheduleGroup, fixture.SchedulerNamespace),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "owner-scoped-time-zone-resolver-reaches-trigger")]
    public async Task RecurringCommand_UsesTheOwnerScopedTimeZoneResolverAsync()
    {
        TimeSpan timeout = OperationTimeout();
        string timeZoneId = $"owner-zone-{NewId.NextGuid():N}";
        TimeZoneInfo ownerZone = TimeZoneInfo.CreateCustomTimeZone(
            timeZoneId,
            TimeSpan.FromHours(3),
            "Owner Zone",
            "Owner Zone");
        var resolverCalls = 0;
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            timeZoneResolver: id =>
            {
                Assert.Equal(timeZoneId, id);
                Interlocked.Increment(ref resolverCalls);
                return ownerZone;
            });
        const string scheduleId = "owner-time-zone";
        const string scheduleGroup = "operations";
        var recurring = new OwnerTimeZoneRecurringSchedule(scheduleId, scheduleGroup, timeZoneId);
        var scheduled = new ConsumeCompletionObserver<ScheduleRecurringMessage>(message =>
            message.Schedule.ScheduleId == scheduleId);

        using (fixture.Bus.ConnectConsumeObserver(scheduled))
        {
            await fixture.SchedulerEndpoint.SendAsync<ScheduleRecurringMessage>(
                new ScheduleRecurringMessageCommand<ScheduledPayload>(recurring, Destination, new ScheduledPayload("owner-zone")),
                TestContext.Current.CancellationToken);
            await scheduled.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        }

        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            QuartzTriggerKey.ForRecurring(scheduleId, scheduleGroup, fixture.SchedulerNamespace),
            TestContext.Current.CancellationToken));
        ICronTrigger cronTrigger = Assert.IsAssignableFrom<ICronTrigger>(trigger);

        Assert.Equal(ownerZone.Id, cronTrigger.TimeZone.Id);
        Assert.Equal(1, Volatile.Read(ref resolverCalls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "unknown-time-zone-fails-closed")]
    public async Task RecurringCommand_RejectsAnUnknownTimeZoneAsync()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(timeout);
        string timeZoneId = $"missing-zone-{NewId.NextGuid():N}";
        const string scheduleId = "unknown-time-zone";
        const string scheduleGroup = "operations";
        var faulted = new ConsumeFaultObserver<ScheduleRecurringMessage>();
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(faulted);

        await fixture.SchedulerEndpoint.SendAsync<ScheduleRecurringMessage>(
            new ScheduleRecurringMessageCommand<ScheduledPayload>(
                new OwnerTimeZoneRecurringSchedule(scheduleId, scheduleGroup, timeZoneId),
                Destination,
                new ScheduledPayload("unknown-zone")),
            TestContext.Current.CancellationToken);
        Exception failure = await faulted.Faulted.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Contains(nameof(TimeZoneNotFoundException), failure.ToString(), StringComparison.Ordinal);
        Assert.Contains(timeZoneId, failure.ToString(), StringComparison.Ordinal);
        Assert.Null(await fixture.Scheduler.GetTrigger(
            QuartzTriggerKey.ForRecurring(scheduleId, scheduleGroup, fixture.SchedulerNamespace),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-RECURRING-CONTROL", "invalid-misfire-policy-fails-closed")]
    public async Task RecurringCommand_RejectsAnUnsupportedMisfirePolicyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(timeout);
        const string scheduleId = "invalid-misfire";
        const string scheduleGroup = "operations";
        var faulted = new ConsumeFaultObserver<ScheduleRecurringMessage>();
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(faulted);

        await fixture.SchedulerEndpoint.SendAsync<ScheduleRecurringMessage>(
            new ScheduleRecurringMessageCommand<ScheduledPayload>(
                new InvalidMisfireRecurringSchedule(scheduleId, scheduleGroup),
                Destination,
                new ScheduledPayload("invalid-policy")),
            TestContext.Current.CancellationToken);
        Exception failure = await faulted.Faulted.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Contains(nameof(RecurringSchedule.MisfirePolicy), failure.ToString(), StringComparison.Ordinal);
        Assert.Contains("not supported", failure.ToString(), StringComparison.Ordinal);
        Assert.Null(await fixture.Scheduler.GetTrigger(
            QuartzTriggerKey.ForRecurring(scheduleId, scheduleGroup, fixture.SchedulerNamespace),
            TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-QUARTZ-DELIVERY", "serialized-payload-and-context-roundtrip")]
    public async Task TriggeredJob_DeliversTheSerializedPayloadAndUserHeadersAsync(bool useRawJson)
    {
        TimeSpan timeout = OperationTimeout();
        var delivered = new TaskCompletionSource<ScheduledDeliveryObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
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
            new EndpointScheduleMessageProvider(_ => Task.FromResult(fixture.SchedulerEndpoint)),
            fixture.Bus.Topology);

        ScheduledMessage<ScheduledPayload> scheduled = await scheduler.ScheduleSendAsync(
            Destination,
            DueAt,
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
            QuartzTriggerKey.ForOneTime(scheduled.TokenId, fixture.SchedulerNamespace),
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

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-RETRY", "scheduler-executes-persistent-retry-policy")]
    public async Task DeliveryFailure_IsRetriedByTheSchedulerPolicyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        RetryPolicy retryPolicy = RetryPolicy.Fixed(1, TimeSpan.FromMilliseconds(250));
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configureScheduler: options => options.DeliveryRetryPolicy = retryPolicy);
        var listener = new RetryAttemptListener(expectedAttemptCount: 2);
        fixture.Scheduler.ListenerManager.AddJobListener(listener);
        Guid tokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f64");
        var consumed = new ConsumeCompletionObserver<ScheduleMessage>(message => message.TokenId == tokenId);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(consumed);

        await fixture.SchedulerEndpoint.SendAsync<ScheduleMessage>(
            new ScheduleMessageCommand<ScheduledPayload>(
                DateTimeOffset.UtcNow.AddMilliseconds(500),
                new Uri("unsupported-transport://localhost/unavailable"),
                new ScheduledPayload("retry"),
                tokenId),
            TestContext.Current.CancellationToken);
        await consumed.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        RetryAttemptObservation[] attempts = await listener.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal([0, 1], attempts.Select(observation => observation.Attempt));
        Assert.Single(attempts.Select(observation => observation.MessageId).Distinct());
    }

    private static async Task SendOneTimeCommandAsync(QuartzTestBus fixture, Guid tokenId, DateTimeOffset dueAt, TimeSpan timeout)
    {
        var consumed = new ConsumeCompletionObserver<ScheduleMessage>(message => message.TokenId == tokenId);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(consumed);
        var command = new ScheduleMessageCommand<ScheduledPayload>(dueAt, Destination, new ScheduledPayload("replacement"), tokenId);

        await fixture.SchedulerEndpoint.SendAsync<ScheduleMessage>(command, TestContext.Current.CancellationToken);
        await consumed.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
    }

    private static async Task SendRecurringCommandAsync(
        QuartzTestBus fixture,
        RecurringSchedule schedule,
        TimeSpan timeout)
    {
        var consumed = new ConsumeCompletionObserver<ScheduleRecurringMessage>(message =>
            message.Schedule.ScheduleId == schedule.ScheduleId
            && message.Schedule.ScheduleGroup == schedule.ScheduleGroup);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(consumed);
        var command = new ScheduleRecurringMessageCommand<ScheduledPayload>(
            schedule,
            Destination,
            new ScheduledPayload("replacement"));

        await fixture.SchedulerEndpoint.SendAsync<ScheduleRecurringMessage>(
            command,
            TestContext.Current.CancellationToken);
        await consumed.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
    }

    private static async Task SendControlAsync<T>(QuartzTestBus fixture, string scheduleId, string scheduleGroup, TimeSpan timeout)
        where T : class
    {
        var consumed = new ConsumeCompletionObserver<T>(_ => true);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(consumed);

        await fixture.SchedulerEndpoint.SendAsync<T>(new
        {
            Timestamp = DueAt,
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
        DateTimeOffset? SentTime);

    private sealed class ForeignQuartzJob : IJob
    {
        public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
    }

    private sealed class RetryAttemptListener(int expectedAttemptCount) : IJobListener
    {
        private readonly List<RetryAttemptObservation> _attempts = [];
        private readonly TaskCompletionSource<RetryAttemptObservation[]> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<RetryAttemptObservation[]> Completed => _completed.Task;

        ValueTask IJobListener.JobToBeExecuted(
            IJobExecutionContext context,
            CancellationToken cancellationToken)
        {
            if (context.JobDetail.JobType != typeof(QuartzScheduledMessageJob<IBus>))
                return ValueTask.CompletedTask;

            lock (_attempts)
            {
                var messageContext = new QuartzScheduledMessageContext(context, ServiceBusMetadataJson.ObjectDeserializer);
                _attempts.Add(new RetryAttemptObservation(context.RetryAttempt, messageContext.MessageId));
                if (_attempts.Count == expectedAttemptCount)
                    _completed.TrySetResult([.. _attempts]);
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed record RetryAttemptObservation(int Attempt, Guid MessageId);

    private sealed class TestRecurringSchedule(
        string scheduleId,
        string scheduleGroup,
        MissedEventPolicy misfirePolicy = MissedEventPolicy.Skip,
        DateTimeOffset? startTime = null) : RecurringSchedule
    {
        public string TimeZoneId => TimeZoneInfo.Utc.Id;
        public DateTimeOffset StartTime => startTime ?? DueAt;
        public DateTimeOffset? EndTime => DueAt.AddDays(2);
        public string ScheduleId => scheduleId;
        public string ScheduleGroup => scheduleGroup;
        public string CronExpression => "0 0 0 ? * *";
        public string Description => "Nightly order processing";
        public MissedEventPolicy MisfirePolicy => misfirePolicy;
    }

    private sealed class OwnerTimeZoneRecurringSchedule(
        string scheduleId,
        string scheduleGroup,
        string timeZoneId) : RecurringSchedule
    {
        public string TimeZoneId => timeZoneId;
        public DateTimeOffset StartTime => DueAt;
        public DateTimeOffset? EndTime => DueAt.AddDays(2);
        public string ScheduleId => scheduleId;
        public string ScheduleGroup => scheduleGroup;
        public string CronExpression => "0 0 0 ? * *";
        public string Description => "Owner-scoped time-zone resolution";
        public MissedEventPolicy MisfirePolicy => MissedEventPolicy.Skip;
    }

    private sealed class InvalidMisfireRecurringSchedule(
        string scheduleId,
        string scheduleGroup) : RecurringSchedule
    {
        public string TimeZoneId => TimeZoneInfo.Utc.Id;
        public DateTimeOffset StartTime => DueAt;
        public DateTimeOffset? EndTime => DueAt.AddDays(2);
        public string ScheduleId => scheduleId;
        public string ScheduleGroup => scheduleGroup;
        public string CronExpression => "0 0 0 ? * *";
        public string Description => "Invalid misfire policy";
        public MissedEventPolicy MisfirePolicy => (MissedEventPolicy)int.MaxValue;
    }

    private sealed class ConsumeFaultObserver<T> : IConsumeObserver
        where T : class
    {
        private readonly TaskCompletionSource<Exception> _faulted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Exception> Faulted => _faulted.Task;

        public Task PreConsumeAsync<TMessage>(ConsumeContext<TMessage> context)
            where TMessage : class => Task.CompletedTask;

        public Task PostConsumeAsync<TMessage>(ConsumeContext<TMessage> context)
            where TMessage : class => Task.CompletedTask;

        public Task ConsumeFaultAsync<TMessage>(ConsumeContext<TMessage> context, Exception exception)
            where TMessage : class
        {
            if (context is ConsumeContext<T>)
                _faulted.TrySetResult(exception);

            return Task.CompletedTask;
        }
    }

}
