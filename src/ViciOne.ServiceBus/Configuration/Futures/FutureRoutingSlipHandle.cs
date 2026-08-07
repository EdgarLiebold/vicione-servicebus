// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Courier.Contracts;


    public interface FutureRoutingSlipHandle
    {
        /// <summary>
        /// The fault state machine event
        /// </summary>
        Event<RoutingSlipFaulted> Faulted { get; }

        /// <summary>
        /// The response state machine event
        /// </summary>
        Event<RoutingSlipCompleted> Completed { get; }
    }
}
