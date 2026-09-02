using System.Collections.Concurrent;
using Quartz;
using ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzRecurringDeliveryIntegrationTests
{
    private static readonly Uri Destination = new("loopback://localhost/quartz-recurring-destination");

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-RECURRING-DELIVERY", "public-api-and-time-domain-headers")]
    public async Task RecurringSchedule_DeliversThroughThePublicApiWithExactScheduleContext()
    {
        TimeSpan timeout = OperationTimeout();
        var deliveries = new ConcurrentQueue<ConsumeContext<RecurringPayload>>();
        var secondDelivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var deliveryCount = 0;
        await using QuartzTestBus fixture = await QuartzTestBus.Start(
            timeout,
            configure: configurator => configurator.ReceiveEndpoint("quartz-recurring-destination", endpoint =>
                endpoint.Handler<RecurringPayload>(context =>
                {
                    deliveries.Enqueue(context);
                    if (Interlocked.Increment(ref deliveryCount) == 2)
                        secondDelivery.TrySetResult();

                    return Task.CompletedTask;
                })));
        var recurringScheduler = new EndpointRecurringMessageScheduler(
            fixture.SchedulerEndpoint,
            fixture.Bus.Topology);
        string scheduleId = $"recurring-{NewId.NextGuid():N}";
        const string scheduleGroup = "operations";
        DateTimeOffset startTime = TimeProvider.System.GetUtcNow().AddSeconds(2);
        var schedule = new TestRecurringSchedule(
            scheduleId,
            scheduleGroup,
            startTime,
            startTime.AddSeconds(3));
        var scheduledCommand = new ConsumeCompletionObserver<ScheduleRecurringMessage>(message =>
            message.Schedule.ScheduleId == scheduleId);
        var canceledCommand = new ConsumeCompletionObserver<CancelScheduledRecurringMessage>(message =>
            message.ScheduleId == scheduleId);
        using ConnectHandle scheduledObserver = fixture.Bus.ConnectConsumeObserver(scheduledCommand);
        using ConnectHandle canceledObserver = fixture.Bus.ConnectConsumeObserver(canceledCommand);

        await recurringScheduler.ScheduleRecurringSend(
            Destination,
            schedule,
            new RecurringPayload("recurring"),
            TestContext.Current.CancellationToken);
        await scheduledCommand.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await secondDelivery.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);

        ConsumeContext<RecurringPayload>[] observed = deliveries.ToArray();
        Assert.True(observed.Length >= 2);
        ConsumeContext<RecurringPayload> second = observed[1];
        Assert.Equal("recurring", second.Message.Value);
        Assert.Equal(scheduleId, second.Headers.Get<string>(MessageHeaders.Quartz.ScheduleId));
        Assert.Equal(scheduleGroup, second.Headers.Get<string>(MessageHeaders.Quartz.ScheduleGroup));
        DateTimeOffset scheduled = Assert.IsType<DateTimeOffset>(
            second.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.Scheduled));
        DateTimeOffset sent = Assert.IsType<DateTimeOffset>(
            second.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.Sent));
        DateTimeOffset previous = Assert.IsType<DateTimeOffset>(
            second.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.PreviousSent));
        DateTimeOffset next = Assert.IsType<DateTimeOffset>(
            second.Headers.Get<DateTimeOffset>(MessageHeaders.Quartz.NextScheduled));
        Assert.True(previous < scheduled);
        Assert.True(scheduled <= sent);
        Assert.Equal(TimeSpan.FromSeconds(1), scheduled - previous);
        Assert.Equal(TimeSpan.FromSeconds(1), next - scheduled);

        await recurringScheduler.CancelScheduledRecurringSend(scheduleId, scheduleGroup);
        await canceledCommand.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        Assert.Null(await fixture.Scheduler.GetTrigger(
            QuartzTriggerKey.ForRecurring(scheduleId, scheduleGroup),
            TestContext.Current.CancellationToken));
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record RecurringPayload(string Value);

    private sealed class TestRecurringSchedule(
        string scheduleId,
        string scheduleGroup,
        DateTimeOffset startTime,
        DateTimeOffset endTime) : RecurringSchedule
    {
        public string TimeZoneId => TimeZoneInfo.Utc.Id;
        public DateTimeOffset StartTime => startTime;
        public DateTimeOffset? EndTime => endTime;
        public string ScheduleId => scheduleId;
        public string ScheduleGroup => scheduleGroup;
        public string CronExpression => "0/1 * * * * ?";
        public string Description => "Recurring delivery contract";
        public MissedEventPolicy MisfirePolicy => MissedEventPolicy.Skip;
    }
}
