// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using Transports;


    public interface RabbitMqDeliveryMetrics :
        DeliveryMetrics
    {
        /// <summary>
        /// The consumer tag that was assigned to the consumer by the broker
        /// </summary>
        string ConsumerTag { get; }
    }
}
