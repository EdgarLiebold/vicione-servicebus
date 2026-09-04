using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

public interface TopicSubscriptionHandle :
    EntityHandle
{
    TopicSubscription TopicSubscription { get; }
}
