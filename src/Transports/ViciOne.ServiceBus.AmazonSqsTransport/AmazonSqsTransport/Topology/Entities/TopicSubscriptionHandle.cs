namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

using ViciOne.ServiceBus.Topology;


public interface TopicSubscriptionHandle :
    EntityHandle
{
    TopicSubscription TopicSubscription { get; }
}
