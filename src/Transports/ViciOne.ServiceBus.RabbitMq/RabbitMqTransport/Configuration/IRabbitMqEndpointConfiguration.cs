using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Defines the contract for rabbit mq endpoint configuration.
/// </summary>
public interface IRabbitMqEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IRabbitMqTopologyConfiguration Topology { get; }
}
