using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by receive connector.</summary>
/// <typeparam name="TEndpointConfigurator">The endpoint configurator type.</typeparam>
public interface IReceiveConnector<out TEndpointConfigurator> :
    IReceiveConnector
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    /// <summary>Adds a receive endpoint.</summary>
    /// <param name="definition">An endpoint definition, which abstracts specific endpoint behaviors from the transport.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureEndpoint">The configuration callback.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<TEndpointConfigurator>? configureEndpoint = null);

    /// <summary>Adds a receive endpoint.</summary>
    /// <param name="queueName">The queue name for the receive endpoint.</param>
    /// <param name="configureEndpoint">The configuration callback.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<TEndpointConfigurator>? configureEndpoint = null);
}


/// <summary>Defines the operations required by receive connector.</summary>
public interface IReceiveConnector :
    IEndpointConfigurationObserverConnector
{
    /// <summary>Adds a receive endpoint.</summary>
    /// <param name="definition">An endpoint definition, which abstracts specific endpoint behaviors from the transport.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureEndpoint">The configuration callback.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null);

    /// <summary>Adds a receive endpoint.</summary>
    /// <param name="queueName">The queue name for the receive endpoint.</param>
    /// <param name="configureEndpoint">The configuration callback.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null);
}
