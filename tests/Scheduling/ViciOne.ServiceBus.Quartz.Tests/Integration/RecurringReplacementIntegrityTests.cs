using System.Reflection;
using System.Text.Json;
using Quartz;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Quartz.Consumers;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class RecurringReplacementIntegrityTests
{
    private static readonly DateTimeOffset Start = new(2100, 2, 3, 4, 5, 6, TimeSpan.Zero);
    private static readonly Uri Destination = new("loopback://localhost/replacement-integrity");
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-QUARTZ-RECURRING", "rejected-replacement-preserves-target-and-neighbor-before-recovery")]
    public async Task InvalidRecurringReplacement_PreservesTargetAndNeighborBeforeValidRecoveryAsync(bool unknownTimeZone)
    {
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(Timeout);
        var original = new Schedule("target", "original", Start);
        var neighbor = new Schedule("neighbor", "neighbor-original", Start.AddHours(2));
        await SendAsync(fixture, original, "target-original");
        await SendAsync(fixture, neighbor, "neighbor-original");
        TriggerKey targetKey = QuartzTriggerKey.ForRecurring(original.ScheduleId, original.ScheduleGroup, fixture.SchedulerNamespace);
        TriggerKey neighborKey = QuartzTriggerKey.ForRecurring(neighbor.ScheduleId, neighbor.ScheduleGroup, fixture.SchedulerNamespace);
        Snapshot targetBefore = Snapshot.From(await GetAsync(fixture, targetKey));
        Snapshot neighborBefore = Snapshot.From(await GetAsync(fixture, neighborKey));
        Assert.NotEqual(targetKey, neighborKey);

        var invalid = new Schedule("target", "must-not-be-stored", Start.AddHours(3))
        {
            CronExpression = unknownTimeZone ? "0 0 8 ? * *" : "not-a-cron-expression",
            TimeZoneId = unknownTimeZone ? "ViciOne/Unresolvable-Admission-Zone" : "UTC",
            MisfirePolicy = MissedEventPolicy.Send
        };
        Exception failure = Assert.IsAssignableFrom<Exception>(await SendAsync(fixture, invalid, "rejected", expectFailure: true));
        Assert.Contains(unknownTimeZone ? nameof(TimeZoneNotFoundException) : nameof(FormatException), failure.ToString(), StringComparison.Ordinal);
        Assert.Equal(targetBefore, Snapshot.From(await GetAsync(fixture, targetKey)));
        Assert.Equal(neighborBefore, Snapshot.From(await GetAsync(fixture, neighborKey)));
        await AssertOnlyPairAsync(fixture, targetKey, neighborKey);

        var replacement = new Schedule("target", "accepted-replacement", Start.AddHours(4))
        {
            CronExpression = "0 15 9 ? * *",
            MisfirePolicy = MissedEventPolicy.Send
        };
        await SendAsync(fixture, replacement, "target-replacement");
        ICronTrigger updated = await GetAsync(fixture, targetKey);
        Assert.Equal(targetKey, updated.Key);
        Assert.Equal(targetBefore.JobKey, updated.JobKey.ToString());
        Assert.Equal(replacement.StartTime, updated.StartTimeUtc);
        Assert.Equal(replacement.EndTime, updated.EndTimeUtc);
        Assert.Equal(replacement.CronExpression, updated.CronExpressionString);
        Assert.Equal("UTC", updated.TimeZone.Id);
        Assert.Equal(replacement.Description, updated.Description);
        Assert.Equal(CronTriggerMisfireInstruction.FireAndProceed, updated.MisfireInstruction);
        Assert.Equal("target", updated.JobDataMap.GetString(QuartzJobDataKeys.ScheduleGroup));
        Assert.Equal(original.ScheduleId, updated.JobDataMap.GetString(QuartzJobDataKeys.ScheduleId));
        Assert.Equal(Destination.ToString(), updated.JobDataMap.GetString(QuartzJobDataKeys.DestinationAddress));
        using JsonDocument body = JsonDocument.Parse(Assert.IsType<string>(updated.JobDataMap.GetString(QuartzJobDataKeys.Body)));
        Assert.Equal("target-replacement", body.RootElement.GetProperty("message").GetProperty("value").GetString());
        AssertVersionHeader(updated, "target-replacement");
        Assert.Equal(neighborBefore, Snapshot.From(await GetAsync(fixture, neighborKey)));
        await AssertOnlyPairAsync(fixture, targetKey, neighborKey);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-QUARTZ-RECURRING", "provider-failure-preserves-triggers-before-command-recovery")]
    public async Task SchedulerProviderFailure_PreservesTriggersAndAllowsCommandRecoveryAsync(bool failFactory)
    {
        string queue = $"quartz-provider-boundary-{NewId.NextGuid():N}";
        ScheduleMessageConsumer<IBus>? consumer = null;
        var contextTokens = new List<CancellationToken>();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(Timeout, configure: bus =>
            bus.ReceiveEndpoint(queue, endpoint => endpoint.Handler<ScheduleRecurringMessage>(context =>
            {
                contextTokens.Add(context.CancellationToken);
                return Assert.IsType<ScheduleMessageConsumer<IBus>>(consumer).ConsumeAsync(context);
            })));
        var original = new Schedule("target", "provider-original", Start);
        var neighbor = new Schedule("neighbor", "provider-neighbor", Start.AddHours(1));
        await SendAsync(fixture, original, "original");
        await SendAsync(fixture, neighbor, "neighbor");
        TriggerKey targetKey = QuartzTriggerKey.ForRecurring(original.ScheduleId, original.ScheduleGroup, fixture.SchedulerNamespace);
        TriggerKey neighborKey = QuartzTriggerKey.ForRecurring(neighbor.ScheduleId, neighbor.ScheduleGroup, fixture.SchedulerNamespace);
        Snapshot targetBefore = Snapshot.From(await GetAsync(fixture, targetKey));
        Snapshot neighborBefore = Snapshot.From(await GetAsync(fixture, neighborKey));
        var failure = new InvalidOperationException("Quartz provider refused the replacement");
        bool reject = true;
        var factoryTokens = new List<CancellationToken>();
        var schedulerCalls = new List<(string Method, CancellationToken Token)>();
        int successfulWrites = 0;
        var scheduler = DispatchProxy.Create<IScheduler, ProviderBoundary>();
        ((ProviderBoundary)(object)scheduler).Callback = (method, args) =>
        {
            var token = Assert.IsType<CancellationToken>(args[^1]);
            schedulerCalls.Add((method.Name, token));
            Assert.Equal(contextTokens[^1], token);
            if (method.Name == nameof(IScheduler.ScheduleJob))
            {
                Assert.Equal(targetKey, Assert.IsAssignableFrom<ITrigger>(args[0]).Key);
                Assert.Equal(ScheduleJobOptions.Replacing, Assert.IsType<ScheduleJobOptions>(args[1]));
                if (reject)
                {
                    Assert.Equal(typeof(ValueTask<DateTimeOffset>), method.ReturnType);
                    return ValueTask.FromException<DateTimeOffset>(failure);
                }
                successfulWrites++;
            }
            else
                Assert.Equal(nameof(IScheduler.GetJobDetail), method.Name);
            return method.Invoke(fixture.Scheduler, args);
        };
        var factory = DispatchProxy.Create<ISchedulerFactory, ProviderBoundary>();
        ((ProviderBoundary)(object)factory).Callback = (method, args) =>
        {
            Assert.Equal(nameof(ISchedulerFactory.GetScheduler), method.Name);
            var token = Assert.IsType<CancellationToken>(Assert.Single(args));
            factoryTokens.Add(token);
            Assert.Equal(contextTokens[^1], token);
            return reject && failFactory
                ? ValueTask.FromException<IScheduler>(failure)
                : ValueTask.FromResult(scheduler);
        };
        consumer = new ScheduleMessageConsumer<IBus>(factory, null, fixture.SchedulerNamespace, RetryPolicy.Fixed(1, TimeSpan.Zero));
        ISendEndpoint commandEndpoint = await fixture.Bus.GetSendEndpointAsync(new Uri($"loopback://localhost/{queue}"), TestContext.Current.CancellationToken);
        var replacement = new Schedule("target", "provider-replacement", Start.AddHours(4));

        Exception observed = Assert.IsAssignableFrom<Exception>(await SendAsync(fixture, replacement, "replacement", true, commandEndpoint));
        Assert.Same(failure, observed.GetBaseException());
        Assert.Equal(Assert.Single(contextTokens), Assert.Single(factoryTokens));
        Assert.Equal(failFactory ? Array.Empty<string>() : [nameof(IScheduler.GetJobDetail), nameof(IScheduler.ScheduleJob)],
            schedulerCalls.Select(call => call.Method));
        Assert.Equal(0, successfulWrites);
        Assert.Equal(targetBefore, Snapshot.From(await GetAsync(fixture, targetKey)));
        Assert.Equal(neighborBefore, Snapshot.From(await GetAsync(fixture, neighborKey)));
        await AssertOnlyPairAsync(fixture, targetKey, neighborKey);

        reject = false;
        await SendAsync(fixture, replacement, "replacement", endpoint: commandEndpoint);
        Assert.Equal(2, contextTokens.Count);
        Assert.Equal(contextTokens, factoryTokens);
        Assert.Equal(1, successfulWrites);
        Assert.Equal(failFactory ? 2 : 4, schedulerCalls.Count);
        ICronTrigger updated = await GetAsync(fixture, targetKey);
        Assert.Equal(replacement.StartTime, updated.StartTimeUtc);
        Assert.Equal(replacement.EndTime, updated.EndTimeUtc);
        Assert.Equal(replacement.Description, updated.Description);
        Assert.Equal(replacement.CronExpression, updated.CronExpressionString);
        using JsonDocument body = JsonDocument.Parse(Assert.IsType<string>(updated.JobDataMap.GetString(QuartzJobDataKeys.Body)));
        Assert.Equal("replacement", body.RootElement.GetProperty("message").GetProperty("value").GetString());
        AssertVersionHeader(updated, "replacement");
        Assert.Equal(neighborBefore, Snapshot.From(await GetAsync(fixture, neighborKey)));
        await AssertOnlyPairAsync(fixture, targetKey, neighborKey);
    }

    private static async Task<Exception?> SendAsync(QuartzTestBus fixture, Schedule schedule, string value,
        bool expectFailure = false, ISendEndpoint? endpoint = null)
    {
        Guid messageId = NewId.NextGuid();
        var completion = new CommandObserver(messageId);
        using ConnectHandle connection = fixture.Bus.ConnectConsumeObserver(completion);
        await (endpoint ?? fixture.SchedulerEndpoint).SendAsync<ScheduleRecurringMessage>(
            new ScheduleRecurringMessageCommand<Payload>(schedule, Destination, new Payload(value)),
            context =>
            {
                context.MessageId = messageId;
                context.CorrelationId = messageId;
                context.Headers.Set("replacement-version", value);
            }, TestContext.Current.CancellationToken);
        Exception? failure = await completion.Completed.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        Assert.Equal(expectFailure ? 0 : 1, completion.Successes);
        Assert.Equal(expectFailure ? 1 : 0, completion.Failures);
        if (expectFailure)
            Assert.NotNull(failure);
        else
            Assert.Null(failure);
        return failure;
    }

    private static void AssertVersionHeader(ICronTrigger trigger, string expected)
    {
        string serialized = Assert.IsType<string>(trigger.JobDataMap.GetString(QuartzJobDataKeys.Headers));
        KeyValuePair<string, JsonElement>[] headers = Assert.IsType<KeyValuePair<string, JsonElement>[]>(
            JsonSerializer.Deserialize<KeyValuePair<string, JsonElement>[]>(serialized,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
        Assert.Equal(expected, Assert.Single(headers, header => header.Key == "replacement-version").Value.GetString());
    }

    private static async Task<ICronTrigger> GetAsync(QuartzTestBus fixture, TriggerKey key) =>
        Assert.IsAssignableFrom<ICronTrigger>(await fixture.Scheduler.GetTrigger(key, TestContext.Current.CancellationToken));

    private static async Task AssertOnlyPairAsync(QuartzTestBus fixture, TriggerKey target, TriggerKey neighbor)
    {
        PagedResult<TriggerHeader> result = await fixture.Scheduler.QueryTriggers(
            new TriggerQuery { Group = GroupMatcher<TriggerKey>.GroupEquals(fixture.SchedulerNamespace) },
            TestContext.Current.CancellationToken);
        Assert.Equal(new[] { target.ToString(), neighbor.ToString() }.Order(), result.Items.Select(item => item.Key.ToString()).Order());
        Assert.Equal(TriggerState.Normal, await fixture.Scheduler.GetTriggerState(target, TestContext.Current.CancellationToken));
        Assert.Equal(TriggerState.Normal, await fixture.Scheduler.GetTriggerState(neighbor, TestContext.Current.CancellationToken));
    }

    private sealed record Snapshot(string Key, string JobKey, DateTimeOffset StartTime, DateTimeOffset? EndTime,
        string? Cron, string TimeZone, string? Description, CronTriggerMisfireInstruction Misfire, RetryPolicy Retry,
        DateTimeOffset? NextFire, string Data)
    {
        public static Snapshot From(ICronTrigger trigger) => new(trigger.Key.ToString(), trigger.JobKey.ToString(),
            trigger.StartTimeUtc, trigger.EndTimeUtc, trigger.CronExpressionString, trigger.TimeZone.Id, trigger.Description,
            trigger.MisfireInstruction, Assert.IsAssignableFrom<RetryPolicy>(trigger.RetryPolicy), trigger.NextFireTimeUtc,
            JsonSerializer.Serialize(trigger.JobDataMap.OrderBy(item => item.Key, StringComparer.Ordinal)));
    }

    private sealed class CommandObserver(Guid messageId) : IConsumeObserver
    {
        private readonly TaskCompletionSource<Exception?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _successes;
        private int _failures;
        public Task<Exception?> Completed => _completion.Task;
        public int Successes => Volatile.Read(ref _successes);
        public int Failures => Volatile.Read(ref _failures);

        public Task PreConsumeAsync<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

        public Task PostConsumeAsync<T>(ConsumeContext<T> context) where T : class
        {
            if (context.MessageId == messageId && context.Message is ScheduleRecurringMessage)
            {
                Interlocked.Increment(ref _successes);
                _completion.TrySetResult(null);
            }
            return Task.CompletedTask;
        }

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception) where T : class
        {
            if (context.MessageId == messageId && context.Message is ScheduleRecurringMessage)
            {
                Interlocked.Increment(ref _failures);
                _completion.TrySetResult(exception);
            }
            return Task.CompletedTask;
        }
    }

    private sealed class Schedule(string group, string description, DateTimeOffset start) : RecurringSchedule
    {
        public string TimeZoneId { get; init; } = "UTC";
        public DateTimeOffset StartTime => start;
        public DateTimeOffset? EndTime => start.AddDays(3);
        public string ScheduleId => "shared-schedule-name";
        public string ScheduleGroup => group;
        public string CronExpression { get; init; } = "0 0 7 ? * *";
        public string Description => description;
        public MissedEventPolicy MisfirePolicy { get; init; } = MissedEventPolicy.Skip;
    }

    private class ProviderBoundary : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Callback { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.NotNull(targetMethod);
            Assert.NotNull(args);
            return Callback(targetMethod, args);
        }
    }

    public sealed record Payload(string Value);
}
