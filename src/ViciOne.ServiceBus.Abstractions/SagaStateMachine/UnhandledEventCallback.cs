// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Threading.Tasks;


    /// <summary>
    /// Callback for an unhandled event in the state machine
    /// </summary>
    /// <typeparam name="TSaga">The state machine instance type</typeparam>
    /// <param name="context">The event context</param>
    /// <returns></returns>
    public delegate Task UnhandledEventCallback<TSaga>(UnhandledEventContext<TSaga> context)
        where TSaga : class, SagaStateMachineInstance;
}
