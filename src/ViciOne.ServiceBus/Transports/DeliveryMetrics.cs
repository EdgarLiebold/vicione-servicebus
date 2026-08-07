// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    public interface DeliveryMetrics
    {
        /// <summary>
        /// The number of messages consumed by the consumer
        /// </summary>
        long DeliveryCount { get; }

        /// <summary>
        /// The highest concurrent message count that was received by the consumer
        /// </summary>
        int ConcurrentDeliveryCount { get; }
    }
}
