using System;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Configures an Azure Service Bus namespace and the receive endpoints hosted in it.</summary>
public interface IServiceBusHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<IServiceBusReceiveEndpointConfigurator>
{
    /// <summary>Gets or sets the resolved namespace settings.</summary>
    ServiceBusHostSettings Settings { get; set; }

    /// <summary>Gets the namespace-relative base path applied to entity addresses.</summary>
    string BasePath { get; }

    /// <summary>Gets the supervisor that owns the shared namespace connection.</summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>Gets the bus topology for the configured namespace.</summary>
    new IServiceBusBusTopology Topology { get; }

    /// <summary>Applies a transport-independent endpoint definition to an Azure Service Bus endpoint configurator.</summary>
    /// <param name="configurator">The receive-endpoint configurator to update.</param>
    /// <param name="definition">The endpoint definition to apply.</param>
    void ApplyEndpointDefinition(IServiceBusReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>Creates a receive-endpoint configuration for a queue name.</summary>
    /// <param name="queueName">The queue name relative to the namespace.</param>
    /// <param name="configure">An optional callback that configures the queue endpoint.</param>
    /// <returns>The configured queue receive endpoint.</returns>
    IServiceBusReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IServiceBusReceiveEndpointConfigurator>? configure = null);

    /// <summary>Creates a queue receive endpoint from precomputed entity and endpoint settings.</summary>
    /// <param name="settings">The queue and processor settings.</param>
    /// <param name="endpointConfiguration">The endpoint-level pipeline and topology configuration.</param>
    /// <param name="configure">An optional callback that further configures the queue endpoint.</param>
    /// <returns>The configured queue receive endpoint.</returns>
    IServiceBusReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(ReceiveEndpointSettings settings, IServiceBusEndpointConfiguration
        endpointConfiguration, Action<IServiceBusReceiveEndpointConfigurator>? configure = null);

    /// <summary>Creates and registers a subscription endpoint for the topic associated with a message type.</summary>
    /// <typeparam name="T">The message type whose publish topology supplies the topic.</typeparam>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">An optional callback that configures the subscription endpoint.</param>
    void SubscriptionEndpoint<T>(string subscriptionName, Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
        where T : class;

    /// <summary>Creates and registers a subscription endpoint for an explicit topic path.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="topicPath">The topic path.</param>
    /// <param name="configure">An optional callback that configures the subscription endpoint.</param>
    void SubscriptionEndpoint(string subscriptionName, string topicPath, Action<IServiceBusSubscriptionEndpointConfigurator>? configure);

    /// <summary>Uses a tilde when formatting namespace segments in entity names.</summary>
    void SetNamespaceSeparatorToTilde();

    /// <summary>Uses an underscore when formatting namespace segments in entity names.</summary>
    void SetNamespaceSeparatorToUnderscore();

    /// <summary>Uses the specified separator when formatting namespace segments in entity names.</summary>
    /// <param name="separator">The separator inserted between namespace segments.</param>
    void SetNamespaceSeparatorTo(string separator);

    /// <summary>Creates a subscription-endpoint configuration for the topic associated with a message type.</summary>
    /// <typeparam name="T">The message type whose publish topology supplies the topic.</typeparam>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">An optional callback that configures the subscription endpoint.</param>
    /// <returns>The configured subscription receive endpoint.</returns>
    IServiceBusSubscriptionEndpointConfiguration CreateSubscriptionEndpointConfiguration<T>(string subscriptionName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
        where T : class;

    /// <summary>Creates a subscription-endpoint configuration for an explicit topic path.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="topicPath">The topic path.</param>
    /// <param name="configure">An optional callback that configures the subscription endpoint.</param>
    /// <returns>The configured subscription receive endpoint.</returns>
    IServiceBusSubscriptionEndpointConfiguration CreateSubscriptionEndpointConfiguration(string subscriptionName, string topicPath,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure);
}
