using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides host-builder registration for the mediator capability package.</summary>
public static class MediatorHostBuilderExtensions
{
    /// <summary>Adds the mediator and configures it using the current host-builder context.</summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The configured mediator.</returns>
    public static IHostBuilder UseMediator(this IHostBuilder hostBuilder,
        Action<HostBuilderContext, IMediatorRegistrationConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            services.AddMediator(configurator => configure?.Invoke(hostContext, configurator));
        });
        return hostBuilder;
    }
}
