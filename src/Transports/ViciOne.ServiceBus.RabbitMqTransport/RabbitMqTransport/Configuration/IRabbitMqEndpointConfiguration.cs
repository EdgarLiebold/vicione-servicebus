using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public interface IRabbitMqEndpointConfiguration :
    IEndpointConfiguration
{
    new IRabbitMqTopologyConfiguration Topology { get; }
}
