using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Connects transport-specific receive endpoints to a running bus.</summary>
/// <typeparam name="TEndpointConfigurator">The endpoint configurator type.</typeparam>
public interface IReceiveEndpointConnector<out TEndpointConfigurator> :
    IReceiveEndpointConnector
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    /// <summary>Connects a receive endpoint to the bus.</summary>
    /// <param name="definition">The transport-independent endpoint definition.</param>
    /// <param name="endpointNameFormatter">Formats the endpoint name.</param>
    /// <param name="configure">Optionally configures the connected endpoint.</param>
    /// <returns>A handle that owns the connected endpoint.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, TEndpointConfigurator>? configure = null);

    /// <summary>Connects a receive endpoint to the bus.</summary>
    /// <param name="queueName">The receive queue name.</param>
    /// <param name="configure">Optionally configures the connected endpoint.</param>
    /// <returns>A handle that owns the connected endpoint.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IBusRegistrationContext, TEndpointConfigurator>? configure = null);
}

/// <summary>Connects transport-neutral receive endpoints to a running bus.</summary>
public interface IReceiveEndpointConnector
{
    /// <summary>Connects a receive endpoint to the bus.</summary>
    /// <param name="definition">The transport-independent endpoint definition.</param>
    /// <param name="endpointNameFormatter">Formats the endpoint name.</param>
    /// <param name="configure">Optionally configures the connected endpoint.</param>
    /// <returns>A handle that owns the connected endpoint.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null);

    /// <summary>Connects a receive endpoint to the bus.</summary>
    /// <param name="queueName">The receive queue name.</param>
    /// <param name="configure">Optionally configures the connected endpoint.</param>
    /// <returns>A handle that owns the connected endpoint.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null);
}
