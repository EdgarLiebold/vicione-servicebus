using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

public interface IInMemoryReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IInMemoryEndpointConfiguration
{
    IInMemoryReceiveEndpointConfigurator Configurator { get; }

    void Build(IHost host);
}
