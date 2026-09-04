using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Scheduling;

public sealed class RelativeSchedulingTimeProviderTests
{
    private static readonly Uri Destination = new("loopback://localhost/deterministic-schedule");
    private static readonly DateTimeOffset SchedulerNow = new(2034, 5, 6, 7, 8, 9, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-TIME-OWNER", "relative-send-uses-scheduler-clock")]
    public async Task RelativeSend_UsesTheSchedulerClockAndAddsTheDelayExactlyAsync()
    {
        var clock = new FakeTimeProvider(SchedulerNow);
        IMessageScheduler scheduler = CreateScheduler<IAdvancedMessageScheduler>(clock, out RecordingScheduleProxy recorder);
        TimeSpan delay = TimeSpan.FromMinutes(37);

        await scheduler.ScheduleSendAsync(Destination, delay, new ScheduledPayload("send"), TestContext.Current.CancellationToken);

        DateTimeOffset dueAt = Assert.Single(recorder.ScheduledTimes);
        Assert.Equal(SchedulerNow + delay, dueAt);
        Assert.Equal(TimeSpan.Zero, dueAt.Offset);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-TIME-OWNER", "relative-publish-uses-scheduler-clock")]
    public async Task RelativePublish_UsesTheSchedulerClockAndAddsTheDelayExactlyAsync()
    {
        var clock = new FakeTimeProvider(SchedulerNow);
        IMessageScheduler scheduler = CreateScheduler<IAdvancedMessageScheduler>(clock, out RecordingScheduleProxy recorder);
        TimeSpan delay = TimeSpan.FromHours(3);

        await scheduler.SchedulePublishAsync(delay, new ScheduledPayload("publish"), TestContext.Current.CancellationToken);

        DateTimeOffset dueAt = Assert.Single(recorder.ScheduledTimes);
        Assert.Equal(SchedulerNow + delay, dueAt);
        Assert.Equal(TimeSpan.Zero, dueAt.Offset);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-TIME-OWNER", "scheduler-context-relative-send")]
    public async Task SchedulerContextRelativeSend_UsesItsExposedClockAsync()
    {
        var clock = new FakeTimeProvider(SchedulerNow);
        MessageSchedulerContext scheduler = CreateScheduler<MessageSchedulerContext>(clock, out RecordingScheduleProxy recorder);
        TimeSpan delay = TimeSpan.FromSeconds(19);

        await scheduler.ScheduleSendAsync(delay, new ScheduledPayload("context"), TestContext.Current.CancellationToken);

        Assert.Equal(SchedulerNow + delay, Assert.Single(recorder.ScheduledTimes));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-TIME-OWNER", "consume-context-relative-send")]
    public async Task ConsumeContextRelativeSend_UsesTheConsumeContextClockAsync()
    {
        var contextClock = new FakeTimeProvider(SchedulerNow);
        var unrelatedSchedulerClock = new FakeTimeProvider(SchedulerNow.AddYears(-10));
        MessageSchedulerContext scheduler = CreateScheduler<MessageSchedulerContext>(unrelatedSchedulerClock, out RecordingScheduleProxy recorder);
        ConsumeContext context = DispatchProxy.Create<ConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(contextClock, scheduler);
        TimeSpan delay = TimeSpan.FromDays(2);

        await context.ScheduleSendAsync(Destination, delay, new ScheduledPayload("consume"), TestContext.Current.CancellationToken);

        Assert.Equal(SchedulerNow + delay, Assert.Single(recorder.ScheduledTimes));
    }

    private static T CreateScheduler<T>(TimeProvider clock, out RecordingScheduleProxy recorder)
        where T : class
    {
        T scheduler = DispatchProxy.Create<T, RecordingScheduleProxy>();
        recorder = (RecordingScheduleProxy)(object)scheduler;
        recorder.Clock = clock;
        return scheduler;
    }

    private sealed record ScheduledPayload(string Value);

    private class RecordingScheduleProxy : DispatchProxy
    {
        public TimeProvider Clock { get; set; } = TimeProvider.System;

        public List<DateTimeOffset> ScheduledTimes { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_TimeProvider")
                return Clock;

            if (targetMethod.Name.StartsWith("Schedule", StringComparison.Ordinal))
            {
                ScheduledTimes.Add(args!.OfType<DateTimeOffset>().Single());
                return CompletedTask(targetMethod.ReturnType);
            }

            throw new NotSupportedException(targetMethod.Name);
        }

        private static object CompletedTask(Type taskType)
        {
            if (taskType == typeof(Task))
                return Task.CompletedTask;

            Type resultType = Assert.Single(taskType.GenericTypeArguments);
            MethodInfo fromResult = typeof(Task).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == nameof(Task.FromResult) && method.IsGenericMethodDefinition);
            return fromResult.MakeGenericMethod(resultType).Invoke(null, [null])!;
        }
    }

    private class ConsumeContextProxy : DispatchProxy
    {
        private TimeProvider _clock = null!;
        private MessageSchedulerContext _scheduler = null!;

        public void Configure(TimeProvider clock, MessageSchedulerContext scheduler)
        {
            _clock = clock;
            _scheduler = scheduler;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "TryGetPayload")
            {
                Type payloadType = targetMethod.GetGenericArguments()[0];
                object? payload = payloadType == typeof(TimeProvider)
                    ? _clock
                    : payloadType == typeof(MessageSchedulerContext)
                        ? _scheduler
                        : null;
                args![0] = payload;
                return payload is not null;
            }

            if (targetMethod.Name == "HasPayloadType")
            {
                var payloadType = (Type)args![0]!;
                return payloadType == typeof(TimeProvider) || payloadType == typeof(MessageSchedulerContext);
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
