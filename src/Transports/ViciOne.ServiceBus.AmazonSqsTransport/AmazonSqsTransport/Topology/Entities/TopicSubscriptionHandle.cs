// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

using ViciOne.ServiceBus.Topology;


public interface TopicSubscriptionHandle :
    EntityHandle
{
    TopicSubscription TopicSubscription { get; }
}
