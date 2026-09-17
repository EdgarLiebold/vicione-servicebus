using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierHostResultParameterContractTests
{
    private static readonly DateTimeOffset CreatedAt = new(2047, 8, 9, 10, 11, 12, TimeSpan.Zero);
    private static readonly Uri ExecuteAddress = new("loopback://localhost/result-parameters-execute");
    private static readonly Uri CompensateAddress = new("loopback://localhost/result-parameters-compensate");
    private static readonly Uri ReplacementAddress = new("loopback://localhost/result-parameters-replacement");

    [Theory]
    [InlineData(LogShape.Typed, "typed-log")]
    [InlineData(LogShape.Projected, "projected-log")]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "execution-log-factories-preserve-typed-and-projected-data")]
    public async Task CompletionLogFactories_PublishTheirTypedOrProjectedDataAsync(LogShape shape, string expectedValue)
    {
        ExecuteObservation observation = CreateExecuteObservation();
        ExecutionResult result = shape switch
        {
            LogShape.Typed => observation.Context.Completed(new ActivityLog(expectedValue)),
            LogShape.Projected => observation.Context.Completed<ActivityLog>(new { Value = expectedValue }),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };

        await result.EvaluateAsync(TestContext.Current.CancellationToken);

        IRoutingSlipActivityCompleted activityCompleted = Single<IRoutingSlipActivityCompleted>(observation.Outgoing);
        Assert.Equal(expectedValue, activityCompleted.Data["value"].ToString());
        Assert.Single(observation.Outgoing.Messages.OfType<IRoutingSlipCompleted>());
    }

    [Theory]
    [InlineData(VariableShape.Object, "object-value")]
    [InlineData(VariableShape.Dictionary, "dictionary-value")]
    [InlineData(VariableShape.Enumerable, "enumerable-value")]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "execution-variable-factories-apply-object-dictionary-and-sequence-updates")]
    public async Task CompletionVariableFactories_PublishEveryInputShapeAndNullRemovalAsync(
        VariableShape shape,
        string expectedValue)
    {
        ExecuteObservation observation = CreateExecuteObservation(existingVariable: true);
        ExecutionResult result = CompleteWithVariables(observation.Context, shape, expectedValue);

        await result.EvaluateAsync(TestContext.Current.CancellationToken);

        IRoutingSlipActivityCompleted activityCompleted = Single<IRoutingSlipActivityCompleted>(observation.Outgoing);
        IRoutingSlipCompleted completed = Single<IRoutingSlipCompleted>(observation.Outgoing);
        AssertVariableUpdate(activityCompleted.Variables, expectedValue);
        AssertVariableUpdate(completed.Variables, expectedValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "completion-and-revision-callbacks-materialize-their-declared-output")]
    public async Task CompletionAndRevisionCallbacks_ApplyAtTheirDeclaredStageAndPublishExactStateAsync()
    {
        ExecuteObservation completion = CreateExecuteObservation();
        int completionCalls = 0;
        ExecutionResult completionResult = completion.Context.Completed(options =>
        {
            completionCalls++;
            options.SetVariable("CallbackValue", "completed");
            options.SetLog(new ActivityLog("callback-log"));
        });

        Assert.Equal(1, completionCalls);
        await completionResult.EvaluateAsync(TestContext.Current.CancellationToken);

        IRoutingSlipActivityCompleted completionEvent = Single<IRoutingSlipActivityCompleted>(completion.Outgoing);
        Assert.Equal("completed", completionEvent.Variables["callbackvalue"]);
        Assert.Equal("callback-log", completionEvent.Data["value"].ToString());

        ExecuteObservation revision = CreateExecuteObservation(addDiscardedActivity: true);
        int revisionCalls = 0;
        IEnumerable<KeyValuePair<string, object>> revisionVariables =
        [
            new("RevisionValue", "revised"),
        ];
        ExecutionResult revisionResult = revision.Context.ReviseItinerary(
            new ActivityLog("revision-log"),
            revisionVariables,
            itinerary =>
            {
                revisionCalls++;
                itinerary.AddActivity("Replacement", ReplacementAddress, new ActivityArguments("replacement"));
            });

        Assert.Equal(0, revisionCalls);
        await revisionResult.EvaluateAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, revisionCalls);
        IRoutingSlipActivityCompleted revisionCompleted = Single<IRoutingSlipActivityCompleted>(revision.Outgoing);
        IRoutingSlipRevised revised = Single<IRoutingSlipRevised>(revision.Outgoing);
        IRoutingSlip forwarded = Single<IRoutingSlip>(revision.Outgoing);
        Assert.Equal("revision-log", revisionCompleted.Data["value"].ToString());
        Assert.Equal("revised", revisionCompleted.Variables["revisionvalue"]);
        Assert.Equal("revised", revised.Variables["revisionvalue"]);
        Assert.Equal("Replacement", Assert.Single(revised.Itinerary).Name);
        Assert.Equal("Discarded", Assert.Single(revised.DiscardedItinerary).Name);
        Assert.Equal("Replacement", Assert.Single(forwarded.Itinerary).Name);
        Assert.Equal("revised", forwarded.Variables["revisionvalue"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "terminate-and-fault-variable-factories-publish-updated-state")]
    public async Task TerminateAndFaultVariableFactories_PublishTheirUpdatedTerminalStateAsync()
    {
        ExecuteObservation termination = CreateExecuteObservation(addDiscardedActivity: true);
        ExecutionResult terminateResult = termination.Context.Terminate(new Dictionary<string, object>
        {
            ["TerminateValue"] = "terminated",
        });

        await terminateResult.EvaluateAsync(TestContext.Current.CancellationToken);

        IRoutingSlipTerminated terminated = Single<IRoutingSlipTerminated>(termination.Outgoing);
        Assert.Equal("terminated", terminated.Variables["terminatevalue"]);
        Assert.Equal("Discarded", Assert.Single(terminated.DiscardedItinerary).Name);
        Assert.Equal("terminated", Single<IRoutingSlipCompleted>(termination.Outgoing).Variables["terminatevalue"]);

        ExecuteObservation fault = CreateExecuteObservation();
        var expectedException = new InvalidOperationException("expected fault");
        ExecutionResult faultResult = fault.Context.FaultedWithVariables(expectedException, new
        {
            FaultValue = "faulted",
        });

        Assert.True(faultResult.IsFaulted(out Exception? actualException));
        Assert.Same(expectedException, actualException);
        await faultResult.EvaluateAsync(TestContext.Current.CancellationToken);

        IRoutingSlipActivityFaulted activityFaulted = Single<IRoutingSlipActivityFaulted>(fault.Outgoing);
        IRoutingSlipFaulted routingSlipFaulted = Single<IRoutingSlipFaulted>(fault.Outgoing);
        Assert.Equal("faulted", activityFaulted.Variables["faultvalue"]);
        Assert.Equal("faulted", routingSlipFaulted.Variables["faultvalue"]);
    }

    [Theory]
    [InlineData(CompensationVariableShape.Object, "object-compensation")]
    [InlineData(CompensationVariableShape.Dictionary, "dictionary-compensation")]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensation-variable-factories-apply-object-and-dictionary-updates")]
    public async Task CompensationVariableFactories_PublishEveryInputShapeAndNullRemovalAsync(
        CompensationVariableShape shape,
        string expectedValue)
    {
        CompensateObservation observation = CreateCompensateObservation();
        CompensationResult result = shape switch
        {
            CompensationVariableShape.Object => observation.Context.Compensated(
                new VariableUpdates(expectedValue, Removed: null)),
            CompensationVariableShape.Dictionary => observation.Context.Compensated(new Dictionary<string, object>
            {
                [nameof(VariableUpdates.Output)] = expectedValue,
                [nameof(VariableUpdates.Removed)] = null!,
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };

        await result.EvaluateAsync(TestContext.Current.CancellationToken);

        IRoutingSlipActivityCompensated compensated = Single<IRoutingSlipActivityCompensated>(observation.Outgoing);
        IRoutingSlipFaulted faulted = Single<IRoutingSlipFaulted>(observation.Outgoing);
        AssertVariableUpdate(compensated.Variables, expectedValue);
        AssertVariableUpdate(faulted.Variables, expectedValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "execute-activity-context-rejects-null-activity")]
    public void ExecuteActivityContextFactory_RejectsNullActivityWithExactParameter()
    {
        ExecuteObservation observation = CreateExecuteObservation();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            observation.Context.CreateActivityContext<object>(null!));

        Assert.Equal("activity", exception.ParamName);
    }

    private static ExecutionResult CompleteWithVariables(
        HostExecuteContext<ActivityArguments> context,
        VariableShape shape,
        string value) => shape switch
        {
            VariableShape.Object => context.CompletedWithVariables(new VariableUpdates(value, Removed: null)),
            VariableShape.Dictionary => context.CompletedWithVariables(new Dictionary<string, object>
            {
                [nameof(VariableUpdates.Output)] = value,
                [nameof(VariableUpdates.Removed)] = null!,
            }),
            VariableShape.Enumerable => context.CompletedWithVariables(
                (IEnumerable<KeyValuePair<string, object>>)
                [
                    new(nameof(VariableUpdates.Output), value),
                    new(nameof(VariableUpdates.Removed), null!),
                ]),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };

    private static ExecuteObservation CreateExecuteObservation(
        bool existingVariable = false,
        bool addDiscardedActivity = false)
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(CreatedAt));
        builder.AddActivity("Current", ExecuteAddress, new ActivityArguments("input"));
        if (addDiscardedActivity)
            builder.AddActivity("Discarded", ExecuteAddress, new ActivityArguments("discarded"));
        if (existingVariable)
            builder.SetVariable(nameof(VariableUpdates.Removed), "existing");

        IRoutingSlip routingSlip = builder.Build();
        var outgoing = new OutgoingMessageRecorder();
        var context = new HostExecuteContext<ActivityArguments>(CompensateAddress, CreateConsumeContext(routingSlip, outgoing));
        return new ExecuteObservation(context, outgoing);
    }

    private static CompensateObservation CreateCompensateObservation()
    {
        Guid executionId = NewId.NextGuid();
        var builder = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(CreatedAt));
        builder.SetVariable(nameof(VariableUpdates.Removed), "existing");
        builder.AddActivityLog(HostMetadataCache.Host, "Compensate", executionId, CreatedAt, TimeSpan.Zero);
        builder.AddCompensateLog(
            executionId,
            CompensateAddress,
            new Dictionary<string, object> { [nameof(ActivityLog.Value)] = "receipt" });

        IRoutingSlip routingSlip = builder.Build();
        var outgoing = new OutgoingMessageRecorder();
        var context = new HostCompensateContext<ActivityLog>(CreateConsumeContext(routingSlip, outgoing));
        return new CompensateObservation(context, outgoing);
    }

    private static ConsumeContext<IRoutingSlip> CreateConsumeContext(
        IRoutingSlip routingSlip,
        OutgoingMessageRecorder outgoing)
    {
        IObjectDeserializer deserializer = ServiceBusMetadataJson.ObjectDeserializer;
        var metadata = new EnvelopeMessageContext(new JsonMessageEnvelope(), deserializer);
        var serializerContext = new SystemTextJsonSerializerContext(
            deserializer,
            ServiceBusMetadataJson.Options,
            SystemTextJsonMessageSerializer.JsonContentType,
            metadata,
            [MessageUrn.ForTypeString<IRoutingSlip>()],
            message: routingSlip);
        ConsumeContext<IRoutingSlip> context = InMemoryOutboxTestContextFactory.Create(
            routingSlip,
            TestContext.Current.CancellationToken,
            outgoingMessages: outgoing,
            serializerContext: serializerContext);
        context.SetTimeProvider(new FakeTimeProvider(CreatedAt));
        return context;
    }

    private static T Single<T>(OutgoingMessageRecorder outgoing)
        where T : class =>
        Assert.Single(outgoing.Messages.OfType<T>());

    private static void AssertVariableUpdate(IReadOnlyDictionary<string, object> variables, string expectedValue)
    {
        Assert.Equal(expectedValue, variables["output"]);
        Assert.DoesNotContain("removed", variables);
    }

    public enum LogShape
    {
        Typed,
        Projected,
    }

    public enum VariableShape
    {
        Object,
        Dictionary,
        Enumerable,
    }

    public enum CompensationVariableShape
    {
        Object,
        Dictionary,
    }

    private sealed record ActivityArguments(string Value);

    private sealed record ActivityLog(string Value);

    private sealed record VariableUpdates(string Output, string? Removed);

    private sealed record ExecuteObservation(
        HostExecuteContext<ActivityArguments> Context,
        OutgoingMessageRecorder Outgoing);

    private sealed record CompensateObservation(
        HostCompensateContext<ActivityLog> Context,
        OutgoingMessageRecorder Outgoing);
}
