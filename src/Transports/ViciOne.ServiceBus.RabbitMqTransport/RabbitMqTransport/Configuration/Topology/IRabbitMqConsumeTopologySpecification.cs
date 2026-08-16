namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration
{
    using Topology;


    public interface IRabbitMqConsumeTopologySpecification :
        ISpecification
    {
        void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
    }
}
