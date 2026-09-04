using ViciOne.ServiceBus.ActiveMqTransport.Topology;

namespace ViciOne.ServiceBus;

public interface IActiveMqConsumeTopologySpecification :
    ISpecification
{
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
