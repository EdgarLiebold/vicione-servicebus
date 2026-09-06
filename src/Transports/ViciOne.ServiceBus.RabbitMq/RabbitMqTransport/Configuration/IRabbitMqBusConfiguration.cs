using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Combines RabbitMQ host, bus endpoint, and topology configuration.</summary>
public interface IRabbitMqBusConfiguration :
    IBusConfiguration
{
    /// <summary>Gets the RabbitMQ host configuration owned by the bus.</summary>
    new IRabbitMqHostConfiguration HostConfiguration { get; }

    /// <summary>Gets the RabbitMQ endpoint configuration used for the bus endpoint.</summary>
    new IRabbitMqEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>Gets the bus-wide RabbitMQ send, publish, and consume topology.</summary>
    new IRabbitMqTopologyConfiguration Topology { get; }

    /// <summary>Creates an isolated endpoint configuration that shares the bus topology.</summary>
    /// <param name="isBusEndpoint">Whether the configuration belongs to the bus endpoint.</param>
    /// <returns>The new RabbitMQ endpoint configuration.</returns>
    IRabbitMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
