using System.Reflection;
using Quartz;
using ViciOne.ServiceBus.Quartz.Consumers;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

public sealed class QuartzSchedulingCommandValidationTests
{
    private static readonly Uri Destination = new("loopback://localhost/quartz-validation");

    [Theory]
    [InlineData(InvalidOneTimeMember.TokenId, typeof(InvalidOperationException), "token")]
    [InlineData(InvalidOneTimeMember.Destination, typeof(ArgumentNullException), "destination")]
    [InlineData(InvalidOneTimeMember.RelativeDestination, typeof(ArgumentException), "absolute")]
    [InlineData(InvalidOneTimeMember.Payload, typeof(ArgumentNullException), "payload")]
    [InlineData(InvalidOneTimeMember.PayloadTypes, typeof(ArgumentNullException), "payloadTypes")]
    [InlineData(InvalidOneTimeMember.EmptyPayloadTypes, typeof(ArgumentException), "message-type")]
    [InlineData(InvalidOneTimeMember.EmptyPayloadType, typeof(ArgumentException), "message-type")]
    [RequirementCoverage("REQ-VSB-QUARTZ-ONE-TIME", "invalid-command-fails-before-scheduler-access")]
    public async Task InvalidOneTimeCommand_FailsBeforeAccessingTheSchedulerAsync(
        InvalidOneTimeMember invalid,
        Type exceptionType,
        string messageFragment)
    {
        var message = new ScheduleMessageCommand
        {
            TokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5fa7"),
            DueAt = new DateTimeOffset(2041, 2, 3, 4, 5, 6, TimeSpan.Zero),
            Destination = Destination,
            Payload = new ValidationPayload("valid"),
            PayloadType = ["urn:message:validation"],
        };
        ApplyInvalidValue(message, invalid);
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory(out SchedulerFactoryProxy factoryProxy);
        var consumer = new ScheduleMessageConsumer<IBus>(
            schedulerFactory,
            timeZoneResolver: null,
            "quartz-validation",
            RetryPolicy.Fixed(1, TimeSpan.Zero));
        ITestConsumeContext<ScheduleMessage> context = CreateContext<ScheduleMessage>(message);

        Exception? failure = await Record.ExceptionAsync(() => consumer.ConsumeAsync(context));

        Assert.NotNull(failure);
        Assert.Equal(exceptionType, failure.GetType());
        Assert.Contains(messageFragment, failure.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, factoryProxy.GetSchedulerCalls);
    }

    [Theory]
    [InlineData(InvalidRecurringMember.Schedule, typeof(InvalidOperationException), "schedule")]
    [InlineData(InvalidRecurringMember.ScheduleId, typeof(ArgumentException), "ScheduleId")]
    [InlineData(InvalidRecurringMember.ScheduleGroup, typeof(ArgumentException), "ScheduleGroup")]
    [InlineData(InvalidRecurringMember.CronExpression, typeof(ArgumentException), "CronExpression")]
    [InlineData(InvalidRecurringMember.TimeZoneId, typeof(ArgumentException), "TimeZoneId")]
    [InlineData(InvalidRecurringMember.EndBeforeStart, typeof(InvalidOperationException), "end before")]
    [InlineData(InvalidRecurringMember.MisfirePolicy, typeof(ArgumentOutOfRangeException), "MisfirePolicy")]
    [InlineData(InvalidRecurringMember.Destination, typeof(ArgumentNullException), "destination")]
    [InlineData(InvalidRecurringMember.RelativeDestination, typeof(ArgumentException), "absolute")]
    [InlineData(InvalidRecurringMember.Payload, typeof(ArgumentNullException), "payload")]
    [InlineData(InvalidRecurringMember.PayloadTypes, typeof(ArgumentNullException), "payloadTypes")]
    [InlineData(InvalidRecurringMember.EmptyPayloadTypes, typeof(ArgumentException), "message-type")]
    [InlineData(InvalidRecurringMember.EmptyPayloadType, typeof(ArgumentException), "message-type")]
    [RequirementCoverage("REQ-VSB-QUARTZ-RECURRING-CONTROL", "invalid-schedule-fails-before-scheduler-access")]
    public async Task InvalidRecurringCommand_FailsBeforeAccessingTheSchedulerAsync(
        InvalidRecurringMember invalid,
        Type exceptionType,
        string messageFragment)
    {
        var schedule = new MutableRecurringSchedule();
        var message = new ScheduleRecurringMessageCommand
        {
            Schedule = schedule,
            Destination = Destination,
            Payload = new ValidationPayload("valid"),
            PayloadType = ["urn:message:validation"],
        };
        ApplyInvalidValue(message, schedule, invalid);
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory(out SchedulerFactoryProxy factoryProxy);
        var consumer = new ScheduleMessageConsumer<IBus>(
            schedulerFactory,
            timeZoneResolver: null,
            "quartz-validation",
            RetryPolicy.Fixed(1, TimeSpan.Zero));
        ITestConsumeContext<ScheduleRecurringMessage> context = CreateContext<ScheduleRecurringMessage>(message);

        Exception? failure = await Record.ExceptionAsync(() => consumer.ConsumeAsync(context));

        Assert.NotNull(failure);
        Assert.Equal(exceptionType, failure.GetType());
        Assert.Contains(messageFragment, failure.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, factoryProxy.GetSchedulerCalls);
    }

