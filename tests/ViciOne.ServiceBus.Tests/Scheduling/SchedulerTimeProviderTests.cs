using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class SchedulerTimeProviderTests
{
    private static readonly DateTimeOffset CommandTime = new(2039, 10, 11, 12, 13, 14, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-TIME-OWNER", "message-scheduler-constructor-boundary")]
    public void MessageScheduler_ExposesTheInjectedClockAndValidatesRequiredOwners()
    {
        var clock = new FakeTimeProvider(CommandTime);
        IScheduleMessageProvider provider = DispatchProxy.Create<IScheduleMessageProvider, UnsupportedProxy>();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnsupportedProxy>();

        var scheduler = new MessageScheduler(provider, topology, clock);
        var defaultScheduler = new MessageScheduler(provider, topology);

        Assert.Same(clock, scheduler.TimeProvider);
        Assert.Same(TimeProvider.System, defaultScheduler.TimeProvider);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() => new MessageScheduler(null!, topology, clock)).ParamName);
        Assert.Equal("busTopology", Assert.Throws<ArgumentNullException>(() => new MessageScheduler(provider, null!, clock)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-TIME-OWNER", "delayed-send-pipe-uses-scheduler-clock")]
    public async Task DelayedSendPipe_UsesTheSchedulerClockWhenTheTransportContextHasNoClock()
    {
        var clock = new FakeTimeProvider(CommandTime);
        DateTime scheduledTime = CommandTime.UtcDateTime + TimeSpan.FromHours(3);
        var context = new InMemorySendContext<ClockProbe>(new ClockProbe());
        var pipe = new ScheduleSendPipe<ClockProbe>(Pipe.Empty<SendContext<ClockProbe>>(), scheduledTime, clock);

        await ((IPipe<SendContext<ClockProbe>>)pipe).Send(context);

        Assert.Equal(TimeSpan.FromHours(3), context.Delay);
        Assert.Same(TimeProvider.System, context.GetTimeProvider());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SCHEDULER-TIME-OWNER", "delayed-scheduler-factory-propagates-clock")]
    public async Task DelayedSchedulerFactory_PropagatesItsClockToTheTransportDelay(bool useBusFactory)
    {
        var clock = new FakeTimeProvider(CommandTime);
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnsupportedProxy>();
        ISendEndpoint endpoint = DispatchProxy.Create<ISendEndpoint, ScheduleEndpointProxy>();
        var endpointCapture = (ScheduleEndpointProxy)(object)endpoint;
        IMessageScheduler scheduler;

        if (useBusFactory)
        {
            IBus bus = DispatchProxy.Create<IBus, SchedulerBusProxy>();
            var busProxy = (SchedulerBusProxy)(object)bus;
            busProxy.Endpoint = endpoint;
            busProxy.Topology = topology;
            scheduler = bus.CreateDelayedMessageScheduler(clock);
        }
        else
        {
            ISendEndpointProvider provider = DispatchProxy.Create<ISendEndpointProvider, SchedulerEndpointProviderProxy>();
            ((SchedulerEndpointProviderProxy)(object)provider).Endpoint = endpoint;
            scheduler = provider.CreateDelayedMessageScheduler(topology, clock);
        }

        await scheduler.ScheduleSend(
            new Uri("loopback://localhost/time-provider"),
            CommandTime.UtcDateTime + TimeSpan.FromHours(3),
            new ClockProbe(),
            TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromHours(3), Assert.IsType<InMemorySendContext<ClockProbe>>(endpointCapture.Context).Delay);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER-CLOCK", "default-schedule-local-boundary")]
    public void DefaultRecurringSchedule_UsesTheInjectedClockAndItsLocalTimeZone()
    {
        var clock = new FakeTimeProvider(CommandTime);
        TimeZoneInfo timeZone = TimeZoneInfo.CreateCustomTimeZone("VSB-Test-UTC+03", TimeSpan.FromHours(3), "VSB Test", "VSB Test");
        clock.SetLocalTimeZone(timeZone);

        var schedule = new TestRecurringSchedule(clock);

        Assert.Equal(timeZone.Id, schedule.TimeZoneId);
        Assert.Equal(CommandTime.ToOffset(TimeSpan.FromHours(3)), schedule.StartTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER-CLOCK", "endpoint-control-command-timestamps")]
    public async Task EndpointRecurringControlCommands_UseTheInjectedClockAndPreserveScheduleIdentity()
    {
        var clock = new FakeTimeProvider(CommandTime);
        ISendEndpoint endpoint = DispatchProxy.Create<ISendEndpoint, CaptureEndpointProxy>();
        var capture = (CaptureEndpointProxy)(object)endpoint;
        var scheduler = new EndpointRecurringMessageScheduler(endpoint, timeProvider: clock);

        await scheduler.CancelScheduledRecurringSend("nightly", "operations");
        await scheduler.PauseScheduledRecurringSend("nightly", "operations");
        await scheduler.ResumeScheduledRecurringSend("nightly", "operations");

        AssertControlCommands(capture.Messages);
        Assert.Same(clock, scheduler.TimeProvider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER-CLOCK", "publish-control-command-timestamps")]
    public async Task PublishRecurringControlCommands_UseTheInjectedClockAndPreserveScheduleIdentity()
    {
        var clock = new FakeTimeProvider(CommandTime);
        IPublishEndpoint endpoint = DispatchProxy.Create<IPublishEndpoint, CaptureEndpointProxy>();
        var capture = (CaptureEndpointProxy)(object)endpoint;
        var scheduler = new PublishRecurringMessageScheduler(endpoint, timeProvider: clock);

        await scheduler.CancelScheduledRecurringSend("nightly", "operations");
        await scheduler.PauseScheduledRecurringSend("nightly", "operations");
        await scheduler.ResumeScheduledRecurringSend("nightly", "operations");

        AssertControlCommands(capture.Messages);
        Assert.Same(clock, scheduler.TimeProvider);
    }

    private static void AssertControlCommands(IReadOnlyList<object> messages)
    {
        Assert.Collection(
            messages,
            message => AssertCommand(Assert.IsAssignableFrom<CancelScheduledRecurringMessage>(message)),
            message => AssertCommand(Assert.IsAssignableFrom<PauseScheduledRecurringMessage>(message)),
            message => AssertCommand(Assert.IsAssignableFrom<ResumeScheduledRecurringMessage>(message)));

    }

    private static void AssertCommand(CancelScheduledRecurringMessage command)
    {
        Assert.Equal(CommandTime.UtcDateTime, command.Timestamp);
        Assert.Equal(DateTimeKind.Utc, command.Timestamp.Kind);
        Assert.Equal("nightly", command.ScheduleId);
        Assert.Equal("operations", command.ScheduleGroup);
    }

    private static void AssertCommand(PauseScheduledRecurringMessage command)
    {
        Assert.Equal(CommandTime.UtcDateTime, command.Timestamp);
        Assert.Equal(DateTimeKind.Utc, command.Timestamp.Kind);
        Assert.Equal("nightly", command.ScheduleId);
        Assert.Equal("operations", command.ScheduleGroup);
    }

    private static void AssertCommand(ResumeScheduledRecurringMessage command)
    {
        Assert.Equal(CommandTime.UtcDateTime, command.Timestamp);
        Assert.Equal(DateTimeKind.Utc, command.Timestamp.Kind);
        Assert.Equal("nightly", command.ScheduleId);
        Assert.Equal("operations", command.ScheduleGroup);
    }

    private sealed class TestRecurringSchedule(TimeProvider timeProvider) : DefaultRecurringSchedule(timeProvider)
    {
        public override string ToString() => CronExpression;
    }

    private sealed record ClockProbe;

    private class CaptureEndpointProxy : DispatchProxy
    {
        public List<object> Messages { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name is "Send" or "Publish")
            {
                Messages.Add(args![0]!);
                return Task.CompletedTask;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class ScheduleEndpointProxy : DispatchProxy
    {
        public SendContext? Context { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "Send"
                && args is [ClockProbe message, IPipe<SendContext<ClockProbe>> pipe, CancellationToken _])
            {
                var context = new InMemorySendContext<ClockProbe>(message);
                Context = context;
                return pipe.Send(context);
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class SchedulerEndpointProviderProxy : DispatchProxy
    {
        public ISendEndpoint Endpoint { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name == "GetSendEndpoint"
                ? Task.FromResult(Endpoint)
                : throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class SchedulerBusProxy : DispatchProxy
    {
        public ISendEndpoint Endpoint { get; set; } = null!;
        public IBusTopology Topology { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_Topology" => Topology,
                "GetSendEndpoint" => Task.FromResult(Endpoint),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }
    }

    private class UnsupportedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
