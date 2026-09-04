using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public interface IActiveMqHost :
    IHost<IActiveMqReceiveEndpointConfigurator>
{
}
