namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IServiceBusEndpointConfiguration :
        IEndpointConfiguration
    {
        new IServiceBusTopologyConfiguration Topology { get; }
    }
}
