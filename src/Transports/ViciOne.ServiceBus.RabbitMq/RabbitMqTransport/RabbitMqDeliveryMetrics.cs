using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Adds the broker-assigned consumer tag to receive-delivery metrics.</summary>
public interface RabbitMqDeliveryMetrics :
    IDeliveryMetrics
{
    /// <summary>The consumer tag that was assigned to the consumer by the broker.</summary>
    string ConsumerTag { get; }
}
