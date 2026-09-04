using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

public interface TopicSubscriptionHandle :
    EntityHandle
{
    TopicToTopicSubscription Subscription { get; }
}
