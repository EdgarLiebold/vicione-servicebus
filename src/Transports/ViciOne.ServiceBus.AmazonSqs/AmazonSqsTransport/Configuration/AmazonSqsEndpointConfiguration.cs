using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Combines transport-neutral endpoint settings with Amazon SQS topology configuration.</summary>
public class AmazonSqsEndpointConfiguration :
    EndpointConfiguration,
    IAmazonSqsEndpointConfiguration
{
    /// <summary>Initializes a root Amazon SQS endpoint configuration.</summary>
    /// <param name="topologyConfiguration">The topology configuration used by the endpoint.</param>
    public AmazonSqsEndpointConfiguration(IAmazonSqsTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration)
    {
        Topology = topologyConfiguration;
    }

    AmazonSqsEndpointConfiguration(IEndpointConfiguration parentConfiguration, IAmazonSqsTopologyConfiguration topologyConfiguration, bool isBusEndpoint)
        : base(parentConfiguration, topologyConfiguration, isBusEndpoint)
    {
        Topology = topologyConfiguration;
    }

    /// <summary>Gets the Amazon SQS topology configuration.</summary>
    public new IAmazonSqsTopologyConfiguration Topology { get; }

    /// <summary>Creates a child endpoint configuration with a cloned topology scope.</summary>
    /// <param name="isBusEndpoint">Whether the child configures the bus endpoint.</param>
    /// <returns>The child Amazon SQS endpoint configuration.</returns>
    public IAmazonSqsEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(Topology);

        return new AmazonSqsEndpointConfiguration(this, topologyConfiguration, isBusEndpoint);
    }
}
