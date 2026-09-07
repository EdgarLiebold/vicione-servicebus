using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Connects transport-specific receive endpoints to a running bus through dependency-injection configuration.</summary>
/// <typeparam name="TEndpointConfigurator">The transport-specific endpoint configurator exposed to callers.</typeparam>
public interface IReceiveEndpointConnector<out TEndpointConfigurator> :
    IReceiveEndpointConnector
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    /// <summary>Creates and starts an endpoint described by a transport-independent definition.</summary>
    /// <param name="definition">The definition that supplies endpoint identity and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name.</param>
    /// <param name="configure">An optional callback that configures the transport-specific endpoint using registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, TEndpointConfigurator>? configure = null);

    /// <summary>Creates and starts a transport-specific endpoint on a named queue.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <param name="configure">An optional callback that configures the transport-specific endpoint using registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IBusRegistrationContext, TEndpointConfigurator>? configure = null);
}

/// <summary>Connects transport-neutral receive endpoints to a running bus through dependency-injection configuration.</summary>
public interface IReceiveEndpointConnector
{
    /// <summary>Creates and starts an endpoint described by a transport-independent definition.</summary>
    /// <param name="definition">The definition that supplies endpoint identity and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name.</param>
    /// <param name="configure">An optional callback that configures the endpoint using registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null);

    /// <summary>Creates and starts an endpoint on a named transport queue.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <param name="configure">An optional callback that configures the endpoint using registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null);
}
