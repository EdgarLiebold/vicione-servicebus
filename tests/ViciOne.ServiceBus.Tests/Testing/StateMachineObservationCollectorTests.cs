using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Internal;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class StateMachineObservationCollectorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-STATE-MACHINE-OBSERVATION", "untyped-event-lifecycle-and-fault")]
    public async Task UntypedEventCallbacks_RecordStartedCompletedAndExactFaultedObservationsAsync()
    {
        var machine = new CollectorStateMachine();
        var instance = new CollectorState { CorrelationId = NewId.NextGuid() };
        var sagaInstance = new ViciOne.ServiceBus.Saga.SagaInstance<CollectorState>(instance);
        ConsumeContext<CollectorSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new CollectorSignal(),
            TestContext.Current.CancellationToken);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<CollectorState, CollectorSignal>(consumeContext, sagaInstance);
        BehaviorContext<CollectorState> context =
            new ViciOneServiceBusStateMachine<CollectorState>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);
        var collector = new StateMachineObservationCollector<CollectorState>(TestContextSaveMode.All, 8);
        var expected = new InvalidOperationException("untyped event failed");

        await collector.PreExecuteAsync(context);
        await collector.PostExecuteAsync(context);
        await collector.ExecuteFaultAsync(context, expected);

        Assert.Collection(
            collector.Events,
            started => AssertObservation(started, instance.CorrelationId, machine.Initial.Enter.Name,
                StateMachineEventExecutionStatus.Started, null),
            completed => AssertObservation(completed, instance.CorrelationId, machine.Initial.Enter.Name,
                StateMachineEventExecutionStatus.Completed, null),
            faulted => AssertObservation(faulted, instance.CorrelationId, machine.Initial.Enter.Name,
                StateMachineEventExecutionStatus.Faulted, expected));
    }

    private static void AssertObservation(
        StateMachineEventObservation observation,
        Guid expectedSagaId,
        string expectedEventName,
        StateMachineEventExecutionStatus expectedStatus,
        Exception? expectedException)
    {
        Assert.Equal(expectedSagaId, observation.SagaId);
        Assert.Equal(expectedEventName, observation.EventName);
        Assert.Null(observation.DataType);
        Assert.Equal(expectedStatus, observation.Status);
        Assert.Same(expectedException, observation.Exception);
    }

    private sealed record CollectorSignal;

    private sealed class CollectorState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    private sealed class CollectorStateMachine : ViciOneServiceBusStateMachine<CollectorState>
    {
        public CollectorStateMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }
    }
}
