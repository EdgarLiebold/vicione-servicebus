using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Represents the callback used to configure receive endpoint.</summary>
public class ConfigureReceiveEndpointDelegate :
    IConfigureReceiveEndpoint
{
    readonly ConfigureEndpointsCallback _callback;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public ConfigureReceiveEndpointDelegate(ConfigureEndpointsCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        _callback = callback;
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="name">The name.</param>
    /// <param name="configurator">The configurator to update.</param>
    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        _callback(name, configurator);
    }
}
