using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores bus registrations in an <see cref="IServiceCollection" /> and resolves them after container construction.</summary>
public class DependencyInjectionContainerRegistrar :
    IContainerRegistrar
{
    /// <summary>The service collection that owns this registrar's components.</summary>
    protected readonly IServiceCollection Collection;

    /// <summary>Creates a registrar over the supplied service collection.</summary>
    /// <param name="collection">The service collection that owns the registrations.</param>
    public DependencyInjectionContainerRegistrar(IServiceCollection collection)
    {
        Collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    /// <summary>Registers a scoped request client that resolves destinations from message topology.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="timeout">The client's default request timeout.</param>
    public virtual void RegisterRequestClient<T>(RequestTimeout timeout = default)
        where T : class
    {
        EnsureRequestClientRegistrationIsUnique<T>(typeof(IRequestClient<T>));
        Collection.AddScoped(provider => GetScopedBusContext(provider).CreateRequestClient<T>(timeout));
    }

    /// <summary>Registers a scoped request client bound to an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The address to which requests are sent.</param>
    /// <param name="timeout">The client's default request timeout.</param>
    public virtual void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);

        EnsureRequestClientRegistrationIsUnique<T>(typeof(IRequestClient<T>));
        Collection.AddScoped(provider => GetScopedBusContext(provider).CreateRequestClient<T>(destinationAddress, timeout));
    }

    /// <summary>Registers the default bus-owned scoped client factory if it is not already present.</summary>
    public virtual void RegisterScopedClientFactory()
    {
        Collection.TryAddScoped(provider => GetScopedBusContext(provider));
    }

    /// <summary>Rejects a second request-client registration for the same bus owner and message.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="serviceType">The owner-qualified request-client service contract.</param>
    protected void EnsureRequestClientRegistrationIsUnique<T>(Type serviceType)
        where T : class
    {
        if (Collection.Any(descriptor => descriptor.ServiceType == serviceType))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Dependency Injection Container Registrar", "unknown", $"A request client for {TypeCache<T>.ShortName} is already configured for this bus owner.", "Correct the named configuration before starting the host"));
        }
    }

    /// <summary>Registers the default bus endpoint naming convention if none exists.</summary>
    /// <param name="endpointNameFormatter">The endpoint naming convention.</param>
    public virtual void RegisterEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        ArgumentNullException.ThrowIfNull(endpointNameFormatter);

        Collection.TryAddSingleton(endpointNameFormatter);
    }

    /// <summary>Returns the registration for a component or adds one through the supplied factory.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <param name="type">The component type that identifies the registration.</param>
    /// <param name="missingRegistrationFactory">The factory used only when the component is not registered.</param>
    /// <returns>The existing or newly added registration.</returns>
    public T GetOrAddRegistration<T>(Type type, Func<Type, T>? missingRegistrationFactory = default)
        where T : class, IRegistration
    {
        ArgumentNullException.ThrowIfNull(type);

        if (TryGetRegistration<T>(type, out var value))
            return value;

        Func<Type, T> factory = missingRegistrationFactory ?? throw new ArgumentNullException(nameof(missingRegistrationFactory));

        value = factory(type)
            ?? throw new InvalidOperationException("The registration factory returned null.");

        AddRegistration(value);

        return value;
    }

    /// <summary>Registers one definition instance under its implementation and service contracts.</summary>
    /// <typeparam name="T">The definition service contract.</typeparam>
    /// <typeparam name="TDefinition">The concrete definition implementation.</typeparam>
    public virtual void AddDefinition<T, TDefinition>()
        where T : class, IDefinition
        where TDefinition : class, T
    {
        EnsureConcreteDefinition<TDefinition>();

        Collection.AddSingleton<TDefinition>();
        Collection.AddSingleton<T>(provider => provider.GetRequiredService<TDefinition>());
    }

    /// <summary>Registers an endpoint definition, optionally passing explicit settings to its constructor.</summary>
    /// <typeparam name="T">The component associated with the endpoint.</typeparam>
    /// <typeparam name="TDefinition">The concrete endpoint definition.</typeparam>
    /// <param name="settings">Optional settings supplied directly during definition activation.</param>
    public virtual void AddEndpointDefinition<T, TDefinition>(IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where T : class
        where TDefinition : class, IEndpointDefinition<T>
    {
        EnsureConcreteDefinition<TDefinition>();

        if (settings == null)
            Collection.AddSingleton<TDefinition>();
        else
        {
            Collection.AddSingleton(provider => ActivatorUtilities.CreateInstance<TDefinition>(provider, settings));
        }

        Collection.AddSingleton<IEndpointDefinition<T>>(provider => provider.GetRequiredService<TDefinition>());
    }

    /// <summary>Returns instance-backed registrations before the service provider is built.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <returns>The registrations stored directly in the service collection.</returns>
    public virtual IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration
    {
        return Collection.Where(x => x.ServiceType == typeof(T)).Select(x => x.ImplementationInstance).OfType<T>();
    }

    /// <summary>Finds a component registration in a built service provider.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <param name="provider">The service provider that contains registrations.</param>
    /// <param name="type">The component type that identifies the registration.</param>
    /// <param name="value">Receives the matching registration when found.</param>
    /// <returns><see langword="true" /> when a matching registration exists.</returns>
    public bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value)
        where T : class, IRegistration
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(type);

        value = GetRegistrations<T>(provider).FirstOrDefault(x => x.Type == type);

        return value != null;
    }

    /// <summary>Resolves every registration of a category from a built service provider.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <param name="provider">The service provider that contains registrations.</param>
    /// <returns>All matching registrations for the default bus owner.</returns>
    public virtual IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
        where T : class, IRegistration
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetServices<T>();
    }

    /// <summary>Resolves an optional component definition.</summary>
    /// <typeparam name="T">The definition service contract.</typeparam>
    /// <param name="provider">The service provider that contains definitions.</param>
    /// <returns>The definition, or <see langword="null" /> when none is registered.</returns>
    public virtual T? GetDefinition<T>(IServiceProvider provider)
        where T : class, IDefinition
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetService<T>();
    }

    /// <summary>Resolves an optional endpoint definition for a component.</summary>
    /// <typeparam name="T">The component associated with the endpoint.</typeparam>
    /// <param name="provider">The service provider that contains endpoint definitions.</param>
    /// <returns>The endpoint definition, or <see langword="null" /> when none is registered.</returns>
    public virtual IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetService<IEndpointDefinition<T>>();
    }

    /// <summary>Composes global and default-bus receive-endpoint callbacks.</summary>
    /// <param name="provider">The service provider that contains endpoint callbacks.</param>
    /// <returns>A callback aggregate that invokes every applicable callback in registration order.</returns>
    public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        IConfigureReceiveEndpoint[] globalConfigureReceiveEndpoints = provider.GetServices<IConfigureReceiveEndpoint>().ToArray();

        return new ConfigureReceiveEndpoint(globalConfigureReceiveEndpoints, GetBusConfigureReceiveEndpoints(provider));
    }

    /// <summary>Resolves the default bus endpoint naming convention.</summary>
    /// <param name="provider">The service provider that contains the optional convention.</param>
    /// <returns>The registered convention or the built-in default.</returns>
    public virtual IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetService<IEndpointNameFormatter>() ?? DefaultEndpointNameFormatter.Instance;
    }

    /// <summary>Resolves callbacks scoped to the default bus owner.</summary>
    /// <param name="provider">The service provider that contains endpoint callbacks.</param>
    /// <returns>The default-bus callbacks in registration order.</returns>
    protected virtual IConfigureReceiveEndpoint[] GetBusConfigureReceiveEndpoints(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetServices<Bind<IBus, IConfigureReceiveEndpoint>>().Select(x => x.Value).ToArray();
    }

    bool TryGetRegistration<T>(Type type, [NotNullWhen(true)] out T? value)
        where T : class, IRegistration
    {
        value = GetRegistrations<T>().FirstOrDefault(x => x.Type == type);

        return value != null;
    }

    /// <summary>Adds an instance-backed registration to the service collection.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <param name="value">The registration to add.</param>
    protected virtual void AddRegistration<T>(T value)
        where T : class, IRegistration
    {
        ArgumentNullException.ThrowIfNull(value);

        Collection.Add(ServiceDescriptor.Singleton(value));
    }

    /// <summary>Resolves the scoped client factory for the default bus owner.</summary>
    /// <param name="provider">The active dependency-injection scope.</param>
    /// <returns>The client factory bound to that scope.</returns>
    protected virtual IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.ClientFactory;
    }

    /// <summary>Rejects definition types that dependency injection cannot instantiate.</summary>
    /// <typeparam name="TDefinition">The definition implementation.</typeparam>
    protected static void EnsureConcreteDefinition<TDefinition>()
        where TDefinition : class
    {
        Type definitionType = typeof(TDefinition);
        if (!definitionType.IsClass || definitionType.IsAbstract)
            throw new ArgumentException($"{TypeCache.GetShortName(definitionType)} is not a concrete definition implementation", nameof(TDefinition));
    }


    class ConfigureReceiveEndpoint :
        IConfigureReceiveEndpoint
    {
        readonly IConfigureReceiveEndpoint[] _global;
        readonly IConfigureReceiveEndpoint[] _typed;

        public ConfigureReceiveEndpoint(IConfigureReceiveEndpoint[] global, IConfigureReceiveEndpoint[] typed)
        {
            _global = global ?? throw new ArgumentNullException(nameof(global));
            _typed = typed ?? throw new ArgumentNullException(nameof(typed));
        }

        public void Configure(string? name, IReceiveEndpointConfigurator configurator)
        {
            ArgumentNullException.ThrowIfNull(configurator);

            for (var i = 0; i < _global.Length; i++)
                _global[i].Configure(name, configurator);

            for (var i = 0; i < _typed.Length; i++)
                _typed[i].Configure(name, configurator);
        }
    }
}


