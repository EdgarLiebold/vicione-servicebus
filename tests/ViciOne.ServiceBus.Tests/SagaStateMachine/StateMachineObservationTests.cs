using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineObservationTests
{
    [Theory]
    [InlineData(MachineStyle.Declarative)]
    [InlineData(MachineStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OBSERVATION", "simple-complete-state-sequence")]
    public async Task SimpleMachine_ReportsTheCompleteStateSequenceAsync(MachineStyle style)
    {
        ObservationScenario scenario = CreateSimpleScenario(style);
        var instance = new ObservationInstance();
        var stateObserver = new StateRecorder();

        using (scenario.Machine.ConnectStateObserver(stateObserver))
        {
            await RaiseAsync(scenario.Machine, instance, scenario.Initialized);
            await RaiseAsync(scenario.Machine, instance, scenario.Finish);
        }

        Assert.Equal(
            new (string? Previous, string Current)[]
            {
                (null, scenario.Machine.Initial.Name),
                (scenario.Machine.Initial.Name, scenario.Running.Name),
                (scenario.Running.Name, scenario.Machine.Final.Name),
            },
            stateObserver.Changes.Select(change => (change.Previous?.Name, change.Current.Name)).ToArray());
        Assert.All(stateObserver.Changes, change => Assert.Same(instance, change.Instance));
        Assert.Same(scenario.Machine.Final, instance.CurrentState);
    }

    [Theory]
    [InlineData(MachineStyle.Declarative)]
    [InlineData(MachineStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OBSERVATION", "substate-events-and-complete-state-sequence")]
    public async Task SubstateMachine_ReportsEveryTransitionAndSelectedRaisedEventAsync(MachineStyle style)
    {
        ObservationScenario scenario = CreateSubstateScenario(style);
        var instance = new ObservationInstance();
        var stateObserver = new StateRecorder();
        var eventObserver = new EventRecorder();

        using (scenario.Machine.ConnectStateObserver(stateObserver))
        using (scenario.Machine.ConnectEventObserver(scenario.Initialized, eventObserver))
        using (scenario.Machine.ConnectEventObserver(scenario.LegCramped!, eventObserver))
        {
            await RaiseAsync(scenario.Machine, instance, scenario.Initialized);
            await RaiseAsync(scenario.Machine, instance, scenario.LegCramped!);
            await RaiseAsync(scenario.Machine, instance, scenario.Finish);
        }

        Assert.Equal(
            new (string? Previous, string Current)[]
            {
                (null, scenario.Machine.Initial.Name),
                (scenario.Machine.Initial.Name, scenario.Running.Name),
                (scenario.Running.Name, scenario.Resting!.Name),
                (scenario.Resting.Name, scenario.Machine.Final.Name),
            },
            stateObserver.Changes.Select(change => (change.Previous?.Name, change.Current.Name)).ToArray());
        Assert.Equal(
            new (string Event, ObservationPhase Phase)[]
            {
                (scenario.Initialized.Name, ObservationPhase.Pre),
                (scenario.Initialized.Name, ObservationPhase.Post),
                (scenario.LegCramped!.Name, ObservationPhase.Pre),
                (scenario.LegCramped.Name, ObservationPhase.Post),
            },
            eventObserver.Events.Select(entry => (entry.Event.Name, entry.Phase)).ToArray());
        Assert.All(stateObserver.Changes, change => Assert.Same(instance, change.Instance));
        Assert.All(eventObserver.Events, entry => Assert.Same(instance, entry.Instance));
        Assert.Same(scenario.Machine.Final, instance.CurrentState);
    }

    [Theory]
    [InlineData(MachineStyle.Declarative)]
    [InlineData(MachineStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OBSERVATION", "substate-return-and-transition-events")]
    public async Task ReturningFromSubstate_ReportsTheSuperstateAndTransitionEventsExactlyAsync(MachineStyle style)
    {
        ObservationScenario scenario = CreateSubstateScenario(style);
        var instance = new ObservationInstance();
        var stateObserver = new StateRecorder();
        var eventObserver = new EventRecorder();

        using (scenario.Machine.ConnectStateObserver(stateObserver))
        using (scenario.Machine.ConnectEventObserver(scenario.Running.BeforeEnter, eventObserver))
        using (scenario.Machine.ConnectEventObserver(scenario.Running.AfterLeave, eventObserver))
        {
            await RaiseAsync(scenario.Machine, instance, scenario.Initialized);
            await RaiseAsync(scenario.Machine, instance, scenario.LegCramped!);
            await RaiseAsync(scenario.Machine, instance, scenario.Recovered!);
            await RaiseAsync(scenario.Machine, instance, scenario.Finish);
        }

        Assert.Equal(
            new (string? Previous, string Current)[]
            {
                (null, scenario.Machine.Initial.Name),
                (scenario.Machine.Initial.Name, scenario.Running.Name),
                (scenario.Running.Name, scenario.Resting!.Name),
                (scenario.Resting.Name, scenario.Running.Name),
                (scenario.Running.Name, scenario.Machine.Final.Name),
            },
            stateObserver.Changes.Select(change => (change.Previous?.Name, change.Current.Name)).ToArray());
        Assert.Equal(
            new (string Event, ObservationPhase Phase)[]
            {
                (scenario.Running.BeforeEnter.Name, ObservationPhase.Pre),
                (scenario.Running.BeforeEnter.Name, ObservationPhase.Post),
                (scenario.Running.AfterLeave.Name, ObservationPhase.Pre),
                (scenario.Running.AfterLeave.Name, ObservationPhase.Post),
            },
            eventObserver.Events.Select(entry => (entry.Event.Name, entry.Phase)).ToArray());
        Assert.All(stateObserver.Changes, change => Assert.Same(instance, change.Instance));
        Assert.All(eventObserver.Events, entry => Assert.Same(instance, entry.Instance));
        Assert.Same(scenario.Machine.Final, instance.CurrentState);
    }

    private static ObservationScenario CreateSimpleScenario(MachineStyle style)
    {
        if (style == MachineStyle.Declarative)
        {
            var machine = new DeclarativeSimpleMachine();
            return new ObservationScenario(machine, machine.Running, null, machine.Initialized, null, null, machine.Finish);
        }

        State running = null!;
        Event initialized = null!;
        Event finish = null!;
        ViciOneServiceBusStateMachine<ObservationInstance> dynamicMachine = ViciOneServiceBusStateMachine<ObservationInstance>.New(builder => builder
            .State("Running", out running)
            .Event("Initialized", out initialized)
            .Event("Finish", out finish)
            .During(builder.Initial)
            .When(initialized, behavior => behavior.TransitionTo(running))
            .During(running)
            .When(finish, behavior => behavior.Finalize()));

        return new ObservationScenario(dynamicMachine, running, null, initialized, null, null, finish);
    }

    private static ObservationScenario CreateSubstateScenario(MachineStyle style)
    {
        if (style == MachineStyle.Declarative)
        {
            var machine = new DeclarativeSubstateMachine();
            return new ObservationScenario(
                machine,
                machine.Running,
                machine.Resting,
                machine.Initialized,
                machine.LegCramped,
                machine.Recovered,
                machine.Finish);
        }

        State<ObservationInstance> running = null!;
        State<ObservationInstance> resting = null!;
        Event initialized = null!;
        Event legCramped = null!;
        Event recovered = null!;
        Event finish = null!;
        ViciOneServiceBusStateMachine<ObservationInstance> dynamicMachine = ViciOneServiceBusStateMachine<ObservationInstance>.New(builder => builder
            .State("Running", out running)
            .Event("Initialized", out initialized)
            .Event("LegCramped", out legCramped)
            .Event("Recovered", out recovered)
            .Event("Finish", out finish)
            .SubState("Resting", running, out resting)
            .During(builder.Initial)
            .When(initialized, behavior => behavior.TransitionTo(running))
            .During(running)
            .When(legCramped, behavior => behavior.TransitionTo(resting))
            .When(finish, behavior => behavior.Finalize())
            .During(resting)
            .When(recovered, behavior => behavior.TransitionTo(running))
            .BeforeEnter(running, behavior => behavior.Then(_ => { }))
            .AfterLeave(running, behavior => behavior.Then(_ => { })));

        return new ObservationScenario(dynamicMachine, running, resting, initialized, legCramped, recovered, finish);
    }

    private static async Task RaiseAsync(StateMachine<ObservationInstance> machine, ObservationInstance instance, Event @event)
    {
        var message = new ObservationSignal();
        ConsumeContext<ObservationSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<ObservationInstance>(instance);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<ObservationInstance, ObservationSignal>(consumeContext, sagaInstance);
        BehaviorContext<ObservationInstance> behaviorContext =
            new ViciOneServiceBusStateMachine<ObservationInstance>.BehaviorContextProxy(machine, sagaContext, @event);

        await machine.RaiseEventAsync(behaviorContext);
    }

    public enum MachineStyle
    {
        Declarative,
        Dynamic,
    }

    private enum ObservationPhase
    {
        Pre,
        Post,
    }

    private sealed record ObservationScenario(
        ViciOneServiceBusStateMachine<ObservationInstance> Machine,
        State Running,
        State? Resting,
        Event Initialized,
        Event? LegCramped,
        Event? Recovered,
        Event Finish);

    public sealed record ObservationSignal;

    private sealed class ObservationInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public State? CurrentState { get; set; }
    }

    private sealed class DeclarativeSimpleMachine : ViciOneServiceBusStateMachine<ObservationInstance>
    {
        public DeclarativeSimpleMachine()
        {
            During(Initial, When(Initialized).TransitionTo(Running));
            During(Running, When(Finish).Finalize());
        }

        public State Running { get; private set; } = null!;

        public Event Initialized { get; private set; } = null!;

        public Event Finish { get; private set; } = null!;
    }

    private sealed class DeclarativeSubstateMachine : ViciOneServiceBusStateMachine<ObservationInstance>
    {
        public DeclarativeSubstateMachine()
        {
            SubState(() => Resting, Running);
            During(Initial, When(Initialized).TransitionTo(Running));
            During(Running, When(LegCramped).TransitionTo(Resting), When(Finish).Finalize());
            During(Resting, When(Recovered).TransitionTo(Running));
            BeforeEnter(Running, behavior => behavior.Then(_ => { }));
            AfterLeave(Running, behavior => behavior.Then(_ => { }));
        }

        public State Running { get; private set; } = null!;

        public State Resting { get; private set; } = null!;

        public Event Initialized { get; private set; } = null!;

        public Event LegCramped { get; private set; } = null!;

        public Event Recovered { get; private set; } = null!;

        public Event Finish { get; private set; } = null!;
    }

    private sealed class StateRecorder : IStateObserver<ObservationInstance>
    {
        public List<StateChange> Changes { get; } = [];

        public Task StateChangedAsync(BehaviorContext<ObservationInstance> context, State currentState, State? previousState)
        {
            Changes.Add(new StateChange(context.Saga, previousState, currentState));
            return Task.CompletedTask;
        }
    }

    private sealed class EventRecorder : IEventObserver<ObservationInstance>
    {
        public List<EventObservation> Events { get; } = [];

        public Task PreExecuteAsync(BehaviorContext<ObservationInstance> context)
        {
            Events.Add(new EventObservation(context.Saga, context.Event, ObservationPhase.Pre));
            return Task.CompletedTask;
        }

        public Task PreExecuteAsync<T>(BehaviorContext<ObservationInstance, T> context)
            where T : class
        {
            Events.Add(new EventObservation(context.Saga, context.Event, ObservationPhase.Pre));
            return Task.CompletedTask;
        }

        public Task PostExecuteAsync(BehaviorContext<ObservationInstance> context)
        {
            Events.Add(new EventObservation(context.Saga, context.Event, ObservationPhase.Post));
            return Task.CompletedTask;
        }

        public Task PostExecuteAsync<T>(BehaviorContext<ObservationInstance, T> context)
            where T : class
        {
            Events.Add(new EventObservation(context.Saga, context.Event, ObservationPhase.Post));
            return Task.CompletedTask;
        }

        public Task ExecuteFaultAsync(BehaviorContext<ObservationInstance> context, Exception exception) =>
            Task.FromException(new InvalidOperationException("The observation scenario did not expect an event fault.", exception));

        public Task ExecuteFaultAsync<T>(BehaviorContext<ObservationInstance, T> context, Exception exception)
            where T : class =>
            Task.FromException(new InvalidOperationException("The observation scenario did not expect an event fault.", exception));
    }

    private sealed record StateChange(ObservationInstance Instance, State? Previous, State Current);

    private sealed record EventObservation(ObservationInstance Instance, Event Event, ObservationPhase Phase);
}
