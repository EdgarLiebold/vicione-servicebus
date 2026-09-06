using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates services from the dependency-injection scope carried by a consume context.</summary>
public static class ConsumeContextActivatorExtensions
{
    /// <summary>
    /// Resolves the service from the consume scope or creates it with the services available in that scope.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="context">The consume context that supplies the dependency-injection scope.</param>
    /// <returns>The resolved or created service instance.</returns>
    public static T GetServiceOrCreateInstance<T>(this ConsumeContext context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TryGetPayload(out IServiceScope? serviceScope))
            return ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceScope.ServiceProvider);

        if (context.TryGetPayload(out IServiceProvider? serviceProvider))
            return ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);

        return ActivatorUtilities.CreateInstance<T>(Provider.Empty);
    }

    /// <summary>
    /// Creates an instance using the services available in the consume scope and the supplied arguments.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="context">The consume context that supplies the dependency-injection scope.</param>
    /// <param name="arguments">Explicit constructor arguments.</param>
    /// <returns>The created instance.</returns>
    public static T CreateInstance<T>(this ConsumeContext context, params object[] arguments)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(arguments);

        if (context.TryGetPayload(out IServiceScope? serviceScope))
            return ActivatorUtilities.CreateInstance<T>(serviceScope.ServiceProvider, arguments);

        if (context.TryGetPayload(out IServiceProvider? serviceProvider))
            return ActivatorUtilities.CreateInstance<T>(serviceProvider, arguments);

        return ActivatorUtilities.CreateInstance<T>(Provider.Empty, arguments);
    }

    static class Provider
    {
        internal static readonly IServiceProvider Empty = new EmptyServiceProvider();

        sealed class EmptyServiceProvider :
            IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                return null;
            }
        }
    }
}
