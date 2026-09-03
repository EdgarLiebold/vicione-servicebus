namespace ViciOne.ServiceBus.InMemoryTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;
    using Transports;


    public interface IInMemoryReceiveEndpointConfiguration :
        IReceiveEndpointConfiguration,
        IInMemoryEndpointConfiguration
    {
        IInMemoryReceiveEndpointConfigurator Configurator { get; }

        void Build(IHost host);
    }
}
