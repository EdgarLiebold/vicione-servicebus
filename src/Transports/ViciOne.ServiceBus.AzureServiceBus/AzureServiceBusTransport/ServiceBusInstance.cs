using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Represents a running Azure Service Bus bus and exposes connectable subscription endpoints.</summary>
public class ServiceBusInstance :
    TransportBusInstance<IServiceBusReceiveEndpointConfigurator>,
    ISubscriptionEndpointConnector
{
    readonly IServiceBusHost _host;

    /// <summary>Creates a running bus instance around its control and host.</summary>
    /// <param name="busControl">The bus lifecycle control.</param>
    /// <param name="host">The Azure Service Bus host.</param>
    /// <param name="hostConfiguration">The active host configuration.</param>
    /// <param name="busRegistrationContext">The service registration context.</param>
    public ServiceBusInstance(IBusControl busControl, IHost<IServiceBusReceiveEndpointConfigurator> host, IHostConfiguration hostConfiguration,
        IBusRegistrationContext busRegistrationContext)
        : base(busControl, host, hostConfiguration, busRegistrationContext)
    {
        _host = host as IServiceBusHost ?? throw new ArgumentException("Host was not an IServiceBusHost", nameof(host));
    }

    /// <summary>Connects a subscription endpoint to the publish topic for a message contract.</summary>
    /// <typeparam name="T">The subscribed message contract.</typeparam>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">Optionally configures the subscription endpoint.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectSubscriptionEndpoint<T>(string subscriptionName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure = null)
        where T : class
    {
        return _host.ConnectSubscriptionEndpoint<T>(subscriptionName, configure);
    }

    /// <summary>Connects a subscription endpoint to a named topic.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="topicName">The namespace-relative topic name.</param>
    /// <param name="configure">Optionally configures the subscription endpoint.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectSubscriptionEndpoint(string subscriptionName, string topicName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure = null)
    {
        return _host.ConnectSubscriptionEndpoint(subscriptionName, topicName, configure);
    }

    /// <summary>Connects a subscription endpoint and applies registration-based endpoint configuration.</summary>
    /// <typeparam name="T">The subscribed message contract.</typeparam>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">Optionally configures the endpoint with access to registration services.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectSubscriptionEndpoint<T>(string subscriptionName,
        Action<IBusRegistrationContext, IServiceBusSubscriptionEndpointConfigurator>? configure = null)
        where T : class
    {
        return _host.ConnectSubscriptionEndpoint<T>(subscriptionName, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints().Configure(subscriptionName, configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }

    /// <summary>Connects a named-topic subscription and applies registration-based endpoint configuration.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="topicName">The namespace-relative topic name.</param>
    /// <param name="configure">Optionally configures the endpoint with access to registration services.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectSubscriptionEndpoint(string subscriptionName, string topicName,
        Action<IBusRegistrationContext, IServiceBusSubscriptionEndpointConfigurator>? configure = null)
    {
        return _host.ConnectSubscriptionEndpoint(subscriptionName, topicName, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints().Configure(subscriptionName, configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }
}
