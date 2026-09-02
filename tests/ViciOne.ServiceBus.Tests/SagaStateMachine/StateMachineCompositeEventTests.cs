using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineCompositeEventTests
{
    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "constituent-status-truth-table")]
    public async Task CompositeEvent_RequiresEveryConstituentAndTracksTheExactStatus(
        StateMachineConstructionStyle style)
    {
        CompositeScenario scenario = CreatePlainScenario(style);
        var firstOnly = new CompositeInstance();
        var secondOnly = new CompositeInstance();
        var complete = new CompositeInstance();

        await Start(scenario, firstOnly);
        await StateMachineTestExecution.Raise(scenario.Machine, firstOnly, scenario.First);
        await Start(scenario, secondOnly);
        await StateMachineTestExecution.Raise(scenario.Machine, secondOnly, scenario.Second);
        await Start(scenario, complete);
        await StateMachineTestExecution.Raise(scenario.Machine, complete, scenario.First);
        await StateMachineTestExecution.Raise(scenario.Machine, complete, scenario.Second);

        Assert.Equal(1, firstOnly.Status.Bits);
        Assert.Equal(["first"], firstOnly.Markers);
        Assert.Equal(0, firstOnly.CompositeCount);
        Assert.Same(scenario.Waiting, await StateMachineTestExecution.GetState(scenario.Machine, firstOnly));

        Assert.Equal(2, secondOnly.Status.Bits);
        Assert.Equal(["second"], secondOnly.Markers);
        Assert.Equal(0, secondOnly.CompositeCount);
        Assert.Same(scenario.Waiting, await StateMachineTestExecution.GetState(scenario.Machine, secondOnly));

        Assert.Equal(3, complete.Status.Bits);
        Assert.Equal(["composite", "first", "second"], complete.Markers.Order().ToArray());
        Assert.Equal(1, complete.CompositeCount);
        Assert.Same(scenario.Machine.Final, await StateMachineTestExecution.GetState(scenario.Machine, complete));
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "constituent-order-and-condition")]
    public async Task CompositeEvent_RunsAfterConstituentActivitiesAndAppliesTheOrderCondition(
        StateMachineConstructionStyle style)
    {
        CompositeScenario scenario = CreateOrderedScenario(style);
        var secondFirst = new CompositeInstance();
        var firstFirst = new CompositeInstance();

        await Start(scenario, secondFirst);
        await StateMachineTestExecution.Raise(scenario.Machine, secondFirst, scenario.Second);
        await StateMachineTestExecution.Raise(scenario.Machine, secondFirst, scenario.First);

        await Start(scenario, firstFirst);
        await StateMachineTestExecution.Raise(scenario.Machine, firstFirst, scenario.First);
        await StateMachineTestExecution.Raise(scenario.Machine, firstFirst, scenario.Second);

        Assert.Equal(["second", "first", "composite"], secondFirst.Markers);
        Assert.True(secondFirst.SecondWasFirst);
        Assert.Equal(1, secondFirst.CompositeCount);
        Assert.Same(scenario.Machine.Final, await StateMachineTestExecution.GetState(scenario.Machine, secondFirst));

        Assert.Equal(["first", "second"], firstFirst.Markers);
        Assert.False(firstFirst.SecondWasFirst);
        Assert.Equal(0, firstFirst.CompositeCount);
        Assert.Equal(3, firstFirst.Status.Bits);
        Assert.Same(scenario.Waiting, await StateMachineTestExecution.GetState(scenario.Machine, firstFirst));
    }

    [Theory]
    [InlineData(CompositeEventOptions.RaiseOnce, 1)]
    [InlineData(CompositeEventOptions.None, 2)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "duplicate-option-exact-count")]
    public async Task CompositeEvent_DuplicateHandlingMatchesTheConfiguredOption(
        CompositeEventOptions options,
        int expectedCount)
    {
        DuplicateScenario scenario = CreateDuplicateScenario(options);
        var instance = new IntCompositeInstance();

        await StateMachineTestExecution.Raise(scenario.Machine, instance, scenario.Start);
        await StateMachineTestExecution.Raise(scenario.Machine, instance, scenario.First);

        Assert.Equal(1, instance.CompositeStatus);
        Assert.Equal(0, instance.CompositeCount);

        await StateMachineTestExecution.Raise(scenario.Machine, instance, scenario.Second);
        await StateMachineTestExecution.Raise(scenario.Machine, instance, scenario.Second);

        Assert.Equal(3, instance.CompositeStatus);
        Assert.Equal(expectedCount, instance.CompositeCount);
        Assert.Same(scenario.Waiting, await StateMachineTestExecution.GetState(scenario.Machine, instance));
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "assigned-int-struct-next-event-surface")]
    public async Task AssignedComposite_ReportsTheExactIntStructAndNextEventSurface(
        StateMachineConstructionStyle style)
    {
        StructAssignedScenario structScenario = CreateStructAssignedScenario(style);
        IntAssignedScenario intScenario = CreateIntAssignedScenario(style);
        var structInstance = new CompositeInstance();
        var intInstance = new IntCompositeInstance();

        AssertEventSurface(structScenario.Machine, structScenario.Waiting, structScenario.Start,
            structScenario.First, structScenario.Second, structScenario.Third);
        AssertEventSurface(intScenario.Machine, intScenario.Waiting, intScenario.Start,
            intScenario.First, intScenario.Second, intScenario.Third);

        await StateMachineTestExecution.Raise(structScenario.Machine, structInstance, structScenario.Start);
        await StateMachineTestExecution.Raise(structScenario.Machine, structInstance, structScenario.First);
        Assert.Equal(0, structInstance.CompositeCount);
        Assert.Same(structScenario.Waiting, structInstance.CurrentState);
        await StateMachineTestExecution.Raise(structScenario.Machine, structInstance, structScenario.Second);

        await StateMachineTestExecution.Raise(intScenario.Machine, intInstance, intScenario.Start);
        Assert.Equal(3, intInstance.CurrentState);
        await StateMachineTestExecution.Raise(intScenario.Machine, intInstance, intScenario.First);
        Assert.Equal(0, intInstance.CompositeCount);
        await StateMachineTestExecution.Raise(intScenario.Machine, intInstance, intScenario.Second);

        Assert.Equal(1, structInstance.CompositeCount);
        Assert.Same(structScenario.Machine.Final, structInstance.CurrentState);
        Assert.Empty(structScenario.Machine.NextEvents(structScenario.Machine.GetState("Final")));

        Assert.Equal(3, intInstance.CompositeStatus);
        Assert.Equal(1, intInstance.CompositeCount);
        Assert.True(intInstance.FirstHandledBeforeComposite);
        Assert.Equal(2, intInstance.CurrentState);
        Assert.Empty(intScenario.Machine.NextEvents(intScenario.Machine.GetState("Final")));
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative, true)]
    [InlineData(StateMachineConstructionStyle.Declarative, false)]
    [InlineData(StateMachineConstructionStyle.Dynamic, true)]
    [InlineData(StateMachineConstructionStyle.Dynamic, false)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "cross-state-declaration-order")]
    public async Task CompositeEvent_AcrossStatesWorksAtEitherDeclarationPoint(
        StateMachineConstructionStyle style,
        bool declareBeforeBindings)
    {
        CrossStateScenario scenario = CreateCrossStateScenario(style, declareBeforeBindings);
        var instance = new CrossStateInstance();

        await StateMachineTestExecution.Raise(scenario.Machine, instance, scenario.Start);
        await StateMachineTestExecution.Raise(scenario.Machine, instance, scenario.First);

        Assert.Same(scenario.WaitingForSecond, await StateMachineTestExecution.GetState(scenario.Machine, instance));
        Assert.Equal(["first"], instance.Markers);
        Assert.Equal(0, instance.CompositeCount);

        await StateMachineTestExecution.Raise(scenario.Machine, instance, scenario.Second);

        Assert.Equal(["first", "composite"], instance.Markers);
        Assert.Equal(1, instance.CompositeCount);
        Assert.Same(scenario.Machine.Final, await StateMachineTestExecution.GetState(scenario.Machine, instance));
    }

    private static async Task Start(CompositeScenario scenario, CompositeInstance instance) =>
        await StateMachineTestExecution.Raise(scenario.Machine, instance, scenario.Start);

    private static void AssertEventSurface<TInstance>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        State waiting,
        Event start,
        Event first,
        Event second,
        Event third)
        where TInstance : class, SagaStateMachineInstance
    {
        Assert.Equal([start.Name], machine.NextEvents(machine.Initial).Select(x => x.Name).Order().ToArray());
        Assert.Equal([start.Name], machine.NextEvents(machine.GetState("Initial")).Select(x => x.Name).Order().ToArray());
        Assert.Equal(
            [first.Name, second.Name, third.Name],
            machine.NextEvents(waiting).Select(x => x.Name).Order().ToArray());
        Assert.Equal(
            [first.Name, second.Name, third.Name],
            machine.NextEvents(machine.GetState(waiting.Name)).Select(x => x.Name).Order().ToArray());
        Assert.Empty(machine.NextEvents(machine.Final));
        Assert.True(machine.IsCompositeEvent(third));
        Assert.False(machine.IsCompositeEvent(first));
        Assert.False(machine.IsCompositeEvent(second));
    }

    private static CompositeScenario CreatePlainScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativePlainCompositeMachine();
            return new CompositeScenario(declarativeMachine, declarativeMachine.Waiting, declarativeMachine.Start,
                declarativeMachine.First, declarativeMachine.Second, declarativeMachine.Third);
        }

        State waiting = null!;
        Event start = null!;
        Event first = null!;
        Event second = null!;
        Event third = null!;
        ViciOneServiceBusStateMachine<CompositeInstance> machine = ViciOneServiceBusStateMachine<CompositeInstance>.New(builder => builder
            .State("Waiting", out waiting)
            .Event("Start", out start)
            .Event("First", out first)
            .Event("Second", out second)
            .InstanceState(instance => instance.CurrentState!)
            .CompositeEvent("Third", out third, instance => instance.Status, first, second)
            .Initially()
            .When(start, behavior => behavior.TransitionTo(waiting))
            .During(waiting)
            .When(first, behavior => behavior.Then(context => context.Saga.Markers.Add("first")))
            .When(second, behavior => behavior.Then(context => context.Saga.Markers.Add("second")))
            .When(third, behavior => behavior
                .Then(context => MarkComposite(context.Saga))
                .Finalize()));

        return new CompositeScenario(machine, waiting, start, first, second, third);
    }

    private static CompositeScenario CreateOrderedScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeOrderedCompositeMachine();
            return new CompositeScenario(declarativeMachine, declarativeMachine.Waiting, declarativeMachine.Start,
                declarativeMachine.First, declarativeMachine.Second, declarativeMachine.Third);
        }

        State waiting = null!;
        Event start = null!;
        Event first = null!;
        Event second = null!;
        Event third = null!;
        ViciOneServiceBusStateMachine<CompositeInstance> machine = ViciOneServiceBusStateMachine<CompositeInstance>.New(builder => builder
            .State("Waiting", out waiting)
            .Event("Start", out start)
            .Event("First", out first)
            .Event("Second", out second)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(start, behavior => behavior.TransitionTo(waiting))
            .During(waiting)
            .When(first, behavior => behavior.Then(context => MarkFirst(context.Saga)))
            .When(second, behavior => behavior.Then(context => MarkSecond(context.Saga)))
            .CompositeEvent("Third", out third, instance => instance.Status, first, second)
            .During(waiting)
            .When(third, context => context.Saga.SecondWasFirst, behavior => behavior
                .Then(context => MarkComposite(context.Saga))
                .Finalize()));

        return new CompositeScenario(machine, waiting, start, first, second, third);
    }

    private static DuplicateScenario CreateDuplicateScenario(CompositeEventOptions options)
    {
        var machine = new DeclarativeDuplicateCompositeMachine(options);
        return new DuplicateScenario(machine, machine.Waiting, machine.Start, machine.First, machine.Second);
    }

    private static StructAssignedScenario CreateStructAssignedScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeStructAssignedMachine();
            return new StructAssignedScenario(declarativeMachine, declarativeMachine.Waiting, declarativeMachine.Start,
                declarativeMachine.First, declarativeMachine.Second, declarativeMachine.Third);
        }

        State waiting = null!;
        Event start = null!;
        Event first = null!;
        Event second = null!;
        Event third = null!;
        ViciOneServiceBusStateMachine<CompositeInstance> machine = ViciOneServiceBusStateMachine<CompositeInstance>.New(builder => builder
            .State("Waiting", out waiting)
            .Event("Start", out start)
            .Event("First", out first)
            .Event("Second", out second)
            .InstanceState(instance => instance.CurrentState!)
            .CompositeEvent("Third", out third, instance => instance.Status, first, second)
            .Initially()
            .When(start, behavior => behavior.TransitionTo(waiting))
            .During(waiting)
            .When(third, behavior => behavior
                .Then(context => MarkComposite(context.Saga))
                .Finalize()));

        return new StructAssignedScenario(machine, waiting, start, first, second, third);
    }

    private static IntAssignedScenario CreateIntAssignedScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeIntAssignedMachine();
            return new IntAssignedScenario(declarativeMachine, declarativeMachine.Waiting, declarativeMachine.Start,
                declarativeMachine.First, declarativeMachine.Second, declarativeMachine.Third);
        }

        State waiting = null!;
        Event start = null!;
        Event first = null!;
        Event second = null!;
        Event third = null!;
        ViciOneServiceBusStateMachine<IntCompositeInstance> machine = ViciOneServiceBusStateMachine<IntCompositeInstance>.New(builder => builder
            .State("Waiting", out waiting)
            .Event("Start", out start)
            .Event("First", out first)
            .Event("Second", out second)
            .InstanceState(instance => instance.CurrentState)
            .CompositeEvent("Third", out third, instance => instance.CompositeStatus, first, second)
            .Initially()
            .When(start, behavior => behavior.TransitionTo(waiting))
            .During(waiting)
            .When(first, behavior => behavior.Then(context => context.Saga.FirstHandled = true))
            .When(third, behavior => behavior
                .Then(context => MarkIntComposite(context.Saga))
                .Finalize()));

        return new IntAssignedScenario(machine, waiting, start, first, second, third);
    }

    private static CrossStateScenario CreateCrossStateScenario(
        StateMachineConstructionStyle style,
        bool declareBeforeBindings)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeCrossStateMachine(declareBeforeBindings);
            return new CrossStateScenario(declarativeMachine, declarativeMachine.Waiting, declarativeMachine.WaitingForSecond,
                declarativeMachine.Start, declarativeMachine.First, declarativeMachine.Second);
        }

        State waiting = null!;
        State waitingForSecond = null!;
        Event start = null!;
        Event first = null!;
        Event second = null!;
        Event third = null!;
        ViciOneServiceBusStateMachine<CrossStateInstance> machine = ViciOneServiceBusStateMachine<CrossStateInstance>.New(builder =>
        {
            builder
                .State("Waiting", out waiting)
                .State("WaitingForSecond", out waitingForSecond)
                .Event("Start", out start)
                .Event("First", out first)
                .Event("Second", out second)
                .InstanceState(instance => instance.CurrentState);

            if (declareBeforeBindings)
                builder.CompositeEvent("Third", out third, instance => instance.CompositeStatus, first, second);

            builder.Initially().When(start, behavior => behavior.TransitionTo(waiting));
            builder.During(waiting).When(first, behavior => behavior
                .Then(context => context.Saga.Markers.Add("first"))
                .TransitionTo(waitingForSecond));

            if (!declareBeforeBindings)
                builder.CompositeEvent("Third", out third, instance => instance.CompositeStatus, first, second);

            builder.During(waitingForSecond).When(third, behavior => behavior
                .Then(context =>
                {
                    context.Saga.CompositeCount++;
                    context.Saga.Markers.Add("composite");
                })
                .Finalize());
        });

        return new CrossStateScenario(machine, waiting, waitingForSecond, start, first, second);
    }

    private static void MarkComposite(CompositeInstance instance)
    {
        instance.CompositeCount++;
        instance.Markers.Add("composite");
    }

    private static void MarkFirst(CompositeInstance instance)
    {
        instance.FirstSeen = true;
        instance.Markers.Add("first");
    }

    private static void MarkSecond(CompositeInstance instance)
    {
        instance.SecondWasFirst = !instance.FirstSeen;
        instance.Markers.Add("second");
    }

    private static void MarkIntComposite(IntCompositeInstance instance)
    {
        instance.CompositeCount++;
        instance.FirstHandledBeforeComposite = instance.FirstHandled;
    }

    private sealed class DeclarativePlainCompositeMachine : ViciOneServiceBusStateMachine<CompositeInstance>
    {
        public DeclarativePlainCompositeMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            CompositeEvent(() => Third, instance => instance.Status, First, Second);
            Initially(When(Start).TransitionTo(Waiting));
            During(Waiting,
                When(First).Then(context => context.Saga.Markers.Add("first")),
                When(Second).Then(context => context.Saga.Markers.Add("second")),
                When(Third).Then(context => MarkComposite(context.Saga)).Finalize());
        }

        public State Waiting { get; private set; } = null!;
        public Event Start { get; private set; } = null!;
        public Event First { get; private set; } = null!;
        public Event Second { get; private set; } = null!;
        public Event Third { get; private set; } = null!;
    }

    private sealed class DeclarativeOrderedCompositeMachine : ViciOneServiceBusStateMachine<CompositeInstance>
    {
        public DeclarativeOrderedCompositeMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(When(Start).TransitionTo(Waiting));
            During(Waiting,
                When(First).Then(context => MarkFirst(context.Saga)),
                When(Second).Then(context => MarkSecond(context.Saga)));
            CompositeEvent(() => Third, instance => instance.Status, First, Second);
            During(Waiting, When(Third, context => context.Saga.SecondWasFirst)
                .Then(context => MarkComposite(context.Saga))
                .Finalize());
        }

        public State Waiting { get; private set; } = null!;
        public Event Start { get; private set; } = null!;
        public Event First { get; private set; } = null!;
        public Event Second { get; private set; } = null!;
        public Event Third { get; private set; } = null!;
    }

    private sealed class DeclarativeDuplicateCompositeMachine : ViciOneServiceBusStateMachine<IntCompositeInstance>
    {
        public DeclarativeDuplicateCompositeMachine(CompositeEventOptions options)
        {
            InstanceState(instance => instance.CurrentState, Waiting);
            CompositeEvent(() => Third, instance => instance.CompositeStatus, options, First, Second);
            Initially(When(Start).TransitionTo(Waiting));
            During(Waiting, When(Third).Then(context => context.Saga.CompositeCount++));
        }

        public State Waiting { get; private set; } = null!;
        public Event Start { get; private set; } = null!;
        public Event First { get; private set; } = null!;
        public Event Second { get; private set; } = null!;
        public Event Third { get; private set; } = null!;
    }

    private sealed class DeclarativeStructAssignedMachine : ViciOneServiceBusStateMachine<CompositeInstance>
    {
        public DeclarativeStructAssignedMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            CompositeEvent(() => Third, instance => instance.Status, First, Second);
            Initially(When(Start).TransitionTo(Waiting));
            During(Waiting, When(Third).Then(context => MarkComposite(context.Saga)).Finalize());
        }

        public State Waiting { get; private set; } = null!;
        public Event Start { get; private set; } = null!;
        public Event First { get; private set; } = null!;
        public Event Second { get; private set; } = null!;
        public Event Third { get; private set; } = null!;
    }

    private sealed class DeclarativeIntAssignedMachine : ViciOneServiceBusStateMachine<IntCompositeInstance>
    {
        public DeclarativeIntAssignedMachine()
        {
            InstanceState(instance => instance.CurrentState);
            CompositeEvent(() => Third, instance => instance.CompositeStatus, First, Second);
            Initially(When(Start).TransitionTo(Waiting));
            During(Waiting,
                When(First).Then(context => context.Saga.FirstHandled = true),
                When(Third).Then(context => MarkIntComposite(context.Saga)).Finalize());
        }

        public State Waiting { get; private set; } = null!;
        public Event Start { get; private set; } = null!;
        public Event First { get; private set; } = null!;
        public Event Second { get; private set; } = null!;
        public Event Third { get; private set; } = null!;
    }

    private sealed class DeclarativeCrossStateMachine : ViciOneServiceBusStateMachine<CrossStateInstance>
    {
        public DeclarativeCrossStateMachine(bool declareBeforeBindings)
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Second);

            if (declareBeforeBindings)
                CompositeEvent(() => Third, instance => instance.CompositeStatus, First, Second);

            Initially(When(Start).TransitionTo(Waiting));
            During(Waiting, When(First)
                .Then(context => context.Saga.Markers.Add("first"))
                .TransitionTo(WaitingForSecond));

            if (!declareBeforeBindings)
                CompositeEvent(() => Third, instance => instance.CompositeStatus, First, Second);

            During(WaitingForSecond, When(Third)
                .Then(context =>
                {
                    context.Saga.CompositeCount++;
                    context.Saga.Markers.Add("composite");
                })
                .Finalize());
        }

        public State Waiting { get; private set; } = null!;
        public State WaitingForSecond { get; private set; } = null!;
        public Event Start { get; private set; } = null!;
        public Event First { get; private set; } = null!;
        public Event Second { get; private set; } = null!;
        public Event Third { get; private set; } = null!;
    }

    private sealed class CompositeInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public State? CurrentState { get; set; }
        public CompositeEventStatus Status { get; set; }
        public int CompositeCount { get; set; }
        public bool FirstSeen { get; set; }
        public bool SecondWasFirst { get; set; }
        public List<string> Markers { get; } = [];
    }

    private sealed class IntCompositeInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public int CurrentState { get; set; }
        public int CompositeStatus { get; set; }
        public int CompositeCount { get; set; }
        public bool FirstHandled { get; set; }
        public bool FirstHandledBeforeComposite { get; set; }
    }

    private sealed class CrossStateInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public int CurrentState { get; set; }
        public int CompositeStatus { get; set; }
        public int CompositeCount { get; set; }
        public List<string> Markers { get; } = [];
    }

    private sealed record CompositeScenario(
        ViciOneServiceBusStateMachine<CompositeInstance> Machine,
        State Waiting,
        Event Start,
        Event First,
        Event Second,
        Event Third);

    private sealed record DuplicateScenario(
        ViciOneServiceBusStateMachine<IntCompositeInstance> Machine,
        State Waiting,
        Event Start,
        Event First,
        Event Second);

    private sealed record StructAssignedScenario(
        ViciOneServiceBusStateMachine<CompositeInstance> Machine,
        State Waiting,
        Event Start,
        Event First,
        Event Second,
        Event Third);

    private sealed record IntAssignedScenario(
        ViciOneServiceBusStateMachine<IntCompositeInstance> Machine,
        State Waiting,
        Event Start,
        Event First,
        Event Second,
        Event Third);

    private sealed record CrossStateScenario(
        ViciOneServiceBusStateMachine<CrossStateInstance> Machine,
        State Waiting,
        State WaitingForSecond,
        Event Start,
        Event First,
        Event Second);
}
