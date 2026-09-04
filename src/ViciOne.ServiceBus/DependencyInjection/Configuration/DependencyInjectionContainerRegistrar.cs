using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a dependency injection container registrar implementation.
/// </summary>
public class DependencyInjectionContainerRegistrar :
    IContainerRegistrar
{
    /// <summary>
    /// Defines the collection value.
    /// </summary>
    protected readonly IServiceCollection Collection;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    public DependencyInjectionContainerRegistrar(IServiceCollection collection)
    {
        Collection = collection;
    }

    /// <summary>
    /// Performs the register request client operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="timeout">The timeout value.</param>
    public virtual void RegisterRequestClient<T>(RequestTimeout timeout)
        where T : class
    {
        EnsureRequestClientRegistrationIsUnique<T>(typeof(IRequestClient<T>));
        Collection.AddScoped(provider => GetScopedBusContext(provider).CreateRequestClient<T>(timeout));
    }

    /// <summary>
    /// Performs the register request client operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="timeout">The timeout value.</param>
    public virtual void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        EnsureRequestClientRegistrationIsUnique<T>(typeof(IRequestClient<T>));
        Collection.AddScoped(provider => GetScopedBusContext(provider).CreateRequestClient<T>(destinationAddress, timeout));
    }

    /// <summary>
    /// Performs the register scoped client factory operation.
    /// </summary>
    public virtual void RegisterScopedClientFactory()
    {
        Collection.TryAddScoped(provider => GetScopedBusContext(provider));
    }

    /// <summary>
    /// Performs the ensure request client registration is unique operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="serviceType">The service type value.</param>
    protected void EnsureRequestClientRegistrationIsUnique<T>(Type serviceType)
        where T : class
    {
        if (Collection.Any(descriptor => descriptor.ServiceType == serviceType))
        {
            throw new ConfigurationException(
                $"A request client for {TypeCache<T>.ShortName} is already configured for this bus owner.");
        }
    }

    /// <summary>
    /// Performs the register endpoint name formatter operation.
    /// </summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    public virtual void RegisterEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        Collection.TryAddSingleton(endpointNameFormatter);
    }

    /// <summary>
    /// Gets or add registration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="type">The type value.</param>
    /// <param name="missingRegistrationFactory">The missing registration factory value.</param>
    /// <returns>The result of the operation.</returns>
    public T GetOrAddRegistration<T>(Type type, Func<Type, T>? missingRegistrationFactory = default)
        where T : class, IRegistration
    {
        if (TryGetRegistration<T>(type, out var value))
            return value;

        Func<Type, T> factory = missingRegistrationFactory ?? throw new ArgumentNullException(nameof(missingRegistrationFactory));

        value = factory(type);

        AddRegistration(value);

        return value;
    }

    /// <summary>
    /// Adds definition to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TDefinition">The t definition type.</typeparam>
    public virtual void AddDefinition<T, TDefinition>()
        where T : class, IDefinition
        where TDefinition : class, T
    {
        Collection.AddSingleton<TDefinition>();
        Collection.AddSingleton<T>(provider => ActivatorUtilities.CreateInstance<TDefinition>(provider));
    }

    /// <summary>
    /// Adds endpoint definition to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TDefinition">The t definition type.</typeparam>
    /// <param name="settings">The settings value.</param>
    public virtual void AddEndpointDefinition<T, TDefinition>(IEndpointSettings<IEndpointDefinition<T>>? settings)
        where T : class
        where TDefinition : class, IEndpointDefinition<T>
    {
        Collection.AddSingleton<TDefinition>();

        if (settings == null)
            Collection.AddSingleton<IEndpointDefinition<T>, TDefinition>();
        else
        {
            Collection.TryAddTransient<IEndpointSettings<IEndpointDefinition<T>>>(
                _ => throw new InvalidOperationException("The settings are no longer configured in the container."));
            Collection.AddSingleton<IEndpointDefinition<T>>(provider => ActivatorUtilities.CreateInstance<TDefinition>(provider, settings));
        }
    }

    /// <summary>
    /// Gets registrations.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public virtual IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration
    {
        return Collection.Where(x => x.ServiceType == typeof(T)).Select(x => x.ImplementationInstance).Cast<T>();
    }

    /// <summary>
    /// Attempts to get registration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <param name="type">The type value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value)
        where T : class, IRegistration
    {
        value = GetRegistrations<T>(provider).FirstOrDefault(x => x.Type == type);

        return value != null;
    }

    /// <summary>
    /// Gets registrations.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public virtual IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
        where T : class, IRegistration
    {
        return provider.GetServices<T>();
    }

    /// <summary>
    /// Gets definition.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public virtual T? GetDefinition<T>(IServiceProvider provider)
        where T : class, IDefinition
    {
        return provider.GetService<T>();
    }

    /// <summary>
    /// Gets endpoint definition.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public virtual IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
        where T : class
    {
        return provider.GetService<IEndpointDefinition<T>>();
    }

    /// <summary>
    /// Gets configure receive endpoints.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider)
    {
        IConfigureReceiveEndpoint[] globalConfigureReceiveEndpoints = provider.GetServices<IConfigureReceiveEndpoint>().ToArray();

        return new ConfigureReceiveEndpoint(globalConfigureReceiveEndpoints, GetBusConfigureReceiveEndpoints(provider));
    }

    /// <summary>
    /// Gets endpoint name formatter.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public virtual IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider)
    {
        return provider.GetService<IEndpointNameFormatter>() ?? DefaultEndpointNameFormatter.Instance;
    }

    /// <summary>
    /// Gets bus configure receive endpoints.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual IConfigureReceiveEndpoint[] GetBusConfigureReceiveEndpoints(IServiceProvider provider)
    {
        return provider.GetServices<Bind<IBus, IConfigureReceiveEndpoint>>().Select(x => x.Value).ToArray();
    }

    bool TryGetRegistration<T>(Type type, [NotNullWhen(true)] out T? value)
        where T : class, IRegistration
    {
        value = GetRegistrations<T>().FirstOrDefault(x => x.Type == type);

        return value != null;
    }

    /// <summary>
    /// Adds registration to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    protected virtual void AddRegistration<T>(T value)
        where T : class, IRegistration
    {
        Collection.Add(ServiceDescriptor.Singleton(value));
    }

    /// <summary>
    /// Gets scoped bus context.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
    {
        return provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.ClientFactory;
    }


    class ConfigureReceiveEndpoint :
        IConfigureReceiveEndpoint
    {
        readonly IConfigureReceiveEndpoint[] _global;
        readonly IConfigureReceiveEndpoint[] _typed;

        public ConfigureReceiveEndpoint(IConfigureReceiveEndpoint[] global, IConfigureReceiveEndpoint[] typed)
        {
            _global = global;
            _typed = typed;
        }

        public void Configure(string? name, IReceiveEndpointConfigurator configurator)
        {
            for (var i = 0; i < _global.Length; i++)
                _global[i].Configure(name, configurator);

            for (var i = 0; i < _typed.Length; i++)
                _typed[i].Configure(name, configurator);
        }
    }
}


