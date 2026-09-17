using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierCompensationResultEvaluationContractTests
{
    private static readonly DateTimeOffset RoutingSlipCreatedAt = new(2048, 9, 10, 11, 12, 13, TimeSpan.Zero);
    private static readonly DateTimeOffset CompensationStartedAt = RoutingSlipCreatedAt.AddMinutes(2);
    private static readonly TimeSpan AttemptDuration = TimeSpan.FromSeconds(7);
    private static readonly TimeSpan DeferredEvaluation = TimeSpan.FromMinutes(3);

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensation-success-final-lifecycle-and-captured-timing")]
    public async Task CompensatedFinalResult_PublishesActivityThenTerminalFaultWithCapturedTimingAndStateAsync()
    {
        CompensationObservation observation = CreateObservation(
            variables: new Dictionary<string, object> { ["Tenant"] = "north" },
            includeActivityException: true);
        observation.Clock.Advance(AttemptDuration);
        CompensationResult result = observation.Context.Compensated();
        observation.Clock.Advance(DeferredEvaluation);

        Assert.False(result.IsFailed(out Exception? failure));
        Assert.Null(failure);

        await result.EvaluateAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            observation.Outgoing.Messages,
            message =>
            {
                IRoutingSlipActivityCompensated compensated = Assert.IsAssignableFrom<IRoutingSlipActivityCompensated>(message);
                Assert.Equal(observation.RoutingSlip.TrackingNumber, compensated.TrackingNumber);
                Assert.Equal(((ActivityContext)observation.Context).ExecutionId, compensated.ExecutionId);
                Assert.Equal(CompensationStartedAt, compensated.Timestamp);
                Assert.Equal(AttemptDuration, compensated.Duration);
                Assert.Equal("Activity-1", compensated.ActivityName);
                Assert.Equal("receipt-1", compensated.Data["receipt"]);
                Assert.Equal("north", compensated.Variables["tenant"]);
            },
            message =>
            {
                IRoutingSlipFaulted faulted = Assert.IsAssignableFrom<IRoutingSlipFaulted>(message);
                Assert.Equal(observation.RoutingSlip.TrackingNumber, faulted.TrackingNumber);
                Assert.Equal(CompensationStartedAt + AttemptDuration, faulted.Timestamp);
                Assert.Equal(CompensationStartedAt + AttemptDuration - RoutingSlipCreatedAt, faulted.Duration);
                Assert.Equal("north", faulted.Variables["tenant"]);
                IActivityException activityException = Assert.Single(faulted.ActivityExceptions);
                Assert.Equal("execution failed", activityException.ExceptionInfo.Message);
            });
        Assert.Empty(observation.Outgoing.Messages.OfType<IRoutingSlip>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensation-success-removes-newest-log-and-continues-in-lifo-order")]
    public async Task CompensatedContinuation_RemovesOnlyNewestLogAndForwardsTheRemainingLifoStateAsync()
    {
        CompensationObservation observation = CreateObservation(compensationLogCount: 3);

        await observation.Context.Compensated().EvaluateAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            observation.Outgoing.Messages,
            message =>
            {
                IRoutingSlipActivityCompensated compensated = Assert.IsAssignableFrom<IRoutingSlipActivityCompensated>(message);
                Assert.Equal("Activity-3", compensated.ActivityName);
                Assert.Equal("receipt-3", compensated.Data["receipt"]);
            },
            message =>
            {
                IRoutingSlip forwarded = Assert.IsAssignableFrom<IRoutingSlip>(message);
                Assert.Collection(
                    forwarded.CompensateLogs,
                    log => Assert.Equal(observation.CompensationExecutionIds[0], log.ExecutionId),
                    log => Assert.Equal(observation.CompensationExecutionIds[1], log.ExecutionId));
                Assert.Equal(CompensateAddress(2), forwarded.GetNextCompensateAddress());
                Assert.Equal(3, forwarded.ActivityLogs.Count);
            });
        Assert.Equal(3, observation.RoutingSlip.CompensateLogs.Count);
        Assert.Empty(observation.Outgoing.Messages.OfType<IRoutingSlipFaulted>());
        Assert.Empty(observation.Outgoing.Messages.OfType<IRoutingSlipCompensationFailed>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensation-variable-updates-are-snapshotted-case-insensitively-with-null-removal")]
    public async Task CompensatedResult_SnapshotsCaseInsensitiveVariableUpdatesAndTreatsNullAsRemovalAsync()
    {
        CompensationObservation observation = CreateObservation(variables: new Dictionary<string, object>
        {
            ["Tenant"] = "north",
            ["RemoveMe"] = "present",
        });
        var updates = new Dictionary<string, object>
        {
            ["TENANT"] = "updated",
            ["RemoveMe"] = null!,
            ["Added"] = "captured",
        };
        CompensationResult result = observation.Context.Compensated(updates);

        updates["TENANT"] = "mutated";
        updates["RemoveMe"] = "restored";
        updates["Added"] = "mutated";
        updates["Late"] = "ignored";

        await result.EvaluateAsync(TestContext.Current.CancellationToken);

        IRoutingSlipActivityCompensated compensated = Assert.Single(
            observation.Outgoing.Messages.OfType<IRoutingSlipActivityCompensated>());
        IRoutingSlipFaulted faulted = Assert.Single(observation.Outgoing.Messages.OfType<IRoutingSlipFaulted>());
        AssertUpdatedVariables(compensated.Variables);
        AssertUpdatedVariables(faulted.Variables);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensation-variable-updates-reject-empty-or-whitespace-keys")]
    public void CompensatedDictionary_RejectsInvalidVariableKeysBeforeEvaluation(string invalidKey)
    {
        CompensationObservation observation = CreateObservation();
        var updates = new Dictionary<string, object> { [invalidKey] = "value" };

        ArgumentException exception = Assert.Throws<ArgumentException>(() => observation.Context.Compensated(updates));

        Assert.Equal("key", exception.ParamName);
        Assert.Empty(observation.Outgoing.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensation-failure-publishes-both-levels-with-original-state-and-captured-timing")]
    public async Task FailedResult_PublishesBothFailureLevelsWithOriginalStateAndCapturedTimingAsync()
    {
        CompensationObservation observation = CreateObservation(
            variables: new Dictionary<string, object> { ["Tenant"] = "north" });
        observation.Clock.Advance(AttemptDuration);
        var expectedFailure = new InvalidOperationException("compensation failed");
        CompensationResult result = observation.Context.Failed(expectedFailure);
        observation.Clock.Advance(DeferredEvaluation);

        Assert.True(result.IsFailed(out Exception? failure));
        Assert.Same(expectedFailure, failure);

        await result.EvaluateAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            observation.Outgoing.Messages,
            message =>
            {
                IRoutingSlipActivityCompensationFailed failed =
                    Assert.IsAssignableFrom<IRoutingSlipActivityCompensationFailed>(message);
                Assert.Equal(observation.RoutingSlip.TrackingNumber, failed.TrackingNumber);
                Assert.Equal(((ActivityContext)observation.Context).ExecutionId, failed.ExecutionId);
                Assert.Equal(CompensationStartedAt, failed.Timestamp);
                Assert.Equal(AttemptDuration, failed.Duration);
                Assert.Equal("Activity-1", failed.ActivityName);
                Assert.Equal("receipt-1", failed.Data["receipt"]);
                Assert.Equal("north", failed.Variables["tenant"]);
                AssertFailure(expectedFailure, failed.ExceptionInfo);
            },
            message =>
            {
                IRoutingSlipCompensationFailed failed = Assert.IsAssignableFrom<IRoutingSlipCompensationFailed>(message);
                Assert.Equal(observation.RoutingSlip.TrackingNumber, failed.TrackingNumber);
                Assert.Equal(CompensationStartedAt + AttemptDuration, failed.Timestamp);
                Assert.Equal(CompensationStartedAt + AttemptDuration - RoutingSlipCreatedAt, failed.Duration);
                Assert.Equal("north", failed.Variables["tenant"]);
                AssertFailure(expectedFailure, failed.ExceptionInfo);
            });
        Assert.Empty(observation.Outgoing.Messages.OfType<IRoutingSlip>());
        Assert.Empty(observation.Outgoing.Messages.OfType<IRoutingSlipActivityCompensated>());
    }

    [Theory]
    [InlineData(CompensationOutcome.Compensated)]
    [InlineData(CompensationOutcome.Failed)]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "compensation-evaluation-pre-cancellation-is-side-effect-free")]
    public async Task Evaluation_RejectsPreCanceledTokenWithoutPublishingAsync(CompensationOutcome outcome)
    {
        CompensationObservation observation = CreateObservation(compensationLogCount: 2);
        CompensationResult result = outcome switch
        {
            CompensationOutcome.Compensated => observation.Context.Compensated(),
            CompensationOutcome.Failed => observation.Context.Failed(new InvalidOperationException("expected")),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            result.EvaluateAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Empty(observation.Outgoing.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "compensation-continuation-rechecks-cancellation-after-endpoint-resolution")]
    public async Task CompensatedContinuation_StopsWhenEndpointResolutionCancelsEvaluationAsync()
    {
        using var evaluationCancellation = new CancellationTokenSource();
        CompensationObservation observation = CreateObservation(
            compensationLogCount: 2,
            decorateContext: context => new EndpointResolutionCancellationContext(context, evaluationCancellation));
        CompensationResult result = observation.Context.Compensated();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            result.EvaluateAsync(evaluationCancellation.Token));

        Assert.Equal(evaluationCancellation.Token, exception.CancellationToken);
        Assert.True(evaluationCancellation.IsCancellationRequested);
        Assert.IsAssignableFrom<IRoutingSlipActivityCompensated>(Assert.Single(observation.Outgoing.Messages));
        Assert.Empty(observation.Outgoing.Messages.OfType<IRoutingSlip>());
        Assert.Empty(observation.Outgoing.Messages.OfType<IRoutingSlipFaulted>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensation-evaluation-propagates-activity-event-delivery-failure")]
    public async Task CompensatedEvaluation_PropagatesActivityEventDeliveryFailureWithoutContinuingAsync()
    {
        var expectedFailure = new ExpectedDispatchException();
        var outgoing = new OutgoingMessageRecorder(_ => throw expectedFailure);
        CompensationObservation observation = CreateObservation(compensationLogCount: 2, outgoing: outgoing);

        ExpectedDispatchException failure = await Assert.ThrowsAsync<ExpectedDispatchException>(() =>
            observation.Context.Compensated().EvaluateAsync(TestContext.Current.CancellationToken));

        Assert.Same(expectedFailure, failure);
        Assert.Empty(observation.Outgoing.Messages);
    }

    private static CompensationObservation CreateObservation(
        int compensationLogCount = 1,
        IDictionary<string, object>? variables = null,
        bool includeActivityException = false,
        OutgoingMessageRecorder? outgoing = null,
        Func<ConsumeContext<IRoutingSlip>, ConsumeContext<IRoutingSlip>>? decorateContext = null)
    {
        var routingSlipClock = new FakeTimeProvider(RoutingSlipCreatedAt);
        var builder = new RoutingSlipBuilder(NewId.NextGuid(), routingSlipClock);
        var executionIds = new List<Guid>();

        for (var index = 1; index <= compensationLogCount; index++)
        {
            Guid executionId = NewId.NextGuid();
            executionIds.Add(executionId);
            builder.AddActivityLog(HostMetadataCache.Host, $"Activity-{index}", executionId, RoutingSlipCreatedAt, TimeSpan.Zero);
            builder.AddCompensateLog(executionId, CompensateAddress(index), new Dictionary<string, object>
            {
                [nameof(CompensationLog.Receipt)] = $"receipt-{index}",
            });
        }

        if (variables is not null)
            builder.SetVariables(variables);
        if (includeActivityException)
        {
            builder.AddActivityException(
                HostMetadataCache.Host,
                "FaultedActivity",
                NewId.NextGuid(),
                RoutingSlipCreatedAt,
                TimeSpan.FromSeconds(1),
                new InvalidOperationException("execution failed"));
        }

        IRoutingSlip routingSlip = builder.Build();
        outgoing ??= new OutgoingMessageRecorder();
        var clock = new FakeTimeProvider(CompensationStartedAt);
        ConsumeContext<IRoutingSlip> consumeContext = CreateConsumeContext(routingSlip, outgoing, clock);
        if (decorateContext is not null)
            consumeContext = decorateContext(consumeContext);

        var context = new HostCompensateContext<CompensationLog>(consumeContext);
        return new CompensationObservation(context, routingSlip, executionIds, outgoing, clock);
    }

    private static ConsumeContext<IRoutingSlip> CreateConsumeContext(
        IRoutingSlip routingSlip,
        OutgoingMessageRecorder outgoing,
        TimeProvider timeProvider)
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
        context.SetTimeProvider(timeProvider);
        return context;
    }

    private static Uri CompensateAddress(int index) =>
        new($"loopback://localhost/evaluation-compensate-{index}");

    private static void AssertUpdatedVariables(IReadOnlyDictionary<string, object> variables)
    {
        Assert.Equal("updated", variables["tenant"]);
        Assert.Equal("captured", variables["added"]);
        Assert.DoesNotContain("removeme", variables);
        Assert.DoesNotContain("late", variables);
    }

    private static void AssertFailure(Exception expected, ExceptionInfo actual)
    {
        Assert.Equal(TypeCache<InvalidOperationException>.ShortName, actual.ExceptionType);
        Assert.Equal(expected.Message, actual.Message);
    }

    public enum CompensationOutcome
    {
        Compensated,
        Failed,
    }

    private sealed record CompensationLog(string Receipt);

    private sealed record CompensationObservation(
        HostCompensateContext<CompensationLog> Context,
        IRoutingSlip RoutingSlip,
        IReadOnlyList<Guid> CompensationExecutionIds,
        OutgoingMessageRecorder Outgoing,
        FakeTimeProvider Clock);

    private sealed class EndpointResolutionCancellationContext :
        ConsumeContextScope<IRoutingSlip>
    {
        public EndpointResolutionCancellationContext(
            ConsumeContext<IRoutingSlip> context,
            CancellationTokenSource evaluationCancellation)
            : base(context)
        {
            ReceiveContext = CancelAfterEndpointResolutionReceiveContext.Create(
                context.Advanced().ReceiveContext,
                evaluationCancellation);
        }
    }

    private class CancelAfterEndpointResolutionReceiveContext :
        DispatchProxy
    {
        private ReceiveContext _context = null!;
        private ISendEndpointProvider _sendEndpointProvider = null!;

        public static ReceiveContext Create(ReceiveContext context, CancellationTokenSource cancellation)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(cancellation);

            ReceiveContext proxy = DispatchProxy.Create<ReceiveContext, CancelAfterEndpointResolutionReceiveContext>();
            var implementation = (CancelAfterEndpointResolutionReceiveContext)(object)proxy;
            implementation._context = context;
            implementation._sendEndpointProvider = new CancelAfterEndpointResolutionProvider(
                context.SendEndpointProvider,
                cancellation);
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_SendEndpointProvider")
                return _sendEndpointProvider;

            try
            {
                return targetMethod.Invoke(_context, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }

    private sealed class CancelAfterEndpointResolutionProvider(
        ISendEndpointProvider provider,
        CancellationTokenSource cancellation) :
        ISendEndpointProvider
    {
        public async Task<ISendEndpoint> GetSendEndpointAsync(
            Uri address,
            CancellationToken cancellationToken = default)
        {
            ISendEndpoint endpoint = await provider.GetSendEndpointAsync(address, cancellationToken);
            await cancellation.CancelAsync();
            return endpoint;
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) =>
            provider.ConnectSendObserver(observer);
    }

    private sealed class ExpectedDispatchException : Exception;
}
