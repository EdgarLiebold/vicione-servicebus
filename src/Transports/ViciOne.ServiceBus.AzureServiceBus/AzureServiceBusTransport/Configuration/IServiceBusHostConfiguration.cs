using System;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Defines the contract for service bus host configuration.
/// </summary>
public interface IServiceBusHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<IServiceBusReceiveEndpointConfigurator>
{
    /// <summary>
    /// Gets or sets the settings value.
    /// </summary>
    ServiceBusHostSettings Settings { get; set; }

    /// <summary>
    /// Gets the base path value.
    /// </summary>
    string BasePath { get; }

    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IServiceBusBusTopology Topology { get; }

    /// <summary>
    /// Apply the endpoint definition to the receive endpoint configurator
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="definition"></param>
    void ApplyEndpointDefinition(IServiceBusReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IServiceBusReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IServiceBusReceiveEndpointConfigurator>? configure = null);

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IServiceBusReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(ReceiveEndpointSettings settings, IServiceBusEndpointConfiguration
        endpointConfiguration, Action<IServiceBusReceiveEndpointConfigurator>? configure = null);

    /// <summary>
    /// Performs the subscription endpoint operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="configure">The configuration callback.</param>
    void SubscriptionEndpoint<T>(string subscriptionName, Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
        where T : class;

    /// <summary>
    /// Performs the subscription endpoint operation.
    /// </summary>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="topicPath">The topic path value.</param>
    /// <param name="configure">The configuration callback.</param>
    void SubscriptionEndpoint(string subscriptionName, string topicPath, Action<IServiceBusSubscriptionEndpointConfigurator>? configure);

    /// <summary>
    /// Sets namespace separator to tilde.
    /// </summary>
    void SetNamespaceSeparatorToTilde();

    /// <summary>
    /// Sets namespace separator to underscore.
    /// </summary>
    void SetNamespaceSeparatorToUnderscore();

    /// <summary>
    /// Sets namespace separator to.
    /// </summary>
    /// <param name="separator">The separator value.</param>
    void SetNamespaceSeparatorTo(string separator);

    /// <summary>
    /// Creates subscription endpoint configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IServiceBusSubscriptionEndpointConfiguration CreateSubscriptionEndpointConfiguration<T>(string subscriptionName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
        where T : class;

    /// <summary>
    /// Creates subscription endpoint configuration.
    /// </summary>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="topicPath">The topic path value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IServiceBusSubscriptionEndpointConfiguration CreateSubscriptionEndpointConfiguration(string subscriptionName, string topicPath,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure);
}
