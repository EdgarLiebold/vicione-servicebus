using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Connects transport-specific receive endpoints to a running bus.</summary>
/// <typeparam name="TEndpointConfigurator">The endpoint configurator type.</typeparam>
public interface IReceiveEndpointConnector<out TEndpointConfigurator> :
    IReceiveEndpointConnector
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    /// <summary>Connects a receive endpoint to the bus.</summary>
    /// <param name="definition">An endpoint definition, which abstracts specific endpoint behaviors from the transport.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, TEndpointConfigurator>? configure = null);

    /// <summary>Connects a receive endpoint to the bus.</summary>
    /// <param name="queueName">The queue name for the receive endpoint.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IBusRegistrationContext, TEndpointConfigurator>? configure = null);
}

/// <summary>Connects transport-neutral receive endpoints to a running bus.</summary>
public interface IReceiveEndpointConnector
{
    /// <summary>Connects a receive endpoint to the bus.</summary>
    /// <param name="definition">An endpoint definition, which abstracts specific endpoint behaviors from the transport.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null);

    /// <summary>Connects a receive endpoint to the bus.</summary>
    /// <param name="queueName">The queue name for the receive endpoint.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null);
}
