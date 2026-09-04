using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

public interface IInMemoryHost :
    IHost<IInMemoryReceiveEndpointConfigurator>
{
    IInMemoryDelayProvider DelayProvider { get; }
}
