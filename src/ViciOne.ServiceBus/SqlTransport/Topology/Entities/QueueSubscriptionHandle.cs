using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Defines the contract for queue subscription handle.
/// </summary>
public interface QueueSubscriptionHandle :
    EntityHandle
{
    /// <summary>
    /// Gets the subscription value.
    /// </summary>
    TopicToQueueSubscription Subscription { get; }
}
