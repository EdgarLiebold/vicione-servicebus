using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for dependency injection.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Creates a single scope for the receive endpoint that is used by all consumers, sagas, messages, etc.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="context"></param>
    public static void UseServiceScope(this IConsumePipeConfigurator configurator, IRegistrationContext context)
    {
        var scopeProvider = new ConsumeScopeProvider(context);
        var specification = new FilterPipeSpecification<ConsumeContext>(new ScopeConsumeFilter(scopeProvider));

        configurator.AddPrePipeSpecification(specification);
    }


    /// <summary>
    /// Creates a scope for each message type, compatible with UseMessageRetry and UseVolatileOutbox
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="context"></param>
    public static void UseMessageScope(this IConsumePipeConfigurator configurator, IRegistrationContext context)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var observer = new MessageScopeConfigurationObserver(configurator, context);
    }

    /// <summary>
    /// Create a request client, using the specified service address, using the <see cref="IClientFactory" /> from the container.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="timeout">The default timeout for requests</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IRequestClient<T> CreateRequestClient<T>(this IServiceProvider provider, RequestTimeout timeout = default)
        where T : class
    {
        return provider.GetRequiredService<IClientFactory>().CreateRequestClient<T>(timeout);
    }

    /// <summary>
    /// Create a request client, using the specified service address, using the <see cref="IClientFactory" /> from the container.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="destinationAddress">The destination service address</param>
    /// <param name="timeout">The default timeout for requests</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IRequestClient<T> CreateRequestClient<T>(this IServiceProvider provider, Uri destinationAddress, RequestTimeout timeout = default)
        where T : class
    {
        return provider.GetRequiredService<IClientFactory>().CreateRequestClient<T>(destinationAddress, timeout);
    }
}
