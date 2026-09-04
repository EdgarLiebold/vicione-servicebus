using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

public interface QueueSubscriptionHandle :
    EntityHandle
{
    TopicToQueueSubscription Subscription { get; }
}
