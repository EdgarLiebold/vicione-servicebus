using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures an Azure Service Bus bus, its namespace, endpoints, and message topology.</summary>
public interface IServiceBusBusFactoryConfigurator :
    IBusFactoryConfigurator<IServiceBusReceiveEndpointConfigurator>,
    IServiceBusQueueEndpointConfigurator
{
    /// <summary>Gets the Azure Service Bus send topology.</summary>
    new IServiceBusSendTopologyConfigurator SendTopology { get; }

    /// <summary>Gets the Azure Service Bus publish topology.</summary>
    new IServiceBusPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>Configures send topology for a message type.</summary>
    /// <typeparam name="T">The message type to configure.</typeparam>
    /// <param name="configureTopology">The callback that configures message-specific send conventions.</param>
    void Send<T>(Action<IServiceBusMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>Configures publish topology for a message type.</summary>
    /// <typeparam name="T">The message type to configure.</typeparam>
    /// <param name="configureTopology">An optional callback that configures its topic.</param>
    void Publish<T>(Action<IServiceBusMessagePublishTopologyConfigurator<T>>? configureTopology = null)
        where T : class;

    /// <summary>Configures publish topology for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that configures its topic.</param>
    void Publish(Type messageType, Action<IServiceBusMessagePublishTopologyConfigurator>? configure = null);

    /// <summary>
    /// Overrides the generated bus endpoint queue name. The value must not match a receive endpoint queue,
    /// because the competing consumers would make message ownership ambiguous.
    /// </summary>
    /// <param name="value">The unique queue name to use for the bus endpoint.</param>
    void OverrideDefaultBusEndpointQueueName(string value);

    /// <summary>Sets the namespace separator to tilde instead of slash, which is compatible with managed identities and RBAC.</summary>
    void SetNamespaceSeparatorToTilde();

    /// <summary>
    /// Sets the namespace separator to underscore instead of slash, which is compatible with managed identities and RBAC.
    /// This is automatically set when using a managed identity token provider.
    /// </summary>
    void SetNamespaceSeparatorToUnderscore();

    /// <summary>Sets the namespace separator to the specified string instead of slash.</summary>
    /// <param name="separator">The separator inserted between namespace segments.</param>
    void SetNamespaceSeparatorTo(string separator);

    /// <summary>Applies fully resolved namespace settings.</summary>
    /// <param name="settings">The namespace, authentication, retry, and transport settings.</param>
    void Host(ServiceBusHostSettings settings);

    /// <summary>Declares a subscription endpoint for a message type's publish topic.</summary>
    /// <typeparam name="T">The message type whose publish topology supplies the topic.</typeparam>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">The callback that configures the subscription endpoint and its consumers.</param>
    void SubscriptionEndpoint<T>(string subscriptionName, Action<IServiceBusSubscriptionEndpointConfigurator> configure)
        where T : class;

    /// <summary>Declares a subscription endpoint for an explicit topic path.</summary>
    /// <param name="subscriptionName">The name of the subscription.</param>
    /// <param name="topicPath">The topic name to subscribe.</param>
    /// <param name="configure">The callback that configures the subscription endpoint and its consumers.</param>
    void SubscriptionEndpoint(string subscriptionName, string topicPath, Action<IServiceBusSubscriptionEndpointConfigurator> configure);
}
