using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Identifies a topic-to-queue subscription within its owning SQL topology builder.</summary>
public interface QueueSubscriptionHandle :
    EntityHandle
{
    /// <summary>Gets the subscription.</summary>
    TopicToQueueSubscription Subscription { get; }
}
