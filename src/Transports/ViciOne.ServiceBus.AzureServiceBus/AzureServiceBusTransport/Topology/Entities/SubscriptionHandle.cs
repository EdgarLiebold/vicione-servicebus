using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Defines the contract for subscription handle.
/// </summary>
public interface SubscriptionHandle :
    EntityHandle
{
    /// <summary>
    /// Gets the subscription value.
    /// </summary>
    Subscription Subscription { get; }
}
