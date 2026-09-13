using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Processes an unhandled state-machine event asynchronously for a supplied state.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <param name="context">The unhandled event context.</param>
/// <param name="state">The state in which the event was not handled.</param>
/// <returns>A task that completes when the unhandled event has been processed for the supplied state.</returns>
public delegate Task StateMachineUnhandledEventCallback<TSaga>(IBehaviorContext<TSaga> context, IState state)
    where TSaga : class, ISagaStateMachineInstance;
