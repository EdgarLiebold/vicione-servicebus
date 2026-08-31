using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineExceptionBehaviorTests
{
    [Theory]
    [InlineData(ConstructionStyle.Declarative)]
    [InlineData(ConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-EXCEPTION", "typed-catch-complete-sync-async-branch-pipeline")]
    public async Task TypedCatch_ExecutesTheExactPipelineAndStopsTheFaultedBehavior(ConstructionStyle style)
    {
        ExceptionScenario scenario = CreateTypedCatchScenario(style);
        var instance = new ExceptionInstance();

        await Raise(scenario.Machine, instance, scenario.Event);

        Assert.Equal(
            ["before", "throw", "if-true", "if-async-true", "if-false-else", "if-async-false-else", "catch", "catch-async"],
            instance.Markers);
        Assert.DoesNotContain("after-throw", instance.Markers);
        Assert.DoesNotContain("if-false-then", instance.Markers);
        Assert.DoesNotContain("if-async-false-then", instance.Markers);
        Assert.DoesNotContain("broad-catch", instance.Markers);
        AssertCaughtApplicationException(instance);
        Assert.Same(scenario.Failed, instance.CurrentState);
    }

    [Theory]
    [InlineData(ConstructionStyle.Declarative)]
    [InlineData(ConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-EXCEPTION", "base-catch-preserves-derived-exception")]
    public async Task BaseCatch_PreservesTheDerivedExceptionAndTransitions(ConstructionStyle style)
    {
        ExceptionScenario scenario = CreateBaseCatchScenario(style);
        var instance = new ExceptionInstance();

        await Raise(scenario.Machine, instance, scenario.Event);

        Assert.Equal(["before", "throw", "base-catch"], instance.Markers);
        Assert.DoesNotContain("after-throw", instance.Markers);
        Assert.DoesNotContain("narrow-catch", instance.Markers);
        AssertCaughtApplicationException(instance);
        Assert.Same(scenario.Failed, instance.CurrentState);
    }

    [Theory]
    [InlineData(ConstructionStyle.Declarative)]
    [InlineData(ConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-EXCEPTION", "empty-catch-continues-following-activity")]
    public async Task EmptyCatch_ContinuesWithTheFollowingActivity(ConstructionStyle style)
    {
        ExceptionScenario scenario = CreateEmptyCatchScenario(style);
        var instance = new ExceptionInstance();

        await Raise(scenario.Machine, instance, scenario.Event);

        Assert.Equal(["throw", "after-catch"], instance.Markers);
        Assert.NotNull(instance.ThrownException);
        Assert.Null(instance.CaughtException);
    }

    [Theory]
    [InlineData(ConstructionStyle.Declarative)]
    [InlineData(ConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-EXCEPTION", "data-event-catch-complete-branch-pipeline")]
    public async Task DataEventCatch_ExecutesTheExactPipelineAndStopsTheFaultedBehavior(ConstructionStyle style)
    {
        DataExceptionScenario scenario = CreateDataCatchScenario(style);
        var instance = new ExceptionInstance();

        await Raise(scenario.Machine, instance, scenario.Event, new ExceptionData("payload"));

        Assert.Equal(
            ["before:payload", "throw", "if-true", "if-async-true", "if-false-else", "if-async-false-else", "catch"],
            instance.Markers);
        Assert.DoesNotContain("after-throw", instance.Markers);
        Assert.DoesNotContain("if-false-then", instance.Markers);
        Assert.DoesNotContain("if-async-false-then", instance.Markers);
        AssertCaughtApplicationException(instance);
        Assert.Same(scenario.Failed, instance.CurrentState);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-EXCEPTION", "nested-else-catch-transitions-to-catch-target")]
    public async Task NestedElseCatch_TransitionsToTheCatchTargetWithoutExecutingTheInterruptedTransition()
    {
        var machine = new DeclarativeNestedElseCatchMachine();
        var instance = new ExceptionInstance();

        await Raise(machine, instance, machine.Initialized);

        Assert.Equal(["else", "throw", "catch"], instance.Markers);
        Assert.Same(machine.Failed, instance.CurrentState);
        Assert.NotSame(machine.NotCompleted, instance.CurrentState);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-EXCEPTION", "catch-finalizes-data-event")]
    public async Task Catch_CanFinalizeTheInstanceAfterADataEventFailure()
    {
        var machine = new DeclarativeFinalizeCatchMachine();
        var instance = new ExceptionInstance();

        await Raise(machine, instance, machine.Initialized, new ExceptionData("finalize"));

        Assert.Equal(["throw:finalize", "catch"], instance.Markers);
        Assert.Same(machine.Final, instance.CurrentState);
    }

    private static void AssertCaughtApplicationException(ExceptionInstance instance)
    {
        Assert.NotNull(instance.ThrownException);
        Assert.Same(instance.ThrownException, instance.CaughtException);
        ApplicationException exception = Assert.IsType<ApplicationException>(instance.CaughtException);
        Assert.Equal("Boom!", exception.Message);
    }

    private static ExceptionScenario CreateTypedCatchScenario(ConstructionStyle style)
    {
        if (style == ConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeTypedCatchMachine();
            return new ExceptionScenario(declarativeMachine, declarativeMachine.Initialized, declarativeMachine.Failed);
        }

        State failed = null!;
        Event initialized = null!;
        ViciOneServiceBusStateMachine<ExceptionInstance> machine = ViciOneServiceBusStateMachine<ExceptionInstance>.New(builder => builder
            .State("Failed", out failed)
            .Event("Initialized", out initialized)
            .During(builder.Initial)
            .When(initialized, behavior => ConfigureTypedCatch(behavior, failed)));

        return new ExceptionScenario(machine, initialized, failed);
    }

    private static ExceptionScenario CreateBaseCatchScenario(ConstructionStyle style)
    {
        if (style == ConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeBaseCatchMachine();
            return new ExceptionScenario(declarativeMachine, declarativeMachine.Initialized, declarativeMachine.Failed);
        }

        State failed = null!;
        Event initialized = null!;
        ViciOneServiceBusStateMachine<ExceptionInstance> machine = ViciOneServiceBusStateMachine<ExceptionInstance>.New(builder => builder
            .State("Failed", out failed)
            .Event("Initialized", out initialized)
            .InstanceState(instance => instance.CurrentState)
            .During(builder.Initial)
            .When(initialized, behavior => ConfigureBaseCatch(behavior, failed)));

        return new ExceptionScenario(machine, initialized, failed);
    }

    private static ExceptionScenario CreateEmptyCatchScenario(ConstructionStyle style)
    {
        if (style == ConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeEmptyCatchMachine();
            return new ExceptionScenario(declarativeMachine, declarativeMachine.Initialized, declarativeMachine.Initial);
        }

        Event initialized = null!;
        ViciOneServiceBusStateMachine<ExceptionInstance> machine = ViciOneServiceBusStateMachine<ExceptionInstance>.New(builder => builder
            .Event("Initialized", out initialized)
            .InstanceState(instance => instance.CurrentState)
            .During(builder.Initial)
            .When(initialized, behavior => ConfigureEmptyCatch(behavior)));

        return new ExceptionScenario(machine, initialized, machine.Initial);
    }

    private static DataExceptionScenario CreateDataCatchScenario(ConstructionStyle style)
    {
        if (style == ConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeDataCatchMachine();
            return new DataExceptionScenario(declarativeMachine, declarativeMachine.Initialized, declarativeMachine.Failed);
        }

        State failed = null!;
        Event<ExceptionData> initialized = null!;
        ViciOneServiceBusStateMachine<ExceptionInstance> machine = ViciOneServiceBusStateMachine<ExceptionInstance>.New(builder => builder
            .State("Failed", out failed)
            .Event("Initialized", out initialized)
            .InstanceState(instance => instance.CurrentState)
            .During(builder.Initial)
            .When(initialized, behavior => ConfigureDataCatch(behavior, failed)));

        return new DataExceptionScenario(machine, initialized, failed);
    }

    private static EventActivityBinder<ExceptionInstance> ConfigureTypedCatch(
        EventActivityBinder<ExceptionInstance> behavior,
        State failed) =>
        behavior
            .Then(context => context.Saga.Markers.Add("before"))
            .Then(context => Throw(context.Saga, "throw"))
            .Then(context => context.Saga.Markers.Add("after-throw"))
            .Catch<ApplicationException>(caught => caught
                .If(_ => true, selected => selected.Then(context => context.Saga.Markers.Add("if-true")))
                .IfAsync(_ => Task.FromResult(true), selected => selected.Then(context => context.Saga.Markers.Add("if-async-true")))
                .IfElse(
                    _ => false,
                    selected => selected.Then(context => context.Saga.Markers.Add("if-false-then")),
                    selected => selected.Then(context => context.Saga.Markers.Add("if-false-else")))
                .IfElseAsync(
                    _ => Task.FromResult(false),
                    selected => selected.Then(context => context.Saga.Markers.Add("if-async-false-then")),
                    selected => selected.Then(context => context.Saga.Markers.Add("if-async-false-else")))
                .Then(context => Capture(context.Saga, context.Exception, "catch"))
                .ThenAsync(context =>
                {
                    context.Saga.Markers.Add("catch-async");
                    return Task.CompletedTask;
                })
                .TransitionTo(failed))
            .Catch<Exception>(caught => caught.Then(context => context.Saga.Markers.Add("broad-catch")));

    private static EventActivityBinder<ExceptionInstance> ConfigureBaseCatch(
        EventActivityBinder<ExceptionInstance> behavior,
        State failed) =>
        behavior
            .Then(context => context.Saga.Markers.Add("before"))
            .Then(context => Throw(context.Saga, "throw"))
            .Then(context => context.Saga.Markers.Add("after-throw"))
            .Catch<ArgumentException>(caught => caught
                .Then(context => context.Saga.Markers.Add("narrow-catch")))
            .Catch<Exception>(caught => caught
                .Then(context => Capture(context.Saga, context.Exception, "base-catch"))
                .TransitionTo(failed));

    private static EventActivityBinder<ExceptionInstance> ConfigureEmptyCatch(EventActivityBinder<ExceptionInstance> behavior) =>
        behavior
            .Then(context => Throw(context.Saga, "throw"))
            .Catch<Exception>(caught => caught)
            .Then(context => context.Saga.Markers.Add("after-catch"));

    private static EventActivityBinder<ExceptionInstance, ExceptionData> ConfigureDataCatch(
        EventActivityBinder<ExceptionInstance, ExceptionData> behavior,
        State failed) =>
        behavior
            .Then(context => context.Saga.Markers.Add($"before:{context.Message.Value}"))
            .Then(context => Throw(context.Saga, "throw"))
            .Then(context => context.Saga.Markers.Add("after-throw"))
            .Catch<Exception>(caught => caught
                .If(_ => true, selected => selected.Then(context => context.Saga.Markers.Add("if-true")))
                .IfAsync(_ => Task.FromResult(true), selected => selected.Then(context => context.Saga.Markers.Add("if-async-true")))
                .IfElse(
                    _ => false,
                    selected => selected.Then(context => context.Saga.Markers.Add("if-false-then")),
                    selected => selected.Then(context => context.Saga.Markers.Add("if-false-else")))
                .IfElseAsync(
                    _ => Task.FromResult(false),
                    selected => selected.Then(context => context.Saga.Markers.Add("if-async-false-then")),
                    selected => selected.Then(context => context.Saga.Markers.Add("if-async-false-else")))
                .Then(context => Capture(context.Saga, context.Exception, "catch"))
                .TransitionTo(failed));

    private static void Throw(ExceptionInstance instance, string marker)
    {
        instance.Markers.Add(marker);
        var exception = new ApplicationException("Boom!");
        instance.ThrownException = exception;
        throw exception;
    }

    private static void Capture(ExceptionInstance instance, Exception exception, string marker)
    {
        instance.Markers.Add(marker);
        instance.CaughtException = exception;
    }

    private static async Task Raise(StateMachine<ExceptionInstance> machine, ExceptionInstance instance, Event @event)
    {
        var message = new ExceptionSignal();
        ConsumeContext<ExceptionSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<ExceptionInstance>(instance);
        await sagaInstance.MarkInUse(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<ExceptionInstance, ExceptionSignal>(consumeContext, sagaInstance);
        BehaviorContext<ExceptionInstance> behaviorContext =
            new ViciOneServiceBusStateMachine<ExceptionInstance>.BehaviorContextProxy(machine, sagaContext, @event);

        await machine.RaiseEvent(behaviorContext);
    }

    private static async Task Raise<T>(StateMachine<ExceptionInstance> machine, ExceptionInstance instance, Event<T> @event, T message)
        where T : class
    {
        ConsumeContext<T> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<ExceptionInstance>(instance);
        await sagaInstance.MarkInUse(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<ExceptionInstance, T>(consumeContext, sagaInstance);
        BehaviorContext<ExceptionInstance, T> behaviorContext =
            new ViciOneServiceBusStateMachine<ExceptionInstance>.BehaviorContextProxy<T>(machine, sagaContext, sagaContext, @event);

        await machine.RaiseEvent(behaviorContext);
    }

    public enum ConstructionStyle
    {
        Declarative,
        Dynamic,
    }

    public sealed record ExceptionSignal;

    public sealed record ExceptionData(string Value);

    private sealed record ExceptionScenario(
        ViciOneServiceBusStateMachine<ExceptionInstance> Machine,
        Event Event,
        State Failed);

    private sealed record DataExceptionScenario(
        ViciOneServiceBusStateMachine<ExceptionInstance> Machine,
        Event<ExceptionData> Event,
        State Failed);

    private sealed class ExceptionInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public State? CurrentState { get; set; }

        public List<string> Markers { get; } = [];

        public Exception? ThrownException { get; set; }

        public Exception? CaughtException { get; set; }
    }

    private sealed class DeclarativeTypedCatchMachine : ViciOneServiceBusStateMachine<ExceptionInstance>
    {
        public DeclarativeTypedCatchMachine()
        {
            During(Initial, ConfigureTypedCatch(When(Initialized), Failed));
        }

        public State Failed { get; private set; } = null!;

        public Event Initialized { get; private set; } = null!;
    }

    private sealed class DeclarativeBaseCatchMachine : ViciOneServiceBusStateMachine<ExceptionInstance>
    {
        public DeclarativeBaseCatchMachine()
        {
            InstanceState(instance => instance.CurrentState);
            During(Initial, ConfigureBaseCatch(When(Initialized), Failed));
        }

        public State Failed { get; private set; } = null!;

        public Event Initialized { get; private set; } = null!;
    }

    private sealed class DeclarativeEmptyCatchMachine : ViciOneServiceBusStateMachine<ExceptionInstance>
    {
        public DeclarativeEmptyCatchMachine()
        {
            InstanceState(instance => instance.CurrentState);
            During(Initial, ConfigureEmptyCatch(When(Initialized)));
        }

        public Event Initialized { get; private set; } = null!;
    }

    private sealed class DeclarativeDataCatchMachine : ViciOneServiceBusStateMachine<ExceptionInstance>
    {
        public DeclarativeDataCatchMachine()
        {
            InstanceState(instance => instance.CurrentState);
            During(Initial, ConfigureDataCatch(When(Initialized), Failed));
        }

        public State Failed { get; private set; } = null!;

        public Event<ExceptionData> Initialized { get; private set; } = null!;
    }

    private sealed class DeclarativeNestedElseCatchMachine : ViciOneServiceBusStateMachine<ExceptionInstance>
    {
        public DeclarativeNestedElseCatchMachine()
        {
            InstanceState(instance => instance.CurrentState);
            During(
                Initial,
                When(Initialized)
                    .IfElse(
                        _ => false,
                        selected => selected.TransitionTo(Completed),
                        selected => selected
                            .Then(context => context.Saga.Markers.Add("else"))
                            .Then(context => Throw(context.Saga, "throw"))
                            .TransitionTo(NotCompleted)
                            .Catch<Exception>(caught => caught
                                .Then(context => context.Saga.Markers.Add("catch"))
                                .TransitionTo(Failed))));
        }

        public State Completed { get; private set; } = null!;

        public State NotCompleted { get; private set; } = null!;

        public State Failed { get; private set; } = null!;

        public Event Initialized { get; private set; } = null!;
    }

    private sealed class DeclarativeFinalizeCatchMachine : ViciOneServiceBusStateMachine<ExceptionInstance>
    {
        public DeclarativeFinalizeCatchMachine()
        {
            InstanceState(instance => instance.CurrentState);
            During(
                Initial,
                When(Initialized)
                    .Then(context => Throw(context.Saga, $"throw:{context.Message.Value}"))
                    .Catch<Exception>(caught => caught
                        .Then(context => context.Saga.Markers.Add("catch"))
                        .Finalize()));
        }

        public Event<ExceptionData> Initialized { get; private set; } = null!;
    }
}
