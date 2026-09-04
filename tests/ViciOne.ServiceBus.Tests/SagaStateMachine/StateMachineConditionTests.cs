using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public enum ConditionEvaluationStyle
{
    Synchronous,
    Asynchronous,
}

public sealed class StateMachineConditionTests
{
    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative, ConditionEvaluationStyle.Synchronous)]
    [InlineData(StateMachineConstructionStyle.Declarative, ConditionEvaluationStyle.Asynchronous)]
    [InlineData(StateMachineConstructionStyle.Dynamic, ConditionEvaluationStyle.Synchronous)]
    [InlineData(StateMachineConstructionStyle.Dynamic, ConditionEvaluationStyle.Asynchronous)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONDITION", "sync-async-complete-branch-matrix")]
    public async Task Conditions_SelectExactlyOneBranchAndControlTheEnterTransitionAsync(
        StateMachineConstructionStyle constructionStyle,
        ConditionEvaluationStyle evaluationStyle)
    {
        ConditionScenario scenario = CreateConditionScenario(constructionStyle, evaluationStyle);
        var normal = new ConditionInstance();
        var initializeOnly = new ConditionInstance();
        var falseBranch = new ConditionInstance();
        var trueBranch = new ConditionInstance();

        await RaiseAsync(scenario.Machine, normal, scenario.Started, new StartSignal(false));
        await RaiseAsync(scenario.Machine, initializeOnly, scenario.Started, new StartSignal(true));
        await RaiseAsync(scenario.Machine, falseBranch, scenario.Explicit, new ExplicitSignal(false));
        await RaiseAsync(scenario.Machine, trueBranch, scenario.Explicit, new ExplicitSignal(true));

        Assert.Same(scenario.Running, normal.CurrentState);
        Assert.Equal((Start: 1, Enter: 1, If: 0, Continue: 1), normal.StartSnapshot);

        Assert.Same(scenario.Initialized, initializeOnly.CurrentState);
        Assert.Equal((Start: 1, Enter: 1, If: 1, Continue: 1), initializeOnly.StartSnapshot);

        Assert.Same(scenario.Machine.Initial, falseBranch.CurrentState);
        Assert.Equal((Predicate: 1, Then: 0, Else: 1), falseBranch.ExplicitSnapshot);

        Assert.Same(scenario.ShouldNotBeHere, trueBranch.CurrentState);
        Assert.Equal((Predicate: 1, Then: 1, Else: 0), trueBranch.ExplicitSnapshot);
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONDITION", "mutually-exclusive-event-filter-routes")]
    public async Task EventFilters_EvaluateBothPredicatesAndSelectExactlyOneRouteAsync(
        StateMachineConstructionStyle style)
    {
        FilterScenario scenario = CreateFilterScenario(style);
        var trueInstance = new ConditionInstance();
        var falseInstance = new ConditionInstance();

        await RaiseAsync(scenario.Machine, trueInstance, scenario.Event, new FilterSignal(true));
        await RaiseAsync(scenario.Machine, falseInstance, scenario.Event, new FilterSignal(false));

        Assert.Same(scenario.True, trueInstance.CurrentState);
        Assert.Equal((TruePredicate: 1, FalsePredicate: 1, TrueRoute: 1, FalseRoute: 0), trueInstance.FilterSnapshot);

        Assert.Same(scenario.False, falseInstance.CurrentState);
        Assert.Equal((TruePredicate: 1, FalsePredicate: 1, TrueRoute: 0, FalseRoute: 1), falseInstance.FilterSnapshot);
    }

    private static ConditionScenario CreateConditionScenario(
        StateMachineConstructionStyle constructionStyle,
        ConditionEvaluationStyle evaluationStyle)
    {
        bool useAsync = evaluationStyle == ConditionEvaluationStyle.Asynchronous;
        if (constructionStyle == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeConditionMachine(useAsync);
            return new ConditionScenario(
                declarativeMachine,
                declarativeMachine.Running,
                declarativeMachine.Initialized,
                declarativeMachine.ShouldNotBeHere,
                declarativeMachine.Started,
                declarativeMachine.Explicit);
        }

        State running = null!;
        State initialized = null!;
        State shouldNotBeHere = null!;
        Event<StartSignal> started = null!;
        Event<ExplicitSignal> explicitEvent = null!;
        ViciOneServiceBusStateMachine<ConditionInstance> machine = ViciOneServiceBusStateMachine<ConditionInstance>.New(builder => builder
            .State("Running", out running)
            .State("Initialized", out initialized)
            .State("ShouldNotBeHere", out shouldNotBeHere)
            .Event("Started", out started)
            .Event("Explicit", out explicitEvent)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(started, behavior => ConfigureStarted(behavior, initialized, useAsync))
            .When(explicitEvent, _ => true, behavior => ConfigureExplicit(behavior, shouldNotBeHere, useAsync))
            .WhenEnter(initialized, behavior => ConfigureEnter(behavior, running, useAsync)));

        return new ConditionScenario(machine, running, initialized, shouldNotBeHere, started, explicitEvent);
    }

    private static FilterScenario CreateFilterScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeFilterMachine();
            return new FilterScenario(
                declarativeMachine,
                declarativeMachine.True,
                declarativeMachine.False,
                declarativeMachine.Filter);
        }

        State trueState = null!;
        State falseState = null!;
        Event<FilterSignal> filter = null!;
        ViciOneServiceBusStateMachine<ConditionInstance> machine = ViciOneServiceBusStateMachine<ConditionInstance>.New(builder => builder
            .State("True", out trueState)
            .State("False", out falseState)
            .Event("Filter", out filter)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(filter, TruePredicate, behavior => behavior
                .Then(context => context.Saga.TrueRoutes++)
                .TransitionTo(trueState))
            .When(filter, FalsePredicate, behavior => behavior
                .Then(context => context.Saga.FalseRoutes++)
                .TransitionTo(falseState)));

        return new FilterScenario(machine, trueState, falseState, filter);
    }

    private static EventActivityBinder<ConditionInstance, StartSignal> ConfigureStarted(
        EventActivityBinder<ConditionInstance, StartSignal> behavior,
        State initialized,
        bool useAsync)
    {
        behavior = behavior.Then(context => context.Saga.InitializeOnly = context.Message.InitializeOnly);
        behavior = useAsync
            ? behavior.IfAsync(StartConditionAsync, selected => selected.Then(context => context.Saga.IfBranches++))
            : behavior.If(StartCondition, selected => selected.Then(context => context.Saga.IfBranches++));

        return behavior
            .Then(context => context.Saga.Continuations++)
            .TransitionTo(initialized);
    }

    private static EventActivityBinder<ConditionInstance, ExplicitSignal> ConfigureExplicit(
        EventActivityBinder<ConditionInstance, ExplicitSignal> behavior,
        State shouldNotBeHere,
        bool useAsync)
    {
        if (useAsync)
        {
            return behavior.IfElseAsync(
                ExplicitConditionAsync,
                selected => selected
                    .Then(context => context.Saga.ThenBranches++)
                    .TransitionTo(shouldNotBeHere),
                selected => selected.Then(context => context.Saga.ElseBranches++));
        }

        return behavior.IfElse(
            ExplicitCondition,
            selected => selected
                .Then(context => context.Saga.ThenBranches++)
                .TransitionTo(shouldNotBeHere),
            selected => selected.Then(context => context.Saga.ElseBranches++));
    }

    private static EventActivityBinder<ConditionInstance> ConfigureEnter(
        EventActivityBinder<ConditionInstance> behavior,
        State running,
        bool useAsync) =>
        useAsync
            ? behavior.IfAsync(EnterConditionAsync, selected => selected.TransitionTo(running))
            : behavior.If(EnterCondition, selected => selected.TransitionTo(running));

    private static bool StartCondition(BehaviorContext<ConditionInstance, StartSignal> context)
    {
        context.Saga.StartConditionEvaluations++;
        return context.Message.InitializeOnly;
    }

    private static Task<bool> StartConditionAsync(BehaviorContext<ConditionInstance, StartSignal> context) =>
        Task.FromResult(StartCondition(context));

    private static bool ExplicitCondition(BehaviorContext<ConditionInstance, ExplicitSignal> context)
    {
        context.Saga.ExplicitConditionEvaluations++;
        return context.Message.TakeThen;
    }

    private static Task<bool> ExplicitConditionAsync(BehaviorContext<ConditionInstance, ExplicitSignal> context) =>
        Task.FromResult(ExplicitCondition(context));

    private static bool EnterCondition(BehaviorContext<ConditionInstance> context)
    {
        context.Saga.EnterConditionEvaluations++;
        return !context.Saga.InitializeOnly;
    }

    private static Task<bool> EnterConditionAsync(BehaviorContext<ConditionInstance> context) =>
        Task.FromResult(EnterCondition(context));

    private static bool TruePredicate(BehaviorContext<ConditionInstance, FilterSignal> context)
    {
        context.Saga.TruePredicateEvaluations++;
        return context.Message.Condition;
    }

    private static bool FalsePredicate(BehaviorContext<ConditionInstance, FilterSignal> context)
    {
        context.Saga.FalsePredicateEvaluations++;
        return !context.Message.Condition;
    }

    private static async Task RaiseAsync<T>(
        StateMachine<ConditionInstance> machine,
        ConditionInstance instance,
        Event<T> @event,
        T message)
        where T : class
    {
        ConsumeContext<T> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<ConditionInstance>(instance);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<ConditionInstance, T>(consumeContext, sagaInstance);
        BehaviorContext<ConditionInstance, T> behaviorContext =
            new ViciOneServiceBusStateMachine<ConditionInstance>.BehaviorContextProxy<T>(machine, sagaContext, sagaContext, @event);

        await machine.RaiseEventAsync(behaviorContext);
    }

    public sealed record StartSignal(bool InitializeOnly);

    public sealed record ExplicitSignal(bool TakeThen);

    public sealed record FilterSignal(bool Condition);

    private sealed record ConditionScenario(
        ViciOneServiceBusStateMachine<ConditionInstance> Machine,
        State Running,
        State Initialized,
        State ShouldNotBeHere,
        Event<StartSignal> Started,
        Event<ExplicitSignal> Explicit);

    private sealed record FilterScenario(
        ViciOneServiceBusStateMachine<ConditionInstance> Machine,
        State True,
        State False,
        Event<FilterSignal> Event);

    private sealed class ConditionInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public State? CurrentState { get; set; }

        public bool InitializeOnly { get; set; }

        public int StartConditionEvaluations { get; set; }

        public int EnterConditionEvaluations { get; set; }

        public int IfBranches { get; set; }

        public int Continuations { get; set; }

        public int ExplicitConditionEvaluations { get; set; }

        public int ThenBranches { get; set; }

        public int ElseBranches { get; set; }

        public int TruePredicateEvaluations { get; set; }

        public int FalsePredicateEvaluations { get; set; }

        public int TrueRoutes { get; set; }

        public int FalseRoutes { get; set; }

        public (int Start, int Enter, int If, int Continue) StartSnapshot =>
            (StartConditionEvaluations, EnterConditionEvaluations, IfBranches, Continuations);

        public (int Predicate, int Then, int Else) ExplicitSnapshot =>
            (ExplicitConditionEvaluations, ThenBranches, ElseBranches);

        public (int TruePredicate, int FalsePredicate, int TrueRoute, int FalseRoute) FilterSnapshot =>
            (TruePredicateEvaluations, FalsePredicateEvaluations, TrueRoutes, FalseRoutes);
    }

    private sealed class DeclarativeConditionMachine : ViciOneServiceBusStateMachine<ConditionInstance>
    {
        public DeclarativeConditionMachine(bool useAsync)
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                ConfigureStarted(When(Started), Initialized, useAsync),
                ConfigureExplicit(When(Explicit, _ => true), ShouldNotBeHere, useAsync));
            WhenEnter(Initialized, behavior => ConfigureEnter(behavior, Running, useAsync));
        }

        public State Running { get; private set; } = null!;

        public State Initialized { get; private set; } = null!;

        public State ShouldNotBeHere { get; private set; } = null!;

        public Event<StartSignal> Started { get; private set; } = null!;

        public Event<ExplicitSignal> Explicit { get; private set; } = null!;
    }

    private sealed class DeclarativeFilterMachine : ViciOneServiceBusStateMachine<ConditionInstance>
    {
        public DeclarativeFilterMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(Filter, TruePredicate)
                    .Then(context => context.Saga.TrueRoutes++)
                    .TransitionTo(True),
                When(Filter, FalsePredicate)
                    .Then(context => context.Saga.FalseRoutes++)
                    .TransitionTo(False));
        }

        public State True { get; private set; } = null!;

        public State False { get; private set; } = null!;

        public Event<FilterSignal> Filter { get; private set; } = null!;
    }
}
