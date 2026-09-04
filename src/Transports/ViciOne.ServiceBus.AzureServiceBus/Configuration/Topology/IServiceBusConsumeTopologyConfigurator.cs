using System;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus consume topology configurator.
/// </summary>
public interface IServiceBusConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IServiceBusConsumeTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IServiceBusMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    void AddSpecification(IServiceBusConsumeTopologySpecification specification);

    /// <summary>
    /// Create a topic subscription on the endpoint
    /// </summary>
    /// <param name="topicName">The topic name</param>
    /// <param name="subscriptionName">The name for the subscription</param>
    /// <param name="callback">Configure the exchange and binding</param>
    void Subscribe(string topicName, string subscriptionName, Action<IServiceBusSubscriptionConfigurator>? callback = null);
}
