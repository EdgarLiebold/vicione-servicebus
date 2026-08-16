namespace ViciOne.ServiceBus.InMemoryTransport
{
    using Transports;


    public interface IInMemoryHost :
        IHost<IInMemoryReceiveEndpointConfigurator>
    {
        IInMemoryDelayProvider DelayProvider { get; }
    }
}
