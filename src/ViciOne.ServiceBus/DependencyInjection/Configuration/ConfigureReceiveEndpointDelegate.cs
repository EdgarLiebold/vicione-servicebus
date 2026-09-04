using System;

namespace ViciOne.ServiceBus.Configuration;

public class ConfigureReceiveEndpointDelegate :
    IConfigureReceiveEndpoint
{
    readonly ConfigureEndpointsCallback _callback;

    public ConfigureReceiveEndpointDelegate(ConfigureEndpointsCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        _callback = callback;
    }

    public void Configure(string name, IReceiveEndpointConfigurator configurator)
    {
        _callback(name, configurator);
    }
}
