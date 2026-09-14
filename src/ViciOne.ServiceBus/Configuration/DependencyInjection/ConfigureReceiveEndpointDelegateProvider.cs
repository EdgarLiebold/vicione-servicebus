using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adapts a context-aware endpoint callback to the receive-endpoint configuration contract.</summary>
internal sealed class ConfigureReceiveEndpointDelegateProvider :
    IConfigureReceiveEndpoint
{
    readonly ConfigureEndpointsProviderCallback _callback;
    readonly IRegistrationContext _context;

    /// <summary>Creates an adapter for a context-aware endpoint callback.</summary>
    /// <param name="context">The registration context supplied to the callback.</param>
    /// <param name="callback">The callback to invoke for each endpoint.</param>
    public ConfigureReceiveEndpointDelegateProvider(IRegistrationContext context, ConfigureEndpointsProviderCallback callback)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>Invokes the callback for a receive endpoint and supplies the captured registration context.</summary>
    /// <param name="name">The endpoint name, or <see langword="null" /> when no name is available.</param>
    /// <param name="configurator">The receive endpoint to configure.</param>
    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        _callback(_context, name, configurator);
    }
}
