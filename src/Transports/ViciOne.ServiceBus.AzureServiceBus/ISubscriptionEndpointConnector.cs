using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Connects Azure Service Bus subscription endpoints that can be stopped independently of the bus.</summary>
public interface ISubscriptionEndpointConnector
{
    /// <summary>Connects a subscription endpoint to the publish topic for a message contract.</summary>
    /// <typeparam name="T">The topic message type.</typeparam>
    /// <param name="subscriptionName">The subscription name for this endpoint.</param>
    /// <param name="configure">Optionally configures the subscription endpoint.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    HostReceiveEndpointHandle ConnectSubscriptionEndpoint<T>(string subscriptionName, Action<IServiceBusSubscriptionEndpointConfigurator>? configure = null)
        where T : class;

    /// <summary>Connects a subscription endpoint to a named topic.</summary>
    /// <param name="subscriptionName">The subscription name for this endpoint.</param>
    /// <param name="topicName">The namespace-relative topic name.</param>
    /// <param name="configure">Optionally configures the subscription endpoint.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    HostReceiveEndpointHandle ConnectSubscriptionEndpoint(string subscriptionName, string topicName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure = null);

    /// <summary>Connects a subscription endpoint to the publish topic for a message contract.</summary>
    /// <typeparam name="T">The topic message type.</typeparam>
    /// <param name="subscriptionName">The subscription name for this endpoint.</param>
    /// <param name="configure">Optionally configures the endpoint with access to registration services.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    HostReceiveEndpointHandle ConnectSubscriptionEndpoint<T>(string subscriptionName, Action<IBusRegistrationContext,
        IServiceBusSubscriptionEndpointConfigurator>? configure = null)
        where T : class;

    /// <summary>Connects a subscription endpoint to a named topic.</summary>
    /// <param name="subscriptionName">The subscription name for this endpoint.</param>
    /// <param name="topicName">The namespace-relative topic name.</param>
    /// <param name="configure">Optionally configures the endpoint with access to registration services.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    HostReceiveEndpointHandle ConnectSubscriptionEndpoint(string subscriptionName, string topicName,
        Action<IBusRegistrationContext, IServiceBusSubscriptionEndpointConfigurator>? configure = null);
}
