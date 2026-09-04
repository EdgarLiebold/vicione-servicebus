using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides extension methods for service bus broker topology.
/// </summary>
public static class ServiceBusBrokerTopologyExtensions
{
    /// <summary>
    /// Performs the log result operation.
    /// </summary>
    /// <param name="topology">The topology value.</param>
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
