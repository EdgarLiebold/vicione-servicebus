using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Identifies a subscription stored in a broker-topology builder.</summary>
public interface SubscriptionHandle :
    EntityHandle
{
    /// <summary>Gets the subscription declaration represented by the handle.</summary>
    Subscription Subscription { get; }
}
