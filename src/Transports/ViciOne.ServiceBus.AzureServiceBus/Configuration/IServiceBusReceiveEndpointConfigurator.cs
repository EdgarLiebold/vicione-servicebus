using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures an Azure Service Bus queue receive endpoint and its additional topic subscriptions.</summary>
public interface IServiceBusReceiveEndpointConfigurator :
    IReceiveEndpointConfigurator,
    IServiceBusQueueEndpointConfigurator
{
    /// <summary>
    /// Sets whether shutdown removes the forwarding subscriptions created for this endpoint. Enable this for
    /// auto-delete queues so subscriptions do not outlive their destination queue and consume the namespace quota.
    /// </summary>
    bool RemoveSubscriptions { set; }

    /// <summary>Adds a named topic subscription that forwards messages to this endpoint's queue.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="subscriptionName">The name for the subscription.</param>
    /// <param name="callback">The callback that configures the topic subscription.</param>
    void Subscribe(string topicName, string subscriptionName, Action<IServiceBusSubscriptionConfigurator>? callback = null);

    /// <summary>Adds a subscription from a message type's publish topic to this endpoint's queue.</summary>
    /// <typeparam name="T">The message type whose publish topology supplies the topic.</typeparam>
    /// <param name="subscriptionName">The name for the subscription.</param>
    /// <param name="callback">An optional callback that configures the topic subscription.</param>
    void Subscribe<T>(string subscriptionName, Action<IServiceBusSubscriptionConfigurator>? callback = null)
        where T : class;
}
