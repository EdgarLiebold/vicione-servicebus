using System;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures subscriptions applied to Azure Service Bus receive endpoints.</summary>
public interface IServiceBusConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IServiceBusConsumeTopology
{
    /// <summary>Gets the consume topology for a message contract.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <returns>The message-specific consume topology.</returns>
    new IServiceBusMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Adds a subscription specification to the endpoint topology.</summary>
    /// <param name="specification">The specification to apply when topology is built.</param>
    void AddSpecification(IServiceBusConsumeTopologySpecification specification);

    /// <summary>Adds a topic subscription to the receive endpoint topology.</summary>
    /// <param name="topicName">The namespace-relative topic name.</param>
    /// <param name="subscriptionName">The name for the subscription.</param>
    /// <param name="callback">Optionally configures the subscription.</param>
    void Subscribe(string topicName, string subscriptionName, Action<IServiceBusSubscriptionConfigurator>? callback = null);
}
