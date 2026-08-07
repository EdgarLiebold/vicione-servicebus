// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface ReceiveTransportCompleted :
        ReceiveTransportEvent
    {
        /// <summary>
        /// The number of messages delivered to the receive endpoint
        /// </summary>
        long DeliveryCount { get; }

        /// <summary>
        /// The maximum concurrent messages delivery to the receive endpoint
        /// </summary>
        long ConcurrentDeliveryCount { get; }
    }
}
