using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

public interface QueueSubscriptionHandle :
    EntityHandle
{
    QueueSubscription QueueSubscription { get; }
}
