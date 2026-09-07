using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Adds temporary and response receive endpoints to a running host.</summary>
public static class HostConnectReceiveEndpointExtensions
{
    /// <summary>Connects a response endpoint using the response endpoint definition.</summary>
    /// <param name="connector">The host connector that owns the endpoint.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name, or <see langword="null" /> for the configured default.</param>
    /// <param name="configureEndpoint">An optional callback that configures the receive endpoint.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    public static IHostReceiveEndpointHandle ConnectResponseEndpoint(this IReceiveConnector connector,
        IEndpointNameFormatter? endpointNameFormatter = null,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        ArgumentNullException.ThrowIfNull(connector);

        return connector.ConnectReceiveEndpoint(new ResponseEndpointDefinition(), endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Connects a temporary receive endpoint whose name and lifetime are owned by the host.</summary>
    /// <param name="connector">The host connector that owns the endpoint.</param>
    /// <param name="configureEndpoint">An optional callback that configures the receive endpoint.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    public static IHostReceiveEndpointHandle ConnectReceiveEndpoint(this IReceiveConnector connector,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        ArgumentNullException.ThrowIfNull(connector);

        return connector.ConnectReceiveEndpoint(new TemporaryEndpointDefinition(), null, configureEndpoint);
    }
}
