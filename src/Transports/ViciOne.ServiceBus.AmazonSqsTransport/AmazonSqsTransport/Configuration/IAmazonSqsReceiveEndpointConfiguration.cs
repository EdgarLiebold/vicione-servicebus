using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

public interface IAmazonSqsReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IAmazonSqsEndpointConfiguration
{
    ReceiveSettings Settings { get; }

    void Build(IHost host);
}
