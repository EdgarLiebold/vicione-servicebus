using ViciOne.ServiceBus.Middleware;
using SagaContracts = ViciOne.ServiceBus.Sagas;
using SagaRuntime = ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Tests.InternalAccess.SagaStateMachine;

public static class SagaStateMachineExecutionTestDriver
{
    public static ISagaMessageFilter<TSaga, TMessage> CreateMessageFilter<TSaga, TMessage>(
        SagaContracts.ISagaStateMachine<TSaga> machine,
        SagaContracts.IEvent<TMessage> @event)
        where TSaga : class, ISaga, SagaContracts.ISagaStateMachineInstance
        where TMessage : class
    {
        return new StateMachineSagaMessageFilter<TSaga, TMessage>(machine, @event);
    }

    public static SagaContracts.IStateMachineActivity<TSaga> CreateTransition<TSaga>(
        SagaContracts.IState<TSaga> toState,
        SagaContracts.IStateAccessor<TSaga> currentStateAccessor)
        where TSaga : class, SagaContracts.ISagaStateMachineInstance
    {
        return new SagaRuntime.TransitionActivity<TSaga>(toState, currentStateAccessor);
    }
}
