namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration
{
    using Topology;


    public interface IServiceBusConsumeTopologySpecification :
        ISpecification
    {
        void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
    }
}
