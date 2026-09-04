using ViciOne.ServiceBus.RabbitMqTransport.Topology;

namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public interface IRabbitMqPublishTopologySpecification :
    ISpecification
{
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
