using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Futures.DependencyInjection;

/// <summary>Registers future services and definitions with the dependency-injection container.</summary>
internal static class FutureServiceCollectionExtensions
{
    /// <summary>Registers a future with its default definition.</summary>
    /// <typeparam name="T">The future state-machine type.</typeparam>
    /// <param name="collection">The service collection that receives the future services.</param>
    /// <returns>The internal future registration.</returns>
    internal static IFutureRegistration RegisterFuture<T>(this IServiceCollection collection)
        where T : class, ISagaStateMachine<FutureState>
    {
        ArgumentNullException.ThrowIfNull(collection);
        return RegisterFuture<T, DefaultFutureDefinition<T>>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers a future with its default definition through a selected container registrar.</summary>
    /// <typeparam name="T">The future state-machine type.</typeparam>
    /// <param name="collection">The service collection that receives the future services.</param>
    /// <param name="registrar">The container registrar that owns registration metadata.</param>
    /// <returns>The internal future registration.</returns>
    internal static IFutureRegistration RegisterFuture<T>(this IServiceCollection collection, IContainerRegistrar registrar)
        where T : class, ISagaStateMachine<FutureState>
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        return RegisterFuture<T, DefaultFutureDefinition<T>>(collection, registrar);
    }

    /// <summary>Registers a future and its strongly typed definition.</summary>
    /// <typeparam name="T">The future state-machine type.</typeparam>
    /// <typeparam name="TDefinition">The future definition type.</typeparam>
    /// <param name="collection">The service collection that receives the future services.</param>
    /// <returns>The internal future registration.</returns>
    internal static IFutureRegistration RegisterFuture<T, TDefinition>(this IServiceCollection collection)
        where T : class, ISagaStateMachine<FutureState>
        where TDefinition : class, IFutureDefinition<T>
    {
        ArgumentNullException.ThrowIfNull(collection);
        return RegisterFuture<T, TDefinition>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers a future and its strongly typed definition through a selected container registrar.</summary>
    /// <typeparam name="T">The future state-machine type.</typeparam>
    /// <typeparam name="TDefinition">The future definition type.</typeparam>
    /// <param name="collection">The service collection that receives the future services.</param>
    /// <param name="registrar">The container registrar that owns registration metadata.</param>
    /// <returns>The internal future registration.</returns>
    internal static IFutureRegistration RegisterFuture<T, TDefinition>(this IServiceCollection collection, IContainerRegistrar registrar)
        where T : class, ISagaStateMachine<FutureState>
        where TDefinition : class, IFutureDefinition<T>
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        ValidateConcreteClosedClass(typeof(T), "TFuture", "future");
        ValidateConcreteClosedClass(typeof(TDefinition), "TDefinition", "future definition");
        return new FutureDefinitionRegistrar<T, TDefinition>().Register(collection, registrar);
    }

    /// <summary>Registers a future with a definition selected by runtime type.</summary>
    /// <typeparam name="T">The future state-machine type.</typeparam>
    /// <param name="collection">The service collection that receives the future services.</param>
    /// <param name="futureDefinitionType">The runtime future definition type.</param>
    /// <returns>The internal future registration.</returns>
    internal static IFutureRegistration RegisterFuture<T>(this IServiceCollection collection, Type futureDefinitionType)
        where T : class, ISagaStateMachine<FutureState>
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(futureDefinitionType);
        return RegisterFuture<T>(collection, new DependencyInjectionContainerRegistrar(collection), futureDefinitionType);
    }

    /// <summary>Registers a future with an optional runtime definition through a selected container registrar.</summary>
    /// <typeparam name="T">The future state-machine type.</typeparam>
    /// <param name="collection">The service collection that receives the future services.</param>
    /// <param name="registrar">The container registrar that owns registration metadata.</param>
    /// <param name="futureDefinitionType">The runtime future definition type, or <see langword="null" /> for the default.</param>
    /// <returns>The internal future registration.</returns>
    internal static IFutureRegistration RegisterFuture<T>(this IServiceCollection collection, IContainerRegistrar registrar, Type? futureDefinitionType)
        where T : class, ISagaStateMachine<FutureState>
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        if (futureDefinitionType == null)
            return RegisterFuture<T, DefaultFutureDefinition<T>>(collection, registrar);

        ValidateConcreteClosedClass(typeof(T), "TFuture", "future");
        ValidateConcreteClosedClass(futureDefinitionType, nameof(futureDefinitionType), "future definition");
        if (!futureDefinitionType.TryGetSingleClosedGenericArguments(typeof(IFutureDefinition<>), out Type[] types) || types[0] != typeof(T))
        {
            throw new ArgumentException($"{TypeCache.GetShortName(futureDefinitionType)} is not a future definition of {TypeCache<T>.ShortName}",
                nameof(futureDefinitionType));
        }

        var register = (IFutureRegistrar)(Activator.CreateInstance(
            typeof(FutureDefinitionRegistrar<,>).MakeGenericType(typeof(T), futureDefinitionType))
            ?? throw new InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
    }

    static void ValidateConcreteClosedClass(Type type, string parameterName, string role)
    {
        if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
        {
            throw new ArgumentException(
                $"The {role} type must be a concrete, closed class: {TypeCache.GetShortName(type)}.",
                parameterName);
        }
    }


    private interface IFutureRegistrar
    {
        IFutureRegistration Register(IServiceCollection collection, IContainerRegistrar registrar);
    }


    private abstract class FutureRegistrar<TFuture> :
        IFutureRegistrar
        where TFuture : class, ISagaStateMachine<FutureState>
    {
        public virtual IFutureRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(registrar);
            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, FutureConsumerKind>());
            collection.TryAddSingleton<TFuture>();

            return registrar.GetOrAddRegistration<IFutureRegistration>(typeof(TFuture), _ => new FutureRegistration<TFuture>(registrar));
        }
    }


    private sealed class FutureDefinitionRegistrar<TFuture, TDefinition> :
        FutureRegistrar<TFuture>
        where TDefinition : class, IFutureDefinition<TFuture>
        where TFuture : class, ISagaStateMachine<FutureState>
    {
        public override IFutureRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(registrar);
            var registration = base.Register(collection, registrar);

            registrar.AddDefinition<IFutureDefinition<TFuture>, TDefinition>();

            return registration;
        }
    }
}
