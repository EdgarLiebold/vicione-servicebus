using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for receive configurator.
/// </summary>
/// <typeparam name="TEndpointConfigurator">The t endpoint configurator type.</typeparam>
public interface IReceiveConfigurator<out TEndpointConfigurator> :
    IReceiveConfigurator
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    /// <summary>
    /// Adds a receive endpoint
    /// </summary>
    /// <param name="definition">
    /// An endpoint definition, which abstracts specific endpoint behaviors from the transport
    /// </param>
    /// <param name="endpointNameFormatter"></param>
    /// <param name="configureEndpoint">The configuration callback</param>
    void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<TEndpointConfigurator>? configureEndpoint = null);

    /// <summary>
    /// Adds a receive endpoint
    /// </summary>
    /// <param name="queueName">The queue name for the receive endpoint</param>
    /// <param name="configureEndpoint">The configuration callback</param>
    void ReceiveEndpoint(string queueName, Action<TEndpointConfigurator> configureEndpoint);
}


/// <summary>
/// Defines the contract for receive configurator.
/// </summary>
public interface IReceiveConfigurator :
    IEndpointConfigurationObserverConnector
{
    /// <summary>
    /// Adds a receive endpoint
    /// </summary>
    /// <param name="definition">
    /// An endpoint definition, which abstracts specific endpoint behaviors from the transport
    /// </param>
    /// <param name="endpointNameFormatter"></param>
    /// <param name="configureEndpoint">The configuration callback</param>
    void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null);

    /// <summary>
    /// Adds a receive endpoint
    /// </summary>
    /// <param name="queueName">The queue name for the receive endpoint</param>
    /// <param name="configureEndpoint">The configuration callback</param>
    void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint);
}
