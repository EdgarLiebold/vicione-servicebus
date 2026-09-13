using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Processes an unhandled state-machine event asynchronously.</summary>
/// <typeparam name="TSaga">The state machine instance type.</typeparam>
/// <param name="context">The event context.</param>
/// <returns>A task that completes when the unhandled event has been processed.</returns>
public delegate Task UnhandledEventCallback<TSaga>(IUnhandledEventContext<TSaga> context)
    where TSaga : class, ISagaStateMachineInstance;
