using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for consume context activator.</summary>
public static class ConsumeContextActivatorExtensions
{
    /// <summary>
    /// If the <see cref="ConsumeContext" /> has an <see cref="IServiceProvider" /> or <see cref="IServiceScope" /> payload,
    /// use that payload to get the service or create an instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The service or create instance.</returns>
    /// <exception cref="PayloadNotFoundException">Thrown when the required context payload is unavailable.</exception>
    public static T GetServiceOrCreateInstance<T>(this ConsumeContext context)
        where T : class
    {
        if (context.TryGetPayload(out IServiceScope? serviceScope))
            return ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceScope.ServiceProvider);

        if (context.TryGetPayload(out IServiceProvider? serviceProvider))
            return ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);

        return ActivatorUtilities.CreateInstance<T>(Provider.Empty);
    }

    /// <summary>
    /// If the <see cref="ConsumeContext" /> has an <see cref="IServiceProvider" /> or <see cref="IServiceScope" /> payload,
    /// use that payload to create an instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The created instance.</returns>
    /// <exception cref="PayloadNotFoundException">Thrown when the required context payload is unavailable.</exception>
    public static T CreateInstance<T>(this ConsumeContext context, params object[] arguments)
        where T : class
    {
        if (context.TryGetPayload(out IServiceScope? serviceScope))
            return ActivatorUtilities.CreateInstance<T>(serviceScope.ServiceProvider, arguments);

        if (context.TryGetPayload(out IServiceProvider? serviceProvider))
            return ActivatorUtilities.CreateInstance<T>(serviceProvider, arguments);

        return ActivatorUtilities.CreateInstance<T>(Provider.Empty);
    }


    static class Provider
    {
        internal static readonly IServiceProvider Empty = new EmptyServiceProvider();


        class EmptyServiceProvider :
            IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                return null;
            }
        }
    }
}
