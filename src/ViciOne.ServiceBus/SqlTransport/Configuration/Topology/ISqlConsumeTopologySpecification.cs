namespace ViciOne.ServiceBus.SqlTransport.Configuration
{
    using Topology;


    public interface ISqlConsumeTopologySpecification :
        ISpecification
    {
        void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
    }
}
