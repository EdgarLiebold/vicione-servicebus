using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineRuntimeContractTests
{
    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "during-any-data-and-initial-exclusion")]
    public async Task DuringAny_HandlesBothPayloadShapesAfterInitialAndRejectsInitialExactlyAsync(
        StateMachineConstructionStyle style)
    {
        AnytimeScenario scenario = CreateAnytimeScenario(style);
        var signalInstance = new RuntimeInstance();
        var dataInstance = new RuntimeInstance();
        var initialInstance = new RuntimeInstance();

        await StateMachineTestExecution.RaiseAsync(scenario.Machine, signalInstance, scenario.Initialize);
        await StateMachineTestExecution.RaiseAsync(scenario.Machine, signalInstance, scenario.Complete);
        await StateMachineTestExecution.RaiseAsync(scenario.Machine, dataInstance, scenario.Initialize);
        await StateMachineTestExecution.RaiseAsync(
            scenario.Machine,
            dataInstance,
            scenario.CompleteWithData,
            new RuntimeData("exact-payload"));

        UnhandledEventException exception = await Assert.ThrowsAsync<UnhandledEventException>(
            () => StateMachineTestExecution.RaiseAsync(scenario.Machine, initialInstance, scenario.Complete));

        Assert.Same(scenario.Machine.Final, signalInstance.CurrentState);
        Assert.Equal(1, signalInstance.SignalCount);
        Assert.Null(signalInstance.Value);
        Assert.Same(scenario.Machine.Final, dataInstance.CurrentState);
        Assert.Equal(0, dataInstance.SignalCount);
        Assert.Equal("exact-payload", dataInstance.Value);
        Assert.Same(scenario.Machine.Initial, initialInstance.CurrentState);
        Assert.Equal(0, initialInstance.SignalCount);
        Assert.Null(initialInstance.Value);
        Assert.Contains(scenario.Complete.Name, exception.Message, StringComparison.Ordinal);
        Assert.Contains(scenario.Machine.Initial.Name, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "before-enter-any-after-leave-any-payload")]
    public async Task AllStateHooks_ReceiveTheExactEnteredAndLeftStatesAsync(
        StateMachineConstructionStyle style)
    {
        TransitionHookScenario scenario = CreateTransitionHookScenario(style);
        var instance = new RuntimeInstance();

        await StateMachineTestExecution.RaiseAsync(scenario.Machine, instance, scenario.Initialize);

        Assert.Same(scenario.Running, instance.CurrentState);
        Assert.Same(scenario.Running, instance.LastEntered);
        Assert.Same(scenario.Machine.Initial, instance.LastLeft);
        Assert.Equal(["enter:Initial", "enter:Running", "leave:Initial"], instance.Markers);
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "nested-raise-same-instance")]
    public async Task NestedRaise_ExecutesTheSecondEventOnTheSameInstanceExactlyOnceAsync(
        StateMachineConstructionStyle style)
    {
        NestedRaiseScenario scenario = CreateNestedRaiseScenario(style);
        var instance = new RuntimeInstance();

        await StateMachineTestExecution.RaiseAsync(
            scenario.Machine,
            instance,
            scenario.Decide,
            new RuntimeDecision(true, "nested-marker"));

        Assert.Same(scenario.True, instance.CurrentState);
        Assert.Equal("nested-marker", instance.Value);
        Assert.Equal(["outer:true", "nested:nested-marker"], instance.Markers);
        Assert.Equal(1, instance.NestedCount);
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "ignored-filtered-unhandled-global-policy-matrix")]
    public async Task UnhandledPolicy_SeparatesIgnoredFilteredUnhandledAndGlobalIgnoreAsync(
        StateMachineConstructionStyle style)
    {
        UnhandledScenario ignored = CreateUnhandledScenario(style, UnhandledPolicy.Ignore);
        UnhandledScenario filtered = CreateUnhandledScenario(style, UnhandledPolicy.FilteredIgnore);
        UnhandledScenario strict = CreateUnhandledScenario(style, UnhandledPolicy.Strict);
        UnhandledScenario global = CreateUnhandledScenario(style, UnhandledPolicy.GlobalIgnore);

        var ignoredInstance = new RuntimeInstance();
        await StateMachineTestExecution.RaiseAsync(ignored.Machine, ignoredInstance, ignored.Start);
        await StateMachineTestExecution.RaiseAsync(ignored.Machine, ignoredInstance, ignored.Start);
        await StateMachineTestExecution.RaiseAsync(ignored.Machine, ignoredInstance, ignored.Charge, new ChargeData(12));

        Assert.Same(ignored.Running, ignoredInstance.CurrentState);
        Assert.Equal(0, ignoredInstance.Volts);
        Assert.Equal(
            ["Charge", "Start"],
            ignored.Machine.NextEvents(ignored.Running).Select(@event => @event.Name).Order().ToArray());

        var filteredInstance = new RuntimeInstance();
        await StateMachineTestExecution.RaiseAsync(filtered.Machine, filteredInstance, filtered.Start);
        await StateMachineTestExecution.RaiseAsync(filtered.Machine, filteredInstance, filtered.Charge, new ChargeData(9));
        UnhandledEventException filteredFailure = await Assert.ThrowsAsync<UnhandledEventException>(
            () => StateMachineTestExecution.RaiseAsync(filtered.Machine, filteredInstance, filtered.Charge, new ChargeData(12)));

        Assert.Contains(filtered.Charge.Name, filteredFailure.Message, StringComparison.Ordinal);
        Assert.Same(filtered.Running, filteredInstance.CurrentState);
        Assert.Equal(0, filteredInstance.Volts);

        var strictInstance = new RuntimeInstance();
        await StateMachineTestExecution.RaiseAsync(strict.Machine, strictInstance, strict.Start);
        UnhandledEventException strictFailure = await Assert.ThrowsAsync<UnhandledEventException>(
            () => StateMachineTestExecution.RaiseAsync(strict.Machine, strictInstance, strict.Start));

        Assert.Contains(strict.Start.Name, strictFailure.Message, StringComparison.Ordinal);
        Assert.Same(strict.Running, strictInstance.CurrentState);

        var globalInstance = new RuntimeInstance();
        await StateMachineTestExecution.RaiseAsync(global.Machine, globalInstance, global.Start);
        await StateMachineTestExecution.RaiseAsync(global.Machine, globalInstance, global.Start);

        Assert.Same(global.Running, globalInstance.CurrentState);
        Assert.Empty(globalInstance.Markers);
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "direct-transition-state-sequence-and-enter-hook")]
    public async Task DirectTransition_ReportsInitialThenTargetAndRunsTheEnterHookAsync(
        StateMachineConstructionStyle style)
    {
        DirectTransitionScenario scenario = CreateDirectTransitionScenario(style);
        var instance = new RuntimeInstance();
        var observer = new StateRecorder();

        using (scenario.Machine.ConnectStateObserver(observer))
            await StateMachineTestExecution.TransitionToStateAsync(scenario.Machine, instance, scenario.Running);

        Assert.Equal(
            new (string? Previous, string Current)[]
            {
                (null, "Initial"),
                ("Initial", "Running"),
            },
            observer.Changes.Select(change => (change.Previous?.Name, change.Current.Name)).ToArray());
        Assert.All(observer.Changes, change => Assert.Same(instance, change.Instance));
        Assert.Equal(["enter:Running"], instance.Markers);
        Assert.Same(scenario.Running, instance.CurrentState);
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "enter-hook-observes-prior-mutation-and-transitions")]
    public async Task EnterHook_ObservesTheCompletedTransitionActivityAndCanTransitionAgainAsync(
        StateMachineConstructionStyle style)
    {
        ChainedEnterScenario scenario = CreateChainedEnterScenario(style);
        var instance = new RuntimeInstance();

        await StateMachineTestExecution.RaiseAsync(scenario.Machine, instance, scenario.Start);

        Assert.Equal(1, instance.SignalCount);
        Assert.Equal(1, instance.OnEnterValue);
        Assert.Equal(["running-enter:1"], instance.Markers);
        Assert.Same(scenario.RunningFaster, instance.CurrentState);
    }

    private static AnytimeScenario CreateAnytimeScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeAnytimeMachine();
            return new AnytimeScenario(
                declarativeMachine,
                declarativeMachine.Ready,
                declarativeMachine.Initialize,
                declarativeMachine.Complete,
                declarativeMachine.CompleteWithData);
        }

        IState ready = null!;
        IEvent initialize = null!;
        IEvent complete = null!;
        IEvent<RuntimeData> completeWithData = null!;
        ViciOneServiceBusStateMachine<RuntimeInstance> machine = ViciOneServiceBusStateMachine<RuntimeInstance>.New(builder => builder
            .State("Ready", out ready)
            .Event("Initialize", out initialize)
            .Event("Complete", out complete)
            .Event("CompleteWithData", out completeWithData)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(initialize, behavior => behavior.TransitionTo(ready))
            .DuringAny()
            .When(complete, behavior => behavior
                .Then(context => context.Saga.SignalCount++)
                .Finalize())
            .When(completeWithData, behavior => behavior
                .Then(context => context.Saga.Value = context.Message.Value)
                .Finalize()));
        return new AnytimeScenario(machine, ready, initialize, complete, completeWithData);
    }

    private static TransitionHookScenario CreateTransitionHookScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeTransitionHookMachine();
            return new TransitionHookScenario(
                declarativeMachine,
                declarativeMachine.Running,
                declarativeMachine.Initialize);
        }

        IState running = null!;
        IEvent initialize = null!;
        ViciOneServiceBusStateMachine<RuntimeInstance> machine = ViciOneServiceBusStateMachine<RuntimeInstance>.New(builder => builder
            .State("Running", out running)
            .Event("Initialize", out initialize)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(initialize, behavior => behavior.TransitionTo(running))
            .BeforeEnterAny(behavior => behavior.Then(context =>
            {
                context.Saga.LastEntered = context.Message;
                context.Saga.Markers.Add($"enter:{context.Message.Name}");
            }))
            .AfterLeaveAny(behavior => behavior.Then(context =>
            {
                context.Saga.LastLeft = context.Message;
                context.Saga.Markers.Add($"leave:{context.Message.Name}");
            })));
        return new TransitionHookScenario(machine, running, initialize);
    }

    private static NestedRaiseScenario CreateNestedRaiseScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeNestedRaiseMachine();
            return new NestedRaiseScenario(
                declarativeMachine,
                declarativeMachine.True,
                declarativeMachine.False,
                declarativeMachine.Decide,
                declarativeMachine.Nested);
        }

        IState @true = null!;
        IState @false = null!;
        IEvent<RuntimeDecision> decide = null!;
        IEvent nested = null!;
        ViciOneServiceBusStateMachine<RuntimeInstance> machine = ViciOneServiceBusStateMachine<RuntimeInstance>.New(builder => builder
            .State("True", out @true)
            .State("False", out @false)
            .Event("Decide", out decide)
            .Event("Nested", out nested)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(decide, context => context.Message.Value, behavior => behavior
                .Then(context =>
                {
                    context.Saga.Value = context.Message.Marker;
                    context.Saga.Markers.Add("outer:true");
                })
                .TransitionTo(@true)
                .Then(context => context.RaiseAsync(nested)))
            .When(decide, context => !context.Message.Value, behavior => behavior.TransitionTo(@false))
            .DuringAny()
            .When(nested, behavior => behavior.Then(context =>
            {
                context.Saga.NestedCount++;
                context.Saga.Markers.Add($"nested:{context.Saga.Value}");
            })));
        return new NestedRaiseScenario(machine, @true, @false, decide, nested);
    }

    private static UnhandledScenario CreateUnhandledScenario(
        StateMachineConstructionStyle style,
        UnhandledPolicy policy)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeUnhandledMachine(policy);
            return new UnhandledScenario(
                declarativeMachine,
                declarativeMachine.Running,
                declarativeMachine.Start,
                declarativeMachine.Charge);
        }

        IState running = null!;
        IEvent start = null!;
        IEvent<ChargeData> charge = null!;
        ViciOneServiceBusStateMachine<RuntimeInstance> machine = ViciOneServiceBusStateMachine<RuntimeInstance>.New(builder =>
        {
            builder
                .State("Running", out running)
                .Event("Start", out start)
                .Event("Charge", out charge)
                .InstanceState(instance => instance.CurrentState!);

            if (policy == UnhandledPolicy.GlobalIgnore)
                builder.OnUnhandledEvent(context => context.IgnoreAsync());

            builder.Initially().When(start, behavior => behavior.TransitionTo(running));

            if (policy == UnhandledPolicy.Ignore)
                builder.During(running).Ignore(start).Ignore(charge);
            else if (policy == UnhandledPolicy.FilteredIgnore)
                builder.During(running).Ignore(start).Ignore(charge, context => context.Message.Volts == 9);
        });
        return new UnhandledScenario(machine, running, start, charge);
    }

    private static DirectTransitionScenario CreateDirectTransitionScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeDirectTransitionMachine();
            return new DirectTransitionScenario(declarativeMachine, declarativeMachine.Running);
        }

        IState running = null!;
        ViciOneServiceBusStateMachine<RuntimeInstance> machine = ViciOneServiceBusStateMachine<RuntimeInstance>.New(builder => builder
            .State("Running", out running)
            .InstanceState(instance => instance.CurrentState!)
            .WhenEnter(running, behavior => behavior.Then(context => context.Saga.Markers.Add("enter:Running"))));
        return new DirectTransitionScenario(machine, running);
    }

    private static ChainedEnterScenario CreateChainedEnterScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeChainedEnterMachine();
            return new ChainedEnterScenario(
                declarativeMachine,
                declarativeMachine.RunningFaster,
                declarativeMachine.Start);
        }

        IState running = null!;
        IState runningFaster = null!;
        IEvent start = null!;
        ViciOneServiceBusStateMachine<RuntimeInstance> machine = ViciOneServiceBusStateMachine<RuntimeInstance>.New(builder => builder
            .State("Running", out running)
            .State("RunningFaster", out runningFaster)
            .Event("Start", out start)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(start, behavior => behavior
                .Then(context => context.Saga.SignalCount = 1)
                .TransitionTo(running))
            .WhenEnter(running, behavior => behavior
                .Then(context =>
                {
                    context.Saga.OnEnterValue = context.Saga.SignalCount;
                    context.Saga.Markers.Add($"running-enter:{context.Saga.SignalCount}");
                })
                .TransitionTo(runningFaster)));
        return new ChainedEnterScenario(machine, runningFaster, start);
    }

    private sealed record AnytimeScenario(
        ViciOneServiceBusStateMachine<RuntimeInstance> Machine,
        IState Ready,
        IEvent Initialize,
        IEvent Complete,
        IEvent<RuntimeData> CompleteWithData);

    private sealed record TransitionHookScenario(
        ViciOneServiceBusStateMachine<RuntimeInstance> Machine,
        IState Running,
        IEvent Initialize);

    private sealed record NestedRaiseScenario(
        ViciOneServiceBusStateMachine<RuntimeInstance> Machine,
        IState True,
        IState False,
        IEvent<RuntimeDecision> Decide,
        IEvent Nested);

    private sealed record UnhandledScenario(
        ViciOneServiceBusStateMachine<RuntimeInstance> Machine,
        IState Running,
        IEvent Start,
        IEvent<ChargeData> Charge);

    private sealed record DirectTransitionScenario(
        ViciOneServiceBusStateMachine<RuntimeInstance> Machine,
        IState Running);

    private sealed record ChainedEnterScenario(
        ViciOneServiceBusStateMachine<RuntimeInstance> Machine,
        IState RunningFaster,
        IEvent Start);

    public sealed record RuntimeData(string Value);

    public sealed record RuntimeDecision(bool Value, string Marker);

    public sealed record ChargeData(int Volts);

    private enum UnhandledPolicy
    {
        Strict,
        Ignore,
        FilteredIgnore,
        GlobalIgnore,
    }

    private sealed class RuntimeInstance : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public IState? CurrentState { get; set; }

        public IState? LastEntered { get; set; }

        public IState? LastLeft { get; set; }

        public int SignalCount { get; set; }

        public int NestedCount { get; set; }

        public int Volts { get; set; }

        public int OnEnterValue { get; set; }

        public string? Value { get; set; }

        public List<string> Markers { get; } = [];
    }

    private sealed class DeclarativeAnytimeMachine : ViciOneServiceBusStateMachine<RuntimeInstance>
    {
        public DeclarativeAnytimeMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(When(Initialize).TransitionTo(Ready));
            DuringAny(
                When(Complete).Then(context => context.Saga.SignalCount++).Finalize(),
                When(CompleteWithData).Then(context => context.Saga.Value = context.Message.Value).Finalize());
        }

        public IState Ready { get; private set; } = null!;

        public IEvent Initialize { get; private set; } = null!;

        public IEvent Complete { get; private set; } = null!;

        public IEvent<RuntimeData> CompleteWithData { get; private set; } = null!;
    }

    private sealed class DeclarativeTransitionHookMachine : ViciOneServiceBusStateMachine<RuntimeInstance>
    {
        public DeclarativeTransitionHookMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(When(Initialize).TransitionTo(Running));
            BeforeEnterAny(behavior => behavior.Then(context =>
            {
                context.Saga.LastEntered = context.Message;
                context.Saga.Markers.Add($"enter:{context.Message.Name}");
            }));
            AfterLeaveAny(behavior => behavior.Then(context =>
            {
                context.Saga.LastLeft = context.Message;
                context.Saga.Markers.Add($"leave:{context.Message.Name}");
            }));
        }

        public IState Running { get; private set; } = null!;

        public IEvent Initialize { get; private set; } = null!;
    }

    private sealed class DeclarativeNestedRaiseMachine : ViciOneServiceBusStateMachine<RuntimeInstance>
    {
        public DeclarativeNestedRaiseMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(Decide, context => context.Message.Value)
                    .Then(context =>
                    {
                        context.Saga.Value = context.Message.Marker;
                        context.Saga.Markers.Add("outer:true");
                    })
                    .TransitionTo(True)
                    .Then(context => context.RaiseAsync(Nested)),
                When(Decide, context => !context.Message.Value).TransitionTo(False));
            DuringAny(When(Nested).Then(context =>
            {
                context.Saga.NestedCount++;
                context.Saga.Markers.Add($"nested:{context.Saga.Value}");
            }));
        }

        public IState True { get; private set; } = null!;

        public IState False { get; private set; } = null!;

        public IEvent<RuntimeDecision> Decide { get; private set; } = null!;

        public IEvent Nested { get; private set; } = null!;
    }

    private sealed class DeclarativeUnhandledMachine : ViciOneServiceBusStateMachine<RuntimeInstance>
    {
        public DeclarativeUnhandledMachine(UnhandledPolicy policy)
        {
            InstanceState(instance => instance.CurrentState!);
            if (policy == UnhandledPolicy.GlobalIgnore)
                OnUnhandledEvent(context => context.IgnoreAsync());

            Initially(When(Start).TransitionTo(Running));

            if (policy == UnhandledPolicy.Ignore)
                During(Running, Ignore(Start), Ignore(Charge));
            else if (policy == UnhandledPolicy.FilteredIgnore)
                During(Running, Ignore(Start), Ignore(Charge, context => context.Message.Volts == 9));
        }

        public IState Running { get; private set; } = null!;

        public IEvent Start { get; private set; } = null!;

        public IEvent<ChargeData> Charge { get; private set; } = null!;
    }

    private sealed class DeclarativeDirectTransitionMachine : ViciOneServiceBusStateMachine<RuntimeInstance>
    {
        public DeclarativeDirectTransitionMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            WhenEnter(Running, behavior => behavior.Then(context => context.Saga.Markers.Add("enter:Running")));
        }

        public IState Running { get; private set; } = null!;
    }

    private sealed class DeclarativeChainedEnterMachine : ViciOneServiceBusStateMachine<RuntimeInstance>
    {
        public DeclarativeChainedEnterMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(Start)
                    .Then(context => context.Saga.SignalCount = 1)
                    .TransitionTo(Running));
            WhenEnter(
                Running,
                behavior => behavior
                    .Then(context =>
                    {
                        context.Saga.OnEnterValue = context.Saga.SignalCount;
                        context.Saga.Markers.Add($"running-enter:{context.Saga.SignalCount}");
                    })
                    .TransitionTo(RunningFaster));
        }

        public IState Running { get; private set; } = null!;

        public IState RunningFaster { get; private set; } = null!;

        public IEvent Start { get; private set; } = null!;
    }

    private sealed class StateRecorder : IStateObserver<RuntimeInstance>
    {
        public List<StateChange> Changes { get; } = [];

        public Task StateChangedAsync(IBehaviorContext<RuntimeInstance> context, IState currentState, IState? previousState)
        {
            Changes.Add(new StateChange(context.Saga, previousState, currentState));
            return Task.CompletedTask;
        }
    }

    private sealed record StateChange(RuntimeInstance Instance, IState? Previous, IState Current);
}
