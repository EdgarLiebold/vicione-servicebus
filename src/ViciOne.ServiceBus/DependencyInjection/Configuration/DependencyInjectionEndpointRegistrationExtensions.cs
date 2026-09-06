using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for dependency injection endpoint registration.</summary>
public static class DependencyInjectionEndpointRegistrationExtensions
{
    /// <summary>Registers endpoint.</summary>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registration">The registration.</param>
    /// <param name="settings">The settings that control the operation.</param>
    /// <returns>The endpoint registration produced by the operation.</returns>
    public static IEndpointRegistration RegisterEndpoint<TDefinition, T>(this IServiceCollection collection, IRegistration registration,
        IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where TDefinition : class, IEndpointDefinition<T>
        where T : class
    {
        return RegisterEndpoint<TDefinition, T>(collection, new DependencyInjectionContainerRegistrar(collection), registration, settings);
    }

    /// <summary>Registers endpoint.</summary>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <param name="registration">The registration.</param>
    /// <param name="settings">The settings that control the operation.</param>
    /// <returns>The endpoint registration produced by the operation.</returns>
    public static IEndpointRegistration RegisterEndpoint<TDefinition, T>(this IServiceCollection collection, IContainerRegistrar registrar,
        IRegistration registration, IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where T : class
        where TDefinition : class, IEndpointDefinition<T>
    {
        return new EndpointRegistrar<TDefinition, T>(registration).Register(registrar, settings);
    }

    /// <summary>Registers endpoint.</summary>
    /// <param name="collection">The collection.</param>
    /// <param name="endpointDefinitionType">The runtime endpoint definition type used by the operation.</param>
    /// <returns>The endpoint registration produced by the operation.</returns>
    public static IEndpointRegistration RegisterEndpoint(this IServiceCollection collection, Type endpointDefinitionType)
    {
        return RegisterEndpoint(collection, new DependencyInjectionContainerRegistrar(collection), endpointDefinitionType);
    }

    /// <summary>Registers endpoint.</summary>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <param name="endpointDefinitionType">The runtime endpoint definition type used by the operation.</param>
    /// <returns>The endpoint registration produced by the operation.</returns>
    public static IEndpointRegistration RegisterEndpoint(this IServiceCollection collection, IContainerRegistrar registrar, Type endpointDefinitionType)
    {
        if (!endpointDefinitionType.TryGetSingleClosedGenericArguments(typeof(IEndpointDefinition<>), out Type[] types))
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
        readonly IRegistration _registration;

        public EndpointRegistrar(IRegistration registration)
        {
            _registration = registration;
        }

        public IEndpointRegistration Register(IContainerRegistrar registrar)
        {
            registrar.AddEndpointDefinition<T, TDefinition>();

            return registrar.GetOrAddRegistration<IEndpointRegistration>(typeof(T), _ => new EndpointRegistration<T>(_registration, registrar));
        }

        public IEndpointRegistration Register(IContainerRegistrar registrar, IEndpointSettings<IEndpointDefinition<T>>? settings)
        {
            registrar.AddEndpointDefinition<T, TDefinition>(settings);

            return registrar.GetOrAddRegistration<IEndpointRegistration>(typeof(T), _ => new EndpointRegistration<T>(_registration, registrar));
        }
    }
}
