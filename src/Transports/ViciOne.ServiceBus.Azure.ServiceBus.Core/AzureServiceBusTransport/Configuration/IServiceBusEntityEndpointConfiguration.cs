using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;

public interface IServiceBusEntityEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IServiceBusEndpointConfiguration
{
    void Build(IHost host);
}
