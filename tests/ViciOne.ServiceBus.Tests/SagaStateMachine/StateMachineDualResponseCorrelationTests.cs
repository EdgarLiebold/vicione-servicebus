using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineDualResponseCorrelationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "two-response-saga-id-correlation-isolation")]
    public async Task TwoResponseRequest_UsesTheRequestHeaderToRouteBothOutcomesToTheirOwnSagaAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"dual-response-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        string endpointName = $"decision-service-{NewId.NextGuid():N}";
        Uri serviceAddress = new(harness.BaseAddress, endpointName);
        var machine = new DualResponseMachine(serviceAddress);
        Guid decoyPayloadId = NewId.NextGuid();
        harness.InMemoryBusConfiguring += configurator => configurator.ReceiveEndpoint(
            endpointName,
            endpoint => endpoint.Handler<DecisionRequest>(context => context.Message.Allow
                ? context.RespondAsync(new DecisionAllowed(decoyPayloadId, "allowed"))
                : context.RespondAsync(new DecisionDenied(decoyPayloadId, "denied"))));
        ISagaStateMachineTestHarness<DualResponseMachine, DualResponseState> sagaHarness =
            harness.AddSagaStateMachine<DualResponseMachine, DualResponseState>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid allowedId = NewId.NextGuid();
            Guid deniedId = NewId.NextGuid();
            await harness.InputQueueSendEndpoint.SendAsync(new BeginDecision(allowedId, Allow: true), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new BeginDecision(deniedId, Allow: false), cancellationToken);

            Assert.Equal(allowedId,
                await sagaHarness.WaitForSagaInStateAsync(allowedId, machine.Resolved, timeout, cancellationToken));
            Assert.Equal(deniedId,
                await sagaHarness.WaitForSagaInStateAsync(deniedId, machine.Resolved, timeout, cancellationToken));

            DualResponseState allowed = Assert.IsType<DualResponseState>(sagaHarness.Sagas.FindById(allowedId));
            DualResponseState denied = Assert.IsType<DualResponseState>(sagaHarness.Sagas.FindById(deniedId));
            Assert.Equal((true, "allowed", 1), (allowed.Allowed, allowed.Reason, allowed.ResponseCount));
            Assert.Equal((false, "denied", 1), (denied.Allowed, denied.Reason, denied.ResponseCount));
            Assert.Null(sagaHarness.Sagas.FindById(decoyPayloadId));

            ISentMessage<DecisionRequest>[] requests = harness.Sent.Snapshot<DecisionRequest>().ToArray();
            Assert.Equal(2, requests.Length);
            Assert.Equal(new Guid?[] { allowedId, deniedId }.Order().ToArray(),
                requests.Select(request => request.Context.RequestId).Order().ToArray());
            foreach (ISentMessage<DecisionRequest> request in requests)
            {
                Assert.Equal(serviceAddress, request.Context.DestinationAddress);
                Assert.True(request.Context.Headers.TryGetHeader(MessageHeaders.Request.Accept, out object? header));
                Assert.Equal(
                    [MessageUrn.ForTypeString<DecisionAllowed>(), MessageUrn.ForTypeString<DecisionDenied>()],
                    Assert.IsAssignableFrom<IList<string>>(header));
            }

            DecisionOutcome[] outcomes = harness.Published.Snapshot<DecisionOutcome>()
                .Select(published => published.Context.Message)
                .OrderBy(outcome => outcome.CorrelationId)
                .ToArray();
            Assert.Equal(
                new[]
                {
                    new DecisionOutcome(allowedId, true, "allowed"),
                    new DecisionOutcome(deniedId, false, "denied"),
                }.OrderBy(outcome => outcome.CorrelationId), outcomes);
            Assert.Empty(harness.Sent.Snapshot<Fault<DecisionRequest>>());
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "two-response-saga-id-fault-isolation")]
    public async Task FaultedTwoResponseRequest_UpdatesOnlyItsOwnSagaAndRetainsTheExactFailureAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"dual-fault-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        string endpointName = $"decision-fault-service-{NewId.NextGuid():N}";
        var machine = new DualResponseMachine(new Uri(harness.BaseAddress, endpointName));
        harness.InMemoryBusConfiguring += configurator => configurator.ReceiveEndpoint(
            endpointName,
            endpoint => endpoint.Handler<DecisionRequest>(context => context.Message.Fail
                ? Task.FromException(new ExpectedDecisionFailure($"rejected:{context.Message.CorrelationId:N}"))
                : context.RespondAsync(new DecisionAllowed(Guid.Empty, "neighbor-allowed"))));
        ISagaStateMachineTestHarness<DualResponseMachine, DualResponseState> sagaHarness =
            harness.AddSagaStateMachine<DualResponseMachine, DualResponseState>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid failingId = NewId.NextGuid();
            Guid healthyId = NewId.NextGuid();
            Guid decoyPayloadId = NewId.NextGuid();
            await harness.InputQueueSendEndpoint.SendAsync(
                new BeginDecision(failingId, Allow: false, Fail: true, PayloadCorrelationId: decoyPayloadId),
                cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new BeginDecision(healthyId, Allow: true), cancellationToken);

            Assert.Equal(failingId,
                await sagaHarness.WaitForSagaInStateAsync(failingId, machine.Resolved, timeout, cancellationToken));
            Assert.Equal(healthyId,
                await sagaHarness.WaitForSagaInStateAsync(healthyId, machine.Resolved, timeout, cancellationToken));

            DualResponseState failed = Assert.IsType<DualResponseState>(sagaHarness.Sagas.FindById(failingId));
            DualResponseState healthy = Assert.IsType<DualResponseState>(sagaHarness.Sagas.FindById(healthyId));
            Assert.Equal((0, 1, "faulted"), (failed.ResponseCount, failed.FaultCount, failed.Reason));
            Assert.Equal((1, 0, "neighbor-allowed"), (healthy.ResponseCount, healthy.FaultCount, healthy.Reason));
            Assert.Null(sagaHarness.Sagas.FindById(decoyPayloadId));

            ISentMessage<Fault<DecisionRequest>> fault = Assert.Single(harness.Sent.Snapshot<Fault<DecisionRequest>>());
            Assert.Equal(failingId, fault.Context.RequestId);
            Assert.Equal(decoyPayloadId, fault.Context.Message.Message.CorrelationId);
            ExceptionInfo failure = Assert.Single(fault.Context.Message.Exceptions);
            Assert.Equal(TypeCache<ExpectedDecisionFailure>.ShortName, failure.ExceptionType);
            Assert.Equal($"rejected:{decoyPayloadId:N}", failure.Message);

            DecisionOutcome outcome = Assert.Single(harness.Published.Snapshot<DecisionOutcome>()).Context.Message;
            Assert.Equal(new DecisionOutcome(healthyId, true, "neighbor-allowed"), outcome);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed record BeginDecision(Guid CorrelationId, bool Allow, bool Fail = false, Guid? PayloadCorrelationId = null)
        : ICorrelatedBy<Guid>;
    public sealed record DecisionRequest(Guid CorrelationId, bool Allow, bool Fail);
    public sealed record DecisionAllowed(Guid CorrelationId, string Reason);
    public sealed record DecisionDenied(Guid CorrelationId, string Reason);
    public sealed record DecisionOutcome(Guid CorrelationId, bool Allowed, string Reason);

    public sealed class DualResponseState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public bool Allowed { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int ResponseCount { get; set; }
        public int FaultCount { get; set; }
    }

    public sealed class DualResponseMachine : ViciOneServiceBusStateMachine<DualResponseState>
    {
        public DualResponseMachine(Uri serviceAddress)
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Begin, configuration =>
            {
                configuration.CorrelateById(context => context.Message.CorrelationId);
                configuration.SelectId(context => context.Message.CorrelationId);
                configuration.InsertOnInitial = true;
            });
            Request(() => Decision, configuration =>
            {
                configuration.ServiceAddress = serviceAddress;
                configuration.Timeout = TimeSpan.Zero;
            });

            Initially(When(Begin)
                .Request(Decision, context => new DecisionRequest(
                    context.Message.PayloadCorrelationId ?? context.Saga.CorrelationId,
                    context.Message.Allow,
                    context.Message.Fail))
                .TransitionTo(Decision.Pending));
            During(Decision.Pending,
                When(Decision.Completed)
                    .Then(context =>
                    {
                        context.Saga.Allowed = true;
                        context.Saga.Reason = context.Message.Reason;
                        context.Saga.ResponseCount++;
                    })
                    .Publish(context => new DecisionOutcome(context.Saga.CorrelationId, true, context.Saga.Reason))
                    .TransitionTo(Resolved),
                When(Decision.Completed2)
                    .Then(context =>
                    {
                        context.Saga.Allowed = false;
                        context.Saga.Reason = context.Message.Reason;
                        context.Saga.ResponseCount++;
                    })
                    .Publish(context => new DecisionOutcome(context.Saga.CorrelationId, false, context.Saga.Reason))
                    .TransitionTo(Resolved),
                When(Decision.Faulted)
                    .Then(context =>
                    {
                        context.Saga.FaultCount++;
                        context.Saga.Reason = "faulted";
                    })
                    .TransitionTo(Resolved));
        }

        public IState Resolved { get; } = null!;
        public IEvent<BeginDecision> Begin { get; } = null!;
        public IRequest<DualResponseState, DecisionRequest, DecisionAllowed, DecisionDenied> Decision { get; } = null!;
    }

    private sealed class ExpectedDecisionFailure(string message) : Exception(message);
}
