using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects transport-specific receive endpoints to a host.</summary>
/// <typeparam name="TEndpointConfigurator">The transport-specific endpoint configurator.</typeparam>
public interface IReceiveConnector<out TEndpointConfigurator> :
    IReceiveConnector
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    /// <summary>Connects a receive endpoint described by a transport-independent definition.</summary>
    /// <param name="definition">The definition that supplies endpoint identity and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name, or <see langword="null" /> to use the host default.</param>
    /// <param name="configureEndpoint">An optional callback that applies transport-specific settings.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<TEndpointConfigurator>? configureEndpoint = null);

    /// <summary>Connects a receive endpoint for a named transport queue.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <param name="configureEndpoint">An optional callback that applies transport-specific settings.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<TEndpointConfigurator>? configureEndpoint = null);
}


/// <summary>Connects transport-neutral receive endpoints to a host.</summary>
public interface IReceiveConnector :
    IEndpointConfigurationObserverConnector
{
    /// <summary>Connects a receive endpoint described by a transport-independent definition.</summary>
    /// <param name="definition">The definition that supplies endpoint identity and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name, or <see langword="null" /> to use the host default.</param>
    /// <param name="configureEndpoint">An optional callback that applies transport-neutral settings.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null);

    /// <summary>Connects a receive endpoint for a named transport queue.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <param name="configureEndpoint">An optional callback that applies transport-neutral settings.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null);
}
