using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Hosts ActiveMQ receive endpoints.</summary>
public interface IActiveMqHost :
    IHost<IActiveMqReceiveEndpointConfigurator>
{
}
