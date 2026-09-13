using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers dependency injection container components with dependency injection.</summary>
public class DependencyInjectionContainerRegistrar :
    IContainerRegistrar
{
    /// <summary>Exposes the collection used by the containing type.</summary>
    protected readonly IServiceCollection Collection;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="collection">The collection.</param>
    public DependencyInjectionContainerRegistrar(IServiceCollection collection)
    {
        Collection = collection;
    }

    /// <summary>Registers request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public virtual void RegisterRequestClient<T>(RequestTimeout timeout)
        where T : class
    {
        EnsureRequestClientRegistrationIsUnique<T>(typeof(IRequestClient<T>));
        Collection.AddScoped(provider => GetScopedBusContext(provider).CreateRequestClient<T>(timeout));
    }

    /// <summary>Registers request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public virtual void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        EnsureRequestClientRegistrationIsUnique<T>(typeof(IRequestClient<T>));
        Collection.AddScoped(provider => GetScopedBusContext(provider).CreateRequestClient<T>(destinationAddress, timeout));
    }

    /// <summary>Registers scoped client factory.</summary>
    public virtual void RegisterScopedClientFactory()
    {
        Collection.TryAddScoped(provider => GetScopedBusContext(provider));
    }

    /// <summary>Ensures request client registration is unique.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="serviceType">The runtime service type used by the operation.</param>
    protected void EnsureRequestClientRegistrationIsUnique<T>(Type serviceType)
        where T : class
    {
        if (Collection.Any(descriptor => descriptor.ServiceType == serviceType))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Dependency Injection Container Registrar", "unknown", $"A request client for {TypeCache<T>.ShortName} is already configured for this bus owner.", "Correct the named configuration before starting the host"));
        }
    }

    /// <summary>Registers endpoint name formatter.</summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    public virtual void RegisterEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        Collection.TryAddSingleton(endpointNameFormatter);
    }

    /// <summary>Gets or add registration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <param name="missingRegistrationFactory">The missing registration factory.</param>
    /// <returns>The or add registration.</returns>
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

    /// <summary>Adds definition to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    public virtual void AddDefinition<T, TDefinition>()
        where T : class, IDefinition
        where TDefinition : class, T
    {
        Collection.AddSingleton<TDefinition>();
        Collection.AddSingleton<T>(provider => ActivatorUtilities.CreateInstance<TDefinition>(provider));
    }

    /// <summary>Adds endpoint definition to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="settings">The settings that control the operation.</param>
    public virtual void AddEndpointDefinition<T, TDefinition>(IEndpointSettings<IEndpointDefinition<T>>? settings)
        where T : class
        where TDefinition : class, IEndpointDefinition<T>
    {
        if (settings == null)
            Collection.AddSingleton<TDefinition>();
        else
        {
            Collection.TryAddTransient<IEndpointSettings<IEndpointDefinition<T>>>(
                _ => throw new InvalidOperationException("The settings are no longer configured in the container."));
            Collection.AddSingleton(provider => ActivatorUtilities.CreateInstance<TDefinition>(provider, settings));
        }

        Collection.AddSingleton<IEndpointDefinition<T>>(provider => provider.GetRequiredService<TDefinition>());
    }

    /// <summary>Gets registrations.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The registrations.</returns>
    public virtual IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration
    {
        return Collection.Where(x => x.ServiceType == typeof(T)).Select(x => x.ImplementationInstance).Cast<T>();
    }

    /// <summary>Attempts to get registration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value)
        where T : class, IRegistration
    {
        value = GetRegistrations<T>(provider).FirstOrDefault(x => x.Type == type);

        return value != null;
    }

    /// <summary>Gets registrations.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The registrations.</returns>
    public virtual IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
        where T : class, IRegistration
    {
        return provider.GetServices<T>();
    }

    /// <summary>Gets definition.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The definition.</returns>
    public virtual T? GetDefinition<T>(IServiceProvider provider)
        where T : class, IDefinition
    {
        return provider.GetService<T>();
    }

    /// <summary>Gets endpoint definition.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The endpoint definition.</returns>
    public virtual IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
        where T : class
    {
        return provider.GetService<IEndpointDefinition<T>>();
    }

    /// <summary>Gets configure receive endpoints.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The configure receive endpoints.</returns>
    public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider)
    {
        IConfigureReceiveEndpoint[] globalConfigureReceiveEndpoints = provider.GetServices<IConfigureReceiveEndpoint>().ToArray();

        return new ConfigureReceiveEndpoint(globalConfigureReceiveEndpoints, GetBusConfigureReceiveEndpoints(provider));
    }

    /// <summary>Gets endpoint name formatter.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The endpoint name formatter.</returns>
    public virtual IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider)
    {
        return provider.GetService<IEndpointNameFormatter>() ?? DefaultEndpointNameFormatter.Instance;
    }

    /// <summary>Gets bus configure receive endpoints.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The bus configure receive endpoints.</returns>
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

    /// <summary>Adds registration to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to process.</param>
    protected virtual void AddRegistration<T>(T value)
        where T : class, IRegistration
    {
        Collection.Add(ServiceDescriptor.Singleton(value));
    }

    /// <summary>Gets scoped bus context.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The scoped bus context.</returns>
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


