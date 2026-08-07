// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SagaStateMachine
{
    using System.Threading.Tasks;


    public delegate Task StateMachineUnhandledEventCallback<TSaga>(BehaviorContext<TSaga> context, State state)
        where TSaga : class, SagaStateMachineInstance;
}
