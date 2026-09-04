using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Defines the contract for amazon sqs endpoint configuration.
/// </summary>
public interface IAmazonSqsEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IAmazonSqsTopologyConfiguration Topology { get; }
}