    [Theory]
    [InlineData(InvalidControlCommand.CancelOneTime)]
    [InlineData(InvalidControlCommand.CancelRecurring)]
    [InlineData(InvalidControlCommand.PauseRecurring)]
    [InlineData(InvalidControlCommand.ResumeRecurring)]
    [RequirementCoverage("REQ-VSB-QUARTZ-RECURRING-CONTROL", "invalid-control-key-fails-before-scheduler-access")]
    public async Task InvalidControlCommand_FailsBeforeAccessingTheSchedulerAsync(InvalidControlCommand invalid)
    {
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory(out SchedulerFactoryProxy factoryProxy);
        Exception? failure = invalid switch
        {
            InvalidControlCommand.CancelOneTime => await ConsumeInvalidCancelAsync(schedulerFactory),
            InvalidControlCommand.CancelRecurring => await ConsumeInvalidRecurringCancelAsync(schedulerFactory),
            InvalidControlCommand.PauseRecurring => await ConsumeInvalidPauseAsync(schedulerFactory),
            InvalidControlCommand.ResumeRecurring => await ConsumeInvalidResumeAsync(schedulerFactory),
            _ => throw new ArgumentOutOfRangeException(nameof(invalid), invalid, null),
        };

        Assert.IsType<ArgumentException>(failure);
        Assert.Equal(0, factoryProxy.GetSchedulerCalls);
    }

    private static async Task<Exception?> ConsumeInvalidCancelAsync(ISchedulerFactory schedulerFactory)
    {
        var consumer = new CancelScheduledMessageConsumer<IBus>(schedulerFactory, "quartz-validation");
        ITestConsumeContext<CancelScheduledMessage> context = CreateContext<CancelScheduledMessage>(
            new CancelScheduledMessageCommand
            {
                TokenId = Guid.Empty,
                Timestamp = new DateTimeOffset(2041, 2, 3, 4, 5, 6, TimeSpan.Zero),
            });
        return await Record.ExceptionAsync(() => consumer.ConsumeAsync(context));
    }

    private static async Task<Exception?> ConsumeInvalidRecurringCancelAsync(ISchedulerFactory schedulerFactory)
    {
        var consumer = new CancelScheduledMessageConsumer<IBus>(schedulerFactory, "quartz-validation");
        ITestConsumeContext<CancelScheduledRecurringMessage> context = CreateContext<CancelScheduledRecurringMessage>(
            new CancelScheduledRecurringMessageCommand
            {
                ScheduleId = " ",
                ScheduleGroup = "group",
                Timestamp = new DateTimeOffset(2041, 2, 3, 4, 5, 6, TimeSpan.Zero),
            });
        return await Record.ExceptionAsync(() => consumer.ConsumeAsync(context));
    }

    private static async Task<Exception?> ConsumeInvalidPauseAsync(ISchedulerFactory schedulerFactory)
    {
        var consumer = new PauseScheduledMessageConsumer<IBus>(schedulerFactory, "quartz-validation");
        ITestConsumeContext<PauseScheduledRecurringMessage> context = CreateContext<PauseScheduledRecurringMessage>(
            new PauseScheduledRecurringMessageCommand
            {
                ScheduleId = "schedule",
                ScheduleGroup = " ",
                Timestamp = new DateTimeOffset(2041, 2, 3, 4, 5, 6, TimeSpan.Zero),
            });
        return await Record.ExceptionAsync(() => consumer.ConsumeAsync(context));
    }

    private static async Task<Exception?> ConsumeInvalidResumeAsync(ISchedulerFactory schedulerFactory)
    {
        var consumer = new ResumeScheduledMessageConsumer<IBus>(schedulerFactory, "quartz-validation");
        ITestConsumeContext<ResumeScheduledRecurringMessage> context = CreateContext<ResumeScheduledRecurringMessage>(
            new ResumeScheduledRecurringMessageCommand
            {
                ScheduleId = " ",
                ScheduleGroup = "group",
                Timestamp = new DateTimeOffset(2041, 2, 3, 4, 5, 6, TimeSpan.Zero),
            });
        return await Record.ExceptionAsync(() => consumer.ConsumeAsync(context));
    }

