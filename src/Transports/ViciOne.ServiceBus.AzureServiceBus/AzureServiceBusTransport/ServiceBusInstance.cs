using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus instance implementation.
/// </summary>
public class ServiceBusInstance :
    TransportBusInstance<IServiceBusReceiveEndpointConfigurator>,
    ISubscriptionEndpointConnector
{
    readonly IServiceBusHost _host;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busControl">The bus control value.</param>
    /// <param name="host">The host value.</param>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busRegistrationContext">The bus registration context value.</param>
    public ServiceBusInstance(IBusControl busControl, IHost<IServiceBusReceiveEndpointConfigurator> host, IHostConfiguration hostConfiguration,
        IBusRegistrationContext busRegistrationContext)
        : base(busControl, host, hostConfiguration, busRegistrationContext)
    {
        _host = host as IServiceBusHost ?? throw new ArgumentException("Host was not an IServiceBusHost", nameof(host));
    }

    /// <summary>
    /// Connects subscription endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectSubscriptionEndpoint<T>(string subscriptionName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure = null)
        where T : class
    {
        return _host.ConnectSubscriptionEndpoint<T>(subscriptionName, configure);
    }

    /// <summary>
    /// Connects subscription endpoint.
    /// </summary>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectSubscriptionEndpoint(string subscriptionName, string topicName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure = null)
    {
        return _host.ConnectSubscriptionEndpoint(subscriptionName, topicName, configure);
    }

    /// <summary>
    /// Connects subscription endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectSubscriptionEndpoint<T>(string subscriptionName,
        Action<IBusRegistrationContext, IServiceBusSubscriptionEndpointConfigurator>? configure = null)
        where T : class
    {
        return _host.ConnectSubscriptionEndpoint<T>(subscriptionName, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints().Configure(subscriptionName, configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }

    /// <summary>
    /// Connects subscription endpoint.
    /// </summary>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectSubscriptionEndpoint(string subscriptionName, string topicName,
        Action<IBusRegistrationContext, IServiceBusSubscriptionEndpointConfigurator>? configure = null)
    {
        return _host.ConnectSubscriptionEndpoint(subscriptionName, topicName, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints().Configure(subscriptionName, configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }
}
