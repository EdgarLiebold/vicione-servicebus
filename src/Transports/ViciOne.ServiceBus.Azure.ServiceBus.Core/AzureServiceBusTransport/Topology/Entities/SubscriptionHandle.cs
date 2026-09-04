using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

public interface SubscriptionHandle :
    EntityHandle
{
    Subscription Subscription { get; }
}
