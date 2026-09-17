using System.Net.Mime;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierRoutingSlipEventPublisherDeepContractTests
{
    private static readonly Guid TrackingNumber = new("7aa9650e-c364-4f66-9489-b84021cbd98c");
    private static readonly Guid ExecutionId = new("b1bd68e3-c170-43cb-b467-90b733425930");
    private static readonly DateTimeOffset Timestamp = new(2026, 9, 17, 10, 30, 0, TimeSpan.Zero);
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(375);

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENT-PUBLISHER", "publisher-and-event-contract-api-is-exact-and-async-symmetric")]
    public void PublisherAndEventContracts_ExposeExactReadOnlyAndAsyncApi()
    {
        Assert.False(typeof(IRoutingSlipEventPublisher).IsPublic);
        MethodInfo[] publisherMethods = typeof(IRoutingSlipEventPublisher).GetMethods();
        Assert.Equal(8, publisherMethods.Length);
        Assert.All(publisherMethods, method =>
        {
            Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);
            Assert.Equal(typeof(Task), method.ReturnType);
            ParameterInfo cancellation = method.GetParameters()[^1];
            Assert.Equal(typeof(CancellationToken), cancellation.ParameterType);
            Assert.Equal("cancellationToken", cancellation.Name);
            Assert.True(cancellation.HasDefaultValue);
        });
        Assert.DoesNotContain(publisherMethods, method =>
            !method.Name.EndsWith("Async", StringComparison.Ordinal) && typeof(Task).IsAssignableFrom(method.ReturnType));

        Assert.False(typeof(RoutingSlipEventPublisher).IsPublic);
        Assert.True(typeof(RoutingSlipEventPublisher).IsSealed);
        Assert.Contains(typeof(IRoutingSlipEventPublisher), typeof(RoutingSlipEventPublisher).GetInterfaces());

        AssertContract<IRoutingSlipActivityCompensated>(
            ("ActivityName", typeof(string)), ("Data", typeof(IReadOnlyDictionary<string, object>)),
            ("Duration", typeof(TimeSpan)), ("ExecutionId", typeof(Guid)), ("Host", typeof(HostInfo)),
            ("Timestamp", typeof(DateTimeOffset)), ("TrackingNumber", typeof(Guid)),
            ("Variables", typeof(IReadOnlyDictionary<string, object>)));
        AssertContract<IRoutingSlipActivityCompensationFailed>(
            ("ActivityName", typeof(string)), ("Data", typeof(IReadOnlyDictionary<string, object>)),
            ("Duration", typeof(TimeSpan)), ("ExceptionInfo", typeof(ExceptionInfo)),
            ("ExecutionId", typeof(Guid)), ("Host", typeof(HostInfo)),
            ("Timestamp", typeof(DateTimeOffset)), ("TrackingNumber", typeof(Guid)),
            ("Variables", typeof(IReadOnlyDictionary<string, object>)));
        AssertContract<IRoutingSlipActivityCompleted>(
            ("ActivityName", typeof(string)), ("Arguments", typeof(IReadOnlyDictionary<string, object>)),
            ("Data", typeof(IReadOnlyDictionary<string, object>)), ("Duration", typeof(TimeSpan)),
            ("ExecutionId", typeof(Guid)), ("Host", typeof(HostInfo)),
            ("Timestamp", typeof(DateTimeOffset)), ("TrackingNumber", typeof(Guid)),
            ("Variables", typeof(IReadOnlyDictionary<string, object>)));
        AssertContract<IRoutingSlipActivityFaulted>(
            ("ActivityName", typeof(string)), ("Arguments", typeof(IReadOnlyDictionary<string, object>)),
            ("Duration", typeof(TimeSpan)), ("ExceptionInfo", typeof(ExceptionInfo)),
            ("ExecutionId", typeof(Guid)), ("Host", typeof(HostInfo)),
            ("Timestamp", typeof(DateTimeOffset)), ("TrackingNumber", typeof(Guid)),
            ("Variables", typeof(IReadOnlyDictionary<string, object>)));
        AssertContract<IRoutingSlipCompensationFailed>(
            ("Duration", typeof(TimeSpan)), ("ExceptionInfo", typeof(ExceptionInfo)),
            ("Host", typeof(HostInfo)), ("Timestamp", typeof(DateTimeOffset)),
            ("TrackingNumber", typeof(Guid)), ("Variables", typeof(IReadOnlyDictionary<string, object>)));
        AssertContract<IRoutingSlipCompleted>(
            ("Duration", typeof(TimeSpan)), ("Timestamp", typeof(DateTimeOffset)),
            ("TrackingNumber", typeof(Guid)), ("Variables", typeof(IReadOnlyDictionary<string, object>)));
        AssertContract<IRoutingSlipFaulted>(
            ("ActivityExceptions", typeof(IReadOnlyList<IActivityException>)), ("Duration", typeof(TimeSpan)),
            ("Timestamp", typeof(DateTimeOffset)), ("TrackingNumber", typeof(Guid)),
            ("Variables", typeof(IReadOnlyDictionary<string, object>)));
        AssertContract<IRoutingSlipRevised>(
            ("ActivityName", typeof(string)), ("DiscardedItinerary", typeof(IReadOnlyList<IActivity>)),
            ("Duration", typeof(TimeSpan)), ("ExecutionId", typeof(Guid)), ("Host", typeof(HostInfo)),
            ("Itinerary", typeof(IReadOnlyList<IActivity>)), ("Timestamp", typeof(DateTimeOffset)),
            ("TrackingNumber", typeof(Guid)), ("Variables", typeof(IReadOnlyDictionary<string, object>)));
        AssertContract<IRoutingSlipTerminated>(
            ("ActivityName", typeof(string)), ("DiscardedItinerary", typeof(IReadOnlyList<IActivity>)),
            ("Duration", typeof(TimeSpan)), ("ExecutionId", typeof(Guid)), ("Host", typeof(HostInfo)),
            ("Timestamp", typeof(DateTimeOffset)), ("TrackingNumber", typeof(Guid)),
            ("Variables", typeof(IReadOnlyDictionary<string, object>)));

        MethodInfo[] accessors = typeof(RoutingSlipEventExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        Assert.Equal(30, accessors.Length);
        Assert.All(accessors, method =>
        {
            Assert.Contains(method.Name, new[]
            {
                nameof(RoutingSlipEventExtensions.GetArgument),
                nameof(RoutingSlipEventExtensions.GetResult),
                nameof(RoutingSlipEventExtensions.GetVariable),
            });
            Assert.True(method.IsGenericMethodDefinition);
            Assert.Equal(3, method.GetParameters().Length);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENT-PUBLISHER", "constructors-and-required-payloads-report-exact-parameters")]
    public void PublisherConstructorsAndMethods_RejectEveryRequiredNullWithExactParameter()
    {
        var transport = new RecordingTransport();
        var routingSlip = new TestRoutingSlip(TrackingNumber, []);
        var values = new Dictionary<string, object>();
        var exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("expected"));

        AssertParameter<ArgumentNullException>("context", () =>
            new RoutingSlipEventPublisher((ICourierContext)null!, routingSlip));
        ICourierContext context = CreateCourierContext(transport, CreateSerializerContext(), out _);
        AssertParameter<ArgumentNullException>("routingSlip", () =>
            new RoutingSlipEventPublisher(context, null!));
        AssertParameter<ArgumentNullException>("sendEndpointProvider", () =>
            new RoutingSlipEventPublisher(null!, transport, routingSlip));
        AssertParameter<ArgumentNullException>("publishEndpoint", () =>
            new RoutingSlipEventPublisher(transport, null!, routingSlip));
        AssertParameter<ArgumentNullException>("routingSlip", () =>
            new RoutingSlipEventPublisher(transport, transport, null!));

        var publisher = new RoutingSlipEventPublisher(transport, transport, routingSlip);
        AssertParameter<ArgumentNullException>("variables", () =>
            publisher.PublishRoutingSlipCompletedAsync(Timestamp, Duration, null!));
        AssertParameter<ArgumentNullException>("variables", () =>
            publisher.PublishRoutingSlipFaultedAsync(Timestamp, Duration, null!, []));
        AssertParameter<ArgumentNullException>("exceptions", () =>
            publisher.PublishRoutingSlipFaultedAsync(Timestamp, Duration, values, null!));
        AssertParameter<ArgumentNullException>("activityName", () => publisher.PublishRoutingSlipActivityCompletedAsync(
            null!, ExecutionId, Timestamp, Duration, values, values, values));
        AssertParameter<ArgumentException>("activityName", () => publisher.PublishRoutingSlipActivityCompensatedAsync(
            " ", ExecutionId, Timestamp, Duration, values, values));
        AssertParameter<ArgumentNullException>("variables", () => publisher.PublishRoutingSlipActivityCompletedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, null!, values, values));
        AssertParameter<ArgumentNullException>("arguments", () => publisher.PublishRoutingSlipActivityCompletedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, values, null!, values));
        AssertParameter<ArgumentNullException>("data", () => publisher.PublishRoutingSlipActivityCompletedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, values, values, null!));
        AssertParameter<ArgumentNullException>("exceptionInfo", () => publisher.PublishRoutingSlipActivityFaultedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, null!, values, values));
        AssertParameter<ArgumentNullException>("variables", () => publisher.PublishRoutingSlipActivityFaultedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, exceptionInfo, null!, values));
        AssertParameter<ArgumentNullException>("arguments", () => publisher.PublishRoutingSlipActivityFaultedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, exceptionInfo, values, null!));
        AssertParameter<ArgumentNullException>("variables", () => publisher.PublishRoutingSlipActivityCompensatedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, null!, values));
        AssertParameter<ArgumentNullException>("data", () => publisher.PublishRoutingSlipActivityCompensatedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, values, null!));
        AssertParameter<ArgumentNullException>("variables", () => publisher.PublishRoutingSlipRevisedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, null!, [], []));
        AssertParameter<ArgumentNullException>("itinerary", () => publisher.PublishRoutingSlipRevisedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, values, null!, []));
        AssertParameter<ArgumentNullException>("previousItinerary", () => publisher.PublishRoutingSlipRevisedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, values, [], null!));
        AssertParameter<ArgumentNullException>("variables", () => publisher.PublishRoutingSlipTerminatedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, null!, []));
        AssertParameter<ArgumentNullException>("previousItinerary", () => publisher.PublishRoutingSlipTerminatedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, values, null!));
        AssertParameter<ArgumentNullException>("exceptionInfo", () => publisher.PublishRoutingSlipActivityCompensationFailedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, Timestamp, Duration, null!, values, values));
        AssertParameter<ArgumentNullException>("variables", () => publisher.PublishRoutingSlipActivityCompensationFailedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, Timestamp, Duration, exceptionInfo, null!, values));
        AssertParameter<ArgumentNullException>("data", () => publisher.PublishRoutingSlipActivityCompensationFailedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, Timestamp, Duration, exceptionInfo, values, null!));

        Assert.Empty(transport.Trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENT-PUBLISHER", "every-lifecycle-method-maps-exact-state-to-topology")]
    public async Task EveryPublisherMethod_MapsExactIdentityTimingHostFailureAndPayloadAsync()
    {
        var transport = new RecordingTransport();
        var publisher = new RoutingSlipEventPublisher(
            transport,
            transport,
            new TestRoutingSlip(TrackingNumber, []));
        var variables = new Dictionary<string, object> { ["tenant"] = "north" };
        var arguments = new Dictionary<string, object> { ["orderId"] = 42 };
        var data = new Dictionary<string, object> { ["receipt"] = "approved" };
        var exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("activity failed"));
        var activityException = new RoutingSlipActivityException(
            "ChargeCard", HostMetadataCache.Host, ExecutionId, Timestamp, Duration, exceptionInfo);
        IActivity activity = new TestActivity("ShipOrder", new Uri("loopback://localhost/ship"), arguments);
        CancellationToken token = TestContext.Current.CancellationToken;

        await publisher.PublishRoutingSlipCompletedAsync(Timestamp, Duration, variables, token);
        await publisher.PublishRoutingSlipFaultedAsync(Timestamp, Duration, variables, [activityException], token);
        await publisher.PublishRoutingSlipActivityCompletedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, variables, arguments, data, token);
        await publisher.PublishRoutingSlipActivityFaultedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, exceptionInfo, variables, arguments, token);
        await publisher.PublishRoutingSlipActivityCompensatedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, variables, data, token);
        await publisher.PublishRoutingSlipRevisedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, variables, [activity], [activity], token);
        await publisher.PublishRoutingSlipTerminatedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, variables, [activity], token);
        await publisher.PublishRoutingSlipActivityCompensationFailedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, Timestamp.AddSeconds(1), Duration.Add(TimeSpan.FromSeconds(1)),
            exceptionInfo, variables, data, token);

        Assert.Empty(transport.ResolvedAddresses);
        Assert.Equal(9, transport.Published.Count);
        Assert.All(transport.Published, entry => Assert.Equal(token, entry.Token));

        IRoutingSlipCompleted completed = Assert.IsAssignableFrom<IRoutingSlipCompleted>(transport.Published[0].Message);
        AssertTerminal(completed.TrackingNumber, completed.Timestamp, completed.Duration, completed.Variables);

        IRoutingSlipFaulted faulted = Assert.IsAssignableFrom<IRoutingSlipFaulted>(transport.Published[1].Message);
        AssertTerminal(faulted.TrackingNumber, faulted.Timestamp, faulted.Duration, faulted.Variables);
        Assert.Equal("ChargeCard", Assert.Single(faulted.ActivityExceptions).Name);

        IRoutingSlipActivityCompleted activityCompleted =
            Assert.IsAssignableFrom<IRoutingSlipActivityCompleted>(transport.Published[2].Message);
        AssertActivity(activityCompleted.Host, activityCompleted.TrackingNumber, activityCompleted.ActivityName,
            activityCompleted.ExecutionId, activityCompleted.Timestamp, activityCompleted.Duration, activityCompleted.Variables);
        Assert.Equal(42, activityCompleted.Arguments["orderId"]);
        Assert.Equal("approved", activityCompleted.Data["receipt"]);

        IRoutingSlipActivityFaulted activityFaulted =
            Assert.IsAssignableFrom<IRoutingSlipActivityFaulted>(transport.Published[3].Message);
        AssertActivity(activityFaulted.Host, activityFaulted.TrackingNumber, activityFaulted.ActivityName,
            activityFaulted.ExecutionId, activityFaulted.Timestamp, activityFaulted.Duration, activityFaulted.Variables);
        Assert.Same(exceptionInfo, activityFaulted.ExceptionInfo);
        Assert.Equal(42, activityFaulted.Arguments["orderId"]);

        IRoutingSlipActivityCompensated activityCompensated =
            Assert.IsAssignableFrom<IRoutingSlipActivityCompensated>(transport.Published[4].Message);
        AssertActivity(activityCompensated.Host, activityCompensated.TrackingNumber, activityCompensated.ActivityName,
            activityCompensated.ExecutionId, activityCompensated.Timestamp, activityCompensated.Duration, activityCompensated.Variables);
        Assert.Equal("approved", activityCompensated.Data["receipt"]);

        IRoutingSlipRevised revised = Assert.IsAssignableFrom<IRoutingSlipRevised>(transport.Published[5].Message);
        AssertActivity(revised.Host, revised.TrackingNumber, revised.ActivityName, revised.ExecutionId,
            revised.Timestamp, revised.Duration, revised.Variables);
        Assert.Equal("ShipOrder", Assert.Single(revised.Itinerary).Name);
        Assert.Equal("ShipOrder", Assert.Single(revised.DiscardedItinerary).Name);

        IRoutingSlipTerminated terminated = Assert.IsAssignableFrom<IRoutingSlipTerminated>(transport.Published[6].Message);
        AssertActivity(terminated.Host, terminated.TrackingNumber, terminated.ActivityName, terminated.ExecutionId,
            terminated.Timestamp, terminated.Duration, terminated.Variables);
        Assert.Equal("ShipOrder", Assert.Single(terminated.DiscardedItinerary).Name);

        IRoutingSlipActivityCompensationFailed activityFailure =
            Assert.IsAssignableFrom<IRoutingSlipActivityCompensationFailed>(transport.Published[7].Message);
        AssertActivity(activityFailure.Host, activityFailure.TrackingNumber, activityFailure.ActivityName,
            activityFailure.ExecutionId, activityFailure.Timestamp, activityFailure.Duration, activityFailure.Variables);
        Assert.Same(exceptionInfo, activityFailure.ExceptionInfo);
        Assert.Equal("approved", activityFailure.Data["receipt"]);

        IRoutingSlipCompensationFailed slipFailure =
            Assert.IsAssignableFrom<IRoutingSlipCompensationFailed>(transport.Published[8].Message);
        Assert.Same(HostMetadataCache.Host, slipFailure.Host);
        Assert.Equal(TrackingNumber, slipFailure.TrackingNumber);
        Assert.Equal(Timestamp.AddSeconds(1), slipFailure.Timestamp);
        Assert.Equal(Duration.Add(TimeSpan.FromSeconds(1)), slipFailure.Duration);
        Assert.Same(exceptionInfo, slipFailure.ExceptionInfo);
        Assert.Equal("north", slipFailure.Variables["tenant"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENT-PUBLISHER", "selection-filter-content-and-custom-envelope-choose-exact-send-path")]
    public async Task SubscriptionSelection_UsesExactFiltersContentsAddressesAndCustomOrDefaultSendPathsAsync()
    {
        var customAddress = new Uri("loopback://localhost/custom-events");
        var defaultAddress = new Uri("loopback://localhost/default-events");
        var excludedAddress = new Uri("loopback://localhost/excluded-events");
        var customEnvelope = new JsonMessageEnvelope
        {
            Message = new CustomSubscriptionMessage("audit"),
            MessageTypes = ["urn:message:custom-subscription"],
        };
        ISubscription[] subscriptions =
        [
            new TestSubscription(customAddress, RoutingSlipEvents.ActivityCompleted,
                RoutingSlipEventContents.Variables | RoutingSlipEventContents.Data, "cHaRgEcArD", customEnvelope),
            new TestSubscription(defaultAddress, RoutingSlipEvents.ActivityCompleted,
                RoutingSlipEventContents.Arguments, null, null),
            new TestSubscription(excludedAddress, RoutingSlipEvents.ActivityCompleted,
                RoutingSlipEventContents.All, "ReserveInventory", null),
            new TestSubscription(excludedAddress, RoutingSlipEvents.ActivityFaulted,
                RoutingSlipEventContents.All, null, null),
        ];
        var endpoint = new RecordingAdvancedSendEndpoint();
        SerializerContext serializerContext = CreateSerializerContext(out RecordingSerializerContextProxy serializerProxy);
        ICourierContext courierContext = CreateCourierContext(endpoint, serializerContext, out CourierContextProxy contextProxy);
        var publisher = new RoutingSlipEventPublisher(courierContext, new TestRoutingSlip(TrackingNumber, subscriptions));
        var variables = new Dictionary<string, object> { ["tenant"] = "north" };
        var arguments = new Dictionary<string, object> { ["orderId"] = 42 };
        var data = new Dictionary<string, object> { ["receipt"] = "approved" };
        CancellationToken token = TestContext.Current.CancellationToken;

        await publisher.PublishRoutingSlipActivityCompletedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, variables, arguments, data, token);

        Assert.Equal([customAddress, defaultAddress], contextProxy.ResolvedAddresses);
        Assert.Equal([token, token], contextProxy.ResolutionTokens);
        Assert.Equal(2, endpoint.Sends.Count);

        SendInvocation customSend = endpoint.Sends[0];
        IRoutingSlipActivityCompleted custom = Assert.IsAssignableFrom<IRoutingSlipActivityCompleted>(customSend.Message);
        Assert.NotNull(customSend.Serializer);
        Assert.Equal("north", custom.Variables["tenant"]);
        Assert.Empty(custom.Arguments);
        Assert.Equal("approved", custom.Data["receipt"]);

        SendInvocation defaultSend = endpoint.Sends[1];
        IRoutingSlipActivityCompleted @default = Assert.IsAssignableFrom<IRoutingSlipActivityCompleted>(defaultSend.Message);
        Assert.Null(defaultSend.Serializer);
        Assert.Empty(@default.Variables);
        Assert.Equal(42, @default.Arguments["orderId"]);
        Assert.Empty(@default.Data);
        Assert.All(endpoint.Sends, send => Assert.Equal(token, send.Token));

        SerializerOverlay overlay = Assert.Single(serializerProxy.Overlays);
        Assert.Same(customEnvelope, overlay.Envelope);
        Assert.Same(customSend.Message, overlay.Message);
        Assert.Same(serializerProxy.Serializer, customSend.Serializer);
        Assert.Equal(0, contextProxy.PublishCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACCESSORS", "argument-merge-is-case-insensitive-and-null-does-not-erase-a-variable")]
    public void ActivityArgumentAccessors_ApplyCaseInsensitiveOverrideAndNullPreservationForBothTypeKinds()
    {
        var variables = new Dictionary<string, object>
        {
            ["shared"] = "variable",
            ["count"] = 27,
        };
        var arguments = new Dictionary<string, object?>
        {
            ["SHARED"] = "argument",
            ["COUNT"] = null,
        }.ToDictionary(pair => pair.Key, pair => pair.Value!);
        var message = new RoutingSlipActivityCompletedMessage(
            HostMetadataCache.Host,
            TrackingNumber,
            "ChargeCard",
            ExecutionId,
            Timestamp,
            Duration,
            variables,
            arguments,
            new Dictionary<string, object>());
        ConsumeContext<IRoutingSlipActivityCompleted> context = CreateConsumeContext<IRoutingSlipActivityCompleted>(message);

        Assert.Equal("argument", context.GetArgument<string>("shared"));
        Assert.Equal(27, context.GetArgument<int>("COUNT"));
        Assert.Equal("fallback", context.GetArgument("missing", "fallback"));
        Assert.Equal(73, context.GetArgument<int>("missing-count", 73));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENT-PUBLISHER", "subscription-send-failure-is-authoritative-and-stops-later-routes")]
    public async Task SubscriptionFailure_PreservesExactExceptionAndStopsLaterSubscriptionsAndTopologyAsync()
    {
        var expected = new ExpectedSendException();
        var transport = new RecordingTransport { FailSendAt = 2, SendFailure = expected };
        ISubscription[] subscriptions =
        [
            new TestSubscription(new Uri("loopback://localhost/first"), RoutingSlipEvents.Completed),
            new TestSubscription(new Uri("loopback://localhost/second"), RoutingSlipEvents.Completed),
            new TestSubscription(new Uri("loopback://localhost/third"), RoutingSlipEvents.Completed),
        ];
        var publisher = new RoutingSlipEventPublisher(
            transport, transport, new TestRoutingSlip(TrackingNumber, subscriptions));

        ExpectedSendException actual = await Assert.ThrowsAsync<ExpectedSendException>(() =>
            publisher.PublishRoutingSlipCompletedAsync(
                Timestamp, Duration, new Dictionary<string, object>(), TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(
            [new Uri("loopback://localhost/first"), new Uri("loopback://localhost/second")],
            transport.ResolvedAddresses);
        Assert.Equal(["resolve", "send", "resolve", "send"], transport.Trace);
        Assert.Equal(2, transport.Sent.Count);
        Assert.Empty(transport.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENT-PUBLISHER", "supplemental-send-precedes-and-does-not-mask-topology-failure")]
    public async Task SupplementalSubscription_CompletesSendBeforePreservingTopologyFailureAsync()
    {
        var expected = new ExpectedPublishException();
        var transport = new RecordingTransport { PublishFailure = _ => expected };
        ISubscription[] subscriptions =
        [
            new TestSubscription(
                new Uri("loopback://localhost/supplemental"),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Supplemental),
        ];
        var publisher = new RoutingSlipEventPublisher(
            transport, transport, new TestRoutingSlip(TrackingNumber, subscriptions));

        ExpectedPublishException actual = await Assert.ThrowsAsync<ExpectedPublishException>(() =>
            publisher.PublishRoutingSlipCompletedAsync(
                Timestamp, Duration, new Dictionary<string, object>(), TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(["resolve", "send", "publish"], transport.Trace);
        Assert.Single(transport.Sent);
        Assert.Single(transport.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "publisher-rechecks-cancellation-between-subscription-and-topology-boundaries")]
    public async Task Cancellation_IsRecheckedBeforeEachLaterSubscriptionAndTopologyDispatchAsync()
    {
        var preCanceledTransport = new RecordingTransport();
        var preCanceledPublisher = new RoutingSlipEventPublisher(
            preCanceledTransport, preCanceledTransport, new TestRoutingSlip(TrackingNumber, []));
        using var preCanceled = new CancellationTokenSource();
        await preCanceled.CancelAsync();

        OperationCanceledException preCanceledFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            preCanceledPublisher.PublishRoutingSlipCompletedAsync(
                Timestamp, Duration, new Dictionary<string, object>(), preCanceled.Token));
        Assert.Equal(preCanceled.Token, preCanceledFailure.CancellationToken);
        Assert.Empty(preCanceledTransport.Trace);

        using var duringResolution = new CancellationTokenSource();
        var resolutionTransport = new RecordingTransport { AfterResolve = (_, _) => duringResolution.Cancel() };
        ISubscription[] resolutionSubscriptions =
        [
            new TestSubscription(
                new Uri("loopback://localhost/resolution"),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Supplemental),
        ];
        var resolutionPublisher = new RoutingSlipEventPublisher(
            resolutionTransport, resolutionTransport, new TestRoutingSlip(TrackingNumber, resolutionSubscriptions));

        OperationCanceledException resolutionFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            resolutionPublisher.PublishRoutingSlipCompletedAsync(
                Timestamp, Duration, new Dictionary<string, object>(), duringResolution.Token));
        Assert.Equal(duringResolution.Token, resolutionFailure.CancellationToken);
        Assert.Equal(["resolve"], resolutionTransport.Trace);
        Assert.Empty(resolutionTransport.Sent);
        Assert.Empty(resolutionTransport.Published);

        using var midFlight = new CancellationTokenSource();
        var transport = new RecordingTransport { AfterSend = (_, _) => midFlight.Cancel() };
        ISubscription[] subscriptions =
        [
            new TestSubscription(
                new Uri("loopback://localhost/first"),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Supplemental),
            new TestSubscription(
                new Uri("loopback://localhost/second"),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Supplemental),
        ];
        var publisher = new RoutingSlipEventPublisher(
            transport, transport, new TestRoutingSlip(TrackingNumber, subscriptions));

        OperationCanceledException midFlightFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            publisher.PublishRoutingSlipCompletedAsync(
                Timestamp, Duration, new Dictionary<string, object>(), midFlight.Token));

        Assert.Equal(midFlight.Token, midFlightFailure.CancellationToken);
        Assert.Equal([new Uri("loopback://localhost/first")], transport.ResolvedAddresses);
        Assert.Equal(["resolve", "send"], transport.Trace);
        Assert.Single(transport.Sent);
        Assert.Empty(transport.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENT-PUBLISHER", "dual-compensation-failures-attempt-both-routes-and-preserve-primary-precedence")]
    public async Task CompensationFailure_WhenBothRoutesFail_AttemptsBothAndPreservesActivityFailureFirstAsync()
    {
        var activityFailure = new ExpectedActivityFailureException();
        var slipFailure = new ExpectedSlipFailureException();
        var transport = new RecordingTransport
        {
            PublishFailure = message => message switch
            {
                IRoutingSlipActivityCompensationFailed => activityFailure,
                IRoutingSlipCompensationFailed => slipFailure,
                _ => null,
            },
        };
        var publisher = new RoutingSlipEventPublisher(
            transport, transport, new TestRoutingSlip(TrackingNumber, []));
        var exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("compensation failed"));

        Task publication = publisher.PublishRoutingSlipActivityCompensationFailedAsync(
            "ChargeCard", ExecutionId, Timestamp, Duration, Timestamp.AddSeconds(1), Duration,
            exceptionInfo, new Dictionary<string, object>(), new Dictionary<string, object>(),
            TestContext.Current.CancellationToken);
        ExpectedActivityFailureException actual = await Assert.ThrowsAsync<ExpectedActivityFailureException>(
            async () => await publication);

        Assert.Same(activityFailure, actual);
        Assert.Collection(
            publication.Exception!.InnerExceptions,
            exception => Assert.Same(activityFailure, exception),
            exception => Assert.Same(slipFailure, exception));
        Assert.Collection(
            transport.Published,
            entry => Assert.IsAssignableFrom<IRoutingSlipActivityCompensationFailed>(entry.Message),
            entry => Assert.IsAssignableFrom<IRoutingSlipCompensationFailed>(entry.Message));
        Assert.Equal(["publish", "publish"], transport.Trace);
    }

    private static void AssertContract<TContract>(params (string Name, Type Type)[] expected)
    {
        Type contract = typeof(TContract);
        Assert.True(contract.IsPublic);
        Assert.True(contract.IsInterface);
        PropertyInfo[] properties = contract.GetProperties().OrderBy(property => property.Name).ToArray();
        Assert.Equal(expected.OrderBy(item => item.Name).Select(item => item.Name), properties.Select(property => property.Name));
        foreach ((string name, Type type) in expected)
        {
            PropertyInfo property = Assert.Single(properties, candidate => candidate.Name == name);
            Assert.Equal(type, property.PropertyType);
            Assert.True(property.GetMethod is { IsPublic: true });
            Assert.Null(property.SetMethod);
        }
    }

    private static void AssertParameter<TException>(string parameterName, Action action)
        where TException : ArgumentException
    {
        TException exception = Assert.Throws<TException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private static void AssertTerminal(
        Guid trackingNumber,
        DateTimeOffset timestamp,
        TimeSpan duration,
        IReadOnlyDictionary<string, object> variables)
    {
        Assert.Equal(TrackingNumber, trackingNumber);
        Assert.Equal(Timestamp, timestamp);
        Assert.Equal(Duration, duration);
        Assert.Equal("north", variables["tenant"]);
    }

    private static void AssertActivity(
        HostInfo host,
        Guid trackingNumber,
        string activityName,
        Guid executionId,
        DateTimeOffset timestamp,
        TimeSpan duration,
        IReadOnlyDictionary<string, object> variables)
    {
        Assert.Same(HostMetadataCache.Host, host);
        Assert.Equal(TrackingNumber, trackingNumber);
        Assert.Equal("ChargeCard", activityName);
        Assert.Equal(ExecutionId, executionId);
        Assert.Equal(Timestamp, timestamp);
        Assert.Equal(Duration, duration);
        Assert.Equal("north", variables["tenant"]);
    }

    private static ICourierContext CreateCourierContext(
        ISendEndpoint endpoint,
        SerializerContext serializerContext,
        out CourierContextProxy proxy)
    {
        ICourierContext context = DispatchProxy.Create<ICourierContext, CourierContextProxy>();
        proxy = (CourierContextProxy)(object)context;
        proxy.Endpoint = endpoint;
        proxy.SerializerContext = serializerContext;
        return context;
    }

    private static SerializerContext CreateSerializerContext() => CreateSerializerContext(out _);

    private static SerializerContext CreateSerializerContext(out RecordingSerializerContextProxy proxy)
    {
        SerializerContext context = DispatchProxy.Create<SerializerContext, RecordingSerializerContextProxy>();
        proxy = (RecordingSerializerContextProxy)(object)context;
        return context;
    }

    private static ConsumeContext<T> CreateConsumeContext<T>(T message)
        where T : class
    {
        TestConsumeContext<T> context = DispatchProxy.Create<TestConsumeContext<T>, ConsumeContextProxy<T>>();
        var proxy = (ConsumeContextProxy<T>)(object)context;
        proxy.Message = message;
        proxy.SerializerContext = CreateSerializerContext();
        return context;
    }

    private interface TestConsumeContext<out T> : ConsumeContext<T>, ConsumeContext
        where T : class;

    private sealed class TestRoutingSlip(Guid trackingNumber, IReadOnlyList<ISubscription> subscriptions) : IRoutingSlip
    {
        public Guid TrackingNumber { get; } = trackingNumber;
        public DateTimeOffset CreateTimestamp { get; } = Timestamp.Subtract(TimeSpan.FromMinutes(1));
        public IReadOnlyList<IActivity> Itinerary { get; } = [];
        public IReadOnlyList<IActivityLog> ActivityLogs { get; } = [];
        public IReadOnlyList<ICompensateLog> CompensateLogs { get; } = [];
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
        public IReadOnlyList<IActivityException> ActivityExceptions { get; } = [];
        public IReadOnlyList<ISubscription> Subscriptions { get; } = subscriptions;
    }

    private sealed class TestSubscription(
        Uri address,
        RoutingSlipEvents events,
        RoutingSlipEventContents include = RoutingSlipEventContents.All,
        string? activityName = null,
        MessageEnvelope? message = null) : ISubscription
    {
        public Uri Address { get; } = address;
        public RoutingSlipEvents Events { get; } = events;
        public RoutingSlipEventContents Include { get; } = include;
        public string? ActivityName { get; } = activityName;
        public MessageEnvelope? Message { get; } = message;
    }

    private sealed class TestActivity(string name, Uri address, IReadOnlyDictionary<string, object> arguments) : IActivity
    {
        public string Name { get; } = name;
        public Uri Address { get; } = address;
        public IReadOnlyDictionary<string, object> Arguments { get; } = arguments;
    }

    private sealed record CustomSubscriptionMessage(string Value);

    private sealed record TransportInvocation(object Message, CancellationToken Token);

    private sealed class RecordingTransport : ISendEndpointProvider, ISendEndpoint, IPublishEndpoint
    {
        public List<string> Trace { get; } = [];
        public List<Uri> ResolvedAddresses { get; } = [];
        public List<TransportInvocation> Sent { get; } = [];
        public List<TransportInvocation> Published { get; } = [];
        public int? FailSendAt { get; init; }
        public Exception? SendFailure { get; init; }
        public Func<object, Exception?>? PublishFailure { get; init; }
        public Action<Uri, CancellationToken>? AfterResolve { get; init; }
        public Action<object, CancellationToken>? AfterSend { get; init; }

        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            ResolvedAddresses.Add(address);
            Trace.Add("resolve");
            AfterResolve?.Invoke(address, cancellationToken);
            return Task.FromResult<ISendEndpoint>(this);
        }

        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            Trace.Add("send");
            Sent.Add(new TransportInvocation(message, cancellationToken));
            AfterSend?.Invoke(message, cancellationToken);
            Exception? failure = SendFailure;
            return FailSendAt == Sent.Count && failure is not null
                ? Task.FromException(failure)
                : Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class => SendAsync(message, cancellationToken);

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            Trace.Add("publish");
            Published.Add(new TransportInvocation(message, cancellationToken));
            Exception? failure = PublishFailure?.Invoke(message);
            return failure is null ? Task.CompletedTask : Task.FromException(failure);
        }

        public Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
            where T : class => PublishAsync(message, cancellationToken);

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();
    }

    private sealed record SendInvocation(object Message, IMessageSerializer? Serializer, CancellationToken Token);

    private sealed class RecordingAdvancedSendEndpoint : IAdvancedSendEndpoint
    {
        public List<SendInvocation> Sends { get; } = [];

        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            Sends.Add(new SendInvocation(message, null, cancellationToken));
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class => SendAsync(message, cancellationToken);

        public async Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            SendContext<T> context = DispatchProxy.Create<SendContext<T>, SendContextProxy<T>>();
            var proxy = (SendContextProxy<T>)(object)context;
            proxy.Message = message;
            await pipe.SendAsync(context);
            Sends.Add(new SendInvocation(message, proxy.Serializer, cancellationToken));
        }

        public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
            where T : class => throw new NotSupportedException();

        public Task SendAsync(object message, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SendAsync<T>(object values, CancellationToken cancellationToken = default)
            where T : class => throw new NotSupportedException();

        public Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class => throw new NotSupportedException();

        public Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
            where T : class => throw new NotSupportedException();

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();
    }

    private class CourierContextProxy : DispatchProxy
    {
        public ISendEndpoint Endpoint { get; set; } = null!;
        public SerializerContext SerializerContext { get; set; } = null!;
        public List<Uri> ResolvedAddresses { get; } = [];
        public List<CancellationToken> ResolutionTokens { get; } = [];
        public int PublishCallCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case "get_Host":
                    return HostMetadataCache.Host;
                case "get_SerializerContext":
                    return SerializerContext;
                case nameof(ISendEndpointProvider.GetSendEndpointAsync):
                    object?[] arguments = args ?? throw new InvalidOperationException("Endpoint arguments are required.");
                    ResolvedAddresses.Add(Assert.IsType<Uri>(arguments[0]));
                    ResolutionTokens.Add(Assert.IsType<CancellationToken>(arguments[1]));
                    return Task.FromResult(Endpoint);
                case nameof(IPublishEndpoint.PublishAsync):
                    PublishCallCount++;
                    throw new Xunit.Sdk.XunitException("A non-supplemental subscription must suppress topology publication.");
                default:
                    throw new NotSupportedException($"Unexpected Courier-context member: {targetMethod?.Name}");
            }
        }
    }

    private sealed record SerializerOverlay(MessageEnvelope Envelope, object Message);

    private class RecordingSerializerContextProxy : DispatchProxy
    {
        public IMessageSerializer Serializer { get; } = new RecordingMessageSerializer();
        public List<SerializerOverlay> Overlays { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(SerializerContext.GetMessageSerializer)
                && args is [MessageEnvelope envelope, object message])
            {
                Overlays.Add(new SerializerOverlay(envelope, message));
                return Serializer;
            }

            if (targetMethod?.Name == nameof(IObjectDeserializer.DeserializeObject))
            {
                object? value = args?[0];
                object? defaultValue = args?[1];
                if (value is null)
                    return defaultValue;

                Type targetType = targetMethod.GetGenericArguments()[0];
                return targetType.IsInstanceOfType(value)
                    ? value
                    : Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.InvariantCulture);
            }

            throw new NotSupportedException($"Unexpected serializer-context member: {targetMethod?.Name}");
        }
    }

    private class ConsumeContextProxy<T> : DispatchProxy
        where T : class
    {
        public T Message { get; set; } = null!;
        public SerializerContext SerializerContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Message" => Message,
            "get_SerializerContext" => SerializerContext,
            _ => throw new NotSupportedException($"Unexpected consume-context member: {targetMethod?.Name}"),
        };
    }

    private class SendContextProxy<T> : DispatchProxy
        where T : class
    {
        public T Message { get; set; } = null!;
        public IMessageSerializer? Serializer { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Message" => Message,
            "get_Serializer" => Serializer,
            "set_Serializer" => SetSerializer(args),
            _ => throw new NotSupportedException($"Unexpected send-context member: {targetMethod?.Name}"),
        };

        private object? SetSerializer(object?[]? args)
        {
            Serializer = Assert.IsAssignableFrom<IMessageSerializer>(args![0]);
            return null;
        }
    }

    private sealed class RecordingMessageSerializer : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/json");

        public MessageBody GetMessageBody<T>(SendContext<T> context)
            where T : class => throw new NotSupportedException();
    }

    private sealed class ExpectedSendException : Exception;

    private sealed class ExpectedPublishException : Exception;

    private sealed class ExpectedActivityFailureException : Exception;

    private sealed class ExpectedSlipFailureException : Exception;
}
