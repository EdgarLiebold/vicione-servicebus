using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
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
    public async Task DelayedSendPipe_UsesTheSchedulerClockWhenTheTransportContextHasNoClockAsync()
    {
        var clock = new FakeTimeProvider(CommandTime);
        DateTimeOffset dueAt = CommandTime.UtcDateTime + TimeSpan.FromHours(3);
        var context = new InMemorySendContext<ClockProbe>(new ClockProbe());
        var pipe = new ScheduleSendPipe<ClockProbe>(Pipe.Empty<SendContext<ClockProbe>>(), dueAt, clock);

        await ((IPipe<SendContext<ClockProbe>>)pipe).SendAsync(context);

        Assert.Equal(TimeSpan.FromHours(3), context.Delay);
        Assert.Same(TimeProvider.System, context.GetTimeProvider());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-TIME-OWNER", "untyped-delayed-send-pipe-preserves-scheduling-metadata")]
    public async Task DelayedSendPipe_AppliesSchedulingMetadataThroughItsUntypedContractAsync()
    {
        var clock = new FakeTimeProvider(CommandTime);
        DateTimeOffset dueAt = CommandTime + TimeSpan.FromMinutes(45);
        Guid tokenId = NewId.NextGuid();
        var context = new InMemorySendContext<RuntimeClockProbe>(new RuntimeClockProbe());
        var pipe = new ScheduleSendPipe<ClockProbe>(Pipe.Empty<SendContext<ClockProbe>>(), dueAt, clock)
        {
            ScheduledMessageId = tokenId,
        };

        await ((ISendContextPipe)pipe).SendAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromMinutes(45), context.Delay);
        Assert.Equal(tokenId, context.ScheduledMessageId);
        Assert.True(context.Headers.TryGetHeader(MessageHeaders.SchedulingTokenId, out object? header));
        Assert.Equal(tokenId.ToString("D"), header);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULE-TOKEN", "accepted-transport-identity-is-stable-after-context-mutation")]
    public async Task DelayedSendPipe_PreservesTheAcceptedTransportIdentityAsync()
    {
        var clock = new FakeTimeProvider(CommandTime);
        var pipe = new ScheduleSendPipe<ClockProbe>(
            Pipe.Empty<SendContext<ClockProbe>>(), CommandTime + TimeSpan.FromMinutes(10), clock);
        Guid configuredToken = NewId.NextGuid();
        Guid transportToken = NewId.NextGuid();
        Guid transportMessageId = NewId.NextGuid();
        var context = new InMemorySendContext<ClockProbe>(new ClockProbe());

        Assert.Null(pipe.ScheduledMessageId);
        Assert.Null(pipe.MessageId);
        pipe.ScheduledMessageId = configuredToken;
        Assert.Equal(configuredToken, pipe.ScheduledMessageId);

        await ((IPipe<SendContext<ClockProbe>>)pipe).SendAsync(context);

        Assert.Equal(configuredToken, context.ScheduledMessageId);
        Assert.Equal(configuredToken, pipe.ScheduledMessageId);
        Assert.Equal(context.MessageId, pipe.MessageId);
        Assert.Equal(TimeSpan.FromMinutes(10), context.Delay);

        context.ScheduledMessageId = transportToken;
        context.MessageId = transportMessageId;
        Assert.Equal(transportToken, pipe.ScheduledMessageId);
        Assert.Equal(transportMessageId, pipe.MessageId);

        ScheduleSendPipe<ClockProbe>.ScheduleSendResult accepted = pipe.AcceptResult();
        Assert.Equal(transportToken, accepted.ScheduledMessageId);
        Assert.Equal(transportMessageId, accepted.MessageId);

        pipe.ScheduledMessageId = NewId.NextGuid();
        context.ScheduledMessageId = NewId.NextGuid();
        context.MessageId = NewId.NextGuid();
        Assert.Equal(transportToken, pipe.ScheduledMessageId);
        Assert.Equal(transportMessageId, pipe.MessageId);
        Assert.Equal(accepted, pipe.AcceptResult());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ((IPipe<SendContext<ClockProbe>>)pipe).SendAsync(new InMemorySendContext<ClockProbe>(new ClockProbe())));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SCHEDULER-TIME-OWNER", "delayed-scheduler-factory-propagates-clock")]
    public async Task DelayedSchedulerFactory_PropagatesItsClockToTheTransportDelayAsync(bool useBusFactory)
    {
        var clock = new FakeTimeProvider(CommandTime);
        IBusTopology topology = DispatchProxy.Create<IBusTopology, UnsupportedProxy>();
        ISendEndpoint endpoint = DispatchProxy.Create<AdvancedScheduleEndpoint, ScheduleEndpointProxy>();
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

        await scheduler.ScheduleSendAsync(
            new Uri("loopback://localhost/time-provider"),
            CommandTime.UtcDateTime + TimeSpan.FromHours(3),
            new ClockProbe(),
            TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromHours(3), Assert.IsType<InMemorySendContext<ClockProbe>>(endpointCapture.Context).Delay);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-TIME-OWNER", "cancellation-command-clock-and-resolution-token")]
    public async Task SchedulerProviders_UseTheirClockAndForwardCallerCancellationAsync()
    {
        var clock = new FakeTimeProvider(CommandTime);
        ISendEndpoint sendEndpoint = DispatchProxy.Create<AdvancedScheduleEndpoint, CaptureEndpointProxy>();
        var sendCapture = (CaptureEndpointProxy)(object)sendEndpoint;
        CancellationToken observedToken = default;
        var endpointProvider = new EndpointScheduleMessageProvider(cancellationToken =>
        {
            observedToken = cancellationToken;
            return Task.FromResult(sendEndpoint);
        }, clock);

        using var callerCancellation = new CancellationTokenSource();
        await endpointProvider.CancelScheduledSendAsync(NewId.NextGuid(), callerCancellation.Token);

        Assert.Equal(callerCancellation.Token, observedToken);
        AssertCancellationTimestamp(Assert.Single(sendCapture.Messages));

        IPublishEndpoint publishEndpoint = DispatchProxy.Create<AdvancedPublishEndpoint, CaptureEndpointProxy>();
        var publishCapture = (CaptureEndpointProxy)(object)publishEndpoint;
        var publishProvider = new PublishScheduleMessageProvider(publishEndpoint, clock);

        await publishProvider.CancelScheduledSendAsync(NewId.NextGuid(), TestContext.Current.CancellationToken);

        AssertCancellationTimestamp(Assert.Single(publishCapture.Messages));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DELAYED-SCHEDULER", "unsupported-cancellation-is-never-reported-as-success")]
    public async Task DelayedScheduler_ExplicitlyRejectsCancellationAfterTransportAcceptanceAsync()
    {
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, UnsupportedProxy>();
        var provider = new DelayedScheduleMessageProvider(endpoints);
        Guid tokenId = NewId.NextGuid();

        NotSupportedException withoutDestination = await Assert.ThrowsAsync<NotSupportedException>(() =>
            provider.CancelScheduledSendAsync(tokenId, TestContext.Current.CancellationToken));
        NotSupportedException withDestination = await Assert.ThrowsAsync<NotSupportedException>(() =>
            provider.CancelScheduledSendAsync(new Uri("loopback://localhost/delayed"), tokenId, TestContext.Current.CancellationToken));

        Assert.Equal(withoutDestination.Message, withDestination.Message);
        Assert.Contains("cannot be canceled", withoutDestination.Message, StringComparison.Ordinal);

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.CancelScheduledSendAsync(tokenId, cancellation.Token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULE-TOKEN", "configured-selector-controls-scheduled-handle")]
    public async Task ScheduleTokenId_UsesTheConfiguredSelectorAndRejectsNullSelectorsAsync()
    {
        Assert.Equal("tokenIdSelector", Assert.Throws<ArgumentNullException>(() =>
            ScheduleTokenId.UseTokenId<NullSelectorProbe>(null!)).ParamName);

        Guid tokenId = NewId.NextGuid();
        ScheduleTokenId.UseTokenId<TokenProbe>(message => message.TokenId);
        ISendEndpoint endpoint = DispatchProxy.Create<AdvancedScheduleEndpoint, ScheduleEndpointProxy>();
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, SchedulerEndpointProviderProxy>();
        ((SchedulerEndpointProviderProxy)(object)endpoints).Endpoint = endpoint;
        var provider = new DelayedScheduleMessageProvider(endpoints, new FakeTimeProvider(CommandTime));

        ScheduledMessage<TokenProbe> scheduled = await provider.ScheduleSendAsync(
            new Uri("loopback://localhost/token-probe"),
            CommandTime + TimeSpan.FromMinutes(10),
            new TokenProbe(tokenId),
            Pipe.Empty<SendContext<TokenProbe>>(),
            TestContext.Current.CancellationToken);

        Assert.Equal(tokenId, scheduled.TokenId);
        Assert.Equal(tokenId, Assert.IsType<InMemorySendContext<TokenProbe>>(
            ((ScheduleEndpointProxy)(object)endpoint).Context).ScheduledMessageId);
        Assert.Equal(1, ((SchedulerEndpointProviderProxy)(object)endpoints).ResolutionCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULE-TOKEN", "empty-selector-token-rejected-before-endpoint-resolution")]
    public async Task DelayedScheduler_RejectsEmptySelectedTokenBeforeResolvingEndpoint_AndRecoversAsync()
    {
        ScheduleTokenId.UseTokenId<AdmissionTokenProbe>(message => message.TokenId);
        ISendEndpoint endpoint = DispatchProxy.Create<AdvancedScheduleEndpoint, ScheduleEndpointProxy>();
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, SchedulerEndpointProviderProxy>();
        var endpointProvider = (SchedulerEndpointProviderProxy)(object)endpoints;
        endpointProvider.Endpoint = endpoint;
        var provider = new DelayedScheduleMessageProvider(endpoints, new FakeTimeProvider(CommandTime));
        Uri destination = new("loopback://localhost/token-probe");
        DateTimeOffset dueAt = CommandTime + TimeSpan.FromMinutes(10);
        Guid validToken = NewId.NextGuid();

        ArgumentException rejected = await Assert.ThrowsAsync<ArgumentException>(() => provider.ScheduleSendAsync(
            destination, dueAt, new AdmissionTokenProbe(Guid.Empty),
            Pipe.Empty<SendContext<AdmissionTokenProbe>>(), TestContext.Current.CancellationToken));
        Assert.Equal("message", rejected.ParamName);
        Assert.Equal(0, endpointProvider.ResolutionCount);
        Assert.Null(((ScheduleEndpointProxy)(object)endpoint).Context);

        ScheduledMessage<AdmissionTokenProbe> accepted = await provider.ScheduleSendAsync(
            destination, dueAt, new AdmissionTokenProbe(validToken),
            Pipe.Empty<SendContext<AdmissionTokenProbe>>(), TestContext.Current.CancellationToken);

        Assert.Equal(1, endpointProvider.ResolutionCount);
        Assert.Equal(validToken, accepted.TokenId);
        var context = Assert.IsType<InMemorySendContext<AdmissionTokenProbe>>(
            ((ScheduleEndpointProxy)(object)endpoint).Context);
        Assert.Equal(validToken, context.ScheduledMessageId);
        Assert.True(context.Headers.TryGetHeader(MessageHeaders.SchedulingTokenId, out object? header));
        Assert.Equal(validToken.ToString("D"), header);
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
        Assert.Equal("0 0 2 * * ?", schedule.CronExpression);
        Assert.Equal(schedule.ScheduleId, schedule.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER-CLOCK", "default-schedule-requires-cron-expression")]
    public void DefaultRecurringSchedule_RejectsAMissingCronExpression(string? cronExpression)
    {
        Assert.ThrowsAny<ArgumentException>(() => new TestRecurringSchedule(TimeProvider.System, cronExpression!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER-CLOCK", "endpoint-control-command-timestamps")]
    public async Task EndpointRecurringControlCommands_UseTheInjectedClockAndPreserveScheduleIdentityAsync()
    {
        var clock = new FakeTimeProvider(CommandTime);
        ISendEndpoint endpoint = DispatchProxy.Create<ISendEndpoint, CaptureEndpointProxy>();
        var capture = (CaptureEndpointProxy)(object)endpoint;
        var scheduler = new EndpointRecurringMessageScheduler(endpoint, timeProvider: clock);

        await scheduler.CancelScheduledRecurringSendAsync("nightly", "operations", TestContext.Current.CancellationToken);
        await scheduler.PauseScheduledRecurringSendAsync("nightly", "operations", TestContext.Current.CancellationToken);
        await scheduler.ResumeScheduledRecurringSendAsync("nightly", "operations", TestContext.Current.CancellationToken);

        AssertControlCommands(capture.Messages);
        Assert.Same(clock, scheduler.TimeProvider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER-CLOCK", "publish-control-command-timestamps")]
    public async Task PublishRecurringControlCommands_UseTheInjectedClockAndPreserveScheduleIdentityAsync()
    {
        var clock = new FakeTimeProvider(CommandTime);
        IPublishEndpoint endpoint = DispatchProxy.Create<IPublishEndpoint, CaptureEndpointProxy>();
        var capture = (CaptureEndpointProxy)(object)endpoint;
        var scheduler = new PublishRecurringMessageScheduler(endpoint, timeProvider: clock);

        await scheduler.CancelScheduledRecurringSendAsync("nightly", "operations", TestContext.Current.CancellationToken);
        await scheduler.PauseScheduledRecurringSendAsync("nightly", "operations", TestContext.Current.CancellationToken);
        await scheduler.ResumeScheduledRecurringSendAsync("nightly", "operations", TestContext.Current.CancellationToken);

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

    private static void AssertCancellationTimestamp(object commandValues)
    {
        PropertyInfo timestamp = commandValues.GetType().GetProperty("Timestamp")
            ?? throw new InvalidOperationException("The cancellation command values do not expose a timestamp.");

        Assert.Equal(CommandTime, Assert.IsType<DateTimeOffset>(timestamp.GetValue(commandValues)));
    }

    private static void AssertCommand(CancelScheduledRecurringMessage command)
    {
        Assert.Equal(CommandTime, command.Timestamp);
        Assert.Equal(TimeSpan.Zero, command.Timestamp.Offset);
        Assert.Equal("nightly", command.ScheduleId);
        Assert.Equal("operations", command.ScheduleGroup);
    }

    private static void AssertCommand(PauseScheduledRecurringMessage command)
    {
        Assert.Equal(CommandTime, command.Timestamp);
        Assert.Equal(TimeSpan.Zero, command.Timestamp.Offset);
        Assert.Equal("nightly", command.ScheduleId);
        Assert.Equal("operations", command.ScheduleGroup);
    }

    private static void AssertCommand(ResumeScheduledRecurringMessage command)
    {
        Assert.Equal(CommandTime, command.Timestamp);
        Assert.Equal(TimeSpan.Zero, command.Timestamp.Offset);
        Assert.Equal("nightly", command.ScheduleId);
        Assert.Equal("operations", command.ScheduleGroup);
    }

    private sealed class TestRecurringSchedule(TimeProvider timeProvider, string cronExpression = "0 0 2 * * ?") :
        DefaultRecurringSchedule(cronExpression, timeProvider: timeProvider)
    {
        public override string ToString() => CronExpression;
    }

    private sealed record ClockProbe;

    private sealed record RuntimeClockProbe;

    private sealed record TokenProbe(Guid TokenId);

    private sealed record AdmissionTokenProbe(Guid TokenId);

    private sealed record NullSelectorProbe;

    private interface AdvancedScheduleEndpoint :
        ISendEndpoint,
        ViciOne.ServiceBus.Advanced.IAdvancedSendEndpoint;

    private interface AdvancedPublishEndpoint :
        IPublishEndpoint,
        ViciOne.ServiceBus.Advanced.IAdvancedPublishEndpoint;

    private class CaptureEndpointProxy : DispatchProxy
    {
        public List<object> Messages { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name is "SendAsync" or "PublishAsync")
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

            if (targetMethod.Name == "SendAsync"
                && args is [ClockProbe message, IPipe<SendContext<ClockProbe>> pipe, CancellationToken _])
            {
                var context = new InMemorySendContext<ClockProbe>(message);
                Context = context;
                return pipe.SendAsync(context);
            }

            if (targetMethod.Name == "SendAsync"
                && args is [TokenProbe tokenMessage, IPipe<SendContext<TokenProbe>> tokenPipe, CancellationToken _])
            {
                var context = new InMemorySendContext<TokenProbe>(tokenMessage);
                Context = context;
                return tokenPipe.SendAsync(context);
            }

            if (targetMethod.Name == "SendAsync"
                && args is [AdmissionTokenProbe admissionMessage, IPipe<SendContext<AdmissionTokenProbe>> admissionPipe, CancellationToken _])
            {
                var context = new InMemorySendContext<AdmissionTokenProbe>(admissionMessage);
                Context = context;
                return admissionPipe.SendAsync(context);
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class SchedulerEndpointProviderProxy : DispatchProxy
    {
        public ISendEndpoint Endpoint { get; set; } = null!;

        public int ResolutionCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "GetSendEndpointAsync")
            {
                ResolutionCount++;
                return Task.FromResult(Endpoint);
            }

            throw new NotSupportedException(targetMethod.Name);
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
                "GetSendEndpointAsync" => Task.FromResult(Endpoint),
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
