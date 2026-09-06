using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Defines RabbitMQ exchange settings for faulted messages.</summary>
public interface ErrorSettings :
    EntitySettings
{
    /// <summary>Builds the exchange and optional queue topology deployed before use.</summary>
    /// <returns>The error broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
