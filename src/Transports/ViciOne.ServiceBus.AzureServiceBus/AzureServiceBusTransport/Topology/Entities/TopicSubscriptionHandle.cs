using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Identifies a topic-to-topic forwarding relationship stored in a broker-topology builder.</summary>
public interface TopicSubscriptionHandle :
    EntityHandle
{
    /// <summary>Gets the forwarding relationship represented by the handle.</summary>
    TopicSubscription TopicSubscription { get; }
}