/// <summary>Stores and resolves registrations isolated to one bus contract.</summary>
/// <typeparam name="TBus">The bus contract that owns the registrations.</typeparam>
public class DependencyInjectionContainerRegistrar<TBus> :
    DependencyInjectionContainerRegistrar
    where TBus : class, IBus
{
    /// <summary>Creates an owner-qualified registrar over the supplied service collection.</summary>
    /// <param name="collection">The service collection that owns the registrations.</param>
    public DependencyInjectionContainerRegistrar(IServiceCollection collection)
        : base(collection)
    {
    }

    /// <summary>Registers an owner-qualified request client that uses message topology.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="timeout">The client's default request timeout.</param>
    public override void RegisterRequestClient<T>(RequestTimeout timeout = default)
    {
        Type serviceType = typeof(Bind<TBus, IRequestClient<T>>);
        EnsureRequestClientRegistrationIsUnique<T>(serviceType);
        Collection.AddScoped(provider => Bind<TBus>.Create(GetScopedBusContext(provider).CreateRequestClient<T>(timeout)));
    }

    /// <summary>Registers an owner-qualified request client bound to an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The address to which requests are sent.</param>
    /// <param name="timeout">The client's default request timeout.</param>
    public override void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);

        Type serviceType = typeof(Bind<TBus, IRequestClient<T>>);
        EnsureRequestClientRegistrationIsUnique<T>(serviceType);
        Collection.AddScoped(provider =>
            Bind<TBus>.Create(GetScopedBusContext(provider).CreateRequestClient<T>(destinationAddress, timeout)));
    }

    /// <summary>Returns owner-qualified instance registrations before container construction.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <returns>The registrations owned by <typeparamref name="TBus" />.</returns>
    public override IEnumerable<T> GetRegistrations<T>()
    {
        return Collection.Where(x => x.ServiceType == typeof(Bind<TBus, T>))
            .Select(x => x.ImplementationInstance).OfType<Bind<TBus, T>>()
            .Select(x => x.Value);
    }

    /// <summary>Resolves every owner-qualified registration from a built service provider.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <param name="provider">The service provider that contains registrations.</param>
    /// <returns>The registrations owned by <typeparamref name="TBus" />.</returns>
    public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetServices<Bind<TBus, T>>().Select(x => x.Value);
    }

    /// <summary>Adds an owner-qualified instance registration to the service collection.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <param name="value">The registration to add.</param>
    protected override void AddRegistration<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Collection.Add(ServiceDescriptor.Singleton(Bind<TBus>.Create(value)));
    }

    /// <summary>Resolves an optional owner-qualified component definition.</summary>
    /// <typeparam name="T">The definition service contract.</typeparam>
    /// <param name="provider">The service provider that contains definitions.</param>
    /// <returns>The definition owned by <typeparamref name="TBus" />, or <see langword="null" />.</returns>
    public override T? GetDefinition<T>(IServiceProvider provider)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetService<Bind<TBus, T>>()?.Value;
    }

    /// <summary>Resolves an optional owner-qualified endpoint definition.</summary>
    /// <typeparam name="T">The component associated with the endpoint.</typeparam>
    /// <param name="provider">The service provider that contains endpoint definitions.</param>
    /// <returns>The endpoint definition owned by <typeparamref name="TBus" />, or <see langword="null" />.</returns>
    public override IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetService<Bind<TBus, IEndpointDefinition<T>>>()?.Value;
    }

    /// <summary>Registers one owner-qualified definition instance under both definition contracts.</summary>
    /// <typeparam name="T">The definition service contract.</typeparam>
    /// <typeparam name="TDefinition">The concrete definition implementation.</typeparam>
    public override void AddDefinition<T, TDefinition>()
    {
        EnsureConcreteDefinition<TDefinition>();

        Collection.AddSingleton<Bind<TBus, TDefinition>>(provider =>
            Bind<TBus>.Create(ActivatorUtilities.CreateInstance<TDefinition>(provider)));
        Collection.AddSingleton<Bind<TBus, T>>(provider =>
            Bind<TBus>.Create<T>(provider.GetRequiredService<Bind<TBus, TDefinition>>().Value));
    }

    /// <summary>Registers an owner-qualified endpoint definition with optional explicit settings.</summary>
    /// <typeparam name="T">The component associated with the endpoint.</typeparam>
    /// <typeparam name="TDefinition">The concrete endpoint definition.</typeparam>
    /// <param name="settings">Optional settings supplied directly during definition activation.</param>
    public override void AddEndpointDefinition<T, TDefinition>(IEndpointSettings<IEndpointDefinition<T>>? settings = null)
    {
        EnsureConcreteDefinition<TDefinition>();

        if (settings == null)
        {
            Collection.AddSingleton<Bind<TBus, TDefinition>>(provider =>
                Bind<TBus>.Create(ActivatorUtilities.CreateInstance<TDefinition>(provider)));
        }
        else
        {
            Collection.AddSingleton<Bind<TBus, TDefinition>>(provider =>
                Bind<TBus>.Create(ActivatorUtilities.CreateInstance<TDefinition>(provider, settings)));
        }

        Collection.AddSingleton<Bind<TBus, IEndpointDefinition<T>>>(provider =>
            Bind<TBus>.Create<IEndpointDefinition<T>>(
                provider.GetRequiredService<Bind<TBus, TDefinition>>().Value));
    }

    /// <summary>Registers the endpoint naming convention for this bus owner.</summary>
    /// <param name="endpointNameFormatter">The endpoint naming convention.</param>
    public override void RegisterEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        ArgumentNullException.ThrowIfNull(endpointNameFormatter);

        Collection.TryAddSingleton(Bind<TBus>.Create(endpointNameFormatter));
    }

    /// <summary>Registers the owner-qualified scoped client factory if it is not already present.</summary>
    public override void RegisterScopedClientFactory()
    {
        Collection.TryAddScoped(provider => Bind<TBus>.Create(GetScopedBusContext(provider)));
    }

    /// <summary>Resolves the client factory scoped to this bus owner.</summary>
    /// <param name="provider">The active dependency-injection scope.</param>
    /// <returns>The owner-qualified client factory bound to that scope.</returns>
    protected override IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetRequiredService<IScopedBusContextProvider<TBus>>().Context.ClientFactory;
    }

    /// <summary>Resolves callbacks scoped to this bus owner.</summary>
    /// <param name="provider">The service provider that contains endpoint callbacks.</param>
    /// <returns>The owner-qualified callbacks in registration order.</returns>
    protected override IConfigureReceiveEndpoint[] GetBusConfigureReceiveEndpoints(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetServices<Bind<TBus, IConfigureReceiveEndpoint>>().Select(x => x.Value).ToArray();
    }

    /// <summary>Resolves this owner's endpoint naming convention with default fallback.</summary>
    /// <param name="provider">The service provider that contains the optional owner-qualified convention.</param>
    /// <returns>The owner-qualified convention or the default bus convention.</returns>
    public override IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var bind = provider.GetService<Bind<TBus, IEndpointNameFormatter>>();
        return bind != null
            ? bind.Value
            : base.GetEndpointNameFormatter(provider);
    }
}
