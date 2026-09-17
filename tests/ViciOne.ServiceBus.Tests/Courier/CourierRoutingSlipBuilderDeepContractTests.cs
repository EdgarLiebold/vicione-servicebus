using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierRoutingSlipBuilderDeepContractTests
{
    private static readonly DateTimeOffset CreatedAt = new(2040, 2, 3, 4, 5, 6, TimeSpan.Zero);
    private static readonly Uri FirstAddress = new("loopback://localhost/deep-builder-first");
    private static readonly Uri SecondAddress = new("loopback://localhost/deep-builder-second");
    private static readonly Uri ThirdAddress = new("loopback://localhost/deep-builder-third");
    private static readonly Uri FirstCompensateAddress = new("loopback://localhost/deep-builder-compensate-first");
    private static readonly Uri SecondCompensateAddress = new("loopback://localhost/deep-builder-compensate-second");

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ROUTING", "route-inspection-selects-first-execute-and-last-compensation")]
    public void RouteInspection_SelectsFirstExecuteAndLastCompensationAndValidatesNull()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(CreatedAt));
        builder.AddActivity("First", FirstAddress);
        builder.AddActivity("Second", SecondAddress);
        AddCompensation(builder, 1, FirstCompensateAddress);
        AddCompensation(builder, 2, SecondCompensateAddress);
        IRoutingSlip routingSlip = builder.Build();

        Assert.False(routingSlip.RanToCompletion());
        Assert.Equal(FirstAddress, routingSlip.GetNextExecuteAddress());
        Assert.Equal(SecondCompensateAddress, routingSlip.GetNextCompensateAddress());

        AssertParameter("routingSlip", () => RoutingSlipExtensions.RanToCompletion(null!));
        AssertParameter("routingSlip", () => RoutingSlipExtensions.GetNextExecuteAddress(null!));
        AssertParameter("routingSlip", () => RoutingSlipExtensions.GetNextCompensateAddress(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-ROUTING", "execute-extension-overloads-delegate-active-and-terminal-state")]
    public async Task ExecuteExtensionOverloads_DelegateActiveAndTerminalStateWithExactCancellationAsync(bool completed)
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        if (!completed)
            builder.AddActivity("First", FirstAddress);
        IRoutingSlip routingSlip = builder.Build();
        var transport = new RecordingTransport();
        using var cancellation = new CancellationTokenSource();

        if (completed)
            await routingSlip.ExecuteAsync(transport, transport, cancellation.Token);
        else
            await transport.ExecuteAsync(routingSlip, cancellation.Token);

        if (completed)
        {
            IRoutingSlipCompleted terminal = Assert.IsAssignableFrom<IRoutingSlipCompleted>(Assert.Single(transport.Published));
            Assert.Equal(routingSlip.TrackingNumber, terminal.TrackingNumber);
            Assert.Equal(cancellation.Token, transport.PublishToken);
            Assert.Empty(transport.Sent);
        }
        else
        {
            Assert.Equal(FirstAddress, transport.RequestedAddress);
            Assert.Equal(cancellation.Token, transport.ResolutionToken);
            Assert.Equal(cancellation.Token, transport.SendToken);
            IRoutingSlip submitted = Assert.IsAssignableFrom<IRoutingSlip>(Assert.Single(transport.Sent));
            Assert.NotSame(routingSlip, submitted);
            Assert.Equal(routingSlip.TrackingNumber, submitted.TrackingNumber);
            Assert.Empty(transport.Published);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ROUTING", "execute-extension-overloads-reject-every-missing-input")]
    public void ExecuteExtensionOverloads_RejectEveryMissingInputWithExactParameter()
    {
        IRoutingSlip routingSlip = new RoutingSlipBuilder(NewId.NextGuid()).Build();
        var transport = new RecordingTransport();

        AssertParameter("source", () => RoutingSlipExtensions.ExecuteAsync<RecordingTransport>(null!, routingSlip));
        AssertParameter("routingSlip", () => RoutingSlipExtensions.ExecuteAsync(transport, null!));
        AssertParameter("routingSlip", () => RoutingSlipExtensions.ExecuteAsync(null!, transport, transport));
        AssertParameter("sendEndpointProvider", () => RoutingSlipExtensions.ExecuteAsync(routingSlip, null!, transport));
        AssertParameter("publishEndpoint", () => RoutingSlipExtensions.ExecuteAsync(routingSlip, transport, null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-ROUTING", "execute-extension-transport-failures-propagate-without-later-effects")]
    public async Task ExecuteExtension_PropagatesTransportFailureWithoutLaterEffectsAsync(bool completed)
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        if (!completed)
            builder.AddActivity("First", FirstAddress);
        IRoutingSlip routingSlip = builder.Build();
        var expectedFailure = new ExpectedTransportFailure();
        var transport = new RecordingTransport
        {
            PublicationFailure = completed ? expectedFailure : null,
            ResolutionFailure = completed ? null : expectedFailure,
        };

        ExpectedTransportFailure actualFailure = completed
            ? await Assert.ThrowsAsync<ExpectedTransportFailure>(() =>
                routingSlip.ExecuteAsync(transport, transport, TestContext.Current.CancellationToken))
            : await Assert.ThrowsAsync<ExpectedTransportFailure>(() =>
                transport.ExecuteAsync(routingSlip, TestContext.Current.CancellationToken));

        Assert.Same(expectedFailure, actualFailure);
        Assert.Empty(transport.Sent);
        Assert.Empty(transport.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER", "variable-updates-are-case-insensitive-ordered-and-null-removing")]
    public void VariableUpdates_AreCaseInsensitiveOrderedAndNullRemovingAcrossEveryInputShape()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.SetVariable("Tenant", "north");
        builder.SetVariable("Removed", (object)"present");
        builder.SetVariables(new
        {
            TENANT = "object-update",
            Removed = (string?)null,
            Added = 17,
        });
        var updates = new List<KeyValuePair<string, object>>
        {
            new("tenant", "sequence-first"),
            new("TENANT", "sequence-last"),
            new("Added", 23),
        };

        builder.SetVariables(updates);
        updates[1] = new KeyValuePair<string, object>("TENANT", "caller-mutated");
        IRoutingSlip routingSlip = builder.Build();

        Assert.Equal(2, routingSlip.Variables.Count);
        Assert.Equal("sequence-last", routingSlip.Variables["tenant"]);
        Assert.Equal(23, routingSlip.Variables["ADDED"]);
        Assert.DoesNotContain("removed", routingSlip.Variables);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER", "variable-enumeration-failure-is-atomic")]
    public void VariableEnumerationFailure_DoesNotApplyAnyEarlierYieldedUpdate()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.SetVariable("Stable", "original");
        var expectedFailure = new ExpectedEnumerationFailure();

        ExpectedEnumerationFailure actualFailure = Assert.Throws<ExpectedEnumerationFailure>(() =>
            builder.SetVariables(FailingUpdates(expectedFailure)));

        Assert.Same(expectedFailure, actualFailure);
        IReadOnlyDictionary<string, object> variables = builder.Build().Variables;
        Assert.Equal("original", Assert.Single(variables).Value);
        Assert.False(variables.ContainsKey("Early"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER-ISOLATION", "clone-constructors-preserve-select-transfer-and-isolate-state")]
    public void CloneConstructors_PreserveSelectedStateTransferSourceInOrderAndRemainIsolated()
    {
        var sourceBuilder = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(CreatedAt));
        sourceBuilder.AddActivity("First", FirstAddress);
        sourceBuilder.AddActivity("Second", SecondAddress);
        sourceBuilder.SetVariable("Tenant", "source");
        AddCompensation(sourceBuilder, 1, FirstCompensateAddress);
        AddCompensation(sourceBuilder, 2, SecondCompensateAddress);
        IRoutingSlip source = sourceBuilder.Build();

        var selectedBuilder = new RoutingSlipBuilder(source, itinerary => itinerary.Skip(1));
        selectedBuilder.SetVariable("Tenant", "selected");
        selectedBuilder.AddActivity("Third", ThirdAddress);
        IRoutingSlip selected = selectedBuilder.Build();

        Assert.Equal(["Second", "Third"], selected.Itinerary.Select(activity => activity.Name));
        Assert.Equal("selected", selected.Variables["tenant"]);
        Assert.Equal(2, selected.ActivityLogs.Count);
        Assert.Equal(2, selected.CompensateLogs.Count);

        var compensationBuilder = new RoutingSlipBuilder(source, source.CompensateLogs.Skip(1));
        IRoutingSlip compensationSelection = compensationBuilder.Build();
        Assert.Equal(SecondCompensateAddress, Assert.Single(compensationSelection.CompensateLogs).Address);
        Assert.Equal(["First", "Second"], compensationSelection.Itinerary.Select(activity => activity.Name));

        var transferBuilder = new RoutingSlipBuilder(source, [], source.Itinerary);
        IList<IActivity> sourceItineraryView = transferBuilder.SourceItinerary;
        Assert.True(sourceItineraryView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => sourceItineraryView.Clear());
        transferBuilder.AddActivity("Third", ThirdAddress);
        Assert.Equal(2, transferBuilder.AddActivitiesFromSourceItinerary());
        Assert.Equal(0, transferBuilder.AddActivitiesFromSourceItinerary());
        Assert.Empty(sourceItineraryView);
        Assert.Equal(
            ["Third", "First", "Second"],
            transferBuilder.Build().Itinerary.Select(activity => activity.Name));

        Assert.Equal(["First", "Second"], source.Itinerary.Select(activity => activity.Name));
        Assert.Equal("source", source.Variables["tenant"]);
        Assert.Equal(2, source.CompensateLogs.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER", "transition-records-preserve-order-values-and-detached-data")]
    public void TransitionRecords_PreserveOrderExactValuesAndDetachedCompensationData()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(CreatedAt));
        Guid firstExecutionId = Guid.Parse("504fa721-b139-4477-8834-bbb31e3ce523");
        Guid secondExecutionId = Guid.Parse("5011453f-771b-4c89-87ca-7f92339198d9");
        var compensationData = new Dictionary<string, object> { ["receipt"] = "original" };
        var localFailure = new InvalidOperationException("local failure");
        localFailure.Data["code"] = "original";
        ExceptionInfo capturedFailure = new FaultExceptionInfo(new ApplicationException("captured failure"));

        builder.AddActivityLog(
            HostMetadataCache.Host,
            "First",
            firstExecutionId,
            CreatedAt,
            TimeSpan.FromSeconds(2));
        builder.AddCompensateLog(firstExecutionId, FirstCompensateAddress, compensationData);
        builder.AddActivityException(
            HostMetadataCache.Host,
            "First",
            firstExecutionId,
            CreatedAt,
            TimeSpan.FromSeconds(3),
            localFailure);
        builder.AddActivityException(
            HostMetadataCache.Host,
            "Second",
            secondExecutionId,
            CreatedAt.AddMinutes(1),
            TimeSpan.FromSeconds(4),
            capturedFailure);

        IRoutingSlip routingSlip = builder.Build();
        compensationData["receipt"] = "caller-mutated";
        localFailure.Data["code"] = "caller-mutated";

        IActivityLog activityLog = Assert.Single(routingSlip.ActivityLogs);
        Assert.Equal(firstExecutionId, activityLog.ExecutionId);
        Assert.Equal("First", activityLog.Name);
        Assert.Equal(CreatedAt, activityLog.Timestamp);
        Assert.Equal(TimeSpan.FromSeconds(2), activityLog.Duration);
        ICompensateLog compensateLog = Assert.Single(routingSlip.CompensateLogs);
        Assert.Equal(firstExecutionId, compensateLog.ExecutionId);
        Assert.Equal(FirstCompensateAddress, compensateLog.Address);
        Assert.Equal("original", compensateLog.Data["receipt"]);
        Assert.Collection(
            routingSlip.ActivityExceptions,
            exception =>
            {
                Assert.Equal(firstExecutionId, exception.ExecutionId);
                Assert.Equal("First", exception.Name);
                Assert.Equal("local failure", exception.ExceptionInfo.Message);
                Assert.Equal("original", exception.ExceptionInfo.Data!["code"]);
            },
            exception =>
            {
                Assert.Equal(secondExecutionId, exception.ExecutionId);
                Assert.Equal("Second", exception.Name);
                Assert.Equal("captured failure", exception.ExceptionInfo.Message);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-BUILDER", "transition-mutators-reject-uncovered-invalid-parameters")]
    public void TransitionMutators_RejectUncoveredInvalidParametersWithExactNames()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        Guid executionId = NewId.NextGuid();

        AssertParameter("name", () => builder.AddActivityLog(
            HostMetadataCache.Host,
            " ",
            executionId,
            CreatedAt,
            TimeSpan.Zero));
        AssertParameter("host", () => builder.AddActivityException(
            null!,
            "Activity",
            executionId,
            CreatedAt,
            TimeSpan.Zero,
            new InvalidOperationException()));
        AssertParameter("name", () => builder.AddActivityException(
            HostMetadataCache.Host,
            " ",
            executionId,
            CreatedAt,
            TimeSpan.Zero,
            new InvalidOperationException()));
        AssertParameter("exception", () => builder.AddActivityException(
            HostMetadataCache.Host,
            "Activity",
            executionId,
            CreatedAt,
            TimeSpan.Zero,
            (Exception)null!));
        AssertParameter("exceptionInfo", () => builder.AddActivityException(
            HostMetadataCache.Host,
            "Activity",
            executionId,
            CreatedAt,
            TimeSpan.Zero,
            (ExceptionInfo)null!));
    }

    private static void AddCompensation(RoutingSlipBuilder builder, int index, Uri address)
    {
        Guid executionId = index == 1
            ? Guid.Parse("1c367149-4acd-4225-b7d7-b6c0d19bec0a")
            : Guid.Parse("2643cf8d-c8d5-4120-9252-bc273238ec07");
        builder.AddActivityLog(
            HostMetadataCache.Host,
            $"Activity-{index}",
            executionId,
            CreatedAt.AddMinutes(index),
            TimeSpan.FromSeconds(index));
        builder.AddCompensateLog(
            executionId,
            address,
            new Dictionary<string, object> { ["index"] = index });
    }

    private static IEnumerable<KeyValuePair<string, object>> FailingUpdates(Exception failure)
    {
        yield return new KeyValuePair<string, object>("Early", "must-not-apply");
        throw failure;
    }

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.ThrowsAny<ArgumentException>(action).ParamName);

    private sealed class ExpectedEnumerationFailure : Exception;

    private sealed class ExpectedTransportFailure : Exception;

    private sealed class RecordingTransport :
        ISendEndpointProvider,
        ISendEndpoint,
        IPublishEndpoint
    {
        public List<object> Published { get; } = [];

        public List<object> Sent { get; } = [];

        public Uri? RequestedAddress { get; private set; }

        public CancellationToken ResolutionToken { get; private set; }

        public CancellationToken SendToken { get; private set; }

        public CancellationToken PublishToken { get; private set; }

        public Exception? ResolutionFailure { get; init; }

        public Exception? PublicationFailure { get; init; }

        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            RequestedAddress = address;
            ResolutionToken = cancellationToken;
            return ResolutionFailure is null
                ? Task.FromResult<ISendEndpoint>(this)
                : Task.FromException<ISendEndpoint>(ResolutionFailure);
        }

        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            SendToken = cancellationToken;
            Sent.Add(message);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(options);
            return SendAsync(message, cancellationToken);
        }

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            PublishToken = cancellationToken;
            if (PublicationFailure is not null)
                return Task.FromException(PublicationFailure);
            Published.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(options);
            return PublishAsync(message, cancellationToken);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();
    }
}