    private static void ApplyInvalidValue(ScheduleMessageCommand message, InvalidOneTimeMember invalid)
    {
        switch (invalid)
        {
            case InvalidOneTimeMember.TokenId:
                message.TokenId = Guid.Empty;
                break;
            case InvalidOneTimeMember.Destination:
                message.Destination = null!;
                break;
            case InvalidOneTimeMember.RelativeDestination:
                message.Destination = new Uri("relative", UriKind.Relative);
                break;
            case InvalidOneTimeMember.Payload:
                message.Payload = null!;
                break;
            case InvalidOneTimeMember.PayloadTypes:
                message.PayloadType = null!;
                break;
            case InvalidOneTimeMember.EmptyPayloadTypes:
                message.PayloadType = [];
                break;
            case InvalidOneTimeMember.EmptyPayloadType:
                message.PayloadType = [" "];
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalid), invalid, null);
        }
    }

    private static void ApplyInvalidValue(
        ScheduleRecurringMessageCommand message,
        MutableRecurringSchedule schedule,
        InvalidRecurringMember invalid)
    {
        switch (invalid)
        {
            case InvalidRecurringMember.Schedule:
                message.Schedule = null!;
                break;
            case InvalidRecurringMember.ScheduleId:
                schedule.ScheduleId = " ";
                break;
            case InvalidRecurringMember.ScheduleGroup:
                schedule.ScheduleGroup = " ";
                break;
            case InvalidRecurringMember.CronExpression:
                schedule.CronExpression = " ";
                break;
            case InvalidRecurringMember.TimeZoneId:
                schedule.TimeZoneId = " ";
                break;
            case InvalidRecurringMember.EndBeforeStart:
                schedule.EndTime = schedule.StartTime.AddTicks(-1);
                break;
            case InvalidRecurringMember.MisfirePolicy:
                schedule.MisfirePolicy = (MissedEventPolicy)int.MaxValue;
                break;
            case InvalidRecurringMember.Destination:
                message.Destination = null!;
                break;
            case InvalidRecurringMember.RelativeDestination:
                message.Destination = new Uri("relative", UriKind.Relative);
                break;
            case InvalidRecurringMember.Payload:
                message.Payload = null!;
                break;
            case InvalidRecurringMember.PayloadTypes:
                message.PayloadType = null!;
                break;
            case InvalidRecurringMember.EmptyPayloadTypes:
                message.PayloadType = [];
                break;
            case InvalidRecurringMember.EmptyPayloadType:
                message.PayloadType = [" "];
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalid), invalid, null);
        }
    }

    private static ITestConsumeContext<TMessage> CreateContext<TMessage>(TMessage message)
        where TMessage : class
    {
        ITestConsumeContext<TMessage> context = DispatchProxy.Create<ITestConsumeContext<TMessage>, ConsumeContextProxy<TMessage>>();
        ((ConsumeContextProxy<TMessage>)(object)context).Message = message;
        return context;
    }

    private static ISchedulerFactory CreateSchedulerFactory(out SchedulerFactoryProxy proxy)
    {
        ISchedulerFactory factory = DispatchProxy.Create<ISchedulerFactory, SchedulerFactoryProxy>();
        proxy = (SchedulerFactoryProxy)(object)factory;
        return factory;
    }

    public enum InvalidOneTimeMember
    {
        TokenId,
        Destination,
        RelativeDestination,
        Payload,
        PayloadTypes,
        EmptyPayloadTypes,
        EmptyPayloadType,
    }

    public enum InvalidRecurringMember
    {
        Schedule,
        ScheduleId,
        ScheduleGroup,
        CronExpression,
        TimeZoneId,
        EndBeforeStart,
        MisfirePolicy,
        Destination,
        RelativeDestination,
        Payload,
        PayloadTypes,
        EmptyPayloadTypes,
        EmptyPayloadType,
    }

    public enum InvalidControlCommand
    {
        CancelOneTime,
        CancelRecurring,
        PauseRecurring,
        ResumeRecurring,
    }

    private interface ITestConsumeContext<out TMessage> : ConsumeContext<TMessage>, ConsumeContext
        where TMessage : class;

    private class ConsumeContextProxy<TMessage> : DispatchProxy
        where TMessage : class
    {
        public TMessage? Message { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_Message" => Message ?? throw new InvalidOperationException("The test message was not configured."),
                "get_CancellationToken" => TestContext.Current.CancellationToken,
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
        }
    }

    private class SchedulerFactoryProxy : DispatchProxy
    {
        public int GetSchedulerCalls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ISchedulerFactory.GetScheduler))
            {
                GetSchedulerCalls++;
                return ValueTask.FromException<IScheduler>(
                    new InvalidOperationException("Invalid commands must not access the scheduler."));
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private sealed record ValidationPayload(string Value);

    private sealed class MutableRecurringSchedule : RecurringSchedule
    {
        public string TimeZoneId { get; set; } = TimeZoneInfo.Utc.Id;
        public DateTimeOffset StartTime { get; set; } = new(2041, 2, 3, 4, 5, 6, TimeSpan.Zero);
        public DateTimeOffset? EndTime { get; set; } = new DateTimeOffset(2041, 3, 3, 4, 5, 6, TimeSpan.Zero);
        public string ScheduleId { get; set; } = "schedule";
        public string ScheduleGroup { get; set; } = "group";
        public string CronExpression { get; set; } = "0 0 12 * * ?";
        public string Description { get; set; } = "validation schedule";
        public MissedEventPolicy MisfirePolicy { get; set; } = MissedEventPolicy.Skip;
    }
}
