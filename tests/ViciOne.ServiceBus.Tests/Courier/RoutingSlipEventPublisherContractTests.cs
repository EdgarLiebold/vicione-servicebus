using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipEventPublisherContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-FLAGS", "canonical-none-and-all-masks")]
    public void SubscriptionFlags_UseCanonicalDotNetNoneAndAllMasks()
    {
        RoutingSlipEvents allEvents = RoutingSlipEvents.Completed
            | RoutingSlipEvents.Faulted
            | RoutingSlipEvents.CompensationFailed
            | RoutingSlipEvents.Terminated
            | RoutingSlipEvents.Revised
            | RoutingSlipEvents.ActivityCompleted
            | RoutingSlipEvents.ActivityFaulted
            | RoutingSlipEvents.ActivityCompensated
            | RoutingSlipEvents.ActivityCompensationFailed;
        RoutingSlipEventContents allContents = RoutingSlipEventContents.Variables
            | RoutingSlipEventContents.Arguments
            | RoutingSlipEventContents.Data
            | RoutingSlipEventContents.Itinerary;

        Assert.Equal(0, (int)RoutingSlipEvents.None);
        Assert.Equal(RoutingSlipEvents.All, allEvents);
        Assert.False(RoutingSlipEvents.All.HasFlag(RoutingSlipEvents.Supplemental));
        Assert.Equal(0, (int)RoutingSlipEventContents.None);
        Assert.Equal(RoutingSlipEventContents.All, allContents);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ROUTING", "empty-next-addresses-return-null")]
    public void EmptyRoutingSlip_HasNoExecutionOrCompensationAddress()
    {
        RoutingSlip routingSlip = new RoutingSlipBuilder(NewId.NextGuid()).Build();

        Assert.True(routingSlip.RanToCompletion());
        Assert.Null(routingSlip.GetNextExecuteAddress());
        Assert.Null(routingSlip.GetNextCompensateAddress());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "compensation-data-content-flag")]
    public async Task CompensationSubscription_WithDataOnlyIncludesDataAndExcludesVariablesAsync()
    {
        var endpoint = new RecordingEndpointProvider();
        var publishEndpoint = new UnexpectedPublishEndpoint();
        var address = new Uri("loopback://localhost/courier-events");
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            address,
            RoutingSlipEvents.ActivityCompensated,
            RoutingSlipEventContents.Data);
        RoutingSlip routingSlip = builder.Build();
        var publisher = new RoutingSlipEventPublisher(endpoint, publishEndpoint, routingSlip);
        var variables = new Dictionary<string, object> { ["private"] = "variable" };
        var data = new Dictionary<string, object> { ["receipt"] = "compensated" };

        await publisher.PublishRoutingSlipActivityCompensatedAsync(
            "ChargeCard",
            NewId.NextGuid(),
            DateTimeOffset.UtcNow,
            TimeSpan.FromMilliseconds(27),
            variables,
            data,
            TestContext.Current.CancellationToken);

        RoutingSlipActivityCompensated message = Assert.IsAssignableFrom<RoutingSlipActivityCompensated>(
            Assert.Single(endpoint.Messages));
        Assert.Empty(message.Variables);
        Assert.Equal("compensated", Assert.IsType<string>(message.Data["receipt"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "excluded-content-isolation")]
    public async Task ExcludedContent_UsesAnIsolatedEmptyDictionaryForEveryEventAsync()
    {
        var endpoint = new RecordingEndpointProvider();
        var address = new Uri("loopback://localhost/courier-events");
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            address,
            RoutingSlipEvents.ActivityCompensated,
            RoutingSlipEventContents.None);
        var publisher = new RoutingSlipEventPublisher(endpoint, new UnexpectedPublishEndpoint(), builder.Build());

        for (var index = 0; index < 2; index++)
        {
            await publisher.PublishRoutingSlipActivityCompensatedAsync(
                "ChargeCard",
                NewId.NextGuid(),
                DateTimeOffset.UtcNow,
                TimeSpan.Zero,
                new Dictionary<string, object> { ["variable"] = index },
                new Dictionary<string, object> { ["data"] = index },
                TestContext.Current.CancellationToken);
        }

        RoutingSlipActivityCompensated[] messages = endpoint.Messages
            .Cast<RoutingSlipActivityCompensated>()
            .ToArray();
        Assert.Equal(2, messages.Length);
        Assert.NotSame(messages[0].Variables, messages[1].Variables);
        Assert.NotSame(messages[0].Data, messages[1].Data);

        messages[0].Variables["poison"] = true;
        messages[0].Data["poison"] = true;

        Assert.Empty(messages[1].Variables);
        Assert.Empty(messages[1].Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION-FLAGS", "all-events-all-content-and-supplemental-delivery")]
    public async Task SupplementalAllSubscription_DeliversAndPublishesEveryLifecycleEventWithAllContentAsync()
    {
        var endpoint = new RecordingEndpointProvider();
        var publishEndpoint = new RecordingPublishEndpoint();
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            new Uri("loopback://localhost/courier-events"),
            RoutingSlipEvents.All | RoutingSlipEvents.Supplemental,
            RoutingSlipEventContents.All);
        var publisher = new RoutingSlipEventPublisher(endpoint, publishEndpoint, builder.Build());
        var variables = new Dictionary<string, object> { ["tenant"] = "north" };
        var arguments = new Dictionary<string, object> { ["orderId"] = 42 };
        var data = new Dictionary<string, object> { ["receipt"] = 27 };
        var itineraryBuilder = new RoutingSlipBuilder(NewId.NextGuid());
        itineraryBuilder.AddActivity("Next", new Uri("loopback://localhost/next"));
        Activity itineraryActivity = Assert.Single(itineraryBuilder.Build().Itinerary);
        var itinerary = new List<Activity> { itineraryActivity };
        var discardedItinerary = new List<Activity> { itineraryActivity };
        var exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("activity failed"));
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        Guid executionId = NewId.NextGuid();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await publisher.PublishRoutingSlipCompletedAsync(timestamp, TimeSpan.Zero, variables, cancellationToken);
        await publisher.PublishRoutingSlipFaultedAsync(timestamp, TimeSpan.Zero, variables, [], cancellationToken);
        await publisher.PublishRoutingSlipActivityCompletedAsync(
            "ChargeCard", executionId, timestamp, TimeSpan.Zero, variables, arguments, data, cancellationToken);
        await publisher.PublishRoutingSlipActivityFaultedAsync(
            "ChargeCard", executionId, timestamp, TimeSpan.Zero, exceptionInfo, variables, arguments, cancellationToken);
        await publisher.PublishRoutingSlipActivityCompensatedAsync(
            "ChargeCard", executionId, timestamp, TimeSpan.Zero, variables, data, cancellationToken);
        await publisher.PublishRoutingSlipRevisedAsync(
            "ChargeCard", executionId, timestamp, TimeSpan.Zero, variables, itinerary, discardedItinerary, cancellationToken);
        await publisher.PublishRoutingSlipTerminatedAsync(
            "ChargeCard", executionId, timestamp, TimeSpan.Zero, variables, discardedItinerary, cancellationToken);
        await publisher.PublishRoutingSlipActivityCompensationFailedAsync(
            "ChargeCard", executionId, timestamp, TimeSpan.Zero, timestamp, TimeSpan.Zero, exceptionInfo, variables, data, cancellationToken);

        AssertEveryLifecycleEventWithAllContent(endpoint.Messages);
        AssertEveryLifecycleEventWithAllContent(publishEndpoint.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "arguments-content-flag-is-independent")]
    public async Task ActivityCompletionSubscription_WithArgumentsOnlyExcludesVariablesAndDataAsync()
    {
        var endpoint = new RecordingEndpointProvider();
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            new Uri("loopback://localhost/courier-events"),
            RoutingSlipEvents.ActivityCompleted,
            RoutingSlipEventContents.Arguments);
        var publisher = new RoutingSlipEventPublisher(endpoint, new UnexpectedPublishEndpoint(), builder.Build());

        await publisher.PublishRoutingSlipActivityCompletedAsync(
            "ChargeCard",
            NewId.NextGuid(),
            DateTimeOffset.UtcNow,
            TimeSpan.Zero,
            new Dictionary<string, object> { ["tenant"] = "north" },
            new Dictionary<string, object> { ["orderId"] = 42 },
            new Dictionary<string, object> { ["receipt"] = 27 },
            TestContext.Current.CancellationToken);

        RoutingSlipActivityCompleted message = Assert.IsAssignableFrom<RoutingSlipActivityCompleted>(
            Assert.Single(endpoint.Messages));
        Assert.Empty(message.Variables);
        Assert.Equal(42, message.Arguments["orderId"]);
        Assert.Empty(message.Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "itinerary-content-flag-is-independent")]
    public async Task RevisionSubscription_WithItineraryOnlyExcludesVariablesAsync()
    {
        var endpoint = new RecordingEndpointProvider();
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            new Uri("loopback://localhost/courier-events"),
            RoutingSlipEvents.Revised,
            RoutingSlipEventContents.Itinerary);
        var publisher = new RoutingSlipEventPublisher(endpoint, new UnexpectedPublishEndpoint(), builder.Build());
        var itineraryBuilder = new RoutingSlipBuilder(NewId.NextGuid());
        itineraryBuilder.AddActivity("Next", new Uri("loopback://localhost/next"));
        Activity activity = Assert.Single(itineraryBuilder.Build().Itinerary);

        await publisher.PublishRoutingSlipRevisedAsync(
            "ChargeCard",
            NewId.NextGuid(),
            DateTimeOffset.UtcNow,
            TimeSpan.Zero,
            new Dictionary<string, object> { ["tenant"] = "north" },
            [activity],
            [activity],
            TestContext.Current.CancellationToken);

        RoutingSlipRevised message = Assert.IsAssignableFrom<RoutingSlipRevised>(Assert.Single(endpoint.Messages));
        Assert.Empty(message.Variables);
        Assert.Single(message.Itinerary);
        Assert.Single(message.DiscardedItinerary);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-EVENT-BOUNDARY", "invalid-identities-and-durations-fail-before-dispatch")]
    public void EventPublisher_RejectsInvalidIdentitiesAndDurationsBeforeDispatch()
    {
        var endpoint = new RecordingEndpointProvider();
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(new Uri("loopback://localhost/courier-events"), RoutingSlipEvents.All);
        var publisher = new RoutingSlipEventPublisher(endpoint, new UnexpectedPublishEndpoint(), builder.Build());
        var values = new Dictionary<string, object>();
        var exceptionInfo = new FaultExceptionInfo(new InvalidOperationException("activity failed"));
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        TimeSpan negative = TimeSpan.FromTicks(-1);

        AssertOutOfRange("duration", () =>
            publisher.PublishRoutingSlipCompletedAsync(timestamp, negative, values, TestContext.Current.CancellationToken));
        AssertOutOfRange("duration", () =>
            publisher.PublishRoutingSlipFaultedAsync(timestamp, negative, values, [], TestContext.Current.CancellationToken));
        AssertEmptyExecutionId(() => publisher.PublishRoutingSlipActivityCompletedAsync(
            "Activity", Guid.Empty, timestamp, TimeSpan.Zero, values, values, values));
        AssertEmptyExecutionId(() => publisher.PublishRoutingSlipActivityFaultedAsync(
            "Activity", Guid.Empty, timestamp, TimeSpan.Zero, exceptionInfo, values, values));
        AssertEmptyExecutionId(() => publisher.PublishRoutingSlipActivityCompensatedAsync(
            "Activity", Guid.Empty, timestamp, TimeSpan.Zero, values, values));
        AssertEmptyExecutionId(() => publisher.PublishRoutingSlipRevisedAsync(
            "Activity", Guid.Empty, timestamp, TimeSpan.Zero, values, [], []));
        AssertEmptyExecutionId(() => publisher.PublishRoutingSlipTerminatedAsync(
            "Activity", Guid.Empty, timestamp, TimeSpan.Zero, values, []));
        AssertEmptyExecutionId(() => publisher.PublishRoutingSlipActivityCompensationFailedAsync(
            "Activity", Guid.Empty, timestamp, TimeSpan.Zero, timestamp, TimeSpan.Zero, exceptionInfo, values, values));
        AssertOutOfRange("routingSlipDuration", () => publisher.PublishRoutingSlipActivityCompensationFailedAsync(
            "Activity", NewId.NextGuid(), timestamp, TimeSpan.Zero, timestamp, negative, exceptionInfo, values, values));

        Assert.Empty(endpoint.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "unselected-lifecycle-events-are-not-delivered")]
    public async Task Subscription_ReceivesOnlySelectedLifecycleEventsAsync()
    {
        var endpoint = new RecordingEndpointProvider();
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            new Uri("loopback://localhost/courier-events"),
            RoutingSlipEvents.Completed,
            RoutingSlipEventContents.All);
        var publisher = new RoutingSlipEventPublisher(endpoint, new UnexpectedPublishEndpoint(), builder.Build());
        var variables = new Dictionary<string, object>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await publisher.PublishRoutingSlipFaultedAsync(
            DateTimeOffset.UtcNow, TimeSpan.Zero, variables, [], cancellationToken);
        Assert.Empty(endpoint.Messages);

        await publisher.PublishRoutingSlipCompletedAsync(
            DateTimeOffset.UtcNow, TimeSpan.Zero, variables, cancellationToken);
        Assert.IsAssignableFrom<RoutingSlipCompleted>(Assert.Single(endpoint.Messages));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "activity-filter-is-case-insensitive-and-exclusive")]
    public async Task ActivitySubscription_ReceivesOnlyItsNamedActivityIgnoringCaseAsync()
    {
        var endpoint = new RecordingEndpointProvider();
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            new Uri("loopback://localhost/courier-events"),
            RoutingSlipEvents.ActivityCompleted,
            RoutingSlipEventContents.All,
            "ChargeCard");
        var publisher = new RoutingSlipEventPublisher(endpoint, new UnexpectedPublishEndpoint(), builder.Build());
        var values = new Dictionary<string, object>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await publisher.PublishRoutingSlipActivityCompletedAsync(
            "ReserveInventory", NewId.NextGuid(), DateTimeOffset.UtcNow, TimeSpan.Zero, values, values, values, cancellationToken);
        Assert.Empty(endpoint.Messages);

        await publisher.PublishRoutingSlipActivityCompletedAsync(
            "chargecard", NewId.NextGuid(), DateTimeOffset.UtcNow, TimeSpan.Zero, values, values, values, cancellationToken);
        Assert.IsAssignableFrom<RoutingSlipActivityCompleted>(Assert.Single(endpoint.Messages));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "non-supplemental-selection-suppresses-topology-publication")]
    public async Task NonSupplementalSubscription_SuppressesTopologyPublicationWhenMixedWithSupplementalSubscriptionAsync()
    {
        var endpoint = new RecordingEndpointProvider();
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        var address = new Uri("loopback://localhost/courier-events");
        builder.AddSubscription(address, RoutingSlipEvents.Completed | RoutingSlipEvents.Supplemental);
        builder.AddSubscription(address, RoutingSlipEvents.Completed);
        var publisher = new RoutingSlipEventPublisher(endpoint, new UnexpectedPublishEndpoint(), builder.Build());

        await publisher.PublishRoutingSlipCompletedAsync(
            DateTimeOffset.UtcNow,
            TimeSpan.Zero,
            new Dictionary<string, object>(),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, endpoint.Messages.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "absence-of-subscriptions-preserves-topology-publication")]
    public async Task NoSubscriptions_PublishesLifecycleEventThroughTopologyAsync()
    {
        var endpoint = new RecordingEndpointProvider();
        var publishEndpoint = new RecordingPublishEndpoint();
        var publisher = new RoutingSlipEventPublisher(
            endpoint,
            publishEndpoint,
            new RoutingSlipBuilder(NewId.NextGuid()).Build());

        await publisher.PublishRoutingSlipCompletedAsync(
            DateTimeOffset.UtcNow,
            TimeSpan.Zero,
            new Dictionary<string, object>(),
            TestContext.Current.CancellationToken);

        Assert.Empty(endpoint.Messages);
        Assert.IsAssignableFrom<RoutingSlipCompleted>(Assert.Single(publishEndpoint.Messages));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "event-publication-observes-live-token")]
    public async Task EventPublication_ObservesCancellationRequestedDuringEndpointResolutionAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        var endpointProvider = new BlockingEndpointProvider();
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            new Uri("loopback://localhost/courier-events"),
            RoutingSlipEvents.Completed,
            RoutingSlipEventContents.All);
        var publisher = new RoutingSlipEventPublisher(
            endpointProvider,
            new UnexpectedPublishEndpoint(),
            builder.Build());
        using var cancellation = new CancellationTokenSource();

        Task publication = publisher.PublishRoutingSlipCompletedAsync(
            DateTimeOffset.UtcNow,
            TimeSpan.Zero,
            new Dictionary<string, object>(),
            cancellation.Token);
        await endpointProvider.Entered.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => publication);
        Assert.Equal(cancellation.Token, endpointProvider.ObservedToken);
    }

    private sealed class RecordingEndpointProvider : ISendEndpointProvider, ISendEndpoint
    {
        public List<object> Messages { get; } = [];

        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ISendEndpoint>(this);
        }

        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(message);
            cancellationToken.ThrowIfCancellationRequested();
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(options);
            return SendAsync(message, cancellationToken);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();
    }

    private sealed class BlockingEndpointProvider : ISendEndpointProvider
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken ObservedToken { get; private set; }

        public async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);
            ObservedToken = cancellationToken;
            Entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Cancellation should end endpoint resolution.");
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();
    }

    private sealed class RecordingPublishEndpoint : IPublishEndpoint
    {
        public List<object> Messages { get; } = [];

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(message);
            cancellationToken.ThrowIfCancellationRequested();
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(options);
            return PublishAsync(message, cancellationToken);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();
    }

    private sealed class UnexpectedPublishEndpoint : IPublishEndpoint
    {
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class =>
            throw new Xunit.Sdk.XunitException("A non-supplemental subscription must suppress topology publication.");

        public Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
            where T : class =>
            throw new Xunit.Sdk.XunitException("A non-supplemental subscription must suppress topology publication.");

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();
    }

    private static void AssertEveryLifecycleEventWithAllContent(IReadOnlyList<object> messages)
    {
        Assert.Collection(
            messages,
            completed => Assert.Equal(
                "north",
                Assert.IsAssignableFrom<RoutingSlipCompleted>(completed).Variables["tenant"]),
            faulted => Assert.Equal(
                "north",
                Assert.IsAssignableFrom<RoutingSlipFaulted>(faulted).Variables["tenant"]),
            completedActivity =>
            {
                RoutingSlipActivityCompleted activity = Assert.IsAssignableFrom<RoutingSlipActivityCompleted>(completedActivity);
                Assert.Equal("north", activity.Variables["tenant"]);
                Assert.Equal(42, activity.Arguments["orderId"]);
                Assert.Equal(27, activity.Data["receipt"]);
            },
            faultedActivity =>
            {
                RoutingSlipActivityFaulted activity = Assert.IsAssignableFrom<RoutingSlipActivityFaulted>(faultedActivity);
                Assert.Equal("north", activity.Variables["tenant"]);
                Assert.Equal(42, activity.Arguments["orderId"]);
            },
            compensated =>
            {
                RoutingSlipActivityCompensated activity = Assert.IsAssignableFrom<RoutingSlipActivityCompensated>(compensated);
                Assert.Equal("north", activity.Variables["tenant"]);
                Assert.Equal(27, activity.Data["receipt"]);
            },
            revised =>
            {
                RoutingSlipRevised routingSlip = Assert.IsAssignableFrom<RoutingSlipRevised>(revised);
                Assert.Equal("north", routingSlip.Variables["tenant"]);
                Assert.Single(routingSlip.Itinerary);
                Assert.Single(routingSlip.DiscardedItinerary);
            },
            terminated =>
            {
                RoutingSlipTerminated routingSlip = Assert.IsAssignableFrom<RoutingSlipTerminated>(terminated);
                Assert.Equal("north", routingSlip.Variables["tenant"]);
                Assert.Single(routingSlip.DiscardedItinerary);
            },
            activityCompensationFailed =>
            {
                RoutingSlipActivityCompensationFailed activity =
                    Assert.IsAssignableFrom<RoutingSlipActivityCompensationFailed>(activityCompensationFailed);
                Assert.Equal("north", activity.Variables["tenant"]);
                Assert.Equal(27, activity.Data["receipt"]);
            },
            compensationFailed => Assert.Equal(
                "north",
                Assert.IsAssignableFrom<RoutingSlipCompensationFailed>(compensationFailed).Variables["tenant"]));
    }

    private static void AssertEmptyExecutionId(Action action)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("executionId", exception.ParamName);
    }

    private static void AssertOutOfRange(string parameterName, Action action)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }
}
