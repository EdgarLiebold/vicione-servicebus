using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus;

public interface ISqlPublishTopology :
    IPublishTopology
{
    new ISqlMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;

    BrokerTopology GetPublishBrokerTopology();
}
