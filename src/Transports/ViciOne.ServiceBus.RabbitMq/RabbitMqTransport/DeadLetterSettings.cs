using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Defines RabbitMQ exchange settings for dead-lettered messages.</summary>
public interface DeadLetterSettings :
    EntitySettings
{
    /// <summary>Builds the exchange and optional queue topology deployed before use.</summary>
    /// <returns>The dead-letter broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
