using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Combines provider-neutral endpoint configuration with RabbitMQ topology.</summary>
public interface IRabbitMqEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>Gets the RabbitMQ topology configuration scoped to this endpoint.</summary>
    new IRabbitMqTopologyConfiguration Topology { get; }
}
