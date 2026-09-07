using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Callback for an unhandled event in the state machine.</summary>
/// <typeparam name="TSaga">The state machine instance type.</typeparam>
/// <param name="context">The event context.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Task UnhandledEventCallback<TSaga>(UnhandledEventContext<TSaga> context)
    where TSaga : class, SagaStateMachineInstance;
