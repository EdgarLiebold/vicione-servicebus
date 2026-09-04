using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration;

public interface IActiveMqReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IActiveMqEndpointConfiguration
{
    ReceiveSettings Settings { get; }

    void Build(IHost host);
}
