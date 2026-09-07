using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Hosts Azure Service Bus receive endpoints and their shared namespace connection resources.</summary>
public interface IServiceBusHost :
    IHost<IServiceBusReceiveEndpointConfigurator>
{
    /// <summary>Connects a subscription endpoint to the publish topic for a message contract.</summary>
    /// <typeparam name="T">The subscribed message contract.</typeparam>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">An optional callback that configures the subscription endpoint.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    IHostReceiveEndpointHandle ConnectSubscriptionEndpoint<T>(string subscriptionName, Action<IServiceBusSubscriptionEndpointConfigurator>? configure = null)
        where T : class;

    /// <summary>Connects a subscription endpoint to a named topic.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="topicName">The namespace-relative topic name.</param>
    /// <param name="configure">An optional callback that configures the subscription endpoint.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    IHostReceiveEndpointHandle ConnectSubscriptionEndpoint(string subscriptionName, string topicName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure = null);
}
