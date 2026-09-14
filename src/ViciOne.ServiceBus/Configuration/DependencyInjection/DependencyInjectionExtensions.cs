using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures dependency-injection scopes and resolves request clients.</summary>
public static class DependencyInjectionExtensions
{
    /// <summary>Creates one dependency-injection scope for an entire receive operation.</summary>
    /// <param name="configurator">The receive pipeline to configure.</param>
    /// <param name="context">The registration context that owns the container.</param>
    public static void UseServiceScope(this IConsumePipeConfigurator configurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var scopeProvider = new ConsumeScopeProvider(context);
        var specification = new FilterPipeSpecification<ConsumeContext>(new ScopeConsumeFilter(scopeProvider));

        configurator.AddPrePipeSpecification(specification);
    }


    /// <summary>Creates a dependency-injection scope for each dispatched message contract.</summary>
    /// <param name="configurator">The receive pipeline to configure.</param>
    /// <param name="context">The registration context that owns the container.</param>
    public static void UseMessageScope(this IConsumePipeConfigurator configurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        _ = new MessageScopeConfigurationObserver(configurator, context);
    }

    /// <summary>Creates a request client through the container's <see cref="IClientFactory" />.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="provider">The service provider that contains the client factory.</param>
    /// <param name="timeout">The timeout override, or an unspecified value to use the configured default.</param>
    /// <returns>A request client for <typeparamref name="T" />.</returns>
    public static IRequestClient<T> CreateRequestClient<T>(this IServiceProvider provider, RequestTimeout timeout = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetRequiredService<IClientFactory>().CreateRequestClient<T>(timeout);
    }

    /// <summary>Creates a request client for an explicit destination through the container's <see cref="IClientFactory" />.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="provider">The service provider that contains the client factory.</param>
    /// <param name="destinationAddress">The destination service address.</param>
    /// <param name="timeout">The timeout override, or an unspecified value to use the configured default.</param>
    /// <returns>A request client for <typeparamref name="T" />.</returns>
    public static IRequestClient<T> CreateRequestClient<T>(this IServiceProvider provider, Uri destinationAddress, RequestTimeout timeout = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(destinationAddress);

        return provider.GetRequiredService<IClientFactory>().CreateRequestClient<T>(destinationAddress, timeout);
    }
}
