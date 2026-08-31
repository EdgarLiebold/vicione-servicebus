using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

internal static class StateMachineTestExecution
{
    public static async Task Raise<TInstance>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        TInstance instance,
        Event @event)
        where TInstance : class, SagaStateMachineInstance
    {
        var message = new StateMachineSignal();
        ConsumeContext<StateMachineSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<TInstance>(instance);
        await sagaInstance.MarkInUse(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<TInstance, StateMachineSignal>(consumeContext, sagaInstance);
        BehaviorContext<TInstance> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy(machine, sagaContext, @event);

        await ((StateMachine<TInstance>)machine).RaiseEvent(behaviorContext);
    }

    public static async Task Raise<TInstance, TMessage>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        TInstance instance,
        Event<TMessage> @event,
        TMessage message)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        ConsumeContext<TMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<TInstance>(instance);
        await sagaInstance.MarkInUse(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<TInstance, TMessage>(consumeContext, sagaInstance);
        BehaviorContext<TInstance, TMessage> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy<TMessage>(machine, sagaContext, sagaContext, @event);

        await ((StateMachine<TInstance>)machine).RaiseEvent(behaviorContext);
    }

    public static async Task<State<TInstance>> GetState<TInstance>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        TInstance instance)
        where TInstance : class, SagaStateMachineInstance
    {
        var message = new StateMachineSignal();
        ConsumeContext<StateMachineSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<TInstance>(instance);
        await sagaInstance.MarkInUse(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<TInstance, StateMachineSignal>(consumeContext, sagaInstance);
        BehaviorContext<TInstance> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);
        return await machine.Accessor.Get(behaviorContext);
    }

    public static async Task TransitionToState<TInstance>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        TInstance instance,
        State state)
        where TInstance : class, SagaStateMachineInstance
    {
        var message = new StateMachineSignal();
        ConsumeContext<StateMachineSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var sagaInstance = new SagaInstance<TInstance>(instance);
        await sagaInstance.MarkInUse(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<TInstance, StateMachineSignal>(consumeContext, sagaInstance);
        BehaviorContext<TInstance> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);
        await behaviorContext.TransitionToState(state);
    }

}

public sealed record StateMachineSignal;
