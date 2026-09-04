using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Defines the contract for topic subscription handle.
/// </summary>
public interface TopicSubscriptionHandle :
    EntityHandle
{
    /// <summary>
    /// Gets the topic subscription value.
    /// </summary>
    TopicSubscription TopicSubscription { get; }
}
