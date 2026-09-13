using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Results;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierHostResultContractTests
{
    private static readonly DateTimeOffset CreatedAt = new(2044, 5, 6, 7, 8, 9, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "every-execution-result-factory-produces-its-declared-state-transition")]
    public void ExecuteContext_ExposesEveryCompletionRevisionTerminationAndFaultShape()
    {
        HostExecuteContext<ActivityArguments> context = CreateExecuteContext(hasCompensation: true);
        var variables = new Dictionary<string, object> { ["result"] = 27 };
        var log = new ActivityLog("receipt");
        int completionCallbacks = 0;
        int itineraryCallbacks = 0;

        ExecutionResult[] completionResults =
        [
            context.Completed(),
            context.Completed(_ => completionCallbacks++),
            context.Completed(log),
            context.Completed(log, _ => completionCallbacks++),
            context.Completed<ActivityLog>(new { Value = "mapped" }),
            context.Completed<ActivityLog>(new { Value = "mapped" }, _ => completionCallbacks++),
            context.CompletedWithVariables(variables),
            context.CompletedWithVariables(new { Result = 27 }),
            context.CompletedWithVariables(log, new { Result = 27 }),
            context.CompletedWithVariables<ActivityLog>(new { Value = "mapped" }, new { Result = 27 }),
            context.CompletedWithVariables(log, variables),
        ];
        ExecutionResult[] revisionResults =
        [
            context.ReviseItinerary(_ => itineraryCallbacks++),
            context.ReviseItinerary(log, _ => itineraryCallbacks++),
            context.ReviseItinerary(log, new { Result = 27 }, _ => itineraryCallbacks++),
            context.ReviseItinerary(log, variables, _ => itineraryCallbacks++),
        ];
        ExecutionResult[] terminationResults =
        [
            context.Terminate(),
            context.Terminate(new { Result = 27 }),
            context.Terminate(variables),
        ];
        var expectedFailure = new InvalidOperationException("expected");
        ExecutionResult[] faultResults =
        [
            context.Faulted(),
            context.Faulted(expectedFailure),
            context.Faulted(expectedFailure, _ => { }),
            context.FaultedWithVariables(expectedFailure, new { Result = 27 }),
            context.FaultedWithVariables(expectedFailure, variables),
        ];

        Assert.All(completionResults, result => Assert.IsType<CompletedExecutionResult<ActivityArguments>>(result));
        Assert.All(revisionResults, result => Assert.IsType<ReviseItineraryExecutionResult<ActivityArguments>>(result));
        Assert.All(terminationResults, result => Assert.IsType<TerminateExecutionResult<ActivityArguments>>(result));
        Assert.All(faultResults, result => Assert.IsType<FaultedExecutionResult<ActivityArguments>>(result));
        Assert.Equal(3, completionCallbacks);

        foreach (ExecutionResult result in revisionResults)
            Assert.NotNull(result);
        Assert.Equal(0, itineraryCallbacks);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "execution-result-factories-validate-every-reference-input")]
    public void ExecuteContext_ResultFactoriesRejectEveryMissingReferenceInput()
    {
        HostExecuteContext<ActivityArguments> context = CreateExecuteContext(hasCompensation: true);
        var log = new ActivityLog("receipt");
        Action<IItineraryBuilder> itinerary = _ => { };
        var failure = new InvalidOperationException("expected");

        AssertParameter("callback", () => context.Completed(null!));
        AssertParameter("log", () => context.Completed<ActivityLog>((ActivityLog)null!));
        AssertParameter("callback", () => context.Completed(log, null!));
        AssertParameter("logValues", () => context.Completed<ActivityLog>((object)null!));
        AssertParameter("callback", () => context.Completed<ActivityLog>(new { Value = "mapped" }, null!));
        AssertParameter("variables", () => context.CompletedWithVariables((object)null!));
        AssertParameter("variables", () => context.CompletedWithVariables((IEnumerable<KeyValuePair<string, object>>)null!));
        AssertParameter("log", () => context.CompletedWithVariables<ActivityLog>(null!, new { Result = 27 }));
        AssertParameter("variables", () => context.CompletedWithVariables(log, (object)null!));
        AssertParameter("logValues", () => context.CompletedWithVariables<ActivityLog>((object)null!, new { Result = 27 }));
        AssertParameter("variables", () => context.CompletedWithVariables<ActivityLog>(new { Value = "mapped" }, null!));
        AssertParameter("variables", () => context.CompletedWithVariables(log, (IEnumerable<KeyValuePair<string, object>>)null!));
        AssertParameter("buildItinerary", () => context.ReviseItinerary(null!));
        AssertParameter("log", () => context.ReviseItinerary<ActivityLog>(null!, itinerary));
        AssertParameter("buildItinerary", () => context.ReviseItinerary(log, null!));
        AssertParameter("variables", () => context.ReviseItinerary(log, (object)null!, itinerary));
        AssertParameter("buildItinerary", () => context.ReviseItinerary(log, new { Result = 27 }, null!));
        AssertParameter("variables", () => context.ReviseItinerary(log, (IEnumerable<KeyValuePair<string, object>>)null!, itinerary));
        AssertParameter("variables", () => context.Terminate((object)null!));
        AssertParameter("variables", () => context.Terminate((IEnumerable<KeyValuePair<string, object>>)null!));
        AssertParameter("exception", () => context.Faulted(null!));
        AssertParameter("exception", () => context.Faulted(null!, _ => { }));
        AssertParameter("callback", () => context.Faulted(failure, null!));
        AssertParameter("exception", () => context.FaultedWithVariables(null!, new { Result = 27 }));
        AssertParameter("variables", () => context.FaultedWithVariables(failure, (object)null!));
        AssertParameter("variables", () => context.FaultedWithVariables(failure, (IEnumerable<KeyValuePair<string, object>>)null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensation-log-results-require-a-compensation-endpoint")]
    public void ExecuteContext_LogBearingResultsRequireACompensationEndpoint()
    {
        HostExecuteContext<ActivityArguments> context = CreateExecuteContext(hasCompensation: false);
        var log = new ActivityLog("receipt");
        var variables = new Dictionary<string, object>();
        Action<IItineraryBuilder> itinerary = _ => { };

        Assert.Throws<InvalidCompensationAddressException>(() => context.Completed(log));
        Assert.Throws<InvalidCompensationAddressException>(() => context.Completed(log, _ => { }));
        Assert.Throws<InvalidCompensationAddressException>(() => context.Completed<ActivityLog>(new { Value = "mapped" }));
        Assert.Throws<InvalidCompensationAddressException>(() => context.Completed<ActivityLog>(new { Value = "mapped" }, _ => { }));
        Assert.Throws<InvalidCompensationAddressException>(() => context.CompletedWithVariables(log, new { Result = 27 }));
        Assert.Throws<InvalidCompensationAddressException>(() =>
            context.CompletedWithVariables<ActivityLog>(new { Value = "mapped" }, new { Result = 27 }));
        Assert.Throws<InvalidCompensationAddressException>(() => context.CompletedWithVariables(log, variables));
        Assert.Throws<InvalidCompensationAddressException>(() => context.ReviseItinerary(log, itinerary));
        Assert.Throws<InvalidCompensationAddressException>(() => context.ReviseItinerary(log, new { Result = 27 }, itinerary));
        Assert.Throws<InvalidCompensationAddressException>(() => context.ReviseItinerary(log, variables, itinerary));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "host-context-requires-an-itinerary-and-binds-exact-arguments")]
    public void HostExecuteContext_RequiresAnActivityAndBindsItsArguments()
    {
        HostExecuteContext<ActivityArguments> context = CreateExecuteContext(hasCompensation: true);
        IRoutingSlip empty = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(CreatedAt)).Build();
        ConsumeContext<IRoutingSlip> emptyContext = CreateConsumeContext(empty);

        ExecuteActivityContext<TestActivity, ActivityArguments> activityContext =
            context.CreateActivityContext(new TestActivity());
        ArgumentException failure = Assert.Throws<ArgumentException>(() =>
            new HostExecuteContext<ActivityArguments>(null, emptyContext));

        Assert.Equal("context", failure.ParamName);
        Assert.Equal("Execute", context.ActivityName);
        Assert.Equal("input", context.Arguments.Value);
        Assert.Equal("input", activityContext.Arguments.Value);
        Assert.IsType<TestActivity>(activityContext.Activity);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-HOST-CONFIGURATION", "activity-host-construction-send-and-probe-boundaries")]
    public async Task ActivityHosts_ValidateConstructionSendAndProbeBoundariesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IPipe<ExecuteContext<ActivityArguments>> executePipe = Pipe.Empty<ExecuteContext<ActivityArguments>>();
        IPipe<CompensateContext<ActivityLog>> compensatePipe = Pipe.Empty<CompensateContext<ActivityLog>>();
        var compensationAddress = new Uri("loopback://localhost/compensate");

        Assert.Equal("executePipe", Assert.Throws<ArgumentNullException>(() =>
            new ExecuteActivityHost<TestActivity, ActivityArguments>(null!, compensationAddress)).ParamName);
        Assert.Equal("compensatePipe", Assert.Throws<ArgumentNullException>(() =>
            new CompensateActivityHost<TestActivity, ActivityLog>(null!)).ParamName);

        var executeHost = new ExecuteActivityHost<TestActivity, ActivityArguments>(executePipe, compensationAddress);
        var executeOnlyHost = new ExecuteActivityHost<TestActivity, ActivityArguments>(executePipe, null);
        var compensateHost = new CompensateActivityHost<TestActivity, ActivityLog>(compensatePipe);
        IPipe<ConsumeContext<IRoutingSlip>> next = Pipe.Empty<ConsumeContext<IRoutingSlip>>();
        var builder = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(CreatedAt));
        builder.AddActivity("Execute", new Uri("loopback://localhost/execute"), new ActivityArguments("input"));
        ConsumeContext<IRoutingSlip> context = CreateConsumeContext(builder.Build());

        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executeHost.SendAsync(null!, next))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executeHost.SendAsync(context, null!))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            compensateHost.SendAsync(null!, next))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            compensateHost.SendAsync(context, null!))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => executeHost.Probe(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => compensateHost.Probe(null!)).ParamName);

        IReadOnlyDictionary<string, object> executeProbe = Scope(executeHost.GetProbeResult(cancellationToken).Results, "filters");
        Assert.Equal("executeActivity", Assert.Contains("filterType", executeProbe));
        Assert.Equal(TypeCache<TestActivity>.ShortName, Assert.Contains("activityType", executeProbe));
        Assert.Equal(TypeCache<ActivityArguments>.ShortName, Assert.Contains("argumentType", executeProbe));
        Assert.Equal(compensationAddress, Assert.Contains("compensateAddress", executeProbe));

        IReadOnlyDictionary<string, object> executeOnlyProbe = Scope(executeOnlyHost.GetProbeResult(cancellationToken).Results, "filters");
        Assert.DoesNotContain("compensateAddress", executeOnlyProbe);

        IReadOnlyDictionary<string, object> compensateProbe = Scope(compensateHost.GetProbeResult(cancellationToken).Results, "filters");
        Assert.Equal("compensateActivity", Assert.Contains("filterType", compensateProbe));
        Assert.Equal(TypeCache<TestActivity>.ShortName, Assert.Contains("activityType", compensateProbe));
        Assert.Equal(TypeCache<ActivityLog>.ShortName, Assert.Contains("logType", compensateProbe));
    }

    private static HostExecuteContext<ActivityArguments> CreateExecuteContext(bool hasCompensation)
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(CreatedAt));
        builder.AddActivity("Execute", new Uri("loopback://localhost/execute"), new ActivityArguments("input"));
        return new HostExecuteContext<ActivityArguments>(
            hasCompensation ? new Uri("loopback://localhost/compensate") : null,
            CreateConsumeContext(builder.Build()));
    }

    private static ConsumeContext<IRoutingSlip> CreateConsumeContext(IRoutingSlip routingSlip)
    {
        SerializerContext serializerContext = CreateSerializerContext(routingSlip);
        ConsumeContext<IRoutingSlip> context = InMemoryOutboxTestContextFactory.Create(
            routingSlip,
            TestContext.Current.CancellationToken,
            serializerContext: serializerContext);
        context.SetTimeProvider(new FakeTimeProvider(CreatedAt));
        return context;
    }

    private static SerializerContext CreateSerializerContext(IRoutingSlip routingSlip)
    {
        IObjectDeserializer deserializer = ServiceBusMetadataJson.ObjectDeserializer;
        var metadata = new EnvelopeMessageContext(new JsonMessageEnvelope(), deserializer);
        return new SystemTextJsonSerializerContext(
            deserializer,
            ServiceBusMetadataJson.Options,
            SystemTextJsonMessageSerializer.JsonContentType,
            metadata,
            [MessageUrn.ForTypeString<IRoutingSlip>()],
            message: routingSlip);
    }

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.ThrowsAny<ArgumentException>(action).ParamName);

    private static IReadOnlyDictionary<string, object> Scope(
        IReadOnlyDictionary<string, object> parent,
        string key) =>
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(Assert.Contains(key, parent));

    private sealed record ActivityArguments(string Value);

    private sealed record ActivityLog(string Value);

    private sealed class TestActivity :
        IExecuteActivity<ActivityArguments>,
        ICompensateActivity<ActivityLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ActivityArguments> context) =>
            Task.FromResult(context.Completed());

        public Task<CompensationResult> CompensateAsync(CompensateContext<ActivityLog> context) =>
            Task.FromResult(context.Compensated());
    }
}
