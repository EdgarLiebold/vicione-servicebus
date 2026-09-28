using System.Collections.Concurrent;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineFaultRecoveryRequestIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "fault-recovery-request-awaits-factory-and-uses-exception-address")]
    public async Task FaultRecoveryRequest_RoutesTheOriginalFailureOrSuppressesAFailedFactoryAsync(bool failFactory)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"fault-recovery-request-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        string targetName = $"recovery-target-{NewId.NextGuid():N}";
        string fallbackName = $"recovery-fallback-{NewId.NextGuid():N}";
        Uri target = new(harness.BaseAddress, targetName);
        Uri fallback = new(harness.BaseAddress, fallbackName);
        Guid sagaId = NewId.NextGuid();
        Guid requestPayloadId = NewId.NextGuid();
        Guid responsePayloadId = NewId.NextGuid();
        var releaseFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factoryEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var delivered = new TaskCompletionSource<ConsumeContext<RepairRequest>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new ConcurrentQueue<string>();
        var original = new OriginalFailure("primary rejected");
        var factoryFailure = new RecoveryFactoryFailure("repair unavailable");
        var machine = new RecoveryMachine(fallback, original, factoryFailure, failFactory,
            releaseFactory.Task, factoryEntered, trace, timeout, requestPayloadId);
        harness.InMemoryBusConfiguring += bus =>
        {
            bus.ReceiveEndpoint(targetName, endpoint => endpoint.Handler<RepairRequest>(async context =>
            {
                trace.Enqueue("service");
                delivered.TrySetResult(context);
                await context.RespondAsync(new RepairAccepted(responsePayloadId, "repaired"));
            }));
            bus.ReceiveEndpoint(fallbackName, endpoint => endpoint.Handler<RepairRequest>(_ =>
                throw new InvalidOperationException("The configured fallback address must not receive the request.")));
        };
        ISagaStateMachineTestHarness<RecoveryMachine, RecoveryState> sagas =
            harness.AddSagaStateMachine<RecoveryMachine, RecoveryState>(machine);

        await harness.StartAsync(token).WaitAsync(timeout, token);
        try
        {
            Task<IConsumedMessage<Start>> consumed = sagas.Consumed.SelectAsync<Start>(token)
                .FirstObservedAsync(cancellationToken: token);
            await harness.InputQueueSendEndpoint.SendAsync(new Start(sagaId, target, "source-payload"), token);
            await factoryEntered.Task.WaitAsync(timeout, token);
            Assert.Equal(["provider", "factory-enter"], trace);
            Assert.False(delivered.Task.IsCompleted);
            Assert.Empty(harness.Sent.Snapshot<RepairRequest>());

            releaseFactory.SetResult();
            IConsumedMessage<Start> start = await consumed.WaitAsync(timeout, token);
            if (failFactory)
            {
                Exception cause = Assert.IsAssignableFrom<Exception>(start.Exception);
                while (cause is EventExecutionException wrapped)
                    cause = Assert.IsAssignableFrom<Exception>(wrapped.InnerException);
                Assert.Same(factoryFailure, cause);
                Assert.Equal(["provider", "factory-enter", "factory-fail"], trace);
                Assert.False(delivered.Task.IsCompleted);
                Assert.Empty(harness.Sent.Snapshot<RepairRequest>());
                Assert.Empty(harness.Published.Snapshot<RepairAccepted>());
                RecoveryState failedState = Assert.IsType<RecoveryState>(sagas.Sagas.FindById(sagaId));
                Assert.Equal(machine.Initial.Name, failedState.CurrentState);
                Assert.Equal(0, failedState.RepairCount);
                Assert.Equal(string.Empty, failedState.Result);
            }
            else
            {
                Assert.Null(start.Exception);
                ConsumeContext<RepairRequest> request = await delivered.Task.WaitAsync(timeout, token);
                Assert.Equal(sagaId, await sagas.WaitForSagaInStateAsync(sagaId, machine.Done, timeout, token));
                Assert.Equal(new RepairRequest(requestPayloadId, original.Message, "source-payload"), request.Message);
                Assert.Equal(target, request.DestinationAddress);
                Assert.Equal(harness.InputQueueAddress, request.ResponseAddress);
                Assert.Equal(sagaId, request.RequestId);
                RecoveryState state = Assert.IsType<RecoveryState>(sagas.Sagas.FindById(sagaId));
                Assert.Equal("repaired", state.Result);
                Assert.Equal(1, state.RepairCount);
                Assert.Equal(responsePayloadId, state.ResponsePayloadId);
                Assert.Equal(["provider", "factory-enter", "factory-return", "service", "response"], trace);
                Assert.Single(harness.Sent.Snapshot<RepairRequest>());
                Assert.Null(sagas.Sagas.FindById(requestPayloadId));
                Assert.Null(sagas.Sagas.FindById(responsePayloadId));
            }
        }
        finally
        {
            releaseFactory.TrySetResult();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed record Start(Guid CorrelationId, Uri Target, string Value) : ICorrelatedBy<Guid>;
    public sealed record RepairRequest(Guid CorrelationId, string Error, string Value);
    public sealed record RepairAccepted(Guid CorrelationId, string Result);
    public sealed class OriginalFailure(string message) : Exception(message);
    public sealed class RecoveryFactoryFailure(string message) : Exception(message);

    public sealed class RecoveryState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public Uri Target { get; set; } = null!;
        public string Result { get; set; } = string.Empty;
        public int RepairCount { get; set; }
        public Guid ResponsePayloadId { get; set; }
    }

    public sealed class RecoveryMachine : ViciOneServiceBusStateMachine<RecoveryState>
    {
        public RecoveryMachine(Uri fallback, OriginalFailure original, RecoveryFactoryFailure factoryFailure,
            bool failFactory, Task releaseFactory, TaskCompletionSource factoryEntered,
            ConcurrentQueue<string> trace, TimeSpan timeout, Guid requestPayloadId)
        {
            InstanceState(state => state.CurrentState);
            Request(() => Repair, request =>
            {
                request.ServiceAddress = fallback;
                request.Timeout = TimeSpan.Zero;
            });
            Initially(When(Started)
                .Then(context =>
                {
                    context.Saga.Target = context.Message.Target;
                    throw original;
                })
                .Catch<OriginalFailure>(caught => caught
                    .Request(Repair,
                        context =>
                        {
                            Assert.Same(original, context.Exception);
                            trace.Enqueue("provider");
                            return context.Saga.Target;
                        },
                        async context =>
                        {
                            Assert.Same(original, context.Exception);
                            trace.Enqueue("factory-enter");
                            factoryEntered.TrySetResult();
                            await releaseFactory.WaitAsync(timeout, context.CancellationToken);
                            if (failFactory)
                            {
                                trace.Enqueue("factory-fail");
                                throw factoryFailure;
                            }

                            trace.Enqueue("factory-return");
                            return new RepairRequest(requestPayloadId, context.Exception.Message, context.Message.Value);
                        })
                    .TransitionTo(Repair.Pending)));
            During(Repair.Pending, When(Repair.Completed)
                .Then(context =>
                {
                    context.Saga.Result = context.Message.Result;
                    context.Saga.RepairCount++;
                    context.Saga.ResponsePayloadId = context.Message.CorrelationId;
                    trace.Enqueue("response");
                })
                .TransitionTo(Done));
        }

        public IEvent<Start> Started { get; } = null!;
        public IRequest<RecoveryState, RepairRequest, RepairAccepted> Repair { get; } = null!;
        public IState Done { get; } = null!;
    }
}
