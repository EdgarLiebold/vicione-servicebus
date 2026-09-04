using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a configure receive endpoint delegate implementation.
/// </summary>
public class ConfigureReceiveEndpointDelegate :
    IConfigureReceiveEndpoint
{
    readonly ConfigureEndpointsCallback _callback;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public ConfigureReceiveEndpointDelegate(ConfigureEndpointsCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        _callback = callback;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="configurator">The configurator value.</param>
    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        _callback(name, configurator);
    }
}
