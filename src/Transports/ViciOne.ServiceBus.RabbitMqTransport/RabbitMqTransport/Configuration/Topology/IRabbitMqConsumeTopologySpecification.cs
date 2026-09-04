using ViciOne.ServiceBus.RabbitMqTransport.Topology;

namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public interface IRabbitMqConsumeTopologySpecification :
    ISpecification
{
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
