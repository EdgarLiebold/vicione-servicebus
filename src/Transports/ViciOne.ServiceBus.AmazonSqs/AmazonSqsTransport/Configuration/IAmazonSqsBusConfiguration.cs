using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Defines the contract for amazon sqs bus configuration.
/// </summary>
public interface IAmazonSqsBusConfiguration :
    IBusConfiguration
{
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    new IAmazonSqsHostConfiguration HostConfiguration { get; }

    /// <summary>
    /// Gets the bus endpoint configuration value.
    /// </summary>
    new IAmazonSqsEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IAmazonSqsTopologyConfiguration Topology { get; }

    /// <summary>
    /// Create an endpoint configuration on the bus, which can later be turned into a receive endpoint
    /// </summary>
    /// <returns></returns>
    IAmazonSqsEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
