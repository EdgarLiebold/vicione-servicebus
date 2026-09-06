using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides configure receive endpoint delegate services.</summary>
public class ConfigureReceiveEndpointDelegateProvider :
    IConfigureReceiveEndpoint
{
    readonly ConfigureEndpointsProviderCallback _callback;
    readonly IRegistrationContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    public ConfigureReceiveEndpointDelegateProvider(IRegistrationContext context, ConfigureEndpointsProviderCallback callback)
    {
        _context = context;
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="name">The name.</param>
    /// <param name="configurator">The configurator to update.</param>
    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        _callback(_context, name, configurator);
    }
}
