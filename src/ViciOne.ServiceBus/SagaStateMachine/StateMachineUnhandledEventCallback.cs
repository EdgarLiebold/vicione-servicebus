using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public delegate Task StateMachineUnhandledEventCallback<TSaga>(BehaviorContext<TSaga> context, State state)
    where TSaga : class, SagaStateMachineInstance;
