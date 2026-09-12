using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipSubscriptionCaptureEndpointTests
{
    private static readonly Uri DestinationAddress = new("loopback://localhost/courier-events");

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-CAPTURE", "constructor-validates-target-destination-and-selections")]
    public void Constructor_RejectsMissingOrInvalidSubscriptionState()
    {
        var target = new RecordingSubscriptionTarget();

        Assert.Equal("target", Assert.Throws<ArgumentNullException>(() => new RoutingSlipSubscriptionCaptureEndpoint(
            null!, DestinationAddress, RoutingSlipEvents.Completed, null)).ParamName);
        Assert.Equal("destinationAddress", Assert.Throws<ArgumentNullException>(() => new RoutingSlipSubscriptionCaptureEndpoint(
            target, null!, RoutingSlipEvents.Completed, null)).ParamName);
        Assert.Equal("activityName", Assert.Throws<ArgumentException>(() => new RoutingSlipSubscriptionCaptureEndpoint(
            target, DestinationAddress, RoutingSlipEvents.Completed, " ")).ParamName);
        Assert.Equal("events", Assert.Throws<ArgumentOutOfRangeException>(() => new RoutingSlipSubscriptionCaptureEndpoint(
            target, DestinationAddress, RoutingSlipEvents.None, null)).ParamName);
        Assert.Equal("contents", Assert.Throws<ArgumentOutOfRangeException>(() => new RoutingSlipSubscriptionCaptureEndpoint(
            target, DestinationAddress, RoutingSlipEvents.Completed, null, (RoutingSlipEventContents)0x40)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-CAPTURE", "typed-send-runs-pipeline-observers-and-captures-exact-envelope")]
    public async Task TypedSend_RunsPipelineAndObserversAroundExactEnvelopeCaptureAsync()
    {
        var target = new RecordingSubscriptionTarget();
        var endpoint = new RoutingSlipSubscriptionCaptureEndpoint(
            target,
            DestinationAddress,
            RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted,
            "ChargeCard",
            RoutingSlipEventContents.Variables);
        var observer = new RecordingSendObserver();
        using ConnectHandle handle = endpoint.ConnectSendObserver(observer);
        Guid correlationId = Guid.Parse("2864dc36-d2a1-4573-acb4-8bad7109b985");
        var message = new SubscriptionMessage("captured");

        await endpoint.SendAsync(
            message,
            new DelegatePipe<SendContext<SubscriptionMessage>>(context =>
            {
                context.CorrelationId = correlationId;
                observer.Events.Add("pipeline");
                return Task.CompletedTask;
            }),
            TestContext.Current.CancellationToken);

        CapturedSubscription captured = Assert.Single(target.Subscriptions);
        Assert.Equal(DestinationAddress, captured.Address);
        Assert.Equal(RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted, captured.Events);
        Assert.Equal(RoutingSlipEventContents.Variables, captured.Contents);
        Assert.Equal("ChargeCard", captured.ActivityName);
        Assert.Equal(correlationId.ToString(), captured.Message.CorrelationId);
        Assert.Equal(DestinationAddress.ToString(), captured.Message.DestinationAddress);
        Assert.Contains(MessageUrn.ForTypeString<SubscriptionMessage>(), captured.Message.MessageTypes!);
        Assert.NotNull(captured.Message.Message);
        Assert.Equal(["pre", "pipeline", "post"], observer.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-CAPTURE", "failed-pipeline-notifies-fault-and-does-not-capture")]
    public async Task FailedSend_NotifiesTheExactFailureWithoutCapturingAMessageAsync()
    {
        var target = new RecordingSubscriptionTarget();
        var endpoint = new RoutingSlipSubscriptionCaptureEndpoint(
            target, DestinationAddress, RoutingSlipEvents.Completed, null);
        var observer = new RecordingSendObserver();
        using ConnectHandle handle = endpoint.ConnectSendObserver(observer);
        var expected = new ExpectedCaptureException();

        Exception failure = await Assert.ThrowsAsync<ExpectedCaptureException>(() => endpoint.SendAsync(
            new SubscriptionMessage("fault"),
            new DelegatePipe<SendContext<SubscriptionMessage>>(_ => Task.FromException(expected)),
            TestContext.Current.CancellationToken));

        Assert.Same(expected, failure);
        Assert.Same(expected, observer.Failure);
        Assert.Equal(["pre", "fault"], observer.Events);
        Assert.Empty(target.Subscriptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-CAPTURE", "runtime-and-initializer-overloads-capture-their-declared-contract")]
    public async Task RuntimeAndInitializerOverloads_CaptureTheirDeclaredMessageContractsAsync()
    {
        var target = new RecordingSubscriptionTarget();
        var endpoint = new RoutingSlipSubscriptionCaptureEndpoint(
            target, DestinationAddress, RoutingSlipEvents.Completed, null);
        var message = new SubscriptionMessage("runtime");

        await endpoint.SendAsync((object)message, TestContext.Current.CancellationToken);
        await endpoint.SendAsync((object)message, typeof(SubscriptionMessage), TestContext.Current.CancellationToken);
        await endpoint.SendAsync((object)message, new DelegatePipe<SendContext>(_ => Task.CompletedTask), TestContext.Current.CancellationToken);
        await endpoint.SendAsync(
            (object)message,
            typeof(SubscriptionMessage),
            new DelegatePipe<SendContext>(_ => Task.CompletedTask),
            TestContext.Current.CancellationToken);
        await endpoint.SendAsync<SubscriptionMessage>(new { Value = "initialized" }, TestContext.Current.CancellationToken);
        await endpoint.SendAsync<SubscriptionMessage>(
            new { Value = "typed-pipe" },
            new DelegatePipe<SendContext<SubscriptionMessage>>(_ => Task.CompletedTask),
            TestContext.Current.CancellationToken);
        await endpoint.SendAsync<SubscriptionMessage>(
            new { Value = "untyped-pipe" },
            new DelegatePipe<SendContext>(_ => Task.CompletedTask),
            TestContext.Current.CancellationToken);

        Assert.Equal(7, target.Subscriptions.Count);
        Assert.All(target.Subscriptions, captured =>
            Assert.Contains(MessageUrn.ForTypeString<SubscriptionMessage>(), captured.Message.MessageTypes!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-CAPTURE", "send-boundaries-reject-null-input-and-pre-cancellation")]
    public async Task SendBoundaries_RejectNullInputsAndHonorPreCancellationAsync()
    {
        var target = new RecordingSubscriptionTarget();
        var endpoint = new RoutingSlipSubscriptionCaptureEndpoint(
            target, DestinationAddress, RoutingSlipEvents.Completed, null);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() => endpoint.ConnectSendObserver(null!)).ParamName);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => endpoint.SendAsync(
            new SubscriptionMessage("canceled"), cancellation.Token));
        Assert.Empty(target.Subscriptions);
    }

    public sealed class SubscriptionMessage
    {
        public SubscriptionMessage()
        {
        }

        public SubscriptionMessage(string value)
        {
            Value = value;
        }

        public string Value { get; set; } = string.Empty;
    }

    private sealed record CapturedSubscription(
        Uri Address,
        RoutingSlipEvents Events,
        RoutingSlipEventContents Contents,
        string? ActivityName,
        MessageEnvelope Message);

    private sealed class RecordingSubscriptionTarget : IRoutingSlipSubscriptionTarget
    {
        public List<CapturedSubscription> Subscriptions { get; } = [];

        public void AddSubscription(
            Uri address,
            RoutingSlipEvents events,
            RoutingSlipEventContents contents,
            string? activityName,
            MessageEnvelope message) =>
            Subscriptions.Add(new CapturedSubscription(address, events, contents, activityName, message));
    }

    private sealed class RecordingSendObserver : ISendObserver
    {
        public List<string> Events { get; } = [];
        public Exception? Failure { get; private set; }

        public Task PreSendAsync<T>(SendContext<T> context) where T : class
        {
            Events.Add("pre");
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context) where T : class
        {
            Events.Add("post");
            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class
        {
            Failure = exception;
            Events.Add("fault");
            return Task.CompletedTask;
        }
    }

    private sealed class DelegatePipe<TContext>(Func<TContext, Task> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context) => callback(context);

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    private sealed class ExpectedCaptureException : Exception;
}
