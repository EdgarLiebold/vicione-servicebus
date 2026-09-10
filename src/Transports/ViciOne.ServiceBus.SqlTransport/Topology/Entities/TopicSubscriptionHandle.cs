using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Controls the lifetime of topic subscription.</summary>
public interface TopicSubscriptionHandle :
    EntityHandle
{
    /// <summary>Gets the subscription.</summary>
    TopicToTopicSubscription Subscription { get; }
}
