using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq host.
/// </summary>
public interface IActiveMqHost :
    IHost<IActiveMqReceiveEndpointConfigurator>
{
}
