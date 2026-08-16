namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration
{
    using Topology;


    public interface IRabbitMqPublishTopologySpecification :
        ISpecification
    {
        void Apply(IPublishEndpointBrokerTopologyBuilder builder);
    }
}