/// <summary>Registers dependency injection container components with dependency injection.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public class DependencyInjectionContainerRegistrar<TBus> :
    DependencyInjectionContainerRegistrar
    where TBus : class, IBus
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="collection">The collection.</param>
    public DependencyInjectionContainerRegistrar(IServiceCollection collection)
        : base(collection)
    {
    }

    /// <summary>Registers request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public override void RegisterRequestClient<T>(RequestTimeout timeout)
    {
        Type serviceType = typeof(Bind<TBus, IRequestClient<T>>);
        EnsureRequestClientRegistrationIsUnique<T>(serviceType);
        Collection.AddScoped(provider => Bind<TBus>.Create(GetScopedBusContext(provider).CreateRequestClient<T>(timeout)));
    }

    /// <summary>Registers request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public override void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
    {
        Type serviceType = typeof(Bind<TBus, IRequestClient<T>>);
        EnsureRequestClientRegistrationIsUnique<T>(serviceType);
        Collection.AddScoped(provider =>
            Bind<TBus>.Create(GetScopedBusContext(provider).CreateRequestClient<T>(destinationAddress, timeout)));
    }

    /// <summary>Gets registrations.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The registrations.</returns>
    public override IEnumerable<T> GetRegistrations<T>()
    {
        return Collection.Where(x => x.ServiceType == typeof(Bind<TBus, T>))
            .Select(x => x.ImplementationInstance).Cast<Bind<TBus, T>>()
            .Select(x => x.Value);
    }

    /// <summary>Gets registrations.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The registrations.</returns>
    public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
    {
        return provider.GetServices<Bind<TBus, T>>().Select(x => x.Value);
    }

    /// <summary>Adds registration to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to process.</param>
    protected override void AddRegistration<T>(T value)
    {
        Collection.Add(ServiceDescriptor.Singleton(Bind<TBus>.Create(value)));
    }

    /// <summary>Gets definition.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The definition.</returns>
    public override T? GetDefinition<T>(IServiceProvider provider)
        where T : class
    {
        return provider.GetService<Bind<TBus, T>>()?.Value;
    }

    /// <summary>Gets endpoint definition.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The endpoint definition.</returns>
    public override IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
    {
        return provider.GetService<Bind<TBus, IEndpointDefinition<T>>>()?.Value;
    }

    /// <summary>Adds definition to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    public override void AddDefinition<T, TDefinition>()
    {
        Collection.AddSingleton<TDefinition>();
        Collection.AddSingleton<Bind<TBus, TDefinition>>();
        Collection.AddSingleton<Bind<TBus, T>>(provider => Bind<TBus>.Create<T>(ActivatorUtilities.CreateInstance<TDefinition>(provider)));
    }

    /// <summary>Adds endpoint definition to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="settings">The settings that control the operation.</param>
    public override void AddEndpointDefinition<T, TDefinition>(IEndpointSettings<IEndpointDefinition<T>>? settings)
    {
        if (settings == null)
        {
            Collection.AddSingleton(provider =>
                Bind<TBus>.Create<IEndpointDefinition<T>>(ActivatorUtilities.CreateInstance<TDefinition>(provider)));
        }
        else
        {
            Collection.TryAddTransient<IEndpointSettings<IEndpointDefinition<T>>>(
                _ => throw new InvalidOperationException("The settings are no longer configured in the container."));

            Collection.AddSingleton(provider =>
                Bind<TBus>.Create<IEndpointDefinition<T>>(ActivatorUtilities.CreateInstance<TDefinition>(provider, settings)));
        }
    }

    /// <summary>Registers endpoint name formatter.</summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    public override void RegisterEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        Collection.TryAddSingleton(Bind<TBus>.Create(endpointNameFormatter));
    }

    /// <summary>Registers scoped client factory.</summary>
    public override void RegisterScopedClientFactory()
    {
        Collection.TryAddScoped(provider => Bind<TBus>.Create(GetScopedBusContext(provider)));
    }

    /// <summary>Gets scoped bus context.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The scoped bus context.</returns>
    protected override IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
    {
        return provider.GetRequiredService<IScopedBusContextProvider<TBus>>().Context.ClientFactory;
    }

    /// <summary>Gets bus configure receive endpoints.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The bus configure receive endpoints.</returns>
    protected override IConfigureReceiveEndpoint[] GetBusConfigureReceiveEndpoints(IServiceProvider provider)
    {
        return provider.GetServices<Bind<TBus, IConfigureReceiveEndpoint>>().Select(x => x.Value).ToArray();
    }

    /// <summary>Gets endpoint name formatter.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The endpoint name formatter.</returns>
    public override IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider)
    {
        var bind = provider.GetService<Bind<TBus, IEndpointNameFormatter>>();
        return bind != null
            ? bind.Value
            : base.GetEndpointNameFormatter(provider);
    }
}
