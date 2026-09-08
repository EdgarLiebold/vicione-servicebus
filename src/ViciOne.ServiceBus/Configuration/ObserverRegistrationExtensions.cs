using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for observer registration.</summary>
public static class ObserverRegistrationExtensions
{
    /// <summary>Adds a global bus observer that is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddBusObserver<T>(this IServiceCollection services)
        where T : class, IBusObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBusObserver, T>());
        return services;
    }

    /// <summary>Adds a global bus observer factory whose observer is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="factory">The factory that creates the observer.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddBusObserver<T>(this IServiceCollection services, Func<IServiceProvider, T> factory)
        where T : class, IBusObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBusObserver, T>(factory));
        return services;
    }

    /// <summary>Adds a bus observer that is connected only to the specified bus registration.</summary>
    /// <typeparam name="TBus">The bus whose lifecycle the observer follows.</typeparam>
    /// <typeparam name="TObserver">The observer type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddBusObserver<TBus, TObserver>(this IServiceCollection services)
        where TBus : class, IBus
        where TObserver : class, IBusObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<TObserver>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBusObserverRegistration, BusObserverRegistration<TBus, TObserver>>());
        return services;
    }

    /// <summary>Adds a bus observer factory whose observer is connected only to the specified bus registration.</summary>
    /// <typeparam name="TBus">The bus whose lifecycle the observer follows.</typeparam>
    /// <typeparam name="TObserver">The observer type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="factory">The factory that creates the observer.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddBusObserver<TBus, TObserver>(this IServiceCollection services, Func<IServiceProvider, TObserver> factory)
        where TBus : class, IBus
        where TObserver : class, IBusObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        services.TryAddSingleton(factory);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IBusObserverRegistration, BusObserverRegistration<TBus, TObserver>>());
        return services;
    }

    /// <summary>Adds a singleton receive-endpoint observer that is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer implementation type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddReceiveEndpointObserver<T>(this IServiceCollection services)
        where T : class, IReceiveEndpointObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReceiveEndpointObserver, T>());
        return services;
    }

    /// <summary>Adds a singleton receive-endpoint observer factory whose observer is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer implementation type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="factory">The factory that creates the observer.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddReceiveEndpointObserver<T>(this IServiceCollection services, Func<IServiceProvider, T> factory)
        where T : class, IReceiveEndpointObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReceiveEndpointObserver, T>(factory));
        return services;
    }

    /// <summary>Adds a singleton receive observer that is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer implementation type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddReceiveObserver<T>(this IServiceCollection services)
        where T : class, IReceiveObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReceiveObserver, T>());
        return services;
    }

    /// <summary>Adds a singleton receive observer factory whose observer is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer implementation type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="factory">The factory that creates the observer.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddReceiveObserver<T>(this IServiceCollection services, Func<IServiceProvider, T> factory)
        where T : class, IReceiveObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReceiveObserver, T>(factory));
        return services;
    }

    /// <summary>Adds a singleton consume observer that is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer implementation type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddConsumeObserver<T>(this IServiceCollection services)
        where T : class, IConsumeObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumeObserver, T>());
        return services;
    }

    /// <summary>Adds a singleton consume observer factory whose observer is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer implementation type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="factory">The factory that creates the observer.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddConsumeObserver<T>(this IServiceCollection services, Func<IServiceProvider, T> factory)
        where T : class, IConsumeObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumeObserver, T>(factory));
        return services;
    }

    /// <summary>Adds a singleton send observer that is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer implementation type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddSendObserver<T>(this IServiceCollection services)
        where T : class, ISendObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISendObserver, T>());
        return services;
    }

    /// <summary>Adds a singleton send observer factory whose observer is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer implementation type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="factory">The factory that creates the observer.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddSendObserver<T>(this IServiceCollection services, Func<IServiceProvider, T> factory)
        where T : class, ISendObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISendObserver, T>(factory));
        return services;
    }

    /// <summary>Adds a singleton publish observer that is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer implementation type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddPublishObserver<T>(this IServiceCollection services)
        where T : class, IPublishObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPublishObserver, T>());
        return services;
    }

    /// <summary>Adds a singleton publish observer factory whose observer is connected to every registered bus.</summary>
    /// <typeparam name="T">The observer implementation type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="factory">The factory that creates the observer.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddPublishObserver<T>(this IServiceCollection services, Func<IServiceProvider, T> factory)
        where T : class, IPublishObserver
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPublishObserver, T>(factory));
        return services;
    }
}
