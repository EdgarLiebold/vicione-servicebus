using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq delivery metrics.
/// </summary>
public interface RabbitMqDeliveryMetrics :
    DeliveryMetrics
{
    /// <summary>
    /// The consumer tag that was assigned to the consumer by the broker
    /// </summary>
    string ConsumerTag { get; }
}
