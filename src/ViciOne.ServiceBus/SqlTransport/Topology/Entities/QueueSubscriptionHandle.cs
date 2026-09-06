using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Controls the lifetime of queue subscription.</summary>
public interface QueueSubscriptionHandle :
    EntityHandle
{
    /// <summary>Gets the subscription.</summary>
    TopicToQueueSubscription Subscription { get; }
}
