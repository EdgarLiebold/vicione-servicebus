namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IRabbitMqEndpointConfiguration :
        IEndpointConfiguration
    {
        new IRabbitMqTopologyConfiguration Topology { get; }
    }
}
