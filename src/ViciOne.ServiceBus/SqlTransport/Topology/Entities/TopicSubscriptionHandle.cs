using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Defines the contract for topic subscription handle.
/// </summary>
public interface TopicSubscriptionHandle :
    EntityHandle
{
    /// <summary>
    /// Gets the subscription value.
    /// </summary>
    TopicToTopicSubscription Subscription { get; }
}
