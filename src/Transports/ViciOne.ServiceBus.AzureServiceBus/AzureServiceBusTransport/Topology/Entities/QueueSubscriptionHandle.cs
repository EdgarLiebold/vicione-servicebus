using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Identifies a topic-to-queue forwarding relationship stored in a broker-topology builder.</summary>
public interface QueueSubscriptionHandle :
    EntityHandle
{
    /// <summary>Gets the forwarding relationship represented by the handle.</summary>
    QueueSubscription QueueSubscription { get; }
}
