using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers typed and runtime-selected endpoint definitions with dependency injection.</summary>
public static class DependencyInjectionEndpointRegistrationExtensions
{
    /// <summary>Registers an endpoint definition for an existing component through the default registrar.</summary>
    /// <typeparam name="TDefinition">The concrete endpoint definition.</typeparam>
    /// <typeparam name="T">The component associated with the endpoint.</typeparam>
    /// <param name="collection">The service collection that owns the endpoint.</param>
    /// <param name="registration">The component registration associated with the endpoint.</param>
    /// <param name="settings">Optional settings passed explicitly to the definition constructor.</param>
    /// <returns>The canonical endpoint registration for <typeparamref name="T" />.</returns>
    public static IEndpointRegistration RegisterEndpoint<TDefinition, T>(this IServiceCollection collection, IRegistration registration,
        IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where TDefinition : class, IEndpointDefinition<T>
        where T : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registration);
        EnsureConcreteEndpointDefinition<TDefinition>();

        return RegisterEndpoint<TDefinition, T>(collection, new DependencyInjectionContainerRegistrar(collection), registration, settings);
    }

    /// <summary>Registers an endpoint definition for an existing component through an explicit registrar.</summary>
    /// <typeparam name="TDefinition">The concrete endpoint definition.</typeparam>
    /// <typeparam name="T">The component associated with the endpoint.</typeparam>
    /// <param name="collection">The service collection that owns the endpoint.</param>
    /// <param name="registrar">The container registrar that owns the endpoint metadata.</param>
    /// <param name="registration">The component registration associated with the endpoint.</param>
    /// <param name="settings">Optional settings passed explicitly to the definition constructor.</param>
    /// <returns>The canonical endpoint registration for <typeparamref name="T" />.</returns>
    public static IEndpointRegistration RegisterEndpoint<TDefinition, T>(this IServiceCollection collection, IContainerRegistrar registrar,
        IRegistration registration, IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where T : class
        where TDefinition : class, IEndpointDefinition<T>
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(registration);
        EnsureConcreteEndpointDefinition<TDefinition>();

        return new EndpointRegistrar<TDefinition, T>(registration).Register(registrar, settings);
    }

    /// <summary>Registers a runtime-selected endpoint definition through the default registrar.</summary>
    /// <param name="collection">The service collection that owns the endpoint.</param>
    /// <param name="endpointDefinitionType">The concrete endpoint definition type.</param>
    /// <returns>The canonical endpoint registration for the definition's component type.</returns>
    public static IEndpointRegistration RegisterEndpoint(this IServiceCollection collection, Type endpointDefinitionType)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(endpointDefinitionType);

        return RegisterEndpoint(collection, new DependencyInjectionContainerRegistrar(collection), endpointDefinitionType);
    }

    /// <summary>Registers a runtime-selected endpoint definition through an explicit registrar.</summary>
    /// <param name="collection">The service collection that owns the endpoint.</param>
    /// <param name="registrar">The container registrar that owns the endpoint metadata.</param>
    /// <param name="endpointDefinitionType">The concrete endpoint definition type.</param>
    /// <returns>The canonical endpoint registration for the definition's component type.</returns>
    public static IEndpointRegistration RegisterEndpoint(this IServiceCollection collection, IContainerRegistrar registrar, Type endpointDefinitionType)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(endpointDefinitionType);

        if (!endpointDefinitionType.IsClass || endpointDefinitionType.IsAbstract
            || !endpointDefinitionType.TryGetSingleClosedGenericArguments(typeof(IEndpointDefinition<>), out Type[] types))
            throw new ArgumentException($"{TypeCache.GetShortName(endpointDefinitionType)} is not an endpoint definition", nameof(endpointDefinitionType));

        var register = (IEndpointRegistrar)(Activator.CreateInstance(typeof(EndpointRegistrar<,>).MakeGenericType(endpointDefinitionType, types[0])) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(registrar);
    }


    interface IEndpointRegistrar
    {
        IEndpointRegistration Register(IContainerRegistrar registrar);
    }


    class EndpointRegistrar<TDefinition, T> :
        IEndpointRegistrar
        where TDefinition : class, IEndpointDefinition<T>
        where T : class
    {
        readonly IRegistration? _registration;

        public EndpointRegistrar()
        {
        }

        public EndpointRegistrar(IRegistration registration)
        {
            _registration = registration ?? throw new ArgumentNullException(nameof(registration));
        }

        public IEndpointRegistration Register(IContainerRegistrar registrar)
        {
            ArgumentNullException.ThrowIfNull(registrar);

            registrar.AddEndpointDefinition<T, TDefinition>();

            return registrar.GetOrAddRegistration<IEndpointRegistration>(typeof(T), _ => _registration == null
                ? new EndpointRegistration<T>(registrar)
                : new EndpointRegistration<T>(_registration, registrar));
        }

        public IEndpointRegistration Register(IContainerRegistrar registrar, IEndpointSettings<IEndpointDefinition<T>>? settings)
        {
            ArgumentNullException.ThrowIfNull(registrar);

            registrar.AddEndpointDefinition<T, TDefinition>(settings);

            return registrar.GetOrAddRegistration<IEndpointRegistration>(typeof(T), _ => new EndpointRegistration<T>(
                _registration ?? throw new InvalidOperationException("A component registration is required when endpoint settings are supplied."),
                registrar));
        }
    }

    static void EnsureConcreteEndpointDefinition<TDefinition>()
    {
        if (typeof(TDefinition).IsAbstract)
            throw new ArgumentException($"{TypeCache<TDefinition>.ShortName} is not a concrete endpoint definition", "TDefinition");
    }
}
