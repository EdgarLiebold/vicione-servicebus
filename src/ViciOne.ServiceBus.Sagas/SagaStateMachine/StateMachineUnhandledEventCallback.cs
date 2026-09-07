using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Represents the method that handles state machine unhandled event callback.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <param name="state">The state.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Task StateMachineUnhandledEventCallback<TSaga>(BehaviorContext<TSaga> context, State state)
    where TSaga : class, SagaStateMachineInstance;
