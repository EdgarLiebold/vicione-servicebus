namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;
    using Transports;


    public interface IServiceBusEntityEndpointConfiguration :
        IReceiveEndpointConfiguration,
        IServiceBusEndpointConfiguration
    {
        void Build(IHost host);
    }
}
