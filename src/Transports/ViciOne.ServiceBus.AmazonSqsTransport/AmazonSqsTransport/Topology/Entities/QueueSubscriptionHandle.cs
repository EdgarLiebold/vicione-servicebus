namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

using ViciOne.ServiceBus.Topology;


public interface QueueSubscriptionHandle :
    EntityHandle
{
    QueueSubscription QueueSubscription { get; }
}
