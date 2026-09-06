using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Logs the topics and subscriptions contained in an Azure Service Bus topology.</summary>
public static class ServiceBusBrokerTopologyExtensions
{
    /// <summary>Writes one informational log entry for every topic and subscription.</summary>
    /// <param name="topology">The topology to describe.</param>
    public static void LogResult(this BrokerTopology topology)
    {
        foreach (var topic in topology.Topics)
            LogContext.Info?.Log("Topic: {Topic}", topic.CreateTopicOptions.Name);

        foreach (var subscription in topology.Subscriptions)
        {
            LogContext.Info?.Log("Subscription: {Subscription}, topic: {Topic}", subscription.CreateSubscriptionOptions.SubscriptionName,
                subscription.CreateSubscriptionOptions.TopicName);
        }
    }
}
