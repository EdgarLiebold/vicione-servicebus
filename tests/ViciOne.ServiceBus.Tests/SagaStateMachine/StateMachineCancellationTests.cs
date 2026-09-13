using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.InternalAccess.SagaStateMachine;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineCancellationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "completion-pre-cancellation")]
    public async Task IsCompletedAsync_PreCanceledTokenSkipsTheCompletionPredicateAsync()
    {
        var machine = new CancellationMachine();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            StateMachineTestExecution.IsCompletedAsync(machine, new CancellationState(), cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, machine.CompletionCheckCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "filter-consume-cancellation")]
    public async Task MessageFilter_PreCanceledConsumeSkipsTheStateMachineBehaviorAsync()
    {
        var machine = new CancellationMachine();
        var instance = new CancellationState();
        using var cancellation = new CancellationTokenSource();
        ConsumeContext<StartMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new StartMessage(instance.CorrelationId),
            cancellation.Token,
            correlationId: instance.CorrelationId);
        var sagaInstance = new SagaInstance<CancellationState>(instance);
        await sagaInstance.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<CancellationState, StartMessage>(consumeContext, sagaInstance);
        ISagaMessageFilter<CancellationState, StartMessage> filter =
            SagaStateMachineExecutionTestDriver.CreateMessageFilter(machine, machine.Start);
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            filter.SendAsync(sagaContext, Pipe.Empty<SagaConsumeContext<CancellationState, StartMessage>>()));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, instance.BehaviorInvocationCount);
        Assert.Equal(0, machine.CompletionCheckCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "required-completion-and-filter-arguments")]
    public void Configuration_RejectsEveryNullRequiredArgument()
    {
        var machine = new CancellationMachine();
        State<CancellationState> initial = Assert.IsAssignableFrom<State<CancellationState>>(machine.Initial);

        Assert.Equal("completed", Assert.Throws<ArgumentNullException>(() => machine.SetCompletionPredicate(null!)).ParamName);
        Assert.Equal("machine", Assert.Throws<ArgumentNullException>(() =>
            SagaStateMachineExecutionTestDriver.CreateMessageFilter<CancellationState, StartMessage>(null!, machine.Start)).ParamName);
        Assert.Equal("event", Assert.Throws<ArgumentNullException>(() =>
            SagaStateMachineExecutionTestDriver.CreateMessageFilter<CancellationState, StartMessage>(machine, null!)).ParamName);
        Assert.Equal("toState", Assert.Throws<ArgumentNullException>(() =>
            SagaStateMachineExecutionTestDriver.CreateTransition<CancellationState>(null!, machine.Accessor)).ParamName);
        Assert.Equal("currentStateAccessor", Assert.Throws<ArgumentNullException>(() =>
            SagaStateMachineExecutionTestDriver.CreateTransition(initial, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "transition-mid-behavior-cancellation")]
    public async Task Transition_CancellationDuringThePriorActivityPreservesTheCurrentStateAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var machine = new MidBehaviorCancellationMachine(cancellation);
        var instance = new CancellationState();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            RaiseWithCancellationAsync(machine, instance, machine.Start, cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Same(machine.Initial, instance.CurrentState);
        Assert.Equal(0, instance.BehaviorInvocationCount);
    }

    private static async Task RaiseWithCancellationAsync(
        ViciOneServiceBusStateMachine<CancellationState> machine,
        CancellationState instance,
        Event @event,
        CancellationToken cancellationToken)
    {
        ConsumeContext<StateMachineSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new StateMachineSignal(),
            cancellationToken);
        var sagaInstance = new SagaInstance<CancellationState>(instance);
        await sagaInstance.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<CancellationState, StateMachineSignal>(consumeContext, sagaInstance);
        BehaviorContext<CancellationState> behaviorContext =
            new ViciOneServiceBusStateMachine<CancellationState>.BehaviorContextProxy(machine, sagaContext, @event);

        await ((StateMachine<CancellationState>)machine).RaiseEventAsync(behaviorContext, cancellationToken);
    }

    public sealed record StartMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class CancellationState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public State? CurrentState { get; set; }

        public int BehaviorInvocationCount { get; set; }
    }

    private sealed class CancellationMachine : ViciOneServiceBusStateMachine<CancellationState>
    {
        public CancellationMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(When(Start).Then(context => context.Saga.BehaviorInvocationCount++));
            SetCompleted((BehaviorContext<CancellationState> _) =>
            {
                CompletionCheckCount++;
                return Task.FromResult(false);
            });
        }

        public Event<StartMessage> Start { get; private set; } = null!;

        public int CompletionCheckCount { get; private set; }

        public void SetCompletionPredicate(Func<BehaviorContext<CancellationState>, Task<bool>> completed) =>
            SetCompleted(completed);
    }

    private sealed class MidBehaviorCancellationMachine : ViciOneServiceBusStateMachine<CancellationState>
    {
        public MidBehaviorCancellationMachine(CancellationTokenSource cancellation)
        {
            ArgumentNullException.ThrowIfNull(cancellation);
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(Start)
                    .Then(_ => cancellation.Cancel())
                    .TransitionTo(Running)
                    .Then(context => context.Saga.BehaviorInvocationCount++));
        }

        public State Running { get; private set; } = null!;

        public Event Start { get; private set; } = null!;
    }
}
