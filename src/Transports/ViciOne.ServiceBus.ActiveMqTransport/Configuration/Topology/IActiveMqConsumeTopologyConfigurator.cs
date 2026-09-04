using ViciOne.ServiceBus.ActiveMqTransport;
using ViciOne.ServiceBus.ActiveMqTransport.Topology;

#nullable enable
namespace ViciOne.ServiceBus;

public interface IActiveMqConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IActiveMqConsumeTopology
{
    new IActiveMqConsumerEndpointQueueNameFormatter? ConsumerEndpointQueueNameFormatter { set; }

    new IActiveMqTemporaryQueueNameFormatter? TemporaryQueueNameFormatter { set; }

    new IActiveMqMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    void AddSpecification(IActiveMqConsumeTopologySpecification specification);
}
