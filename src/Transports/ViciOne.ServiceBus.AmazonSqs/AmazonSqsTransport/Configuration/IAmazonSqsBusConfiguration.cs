using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Coordinates Amazon SQS host, endpoint, routing, and topology configuration for a bus.</summary>
public interface IAmazonSqsBusConfiguration :
    IBusConfiguration
{
    /// <summary>Gets the Amazon SQS host configuration.</summary>
    new IAmazonSqsHostConfiguration HostConfiguration { get; }

    /// <summary>Gets the configuration inherited by the default bus endpoint.</summary>
    new IAmazonSqsEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>Gets the Amazon SQS transport topology configuration.</summary>
    new IAmazonSqsTopologyConfiguration Topology { get; }

    /// <summary>Creates a child endpoint configuration with its own consume-topology scope.</summary>
    /// <param name="isBusEndpoint">Whether the child configures the default bus endpoint.</param>
    /// <returns>The child Amazon SQS endpoint configuration.</returns>
    IAmazonSqsEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
