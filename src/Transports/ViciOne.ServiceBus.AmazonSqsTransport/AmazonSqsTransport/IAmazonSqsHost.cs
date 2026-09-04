using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public interface IAmazonSqsHost :
    IHost<IAmazonSqsReceiveEndpointConfigurator>
{
    new IAmazonSqsBusTopology Topology { get; }
}
