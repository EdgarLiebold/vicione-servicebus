using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public enum StateMachineConstructionStyle
{
    Declarative,
    Dynamic,
}

public sealed class StateMachineActivityTests
{
    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "transition-lifecycle-complete")]
    public async Task TransitionLifecycle_ExecutesEveryHookInOrderWithTheExactStatePayloadAsync(
        StateMachineConstructionStyle style)
    {
        LifecycleScenario scenario = CreateLifecycleScenario(style);
        var instance = new ActivityInstance();

        await RaiseAsync(scenario.Machine, instance, scenario.Initialized);

        Assert.Equal(
            [
                $"initial-enter:{scenario.Machine.Initial.Name}",
                $"initializing-before-enter:{scenario.Initializing.Name}:{scenario.Machine.Initial.Name}",
                $"initial-after-leave:{scenario.Machine.Initial.Name}:{scenario.Initializing.Name}",
                $"running-enter:{scenario.Running.Name}",
                $"final-before-enter:{scenario.Machine.Final.Name}:{scenario.Running.Name}",
            ],
            instance.Markers);
        Assert.Same(scenario.Initializing, instance.EnteredState);
        Assert.Same(scenario.Machine.Initial, instance.LeftState);
        Assert.Same(scenario.Running, instance.StateBeforeFinal);
        Assert.Same(scenario.Machine.Final, instance.CurrentState);
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "initial-binding-equivalence")]
    public async Task DuringInitialAndInitially_BothExecuteAndTransitionExactlyAsync(
        StateMachineConstructionStyle style)
    {
        TransitionScenario during = CreateInitialTransitionScenario(style, useInitially: false);
        TransitionScenario initially = CreateInitialTransitionScenario(style, useInitially: true);
        var duringInstance = new ActivityInstance();
        var initiallyInstance = new ActivityInstance();

        await RaiseAsync(during.Machine, duringInstance, during.Event);
        await RaiseAsync(initially.Machine, initiallyInstance, initially.Event);

        Assert.Equal(["during-initial"], duringInstance.Markers);
        Assert.Same(during.Running, duringInstance.CurrentState);
        Assert.Equal(["initially"], initiallyInstance.Markers);
        Assert.Same(initially.Running, initiallyInstance.CurrentState);
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "finalize-finally-exact")]
    public async Task Finalize_EntersFinalAndRunsFinallyExactlyOnceAsync(StateMachineConstructionStyle style)
    {
        FinalizeScenario scenario = CreateFinalizeScenario(style);
        var instance = new ActivityInstance();

        await RaiseAsync(scenario.Machine, instance, scenario.Event);

        Assert.Equal(["before-finalize", "finally"], instance.Markers);
        Assert.Equal("Finalized", instance.Value);
        Assert.Same(scenario.Machine.Final, instance.CurrentState);
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "custom-data-activity-continuation")]
    public async Task CustomDataActivity_ReceivesThePayloadAndContinuesTheBehaviorAsync(
        StateMachineConstructionStyle style)
    {
        DataScenario scenario = CreateCustomActivityScenario(style);
        var instance = new ActivityInstance();
        var message = new ActivityData("custom-value");

        await RaiseAsync(scenario.Machine, instance, scenario.Event, message);

        Assert.Equal("custom-value", instance.Value);
        Assert.Equal(["custom-activity", "after-custom-activity"], instance.Markers);
        Assert.Same(scenario.Running, instance.CurrentState);
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "data-event-action-continuation")]
    public async Task DataEventAction_CopiesThePayloadAndContinuesToTheTransitionAsync(
        StateMachineConstructionStyle style)
    {
        DataScenario scenario = CreateDataActionScenario(style);
        var instance = new ActivityInstance();
        var message = new ActivityData("Audi", "A6");

        await RaiseAsync(scenario.Machine, instance, scenario.Event, message);

        Assert.Equal("Audi", instance.Value);
        Assert.Equal("A6", instance.SecondaryValue);
        Assert.Equal(["copy:Audi:A6", "after-copy"], instance.Markers);
        Assert.Same(scenario.Running, instance.CurrentState);
    }

    private static LifecycleScenario CreateLifecycleScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeLifecycleMachine();
            return new LifecycleScenario(
                declarativeMachine,
                declarativeMachine.Initializing,
                declarativeMachine.Running,
                declarativeMachine.Initialized);
        }

        IState initializing = null!;
        IState running = null!;
        IEvent initialized = null!;
        ViciOneServiceBusStateMachine<ActivityInstance> machine = ViciOneServiceBusStateMachine<ActivityInstance>.New(builder => builder
            .State("Initializing", out initializing)
            .State("Running", out running)
            .Event("Initialized", out initialized)
            .InstanceState(instance => instance.CurrentState!)
            .During(initializing)
            .When(initialized, behavior => behavior.TransitionTo(running))
            .DuringAny()
            .When(builder.Initial.Enter, behavior => behavior
                .Then(context => context.Saga.Markers.Add($"initial-enter:{context.Saga.CurrentState!.Name}"))
                .TransitionTo(initializing))
            .When(builder.Initial.AfterLeave, behavior => behavior.Then(context =>
            {
                context.Saga.LeftState = context.Message;
                context.Saga.Markers.Add($"initial-after-leave:{context.Message.Name}:{context.Saga.CurrentState!.Name}");
            }))
            .When(initializing.BeforeEnter, behavior => behavior.Then(context =>
            {
                context.Saga.EnteredState = context.Message;
                context.Saga.Markers.Add($"initializing-before-enter:{context.Message.Name}:{context.Saga.CurrentState!.Name}");
            }))
            .When(running.Enter, behavior => behavior
                .Then(context => context.Saga.Markers.Add($"running-enter:{context.Saga.CurrentState!.Name}"))
                .Finalize())
            .When(builder.Final.BeforeEnter, behavior => behavior.Then(context =>
            {
                context.Saga.StateBeforeFinal = context.Saga.CurrentState;
                context.Saga.Markers.Add($"final-before-enter:{context.Message.Name}:{context.Saga.CurrentState!.Name}");
            })));

        return new LifecycleScenario(machine, initializing, running, initialized);
    }

    private static TransitionScenario CreateInitialTransitionScenario(
        StateMachineConstructionStyle style,
        bool useInitially)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            DeclarativeInitialTransitionMachine declarativeMachine = useInitially
                ? new DeclarativeInitiallyMachine()
                : new DeclarativeDuringInitialMachine();
            return new TransitionScenario(
                declarativeMachine,
                declarativeMachine.Running,
                declarativeMachine.Initialized);
        }

        IState running = null!;
        IEvent initialized = null!;
        ViciOneServiceBusStateMachine<ActivityInstance> machine = ViciOneServiceBusStateMachine<ActivityInstance>.New(builder =>
        {
            builder
                .State("Running", out running)
                .Event("Initialized", out initialized)
                .InstanceState(instance => instance.CurrentState!);

            if (useInitially)
            {
                builder.Initially().When(initialized, behavior => behavior
                    .Then(context => context.Saga.Markers.Add("initially"))
                    .TransitionTo(running));
            }
            else
            {
                builder.During(builder.Initial).When(initialized, behavior => behavior
                    .Then(context => context.Saga.Markers.Add("during-initial"))
                    .TransitionTo(running));
            }
        });

        return new TransitionScenario(machine, running, initialized);
    }

    private static FinalizeScenario CreateFinalizeScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeFinalizeMachine();
            return new FinalizeScenario(declarativeMachine, declarativeMachine.Finish);
        }

        IEvent finish = null!;
        ViciOneServiceBusStateMachine<ActivityInstance> machine = ViciOneServiceBusStateMachine<ActivityInstance>.New(builder => builder
            .Event("Finish", out finish)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(finish, behavior => behavior
                .Then(context => context.Saga.Markers.Add("before-finalize"))
                .Finalize())
            .Finally(behavior => behavior.Then(context =>
            {
                context.Saga.Markers.Add("finally");
                context.Saga.Value = "Finalized";
            })));

        return new FinalizeScenario(machine, finish);
    }

    private static DataScenario CreateCustomActivityScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeCustomActivityMachine();
            return new DataScenario(declarativeMachine, declarativeMachine.Running, declarativeMachine.Create);
        }

        IState running = null!;
        IEvent<ActivityData> create = null!;
        ViciOneServiceBusStateMachine<ActivityInstance> machine = ViciOneServiceBusStateMachine<ActivityInstance>.New(builder => builder
            .State("Running", out running)
            .Event("Create", out create)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(create, behavior => behavior
                .Execute(_ => new SetValueActivity())
                .Then(context => context.Saga.Markers.Add("after-custom-activity"))
                .TransitionTo(running)));

        return new DataScenario(machine, running, create);
    }

    private static DataScenario CreateDataActionScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeDataActionMachine();
            return new DataScenario(declarativeMachine, declarativeMachine.Running, declarativeMachine.Initialize);
        }

        IState running = null!;
        IEvent<ActivityData> initialize = null!;
        ViciOneServiceBusStateMachine<ActivityInstance> machine = ViciOneServiceBusStateMachine<ActivityInstance>.New(builder => builder
            .State("Running", out running)
            .Event("Initialize", out initialize)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(initialize, behavior => ConfigureDataAction(behavior, running)));

        return new DataScenario(machine, running, initialize);
    }

    private static IEventActivityBinder<ActivityInstance, ActivityData> ConfigureDataAction(
        IEventActivityBinder<ActivityInstance, ActivityData> behavior,
        IState running) =>
        behavior
            .Then(context =>
            {
                context.Saga.Value = context.Message.Value;
                context.Saga.SecondaryValue = context.Message.SecondaryValue;
                context.Saga.Markers.Add($"copy:{context.Message.Value}:{context.Message.SecondaryValue}");
            })
            .Then(context => context.Saga.Markers.Add("after-copy"))
            .TransitionTo(running);

    private static async Task RaiseAsync(
        IStateMachine<ActivityInstance> machine,
        ActivityInstance instance,
        IEvent @event)
    {
        var message = new ActivitySignal();
        ConsumeContext<ActivitySignal> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<ActivityInstance>(instance);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<ActivityInstance, ActivitySignal>(consumeContext, sagaInstance);
        IBehaviorContext<ActivityInstance> behaviorContext =
            new ViciOneServiceBusStateMachine<ActivityInstance>.BehaviorContextProxy(machine, sagaContext, @event);

        await machine.RaiseEventAsync(behaviorContext);
    }

    private static async Task RaiseAsync<T>(
        IStateMachine<ActivityInstance> machine,
        ActivityInstance instance,
        IEvent<T> @event,
        T message)
        where T : class
    {
        ConsumeContext<T> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<ActivityInstance>(instance);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<ActivityInstance, T>(consumeContext, sagaInstance);
        IBehaviorContext<ActivityInstance, T> behaviorContext =
            new ViciOneServiceBusStateMachine<ActivityInstance>.BehaviorContextProxy<T>(machine, sagaContext, sagaContext, @event);

        await machine.RaiseEventAsync(behaviorContext);
    }

    public sealed record ActivitySignal;

    public sealed record ActivityData(string Value, string SecondaryValue = "");

    private sealed record LifecycleScenario(
        ViciOneServiceBusStateMachine<ActivityInstance> Machine,
        IState Initializing,
        IState Running,
        IEvent Initialized);

    private sealed record TransitionScenario(
        ViciOneServiceBusStateMachine<ActivityInstance> Machine,
        IState Running,
        IEvent Event);

    private sealed record FinalizeScenario(
        ViciOneServiceBusStateMachine<ActivityInstance> Machine,
        IEvent Event);

    private sealed record DataScenario(
        ViciOneServiceBusStateMachine<ActivityInstance> Machine,
        IState Running,
        IEvent<ActivityData> Event);

    private sealed class ActivityInstance : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public IState? CurrentState { get; set; }

        public IState? EnteredState { get; set; }

        public IState? LeftState { get; set; }

        public IState? StateBeforeFinal { get; set; }

        public string? Value { get; set; }

        public string? SecondaryValue { get; set; }

        public List<string> Markers { get; } = [];
    }

    private sealed class SetValueActivity : IStateMachineActivity<ActivityInstance, ActivityData>
    {
        public Task ExecuteAsync(
            IBehaviorContext<ActivityInstance, ActivityData> context,
            IBehavior<ActivityInstance, ActivityData> next)
        {
            context.Saga.Value = context.Message.Value;
            context.Saga.Markers.Add("custom-activity");
            return next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<ActivityInstance, ActivityData, TException> context,
            IBehavior<ActivityInstance, ActivityData> next)
            where TException : Exception =>
            next.FaultedAsync(context);

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context) => context.CreateScope("setValue");
    }

    private sealed class DeclarativeLifecycleMachine : ViciOneServiceBusStateMachine<ActivityInstance>
    {
        public DeclarativeLifecycleMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            During(Initializing, When(Initialized).TransitionTo(Running));
            DuringAny(
                When(Initial.Enter)
                    .Then(context => context.Saga.Markers.Add($"initial-enter:{context.Saga.CurrentState!.Name}"))
                    .TransitionTo(Initializing),
                When(Initial.AfterLeave).Then(context =>
                {
                    context.Saga.LeftState = context.Message;
                    context.Saga.Markers.Add($"initial-after-leave:{context.Message.Name}:{context.Saga.CurrentState!.Name}");
                }),
                When(Initializing.BeforeEnter).Then(context =>
                {
                    context.Saga.EnteredState = context.Message;
                    context.Saga.Markers.Add($"initializing-before-enter:{context.Message.Name}:{context.Saga.CurrentState!.Name}");
                }),
                When(Running.Enter)
                    .Then(context => context.Saga.Markers.Add($"running-enter:{context.Saga.CurrentState!.Name}"))
                    .Finalize(),
                When(Final.BeforeEnter).Then(context =>
                {
                    context.Saga.StateBeforeFinal = context.Saga.CurrentState;
                    context.Saga.Markers.Add($"final-before-enter:{context.Message.Name}:{context.Saga.CurrentState!.Name}");
                }));
        }

        public IState Initializing { get; private set; } = null!;

        public IState Running { get; private set; } = null!;

        public IEvent Initialized { get; private set; } = null!;
    }

    private abstract class DeclarativeInitialTransitionMachine : ViciOneServiceBusStateMachine<ActivityInstance>
    {
        public IState Running { get; protected set; } = null!;

        public IEvent Initialized { get; protected set; } = null!;
    }

    private sealed class DeclarativeDuringInitialMachine : DeclarativeInitialTransitionMachine
    {
        public DeclarativeDuringInitialMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            During(
                Initial,
                When(Initialized)
                    .Then(context => context.Saga.Markers.Add("during-initial"))
                    .TransitionTo(Running));
        }
    }

    private sealed class DeclarativeInitiallyMachine : DeclarativeInitialTransitionMachine
    {
        public DeclarativeInitiallyMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(Initialized)
                    .Then(context => context.Saga.Markers.Add("initially"))
                    .TransitionTo(Running));
        }
    }

    private sealed class DeclarativeFinalizeMachine : ViciOneServiceBusStateMachine<ActivityInstance>
    {
        public DeclarativeFinalizeMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(Finish)
                    .Then(context => context.Saga.Markers.Add("before-finalize"))
                    .Finalize());
            Finally(behavior => behavior.Then(context =>
            {
                context.Saga.Markers.Add("finally");
                context.Saga.Value = "Finalized";
            }));
        }

        public IEvent Finish { get; private set; } = null!;
    }

    private sealed class DeclarativeCustomActivityMachine : ViciOneServiceBusStateMachine<ActivityInstance>
    {
        public DeclarativeCustomActivityMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(Create)
                    .Execute(_ => new SetValueActivity())
                    .Then(context => context.Saga.Markers.Add("after-custom-activity"))
                    .TransitionTo(Running));
        }

        public IState Running { get; private set; } = null!;

        public IEvent<ActivityData> Create { get; private set; } = null!;
    }

    private sealed class DeclarativeDataActionMachine : ViciOneServiceBusStateMachine<ActivityInstance>
    {
        public DeclarativeDataActionMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(ConfigureDataAction(When(Initialize), Running));
        }

        public IState Running { get; private set; } = null!;

        public IEvent<ActivityData> Initialize { get; private set; } = null!;
    }
}
