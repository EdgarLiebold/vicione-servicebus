using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Identifies a topic-to-topic subscription within its owning SQL topology builder.</summary>
public interface TopicSubscriptionHandle :
    EntityHandle
{
    /// <summary>Gets the subscription.</summary>
    TopicToTopicSubscription Subscription { get; }
}
