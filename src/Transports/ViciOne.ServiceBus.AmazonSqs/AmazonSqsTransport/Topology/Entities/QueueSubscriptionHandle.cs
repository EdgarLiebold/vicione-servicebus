using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Identifies a topic-to-queue subscription within a broker-topology builder.</summary>
public interface QueueSubscriptionHandle :
    EntityHandle
{
    /// <summary>Gets the subscription declaration represented by the handle.</summary>
    QueueSubscription QueueSubscription { get; }
}
