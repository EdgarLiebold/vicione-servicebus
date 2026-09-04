using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a configure receive endpoint delegate provider implementation.
/// </summary>
public class ConfigureReceiveEndpointDelegateProvider :
    IConfigureReceiveEndpoint
{
    readonly ConfigureEndpointsProviderCallback _callback;
    readonly IRegistrationContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="callback">The callback value.</param>
    public ConfigureReceiveEndpointDelegateProvider(IRegistrationContext context, ConfigureEndpointsProviderCallback callback)
    {
        _context = context;
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="configurator">The configurator value.</param>
    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        _callback(_context, name, configurator);
    }
}
