using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adapts an endpoint callback to the receive-endpoint configuration contract.</summary>
internal sealed class ConfigureReceiveEndpointDelegate :
    IConfigureReceiveEndpoint
{
    readonly ConfigureEndpointsCallback _callback;

    /// <summary>Creates an adapter for an endpoint callback.</summary>
    /// <param name="callback">The callback to invoke for each endpoint.</param>
    public ConfigureReceiveEndpointDelegate(ConfigureEndpointsCallback callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>Invokes the callback for a receive endpoint.</summary>
    /// <param name="name">The endpoint name, or <see langword="null" /> when no name is available.</param>
    /// <param name="configurator">The receive endpoint to configure.</param>
    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        _callback(name, configurator);
    }
}
