using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

internal static class StateMachineTestExecution
{
    public static async Task RaiseAsync<TInstance>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        TInstance instance,
        IEvent @event)
        where TInstance : class, ISagaStateMachineInstance
    {
        var message = new StateMachineSignal();
        ConsumeContext<StateMachineSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<TInstance>(instance);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<TInstance, StateMachineSignal>(consumeContext, sagaInstance);
        IBehaviorContext<TInstance> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy(machine, sagaContext, @event);

        await ((IStateMachine<TInstance>)machine).RaiseEventAsync(behaviorContext);
    }

    public static async Task RaiseAsync<TInstance, TMessage>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        TInstance instance,
        IEvent<TMessage> @event,
        TMessage message)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        ConsumeContext<TMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<TInstance>(instance);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<TInstance, TMessage>(consumeContext, sagaInstance);
        IBehaviorContext<TInstance, TMessage> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy<TMessage>(machine, sagaContext, sagaContext, @event);

        await ((IStateMachine<TInstance>)machine).RaiseEventAsync(behaviorContext);
    }

    public static async Task<IState<TInstance>> GetStateAsync<TInstance>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        TInstance instance)
        where TInstance : class, ISagaStateMachineInstance
    {
        var message = new StateMachineSignal();
        ConsumeContext<StateMachineSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<TInstance>(instance);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<TInstance, StateMachineSignal>(consumeContext, sagaInstance);
        IBehaviorContext<TInstance> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);
        return await machine.Accessor.GetAsync(behaviorContext)
            ?? throw new Xunit.Sdk.XunitException("Expected the state-machine accessor to return the current state.");
    }

    public static async Task<bool> IsCompletedAsync<TInstance>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        TInstance instance,
        CancellationToken cancellationToken)
        where TInstance : class, ISagaStateMachineInstance
    {
        var message = new StateMachineSignal();
        ConsumeContext<StateMachineSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<TInstance>(instance);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<TInstance, StateMachineSignal>(consumeContext, sagaInstance);
        IBehaviorContext<TInstance> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);

        return await ((ISagaStateMachine<TInstance>)machine).IsCompletedAsync(behaviorContext, cancellationToken);
    }

    public static async Task TransitionToStateAsync<TInstance>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        TInstance instance,
        IState state)
        where TInstance : class, ISagaStateMachineInstance
    {
        var message = new StateMachineSignal();
        ConsumeContext<StateMachineSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<TInstance>(instance);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<TInstance, StateMachineSignal>(consumeContext, sagaInstance);
        IBehaviorContext<TInstance> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);
        await behaviorContext.TransitionToStateAsync(state);
    }

}

public sealed record StateMachineSignal;
