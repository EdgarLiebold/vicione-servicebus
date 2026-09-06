using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Represents an Amazon SQS host that creates receive endpoints and exposes bus topology.</summary>
public interface IAmazonSqsHost :
    IHost<IAmazonSqsReceiveEndpointConfigurator>
{
    /// <summary>Gets the Amazon SQS bus topology.</summary>
    new IAmazonSqsBusTopology Topology { get; }
}
