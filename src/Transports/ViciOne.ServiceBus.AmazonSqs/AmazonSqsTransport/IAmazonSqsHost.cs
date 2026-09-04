using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs host.
/// </summary>
public interface IAmazonSqsHost :
    IHost<IAmazonSqsReceiveEndpointConfigurator>
{
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IAmazonSqsBusTopology Topology { get; }
}
