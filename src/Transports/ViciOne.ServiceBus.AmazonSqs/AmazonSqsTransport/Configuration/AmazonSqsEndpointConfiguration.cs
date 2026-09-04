using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs endpoint configuration implementation.
/// </summary>
public class AmazonSqsEndpointConfiguration :
    EndpointConfiguration,
    IAmazonSqsEndpointConfiguration
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
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

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public new IAmazonSqsTopologyConfiguration Topology { get; }

    /// <summary>
    /// Creates endpoint configuration.
    /// </summary>
    /// <param name="isBusEndpoint">The is bus endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public IAmazonSqsEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(Topology);

        return new AmazonSqsEndpointConfiguration(this, topologyConfiguration, isBusEndpoint);
    }
}
