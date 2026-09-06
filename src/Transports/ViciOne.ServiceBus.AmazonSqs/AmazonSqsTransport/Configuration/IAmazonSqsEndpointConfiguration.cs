using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Exposes transport-neutral endpoint settings with Amazon SQS topology configuration.</summary>
public interface IAmazonSqsEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>Gets the Amazon SQS topology configuration.</summary>
    new IAmazonSqsTopologyConfiguration Topology { get; }
}
