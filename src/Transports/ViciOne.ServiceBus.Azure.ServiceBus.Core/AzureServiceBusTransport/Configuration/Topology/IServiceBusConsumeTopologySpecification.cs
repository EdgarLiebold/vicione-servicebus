using ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;

public interface IServiceBusConsumeTopologySpecification :
    ISpecification
{
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
