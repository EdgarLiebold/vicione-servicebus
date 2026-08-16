namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

using ViciOne.ServiceBus.Configuration;
using Transports;


public interface IAmazonSqsReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IAmazonSqsEndpointConfiguration
{
    ReceiveSettings Settings { get; }

    void Build(IHost host);
}
