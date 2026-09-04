using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

public interface TopicSubscriptionHandle :
    EntityHandle
{
    TopicSubscription TopicSubscription { get; }
}
