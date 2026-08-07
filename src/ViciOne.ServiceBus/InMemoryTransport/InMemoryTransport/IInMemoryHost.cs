// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.InMemoryTransport
{
    using Transports;


    public interface IInMemoryHost :
        IHost<IInMemoryReceiveEndpointConfigurator>
    {
        IInMemoryDelayProvider DelayProvider { get; }
    }
}
