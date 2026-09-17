using System.Reflection;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierRoutingSlipSubscriptionDeepContractTests
{
    private static readonly Uri DestinationAddress = new("loopback://localhost/deep-courier-events");

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTIONS", "subscription-api-separates-contract-materializer-and-capture-target")]
    public void SubscriptionApi_SeparatesReadOnlyContractWritableMaterializerAndInternalCaptureTarget()
    {
        Assert.True(typeof(ISubscription).IsPublic);
        Assert.All(typeof(ISubscription).GetProperties(), property =>
        {
            Assert.True(property.GetMethod is { IsPublic: true });
            Assert.Null(property.SetMethod);
        });

        Assert.False(typeof(RoutingSlipSubscription).IsPublic);
        Assert.True(typeof(RoutingSlipSubscription).IsSealed);
        Assert.NotNull(typeof(RoutingSlipSubscription).GetConstructor(Type.EmptyTypes));
        Assert.All(typeof(RoutingSlipSubscription).GetProperties(BindingFlags.Instance | BindingFlags.Public), property =>
        {
            Assert.True(property.GetMethod is { IsPublic: true });
            Assert.True(property.SetMethod is { IsPublic: true });
        });

        Assert.False(typeof(IRoutingSlipSubscriptionTarget).IsPublic);
        MethodInfo targetMethod = Assert.Single(typeof(IRoutingSlipSubscriptionTarget).GetMethods());
        Assert.Equal(nameof(IRoutingSlipSubscriptionTarget.AddSubscription), targetMethod.Name);
        Assert.Equal(typeof(void), targetMethod.ReturnType);
        Assert.Equal(
            [typeof(Uri), typeof(RoutingSlipEvents), typeof(RoutingSlipEventContents), typeof(string), typeof(MessageEnvelope)],
            targetMethod.GetParameters().Select(parameter => parameter.ParameterType));

        Assert.False(typeof(RoutingSlipSubscriptionCaptureEndpoint).IsPublic);
        Assert.True(typeof(RoutingSlipSubscriptionCaptureEndpoint).IsSealed);
        Assert.Contains(typeof(ISendEndpoint), typeof(RoutingSlipSubscriptionCaptureEndpoint).GetInterfaces());
        Assert.Contains(typeof(ViciOne.ServiceBus.Advanced.IAdvancedSendEndpoint),
            typeof(RoutingSlipSubscriptionCaptureEndpoint).GetInterfaces());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-FLAGS", "selection-validates-every-defined-combination-and-unknown-boundary")]
    public void SelectionValidation_AcceptsEveryDefinedCombinationAndRejectsEveryEmptyOrUnknownBoundary()
    {
        RoutingSlipEvents[] lifecycleEvents =
        [
            RoutingSlipEvents.Completed,
            RoutingSlipEvents.Faulted,
            RoutingSlipEvents.CompensationFailed,
            RoutingSlipEvents.Terminated,
            RoutingSlipEvents.Revised,
            RoutingSlipEvents.ActivityCompleted,
            RoutingSlipEvents.ActivityFaulted,
            RoutingSlipEvents.ActivityCompensated,
            RoutingSlipEvents.ActivityCompensationFailed,
        ];

        for (var subset = 1; subset < 1 << lifecycleEvents.Length; subset++)
        {
            var selection = RoutingSlipEvents.None;
            for (var index = 0; index < lifecycleEvents.Length; index++)
            {
                if ((subset & 1 << index) != 0)
                    selection |= lifecycleEvents[index];
            }

            Assert.Equal(selection, RoutingSlipSubscriptionSelection.Validate(selection, "selection"));
            Assert.Equal(
                selection | RoutingSlipEvents.Supplemental,
                RoutingSlipSubscriptionSelection.Validate(selection | RoutingSlipEvents.Supplemental, "selection"));
        }

        RoutingSlipEvents[] invalidEvents =
        [
            RoutingSlipEvents.None,
            RoutingSlipEvents.Supplemental,
            (RoutingSlipEvents)0x20,
            (RoutingSlipEvents)0x20000,
            (RoutingSlipEvents)(-1),
        ];
        foreach (RoutingSlipEvents selection in invalidEvents)
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                RoutingSlipSubscriptionSelection.Validate(selection, "selection"));
            Assert.Equal("selection", exception.ParamName);
            Assert.Equal(selection, exception.ActualValue);
        }

        for (var rawContents = 0; rawContents <= (int)RoutingSlipEventContents.All; rawContents++)
        {
            var selection = (RoutingSlipEventContents)rawContents;
            Assert.Equal(selection, RoutingSlipSubscriptionSelection.Validate(selection, "selection"));
        }

        RoutingSlipEventContents[] invalidContents =
        [
            (RoutingSlipEventContents)0x10,
            (RoutingSlipEventContents)int.MinValue,
            (RoutingSlipEventContents)(-1),
        ];
        foreach (RoutingSlipEventContents selection in invalidContents)
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                RoutingSlipSubscriptionSelection.Validate(selection, "selection"));
            Assert.Equal("selection", exception.ParamName);
            Assert.Equal(selection, exception.ActualValue);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTIONS", "strong-subscription-preserves-address-filter-flags-and-envelope-identity")]
    public void StrongSubscription_PreservesAddressFilterFlagsEnvelopeIdentityAndExactValidationParameters()
    {
        var envelope = new MutableMessageEnvelope { Message = "captured" };
        RoutingSlipEvents events = RoutingSlipEvents.ActivityCompleted | RoutingSlipEvents.Supplemental;
        var subscription = new RoutingSlipSubscription(
            DestinationAddress,
            events,
            RoutingSlipEventContents.None,
            "cHaRgEcArD",
            envelope);

        Assert.Same(DestinationAddress, subscription.Address);
        Assert.Equal(events, subscription.Events);
        Assert.Equal(RoutingSlipEventContents.None, subscription.Include);
        Assert.Equal("cHaRgEcArD", subscription.ActivityName);
        Assert.Same(envelope, subscription.Message);

        Assert.Equal("address", Assert.Throws<ArgumentNullException>(() => new RoutingSlipSubscription(
            null!, RoutingSlipEvents.Completed, RoutingSlipEventContents.None)).ParamName);
        Assert.Equal("activityName", Assert.Throws<ArgumentException>(() => new RoutingSlipSubscription(
            DestinationAddress, RoutingSlipEvents.Completed, RoutingSlipEventContents.None, " ")).ParamName);
        Assert.Equal("events", Assert.Throws<ArgumentOutOfRangeException>(() => new RoutingSlipSubscription(
            DestinationAddress, RoutingSlipEvents.Supplemental, RoutingSlipEventContents.None)).ParamName);
        Assert.Equal("include", Assert.Throws<ArgumentOutOfRangeException>(() => new RoutingSlipSubscription(
            DestinationAddress, RoutingSlipEvents.Completed, (RoutingSlipEventContents)0x10)).ParamName);

        var withoutOptionals = new RoutingSlipSubscription(
            DestinationAddress, RoutingSlipEvents.Completed, RoutingSlipEventContents.None);
        Assert.Null(withoutOptionals.ActivityName);
        Assert.Null(withoutOptionals.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTIONS", "received-subscription-detaches-fields-and-wraps-invalid-wire-state")]
    public void ReceivedSubscription_DetachesFieldsAndEnvelopeAndWrapsInvalidWireState()
    {
        var messageTypes = new List<string> { "urn:message:original" };
        var headers = new Dictionary<string, object?> { ["tenant"] = "north" };
        var envelope = new MutableMessageEnvelope
        {
            Message = "original",
            MessageTypes = messageTypes,
            Headers = headers,
        };
        var source = new MutableSubscription
        {
            Address = DestinationAddress,
            Events = RoutingSlipEvents.ActivityFaulted | RoutingSlipEvents.Supplemental,
            Include = RoutingSlipEventContents.Arguments,
            ActivityName = "ChargeCard",
            Message = envelope,
        };

        var snapshot = new RoutingSlipSubscription(source);
        Assert.Equal(1, source.AddressReadCount);
        Assert.Equal(1, source.MessageReadCount);
        source.Address = new Uri("loopback://localhost/changed-events");
        source.Events = RoutingSlipEvents.Completed;
        source.Include = RoutingSlipEventContents.None;
        source.ActivityName = "Changed";
        source.Message = null;
        envelope.Message = "mutated";
        messageTypes.Add("urn:message:added");
        headers["tenant"] = "south";

        Assert.NotSame(source, snapshot);
        Assert.Same(DestinationAddress, snapshot.Address);
        Assert.Equal(RoutingSlipEvents.ActivityFaulted | RoutingSlipEvents.Supplemental, snapshot.Events);
        Assert.Equal(RoutingSlipEventContents.Arguments, snapshot.Include);
        Assert.Equal("ChargeCard", snapshot.ActivityName);
        MessageEnvelope messageSnapshot = Assert.IsAssignableFrom<MessageEnvelope>(snapshot.Message);
        Assert.NotSame(envelope, messageSnapshot);
        Assert.Equal("original", messageSnapshot.Message);
        Assert.Equal(["urn:message:original"], messageSnapshot.MessageTypes);
        Assert.Equal("north", messageSnapshot.Headers?["TENANT"]);

        Assert.Equal("subscription", Assert.Throws<ArgumentNullException>(() => new RoutingSlipSubscription(null!)).ParamName);

        source.Address = null!;
        Assert.Throws<SerializationException>(() => new RoutingSlipSubscription(source));
        source.Address = DestinationAddress;

        source.Events = RoutingSlipEvents.None;
        SerializationException invalidEvents = Assert.Throws<SerializationException>(() => new RoutingSlipSubscription(source));
        var eventCause = Assert.IsType<ArgumentOutOfRangeException>(invalidEvents.InnerException);
        Assert.Equal(nameof(ISubscription.Events), eventCause.ParamName);
        source.Events = RoutingSlipEvents.Completed;

        source.Include = (RoutingSlipEventContents)0x10;
        SerializationException invalidContents = Assert.Throws<SerializationException>(() => new RoutingSlipSubscription(source));
        var contentCause = Assert.IsType<ArgumentOutOfRangeException>(invalidContents.InnerException);
        Assert.Equal(nameof(ISubscription.Include), contentCause.ParamName);
        source.Include = RoutingSlipEventContents.None;

        source.ActivityName = " ";
        SerializationException invalidActivity = Assert.Throws<SerializationException>(() => new RoutingSlipSubscription(source));
        Assert.IsType<ArgumentException>(invalidActivity.InnerException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-CAPTURE", "all-send-overloads-report-exact-null-parameters")]
    public async Task CaptureEndpoint_AllSendOverloadsReportExactNullParametersAsync()
    {
        var endpoint = new RoutingSlipSubscriptionCaptureEndpoint(
            new RecordingTarget(), DestinationAddress, RoutingSlipEvents.Completed, null);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var message = new SubscriptionMessage("value");
        IPipe<SendContext<SubscriptionMessage>> typedPipe = Pipe.Empty<SendContext<SubscriptionMessage>>();
        IPipe<SendContext> untypedPipe = Pipe.Empty<SendContext>();

        await AssertNullAsync("message", () => endpoint.SendAsync<SubscriptionMessage>(null!, cancellationToken));
        await AssertNullAsync("message", () => endpoint.SendAsync<SubscriptionMessage>(
            null!, typedPipe, cancellationToken));
        await AssertNullAsync("pipe", () => endpoint.SendAsync(
            message, (IPipe<SendContext<SubscriptionMessage>>)null!, cancellationToken));
        await AssertNullAsync("pipe", () => endpoint.SendAsync(
            message, (IPipe<SendContext>)null!, cancellationToken));
        await AssertNullAsync("message", () => endpoint.SendAsync((object)null!, cancellationToken));
        await AssertNullAsync("messageType", () => endpoint.SendAsync(
            (object)message, (Type)null!, cancellationToken));
        await AssertNullAsync("message", () => endpoint.SendAsync(
            (object)null!, untypedPipe, cancellationToken));
        await AssertNullAsync("pipe", () => endpoint.SendAsync(
            (object)message, (IPipe<SendContext>)null!, cancellationToken));
        await AssertNullAsync("messageType", () => endpoint.SendAsync(
            (object)message, null!, untypedPipe, cancellationToken));
        await AssertNullAsync("pipe", () => endpoint.SendAsync(
            (object)message, typeof(SubscriptionMessage), null!, cancellationToken));
        await AssertNullAsync("values", () => endpoint.SendAsync<SubscriptionMessage>(
            (object)null!, cancellationToken));
        await AssertNullAsync("values", () => endpoint.SendAsync<SubscriptionMessage>(
            (object)null!, typedPipe, cancellationToken));
        await AssertNullAsync("pipe", () => endpoint.SendAsync<SubscriptionMessage>(
            new { Value = "typed" }, (IPipe<SendContext<SubscriptionMessage>>)null!, cancellationToken));
        await AssertNullAsync("values", () => endpoint.SendAsync<SubscriptionMessage>(
            (object)null!, untypedPipe, cancellationToken));
        await AssertNullAsync("pipe", () => endpoint.SendAsync<SubscriptionMessage>(
            new { Value = "untyped" }, (IPipe<SendContext>)null!, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-CAPTURE", "target-failure-remains-authoritative-over-fault-observer-failure")]
    public async Task CaptureTargetFailure_RemainsAuthoritativeWhenTheFaultObserverAlsoFailsAsync()
    {
        var targetFailure = new ExpectedTargetException();
        var observerFailure = new ExpectedObserverException();
        var trace = new List<string>();
        var target = new RecordingTarget(trace, targetFailure);
        var endpoint = new RoutingSlipSubscriptionCaptureEndpoint(
            target, DestinationAddress, RoutingSlipEvents.Completed, "ChargeCard");
        var observer = new PhaseObserver(trace, faultFailure: observerFailure);
        using ConnectHandle handle = endpoint.ConnectSendObserver(observer);

        ExpectedTargetException actual = await Assert.ThrowsAsync<ExpectedTargetException>(() => endpoint.SendAsync(
            new SubscriptionMessage("target-fault"),
            new DelegatePipe<SendContext<SubscriptionMessage>>(_ =>
            {
                trace.Add("pipe");
                return Task.CompletedTask;
            }),
            TestContext.Current.CancellationToken));

        Assert.Same(targetFailure, actual);
        Assert.Same(targetFailure, observer.ObservedFailure);
        Assert.Equal(["pre", "pipe", "target", "fault"], trace);
        Assert.Equal(1, target.CallCount);
        Assert.Empty(target.Captures);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-CAPTURE", "post-observer-failure-occurs-after-one-durable-capture")]
    public async Task PostObserverFailure_ReportsFaultAfterExactlyOneCaptureAsync()
    {
        var expected = new ExpectedObserverException();
        var trace = new List<string>();
        var target = new RecordingTarget(trace);
        var endpoint = new RoutingSlipSubscriptionCaptureEndpoint(
            target, DestinationAddress, RoutingSlipEvents.Completed, null);
        var observer = new PhaseObserver(trace, postFailure: expected);
        using ConnectHandle handle = endpoint.ConnectSendObserver(observer);

        ExpectedObserverException actual = await Assert.ThrowsAsync<ExpectedObserverException>(() => endpoint.SendAsync(
            new SubscriptionMessage("post-fault"), TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Same(expected, observer.ObservedFailure);
        Assert.Equal(["pre", "target", "post", "fault"], trace);
        Assert.Equal(1, target.CallCount);
        CapturedSubscription captured = Assert.Single(target.Captures);
        Assert.Equal(DestinationAddress, captured.Address);
        Assert.Equal(RoutingSlipEvents.Completed, captured.Events);
        Assert.Equal(RoutingSlipEventContents.All, captured.Contents);
        Assert.Null(captured.ActivityName);
        Assert.Equal(DestinationAddress.ToString(), captured.Message.DestinationAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-CAPTURE", "pre-and-midflight-cancellation-have-distinct-observer-boundaries")]
    public async Task Cancellation_PreCanceledSkipsObserversWhilePipelineCancellationReportsFaultAndSkipsCaptureAsync()
    {
        var trace = new List<string>();
        var target = new RecordingTarget(trace);
        var endpoint = new RoutingSlipSubscriptionCaptureEndpoint(
            target, DestinationAddress, RoutingSlipEvents.Completed, null);
        var observer = new PhaseObserver(trace);
        using ConnectHandle handle = endpoint.ConnectSendObserver(observer);
        using var preCanceled = new CancellationTokenSource();
        await preCanceled.CancelAsync();

        OperationCanceledException preFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            endpoint.SendAsync(new SubscriptionMessage("pre-canceled"), preCanceled.Token));
        Assert.Equal(preCanceled.Token, preFailure.CancellationToken);
        Assert.Empty(trace);
        Assert.Equal(0, target.CallCount);

        using var midFlight = new CancellationTokenSource();
        OperationCanceledException pipelineFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => endpoint.SendAsync(
            new SubscriptionMessage("mid-flight"),
            new DelegatePipe<SendContext<SubscriptionMessage>>(_ =>
            {
                trace.Add("pipe");
                midFlight.Cancel();
                return Task.FromCanceled(midFlight.Token);
            }),
            midFlight.Token));

        Assert.Equal(midFlight.Token, pipelineFailure.CancellationToken);
        Assert.Same(pipelineFailure, observer.ObservedFailure);
        Assert.Equal(["pre", "pipe", "fault"], trace);
        Assert.Equal(0, target.CallCount);
        Assert.Empty(target.Captures);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-CAPTURE", "runtime-contract-mismatch-fails-before-observation-or-capture")]
    public async Task RuntimeContractMismatch_FailsBeforeObserverNotificationOrCaptureAsync()
    {
        var trace = new List<string>();
        var target = new RecordingTarget(trace);
        var endpoint = new RoutingSlipSubscriptionCaptureEndpoint(
            target, DestinationAddress, RoutingSlipEvents.Completed, null);
        using ConnectHandle handle = endpoint.ConnectSendObserver(new PhaseObserver(trace));
        var message = new SubscriptionMessage("mismatch");

        ArgumentException mismatch = await Assert.ThrowsAsync<ArgumentException>(() => endpoint.SendAsync(
            (object)message, typeof(UnrelatedMessage), TestContext.Current.CancellationToken));
        Assert.Equal("message", mismatch.ParamName);
        ArgumentException valueType = await Assert.ThrowsAsync<ArgumentException>(() => endpoint.SendAsync(
            (object)message, typeof(int), TestContext.Current.CancellationToken));
        Assert.Equal("messageType", valueType.ParamName);

        Assert.Empty(trace);
        Assert.Equal(0, target.CallCount);
        Assert.Empty(target.Captures);
    }

    private static async Task AssertNullAsync(string parameterName, Func<Task> action)
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private sealed record CapturedSubscription(
        Uri Address,
        RoutingSlipEvents Events,
        RoutingSlipEventContents Contents,
        string? ActivityName,
        MessageEnvelope Message);

    private sealed class RecordingTarget(List<string>? trace = null, Exception? failure = null) : IRoutingSlipSubscriptionTarget
    {
        public int CallCount { get; private set; }

        public List<CapturedSubscription> Captures { get; } = [];

        public void AddSubscription(
            Uri address,
            RoutingSlipEvents events,
            RoutingSlipEventContents contents,
            string? activityName,
            MessageEnvelope message)
        {
            CallCount++;
            trace?.Add("target");
            if (failure is not null)
                throw failure;

            Captures.Add(new CapturedSubscription(address, events, contents, activityName, message));
        }
    }

    private sealed class PhaseObserver(
        List<string> trace,
        Exception? preFailure = null,
        Exception? postFailure = null,
        Exception? faultFailure = null) : ISendObserver
    {
        public Exception? ObservedFailure { get; private set; }

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            trace.Add("pre");
            return preFailure is null ? Task.CompletedTask : Task.FromException(preFailure);
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            trace.Add("post");
            return postFailure is null ? Task.CompletedTask : Task.FromException(postFailure);
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            ObservedFailure = exception;
            trace.Add("fault");
            return faultFailure is null ? Task.CompletedTask : Task.FromException(faultFailure);
        }
    }

    private sealed class DelegatePipe<TContext>(Func<TContext, Task> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context) => callback(context);

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    private sealed class MutableSubscription : ISubscription
    {
        private Uri? _address = DestinationAddress;
        private MessageEnvelope? _message;

        public int AddressReadCount { get; private set; }

        public int MessageReadCount { get; private set; }

        public Uri Address
        {
            get
            {
                AddressReadCount++;
                return _address!;
            }
            set => _address = value;
        }

        public RoutingSlipEvents Events { get; set; } = RoutingSlipEvents.Completed;
        public RoutingSlipEventContents Include { get; set; }
        public string? ActivityName { get; set; }

        public MessageEnvelope? Message
        {
            get
            {
                MessageReadCount++;
                return _message;
            }
            set => _message = value;
        }
    }

    private sealed class MutableMessageEnvelope : MessageEnvelope
    {
        public string? MessageId { get; set; }
        public string? RequestId { get; set; }
        public string? CorrelationId { get; set; }
        public string? ConversationId { get; set; }
        public string? InitiatorId { get; set; }
        public string? SourceAddress { get; set; }
        public string? DestinationAddress { get; set; }
        public string? ResponseAddress { get; set; }
        public string? FaultAddress { get; set; }
        public IReadOnlyList<string>? MessageTypes { get; set; }
        public object? Message { get; set; }
        public DateTimeOffset? ExpirationTime { get; set; }
        public DateTimeOffset? SentTime { get; set; }
        public IReadOnlyDictionary<string, object?>? Headers { get; set; }
        public HostInfo? Host { get; set; }
    }

    public sealed record SubscriptionMessage(string Value);

    public sealed record UnrelatedMessage(string Value);

    private sealed class ExpectedTargetException : Exception;

    private sealed class ExpectedObserverException : Exception;
}
