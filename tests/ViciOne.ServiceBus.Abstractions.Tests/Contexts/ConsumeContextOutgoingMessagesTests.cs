using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Contexts;

public sealed class ConsumeContextOutgoingMessagesTests
{
    private static readonly Uri Destination = new("loopback://localhost/outgoing-destination");
    private static readonly DateTimeOffset DueAt = new(2044, 5, 6, 7, 8, 9, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-CONSUME-OUTGOING", "constructor-requires-owning-consume-context")]
    public void Constructor_RejectsMissingConsumeContext()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new ConsumeContextOutgoingMessages(null!));

        Assert.Equal("context", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-CONSUME-OUTGOING", "routed-send-requires-route-capable-provider")]
    public async Task RoutedSend_RejectsAProviderWithoutRouteCapabilityBeforeEndpointResolutionAsync()
    {
        TestFixture fixture = CreateFixture(new UnroutedSendEndpointProvider());

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            fixture.Outgoing.SendAsync(new OutgoingMessage("unrouted"), TestContext.Current.CancellationToken));

        Assert.Contains(typeof(OutgoingMessage).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, fixture.Context.ReceiveContextReadCount);
        Assert.Equal(0, fixture.Context.EndpointResolutionCount);
        Assert.Equal(0, fixture.Endpoint.SendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-CONSUME-OUTGOING", "explicit-send-forwards-all-arguments-once")]
    public async Task ExplicitSend_ForwardsTheExactDestinationMessageOptionsAndTokenAsync()
    {
        var message = new OutgoingMessage("explicit");
        var options = new SendOptions
        {
            Headers = new Dictionary<string, object?> { ["tenant"] = "north" },
            PartitionKey = "tenant-7",
        };
        using var cancellation = new CancellationTokenSource();
        TestFixture fixture = CreateFixture(new UnroutedSendEndpointProvider());

        await fixture.Outgoing.SendAsync(Destination, message, options, cancellation.Token);

        Assert.Equal(0, fixture.Context.ReceiveContextReadCount);
        Assert.Equal(1, fixture.Context.EndpointResolutionCount);
        Assert.Equal(Destination, fixture.Context.ResolvedDestination);
        Assert.Equal(cancellation.Token, fixture.Context.EndpointResolutionToken);
        Assert.Equal(1, fixture.Endpoint.SendCount);
        Assert.Same(message, fixture.Endpoint.Message);
        Assert.Same(options, fixture.Endpoint.Options);
        Assert.Equal(cancellation.Token, fixture.Endpoint.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-CONSUME-OUTGOING", "default-and-configured-publish-forward-exact-arguments")]
    public async Task PublishOverloads_ForwardTheExactMessagesOptionsAndTokensOnceAsync()
    {
        var defaultMessage = new OutgoingMessage("default-publish");
        var configuredMessage = new OutgoingMessage("configured-publish");
        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object?> { ["tenant"] = "south" },
            PartitionKey = "tenant-9",
        };
        using var defaultCancellation = new CancellationTokenSource();
        using var configuredCancellation = new CancellationTokenSource();
        TestFixture fixture = CreateFixture(new UnroutedSendEndpointProvider());

        await fixture.Outgoing.PublishAsync(defaultMessage, defaultCancellation.Token);
        await fixture.Outgoing.PublishAsync(configuredMessage, options, configuredCancellation.Token);

        Assert.Equal(2, fixture.Context.Publications.Count);
        Assert.Same(defaultMessage, fixture.Context.Publications[0].Message);
        Assert.Null(fixture.Context.Publications[0].Options);
        Assert.Equal(defaultCancellation.Token, fixture.Context.Publications[0].CancellationToken);
        Assert.Same(configuredMessage, fixture.Context.Publications[1].Message);
        Assert.Same(options, fixture.Context.Publications[1].Options);
        Assert.Equal(configuredCancellation.Token, fixture.Context.Publications[1].CancellationToken);
        Assert.Equal(0, fixture.Context.EndpointResolutionCount);
        Assert.Equal(0, fixture.Endpoint.SendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-CONSUME-OUTGOING", "scheduled-send-forwards-all-arguments-and-result")]
    public async Task ScheduledSend_UsesTheContextSchedulerAndReturnsItsExactResultAsync()
    {
        var message = new OutgoingMessage("scheduled");
        var expected = new RecordedScheduledMessage<OutgoingMessage>(
            Guid.Parse("8e899ca3-7448-4058-8ef5-d6670144b54a"), DueAt, Destination, message);
        MessageSchedulerContext scheduler = CreateScheduler(expected, out RecordingMessageSchedulerProxy schedulerRecorder);
        using var cancellation = new CancellationTokenSource();
        TestFixture fixture = CreateFixture(new UnroutedSendEndpointProvider(), scheduler);

        ScheduledMessage<OutgoingMessage> actual = await fixture.Outgoing.ScheduleSendAsync(
            Destination,
            DueAt,
            message,
            cancellation.Token);

        Assert.Same(expected, actual);
        Assert.Equal(1, schedulerRecorder.CallCount);
        Assert.Equal(Destination, schedulerRecorder.Destination);
        Assert.Equal(DueAt, schedulerRecorder.DueAt);
        Assert.Same(message, schedulerRecorder.Message);
        Assert.Equal(cancellation.Token, schedulerRecorder.CancellationToken);
        Assert.Equal(0, fixture.Context.EndpointResolutionCount);
        Assert.Empty(fixture.Context.Publications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-CONSUME-OUTGOING", "scheduled-send-requires-context-scheduler")]
    public void ScheduledSend_RejectsAnUnavailableSchedulerBeforeAnyOutgoingWork()
    {
        TestFixture fixture = CreateFixture(new UnroutedSendEndpointProvider());

        void ScheduleWithoutScheduler() => _ = fixture.Outgoing.ScheduleSendAsync(
                Destination,
                DueAt,
                new OutgoingMessage("unscheduled"),
                TestContext.Current.CancellationToken);
        ConfigurationException exception = Assert.Throws<ConfigurationException>(ScheduleWithoutScheduler);

        Assert.Contains("No message scheduler", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, fixture.Context.EndpointResolutionCount);
        Assert.Empty(fixture.Context.Publications);
        Assert.Equal(0, fixture.Endpoint.SendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-CONSUME-OUTGOING", "every-null-boundary-fails-before-dependency-work")]
    public async Task EveryNullArgument_IsRejectedBeforeAnyDependencyWorkAsync()
    {
        MessageSchedulerContext scheduler = CreateScheduler(
            new RecordedScheduledMessage<OutgoingMessage>(Guid.Empty, DueAt, Destination, new OutgoingMessage("unused")),
            out RecordingMessageSchedulerProxy schedulerRecorder);
        TestFixture fixture = CreateFixture(new UnroutedSendEndpointProvider(), scheduler);
        var message = new OutgoingMessage("valid");
        var sendOptions = new SendOptions();
        var publishOptions = new PublishOptions();

        await AssertParameterAsync("message", () => fixture.Outgoing.SendAsync<OutgoingMessage>(null!));
        await AssertParameterAsync("destination", () => fixture.Outgoing.SendAsync(null!, message, sendOptions));
        await AssertParameterAsync("message", () => fixture.Outgoing.SendAsync<OutgoingMessage>(Destination, null!, sendOptions));
        await AssertParameterAsync("options", () => fixture.Outgoing.SendAsync(Destination, message, null!));
        AssertParameter("message", () => fixture.Outgoing.PublishAsync<OutgoingMessage>(null!));
        AssertParameter("message", () => fixture.Outgoing.PublishAsync<OutgoingMessage>(null!, publishOptions));
        AssertParameter("options", () => fixture.Outgoing.PublishAsync(message, null!));
        AssertParameter("destination", () => fixture.Outgoing.ScheduleSendAsync(null!, DueAt, message));
        AssertParameter("message", () => fixture.Outgoing.ScheduleSendAsync<OutgoingMessage>(Destination, DueAt, null!));

        Assert.Equal(0, fixture.Context.ReceiveContextReadCount);
        Assert.Equal(0, fixture.Context.EndpointResolutionCount);
        Assert.Empty(fixture.Context.Publications);
        Assert.Equal(0, fixture.Endpoint.SendCount);
        Assert.Equal(0, schedulerRecorder.CallCount);
    }

    private static TestFixture CreateFixture(ISendEndpointProvider provider, MessageSchedulerContext? scheduler = null)
    {
        var endpoint = new RecordingSendEndpoint();
        ConsumeContext context = DispatchProxy.Create<ConsumeContext, RecordingConsumeContextProxy>();
        var contextRecorder = (RecordingConsumeContextProxy)(object)context;
        contextRecorder.ReceiveContext = CreateReceiveContext(provider);
        contextRecorder.Endpoint = endpoint;
        contextRecorder.Scheduler = scheduler;
        return new TestFixture(new ConsumeContextOutgoingMessages(context), contextRecorder, endpoint);
    }

    private static ReceiveContext CreateReceiveContext(ISendEndpointProvider provider)
    {
        ReceiveContext context = DispatchProxy.Create<ReceiveContext, RecordingReceiveContextProxy>();
        ((RecordingReceiveContextProxy)(object)context).SendEndpointProvider = provider;
        return context;
    }

    private static MessageSchedulerContext CreateScheduler(
        ScheduledMessage<OutgoingMessage> result,
        out RecordingMessageSchedulerProxy recorder)
    {
        MessageSchedulerContext scheduler = DispatchProxy.Create<MessageSchedulerContext, RecordingMessageSchedulerProxy>();
        recorder = (RecordingMessageSchedulerProxy)(object)scheduler;
        recorder.Result = result;
        return scheduler;
    }

    private static async Task AssertParameterAsync(string parameterName, Func<Task> action)
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private static void AssertParameter(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private sealed record OutgoingMessage(string Value);

    private sealed record RecordedScheduledMessage<T>(Guid TokenId, DateTimeOffset DueAt, Uri Destination, T Payload) :
        ScheduledMessage<T>
        where T : class;

    private sealed record Publication(object Message, PublishOptions? Options, CancellationToken CancellationToken);

    private sealed record TestFixture(
        IOutgoingMessages Outgoing,
        RecordingConsumeContextProxy Context,
        RecordingSendEndpoint Endpoint);

    private class RecordingConsumeContextProxy : DispatchProxy
    {
        public ReceiveContext ReceiveContext { get; set; } = null!;

        public ISendEndpoint Endpoint { get; set; } = null!;

        public MessageSchedulerContext? Scheduler { get; set; }

        public int ReceiveContextReadCount { get; private set; }

        public int EndpointResolutionCount { get; private set; }

        public Uri? ResolvedDestination { get; private set; }

        public CancellationToken EndpointResolutionToken { get; private set; }

        public List<Publication> Publications { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ArgumentNullException.ThrowIfNull(args);

            switch (targetMethod.Name)
            {
                case "get_ReceiveContext":
                    ReceiveContextReadCount++;
                    return ReceiveContext;

                case "GetSendEndpointAsync":
                    EndpointResolutionCount++;
                    ResolvedDestination = Assert.IsType<Uri>(args[0]);
                    EndpointResolutionToken = Assert.IsType<CancellationToken>(args[1]);
                    return Task.FromResult(Endpoint);

                case "PublishAsync":
                    Publications.Add(args.Length switch
                    {
                        2 => new Publication(
                            Assert.IsAssignableFrom<object>(args[0]),
                            null,
                            Assert.IsType<CancellationToken>(args[1])),
                        3 => new Publication(
                            Assert.IsAssignableFrom<object>(args[0]),
                            Assert.IsType<PublishOptions>(args[1]),
                            Assert.IsType<CancellationToken>(args[2])),
                        _ => throw new NotSupportedException(targetMethod.ToString()),
                    });
                    return Task.CompletedTask;

                case "TryGetPayload":
                    Type payloadType = targetMethod.GetGenericArguments()[0];
                    bool found = Scheduler is not null && payloadType.IsInstanceOfType(Scheduler);
                    args[0] = found ? Scheduler : null;
                    return found;

                default:
                    throw new NotSupportedException(targetMethod.ToString());
            }
        }
    }

    private class RecordingReceiveContextProxy : DispatchProxy
    {
        public ISendEndpointProvider SendEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_SendEndpointProvider")
                return SendEndpointProvider;

            throw new NotSupportedException(targetMethod?.ToString());
        }
    }

    private class RecordingMessageSchedulerProxy : DispatchProxy
    {
        public ScheduledMessage<OutgoingMessage> Result { get; set; } = null!;

        public int CallCount { get; private set; }

        public Uri? Destination { get; private set; }

        public DateTimeOffset DueAt { get; private set; }

        public object? Message { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ArgumentNullException.ThrowIfNull(args);
            if (targetMethod.Name != "ScheduleSendAsync" || !targetMethod.IsGenericMethod || args.Length != 4)
                throw new NotSupportedException(targetMethod.ToString());

            CallCount++;
            Destination = Assert.IsType<Uri>(args[0]);
            DueAt = Assert.IsType<DateTimeOffset>(args[1]);
            Message = args[2];
            CancellationToken = Assert.IsType<CancellationToken>(args[3]);
            return Task.FromResult(Result);
        }
    }

    private sealed class RecordingSendEndpoint : ISendEndpoint
    {
        public int SendCount { get; private set; }

        public object? Message { get; private set; }

        public SendOptions? Options { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Record(message, null, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SendAsync<TMessage>(TMessage message, SendOptions options, CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Record(message, options, cancellationToken);
            return Task.CompletedTask;
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();

        private void Record(object message, SendOptions? options, CancellationToken cancellationToken)
        {
            SendCount++;
            Message = message;
            Options = options;
            CancellationToken = cancellationToken;
        }
    }

    private sealed class UnroutedSendEndpointProvider : ISendEndpointProvider
    {
        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The consume context owns endpoint resolution.");

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();
    }

    private sealed class EmptyConnectHandle : ConnectHandle
    {
        public void Dispose()
        {
        }

        public void Disconnect()
        {
        }
    }
}
