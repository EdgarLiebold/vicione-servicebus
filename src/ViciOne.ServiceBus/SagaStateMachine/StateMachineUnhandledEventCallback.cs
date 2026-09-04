using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Represents the method that handles state machine unhandled event callback.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <param name="context">The operation context.</param>
/// <param name="state">The state value.</param>
/// <returns>The result of the operation.</returns>
public delegate Task StateMachineUnhandledEventCallback<TSaga>(BehaviorContext<TSaga> context, State state)
    where TSaga : class, SagaStateMachineInstance;
