using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Defines the contract for queue subscription handle.
/// </summary>
public interface QueueSubscriptionHandle :
    EntityHandle
{
    /// <summary>
    /// Gets the queue subscription value.
    /// </summary>
    QueueSubscription QueueSubscription { get; }
}