/// <summary>
/// Provides a dependency injection container registrar implementation.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
public class DependencyInjectionContainerRegistrar<TBus> :
    DependencyInjectionContainerRegistrar
    where TBus : class, IBus
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    public DependencyInjectionContainerRegistrar(IServiceCollection collection)
        : base(collection)
    {
    }

    /// <summary>
    /// Performs the register request client operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="timeout">The timeout value.</param>
    public override void RegisterRequestClient<T>(RequestTimeout timeout)
    {
        Type serviceType = typeof(Bind<TBus, IRequestClient<T>>);
        EnsureRequestClientRegistrationIsUnique<T>(serviceType);
        Collection.AddScoped(provider => Bind<TBus>.Create(GetScopedBusContext(provider).CreateRequestClient<T>(timeout)));
    }

    /// <summary>
    /// Performs the register request client operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="timeout">The timeout value.</param>
    public override void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
    {
        Type serviceType = typeof(Bind<TBus, IRequestClient<T>>);
        EnsureRequestClientRegistrationIsUnique<T>(serviceType);
        Collection.AddScoped(provider =>
            Bind<TBus>.Create(GetScopedBusContext(provider).CreateRequestClient<T>(destinationAddress, timeout)));
    }

    /// <summary>
    /// Gets registrations.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<T> GetRegistrations<T>()
    {
        return Collection.Where(x => x.ServiceType == typeof(Bind<TBus, T>))
            .Select(x => x.ImplementationInstance).Cast<Bind<TBus, T>>()
            .Select(x => x.Value);
    }

    /// <summary>
    /// Gets registrations.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
    {
        return provider.GetServices<Bind<TBus, T>>().Select(x => x.Value);
    }

    /// <summary>
    /// Adds registration to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    protected override void AddRegistration<T>(T value)
    {
        Collection.Add(ServiceDescriptor.Singleton(Bind<TBus>.Create(value)));
    }

    /// <summary>
    /// Gets definition.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public override T? GetDefinition<T>(IServiceProvider provider)
        where T : class
    {
        return provider.GetService<Bind<TBus, T>>()?.Value;
    }

    /// <summary>
    /// Gets endpoint definition.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public override IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
    {
        return provider.GetService<Bind<TBus, IEndpointDefinition<T>>>()?.Value;
    }

    /// <summary>
    /// Adds definition to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TDefinition">The t definition type.</typeparam>
    public override void AddDefinition<T, TDefinition>()
    {
        Collection.AddSingleton<TDefinition>();
        Collection.AddSingleton<Bind<TBus, TDefinition>>();
        Collection.AddSingleton<Bind<TBus, T>>(provider => Bind<TBus>.Create<T>(ActivatorUtilities.CreateInstance<TDefinition>(provider)));
    }

    /// <summary>
    /// Adds endpoint definition to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TDefinition">The t definition type.</typeparam>
    /// <param name="settings">The settings value.</param>
    public override void AddEndpointDefinition<T, TDefinition>(IEndpointSettings<IEndpointDefinition<T>>? settings)
    {
        Collection.AddSingleton<TDefinition>();

        if (settings == null)
            Collection.AddSingleton(provider => Bind<TBus>.Create<IEndpointDefinition<T>>(provider.GetRequiredService<TDefinition>()));
        else
        {
            Collection.TryAddTransient<IEndpointSettings<IEndpointDefinition<T>>>(
                _ => throw new InvalidOperationException("The settings are no longer configured in the container."));

            Collection.AddSingleton(provider =>
                Bind<TBus>.Create<IEndpointDefinition<T>>(ActivatorUtilities.CreateInstance<TDefinition>(provider, settings)));
        }
    }

    /// <summary>
    /// Performs the register endpoint name formatter operation.
    /// </summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    public override void RegisterEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        Collection.TryAddSingleton(Bind<TBus>.Create(endpointNameFormatter));
    }

    /// <summary>
    /// Performs the register scoped client factory operation.
    /// </summary>
    public override void RegisterScopedClientFactory()
    {
        Collection.TryAddScoped(provider => Bind<TBus>.Create(GetScopedBusContext(provider)));
    }

    /// <summary>
    /// Gets scoped bus context.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    protected override IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
    {
        return provider.GetRequiredService<IScopedBusContextProvider<TBus>>().Context.ClientFactory;
    }

    /// <summary>
    /// Gets bus configure receive endpoints.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    protected override IConfigureReceiveEndpoint[] GetBusConfigureReceiveEndpoints(IServiceProvider provider)
    {
        return provider.GetServices<Bind<TBus, IConfigureReceiveEndpoint>>().Select(x => x.Value).ToArray();
    }

    /// <summary>
    /// Gets endpoint name formatter.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public override IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider)
    {
        var bind = provider.GetService<Bind<TBus, IEndpointNameFormatter>>();
        return bind != null
            ? bind.Value
            : base.GetEndpointNameFormatter(provider);
    }
}
