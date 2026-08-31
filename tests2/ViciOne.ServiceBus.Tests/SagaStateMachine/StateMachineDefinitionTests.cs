using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineDefinitionTests
{
    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-DEFINITION", "exact-state-event-and-next-event-surface")]
    public async Task Definition_InitializesAndEnumeratesTheExactStateEventAndReachableEventSurface(
        StateMachineConstructionStyle style)
    {
        DefinitionScenario scenario = CreateDefinitionScenario(style);
        var instance = new DefinitionInstance();

        await StateMachineTestExecution.Raise(scenario.Machine, instance, scenario.Start);

        Assert.Equal(typeof(DefinitionInstance), ((StateMachine)scenario.Machine).InstanceType);
        Assert.Equal(
            ["Final", "Initial", "Loved", "Pissed", "Running"],
            scenario.Machine.States.Select(state => state.Name).Order().ToArray());
        Assert.Equal(
            ["Finish", "Handshake", "Ignored", "Start"],
            scenario.Machine.Events.Select(@event => @event.Name).Order().ToArray());
        Assert.Equal(
            ["Finish", "Handshake", "Ignored"],
            scenario.Machine.NextEvents(scenario.Running).Select(@event => @event.Name).Order().ToArray());
        Assert.Same(scenario.Running, instance.CurrentState);
        Assert.Same(scenario.Machine.Initial, scenario.Machine.GetState("Initial"));
        Assert.Same(scenario.Machine.Final, scenario.Machine.GetState("Final"));
        Assert.Same(scenario.Running, scenario.Machine.GetState("Running"));
        Assert.IsType<ViciOneServiceBusStateMachine<DefinitionInstance>.StateMachineState>(scenario.Machine.Initial);
        Assert.All(
            [scenario.Machine.Initial, scenario.Machine.Final, scenario.Running, scenario.Loved, scenario.Pissed],
            state => Assert.Contains(state, scenario.Machine.States));
        Assert.All(
            [scenario.Start, scenario.Finish, scenario.Handshake, scenario.Ignored],
            @event => Assert.Contains(@event, scenario.Machine.Events));
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-DEFINITION", "independent-instance-state-properties")]
    public async Task TwoMachines_KeepIndependentStatePropertiesOnTheSameSaga(
        StateMachineConstructionStyle style)
    {
        IndependentStateScenario scenario = CreateIndependentStateScenario(style);
        var instance = new IndependentStateInstance();

        await StateMachineTestExecution.Raise(scenario.TopMachine, instance, scenario.TopStart);

        Assert.Same(scenario.TopRunning, instance.TopState);
        Assert.Null(instance.BottomState);

        await StateMachineTestExecution.Raise(scenario.BottomMachine, instance, scenario.BottomStart);

        Assert.Same(scenario.TopRunning, instance.TopState);
        Assert.Same(scenario.BottomRunning, instance.BottomState);
        Assert.NotSame(instance.TopState, instance.BottomState);
    }

    private static DefinitionScenario CreateDefinitionScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeDefinitionMachine();
            return new DefinitionScenario(
                declarativeMachine,
                declarativeMachine.Running,
                declarativeMachine.Loved,
                declarativeMachine.Pissed,
                declarativeMachine.Start,
                declarativeMachine.Finish,
                declarativeMachine.Handshake,
                declarativeMachine.Ignored);
        }

        State running = null!;
        State loved = null!;
        State pissed = null!;
        Event start = null!;
        Event finish = null!;
        Event<HandshakeData> handshake = null!;
        Event<IgnoredData> ignored = null!;
        ViciOneServiceBusStateMachine<DefinitionInstance> machine = ViciOneServiceBusStateMachine<DefinitionInstance>.New(builder => builder
            .State("Running", out running)
            .State("Loved", out loved)
            .State("Pissed", out pissed)
            .Event("Start", out start)
            .Event("Finish", out finish)
            .Event("Handshake", out handshake)
            .Event("Ignored", out ignored)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(start, behavior => behavior.TransitionTo(running))
            .During(running)
            .When(handshake, behavior => behavior.TransitionTo(loved))
            .Ignore(ignored)
            .DuringAny()
            .When(finish, behavior => behavior.Finalize()));

        return new DefinitionScenario(machine, running, loved, pissed, start, finish, handshake, ignored);
    }

    private static IndependentStateScenario CreateIndependentStateScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeTopMachine = new DeclarativeTopMachine();
            var declarativeBottomMachine = new DeclarativeBottomMachine();
            return new IndependentStateScenario(
                declarativeTopMachine,
                declarativeTopMachine.Running,
                declarativeTopMachine.Start,
                declarativeBottomMachine,
                declarativeBottomMachine.Running,
                declarativeBottomMachine.Start);
        }

        State topRunning = null!;
        Event topStart = null!;
        ViciOneServiceBusStateMachine<IndependentStateInstance> topMachine =
            ViciOneServiceBusStateMachine<IndependentStateInstance>.New(builder => builder
                .State("TopRunning", out topRunning)
                .Event("TopStart", out topStart)
                .InstanceState(instance => instance.TopState!)
                .Initially()
                .When(topStart, behavior => behavior.TransitionTo(topRunning)));

        State bottomRunning = null!;
        Event bottomStart = null!;
        ViciOneServiceBusStateMachine<IndependentStateInstance> bottomMachine =
            ViciOneServiceBusStateMachine<IndependentStateInstance>.New(builder => builder
                .State("BottomRunning", out bottomRunning)
                .Event("BottomStart", out bottomStart)
                .InstanceState(instance => instance.BottomState!)
                .Initially()
                .When(bottomStart, behavior => behavior.TransitionTo(bottomRunning)));

        return new IndependentStateScenario(topMachine, topRunning, topStart, bottomMachine, bottomRunning, bottomStart);
    }

    private sealed record DefinitionScenario(
        ViciOneServiceBusStateMachine<DefinitionInstance> Machine,
        State Running,
        State Loved,
        State Pissed,
        Event Start,
        Event Finish,
        Event<HandshakeData> Handshake,
        Event<IgnoredData> Ignored);

    private sealed record IndependentStateScenario(
        ViciOneServiceBusStateMachine<IndependentStateInstance> TopMachine,
        State TopRunning,
        Event TopStart,
        ViciOneServiceBusStateMachine<IndependentStateInstance> BottomMachine,
        State BottomRunning,
        Event BottomStart);

    public sealed record HandshakeData;

    public sealed record IgnoredData;

    private sealed class DefinitionInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public State? CurrentState { get; set; }
    }

    private sealed class IndependentStateInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public State? TopState { get; set; }

        public State? BottomState { get; set; }
    }

    private sealed class DeclarativeDefinitionMachine : ViciOneServiceBusStateMachine<DefinitionInstance>
    {
        public DeclarativeDefinitionMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(When(Start).TransitionTo(Running));
            During(Running, When(Handshake).TransitionTo(Loved), Ignore(Ignored));
            DuringAny(When(Finish).Finalize());
        }

        public State Running { get; private set; } = null!;

        public State Loved { get; private set; } = null!;

        public State Pissed { get; private set; } = null!;

        public Event Start { get; private set; } = null!;

        public Event Finish { get; private set; } = null!;

        public Event<HandshakeData> Handshake { get; private set; } = null!;

        public Event<IgnoredData> Ignored { get; private set; } = null!;
    }

    private sealed class DeclarativeTopMachine : ViciOneServiceBusStateMachine<IndependentStateInstance>
    {
        public DeclarativeTopMachine()
        {
            InstanceState(instance => instance.TopState!);
            Initially(When(Start).TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;

        public Event Start { get; private set; } = null!;
    }

    private sealed class DeclarativeBottomMachine : ViciOneServiceBusStateMachine<IndependentStateInstance>
    {
        public DeclarativeBottomMachine()
        {
            InstanceState(instance => instance.BottomState!);
            Initially(When(Start).TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;

        public Event Start { get; private set; } = null!;
    }
}
