using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

public interface QueueSubscriptionHandle :
    EntityHandle
{
    QueueSubscription QueueSubscription { get; }
}
