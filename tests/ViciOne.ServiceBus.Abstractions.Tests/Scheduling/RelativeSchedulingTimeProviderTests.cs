using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Scheduling;
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
        var recorder = new RecordingApplicationScheduler(clock);
        IMessageScheduler scheduler = recorder;
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
        var recorder = new RecordingApplicationScheduler(clock);
        IMessageScheduler scheduler = recorder;
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

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-DELAY", "application-boundaries")]
    public async Task ApplicationRelativeOverloads_RejectNegativeDelayAndPreserveCancellationWithoutReadingTheClockAsync()
    {
        IMessageScheduler scheduler = new RecordingApplicationScheduler(new ThrowingTimeProvider());
        var message = new ScheduledPayload("boundary");
        TimeSpan negativeDelay = TimeSpan.FromTicks(-1);
        CancellationToken testCancellation = TestContext.Current.CancellationToken;

        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(Destination, negativeDelay, message, testCancellation);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(Destination, negativeDelay, message, new ScheduleOptions(), testCancellation);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.SchedulePublishAsync(negativeDelay, message, testCancellation);
        });

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        Task<ScheduledMessage<ScheduledPayload>> canceledTask = scheduler.ScheduleSendAsync(
            Destination,
            TimeSpan.Zero,
            message,
            cancellationSource.Token);
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledTask);

        Assert.Equal(cancellationSource.Token, canceled.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-DELAY", "advanced-overload-boundaries")]
    public void AdvancedRelativeOverloads_AllRejectNegativeDelayBeforeReadingTheClock()
    {
        IMessageScheduler scheduler = CreateScheduler<IAdvancedMessageScheduler>(new ThrowingTimeProvider(), out _);
        var message = new ScheduledPayload("advanced");
        object values = new { Value = "initialized" };
        IPipe<SendContext<ScheduledPayload>> typedPipe = Pipe.Empty<SendContext<ScheduledPayload>>();
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();
        TimeSpan negativeDelay = TimeSpan.FromTicks(-1);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(Destination, negativeDelay, message, typedPipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(Destination, negativeDelay, message, pipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(Destination, negativeDelay, (object)message, typeof(ScheduledPayload), cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(Destination, negativeDelay, (object)message, pipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(Destination, negativeDelay, (object)message, typeof(ScheduledPayload), pipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync<ScheduledPayload>(Destination, negativeDelay, values, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync<ScheduledPayload>(Destination, negativeDelay, values, typedPipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync<ScheduledPayload>(Destination, negativeDelay, values, pipe, cancellationToken);
        });

        AssertNegativeDelay(() =>
        {
            _ = scheduler.SchedulePublishAsync(negativeDelay, message, typedPipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.SchedulePublishAsync(negativeDelay, message, pipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.SchedulePublishAsync(negativeDelay, (object)message, typeof(ScheduledPayload), cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.SchedulePublishAsync(negativeDelay, (object)message, pipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.SchedulePublishAsync(negativeDelay, (object)message, typeof(ScheduledPayload), pipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.SchedulePublishAsync<ScheduledPayload>(negativeDelay, values, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.SchedulePublishAsync<ScheduledPayload>(negativeDelay, values, typedPipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.SchedulePublishAsync<ScheduledPayload>(negativeDelay, values, pipe, cancellationToken);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-DELAY", "context-overload-boundaries")]
    public void SchedulerContextRelativeOverloads_AllRejectNegativeDelayBeforeReadingTheClock()
    {
        MessageSchedulerContext scheduler = CreateScheduler<MessageSchedulerContext>(new ThrowingTimeProvider(), out _);
        var message = new ScheduledPayload("context");
        object values = new { Value = "initialized" };
        IPipe<SendContext<ScheduledPayload>> typedPipe = Pipe.Empty<SendContext<ScheduledPayload>>();
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();
        TimeSpan negativeDelay = TimeSpan.FromTicks(-1);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(negativeDelay, message, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(negativeDelay, message, typedPipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(negativeDelay, message, pipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(negativeDelay, (object)message, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(negativeDelay, (object)message, typeof(ScheduledPayload), cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(negativeDelay, (object)message, pipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync(negativeDelay, (object)message, typeof(ScheduledPayload), pipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync<ScheduledPayload>(negativeDelay, values, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync<ScheduledPayload>(negativeDelay, values, typedPipe, cancellationToken);
        });
        AssertNegativeDelay(() =>
        {
            _ = scheduler.ScheduleSendAsync<ScheduledPayload>(negativeDelay, values, pipe, cancellationToken);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULE-PUBLISH", "consume-context-required-inputs")]
    public void ConsumeContextPublishOverloads_RejectEveryMissingRequiredInputBeforeScheduling()
    {
        var clock = new FakeTimeProvider(SchedulerNow);
        MessageSchedulerContext scheduler = CreateScheduler<MessageSchedulerContext>(clock, out RecordingScheduleProxy recorder);
        ConsumeContext context = DispatchProxy.Create<ConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(clock, scheduler);
        var message = new ScheduledPayload("publish");
        IPipe<SendContext<ScheduledPayload>> typedPipe = Pipe.Empty<SendContext<ScheduledPayload>>();
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("message", () => context.SchedulePublishAsync<ScheduledPayload>(SchedulerNow, null!, cancellationToken));
        AssertNull("message", () => context.SchedulePublishAsync<ScheduledPayload>(SchedulerNow, null!, typedPipe, cancellationToken));
        AssertNull("pipe", () => context.SchedulePublishAsync(SchedulerNow, message, (IPipe<SendContext<ScheduledPayload>>)null!, cancellationToken));
        AssertNull("message", () => context.SchedulePublishAsync<ScheduledPayload>(SchedulerNow, null!, pipe, cancellationToken));
        AssertNull("pipe", () => context.SchedulePublishAsync(SchedulerNow, message, (IPipe<SendContext>)null!, cancellationToken));

        AssertNull("message", () => context.SchedulePublishAsync(SchedulerNow, (object)null!, cancellationToken));
        AssertNull("message", () => context.SchedulePublishAsync(SchedulerNow, (object)null!, typeof(ScheduledPayload), cancellationToken));
        AssertNull("messageType", () => context.SchedulePublishAsync(SchedulerNow, (object)message, (Type)null!, cancellationToken));
        AssertNull("message", () => context.SchedulePublishAsync(SchedulerNow, (object)null!, pipe, cancellationToken));
        AssertNull("pipe", () => context.SchedulePublishAsync(SchedulerNow, (object)message, (IPipe<SendContext>)null!, cancellationToken));
        AssertNull("message", () => context.SchedulePublishAsync(SchedulerNow, (object)null!, typeof(ScheduledPayload), pipe, cancellationToken));
        AssertNull("messageType", () => context.SchedulePublishAsync(SchedulerNow, (object)message, (Type)null!, pipe, cancellationToken));
        AssertNull("pipe", () => context.SchedulePublishAsync(SchedulerNow, (object)message, typeof(ScheduledPayload), null!, cancellationToken));

        AssertNull("values", () => context.SchedulePublishAsync<ScheduledPayload>(SchedulerNow, (object)null!, cancellationToken));
        AssertNull("values", () => context.SchedulePublishAsync<ScheduledPayload>(SchedulerNow, (object)null!, typedPipe, cancellationToken));
        AssertNull("pipe", () => context.SchedulePublishAsync<ScheduledPayload>(SchedulerNow, new { Value = "publish" }, (IPipe<SendContext<ScheduledPayload>>)null!, cancellationToken));
        AssertNull("values", () => context.SchedulePublishAsync<ScheduledPayload>(SchedulerNow, (object)null!, pipe, cancellationToken));
        AssertNull("pipe", () => context.SchedulePublishAsync<ScheduledPayload>(SchedulerNow, new { Value = "publish" }, (IPipe<SendContext>)null!, cancellationToken));

        Assert.Empty(recorder.ScheduledTimes);
    }

    private static T CreateScheduler<T>(TimeProvider clock, out RecordingScheduleProxy recorder)
        where T : class
    {
        T scheduler = DispatchProxy.Create<T, RecordingScheduleProxy>();
        recorder = (RecordingScheduleProxy)(object)scheduler;
        recorder.Clock = clock;
        return scheduler;
    }

    private static void AssertNull(string parameterName, Action operation)
    {
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(operation).ParamName);
    }

    private static void AssertNegativeDelay(Action operation)
    {
        Assert.Equal("delay", Assert.Throws<ArgumentOutOfRangeException>(operation).ParamName);
    }

    private sealed record ScheduledPayload(string Value);

    private sealed class RecordingApplicationScheduler(TimeProvider timeProvider) : IMessageScheduler
    {
        public TimeProvider TimeProvider { get; } = timeProvider;

        public List<DateTimeOffset> ScheduledTimes { get; } = [];

        public Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, DateTimeOffset dueAt, TMessage message,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            ScheduledTimes.Add(dueAt);
            return Task.FromResult<ScheduledMessage<TMessage>>(null!);
        }

        public Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, TimeSpan delay, TMessage message,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            ArgumentNullException.ThrowIfNull(destination);
            ArgumentNullException.ThrowIfNull(message);
            if (!destination.IsAbsoluteUri)
                throw new ArgumentException("The scheduled destination must be an absolute URI.", nameof(destination));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<ScheduledMessage<TMessage>>(cancellationToken);

            return ScheduleSendAsync(destination, RelativeScheduleTime.GetDueAt(this, delay), message, cancellationToken);
        }

        public Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, DateTimeOffset dueAt, TMessage message,
            ScheduleOptions options, CancellationToken cancellationToken = default)
            where TMessage : class
        {
            ScheduledTimes.Add(dueAt);
            return Task.FromResult<ScheduledMessage<TMessage>>(null!);
        }

        public Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, TimeSpan delay, TMessage message,
            ScheduleOptions options, CancellationToken cancellationToken = default)
            where TMessage : class
        {
            ArgumentNullException.ThrowIfNull(destination);
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(options);
            if (!destination.IsAbsoluteUri)
                throw new ArgumentException("The scheduled destination must be an absolute URI.", nameof(destination));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<ScheduledMessage<TMessage>>(cancellationToken);

            return ScheduleSendAsync(destination, RelativeScheduleTime.GetDueAt(this, delay), message, options, cancellationToken);
        }

        public Task<ScheduledMessage<TMessage>> SchedulePublishAsync<TMessage>(DateTimeOffset dueAt, TMessage message,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            ScheduledTimes.Add(dueAt);
            return Task.FromResult<ScheduledMessage<TMessage>>(null!);
        }

        public Task<ScheduledMessage<TMessage>> SchedulePublishAsync<TMessage>(TimeSpan delay, TMessage message,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            ArgumentNullException.ThrowIfNull(message);
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<ScheduledMessage<TMessage>>(cancellationToken);

            return SchedulePublishAsync(RelativeScheduleTime.GetDueAt(this, delay), message, cancellationToken);
        }

        public Task CancelScheduledSendAsync(ScheduledMessage scheduled, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ThrowingTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            throw new InvalidOperationException("The clock must not be read for a rejected scheduling request.");
    }

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
