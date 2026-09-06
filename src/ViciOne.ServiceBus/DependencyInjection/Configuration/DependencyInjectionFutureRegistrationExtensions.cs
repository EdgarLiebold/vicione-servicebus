using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for dependency injection future registration.</summary>
public static class DependencyInjectionFutureRegistrationExtensions
{
    /// <summary>Registers future.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <returns>The future registration produced by the operation.</returns>
    public static IFutureRegistration RegisterFuture<T>(this IServiceCollection collection)
        where T : class, SagaStateMachine<FutureState>
    {
        return RegisterFuture<T, DefaultFutureDefinition<T>>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers future.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <returns>The future registration produced by the operation.</returns>
    public static IFutureRegistration RegisterFuture<T>(this IServiceCollection collection, IContainerRegistrar registrar)
        where T : class, SagaStateMachine<FutureState>
    {
        return RegisterFuture<T, DefaultFutureDefinition<T>>(collection, registrar);
    }

    /// <summary>Registers future.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <returns>The future registration produced by the operation.</returns>
    public static IFutureRegistration RegisterFuture<T, TDefinition>(this IServiceCollection collection)
        where T : class, SagaStateMachine<FutureState>
        where TDefinition : class, IFutureDefinition<T>
    {
        return RegisterFuture<T, TDefinition>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers future.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <returns>The future registration produced by the operation.</returns>
    public static IFutureRegistration RegisterFuture<T, TDefinition>(this IServiceCollection collection, IContainerRegistrar registrar)
        where T : class, SagaStateMachine<FutureState>
        where TDefinition : class, IFutureDefinition<T>
    {
        return new FutureDefinitionRegistrar<T, TDefinition>().Register(collection, registrar);
    }

    /// <summary>Registers future.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="futureDefinitionType">The runtime future definition type used by the operation.</param>
    /// <returns>The future registration produced by the operation.</returns>
    public static IFutureRegistration RegisterFuture<T>(this IServiceCollection collection, Type futureDefinitionType)
        where T : class, SagaStateMachine<FutureState>
    {
        return RegisterFuture<T>(collection, new DependencyInjectionContainerRegistrar(collection), futureDefinitionType);
    }

    /// <summary>Registers future.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <param name="futureDefinitionType">The runtime future definition type used by the operation.</param>
    /// <returns>The future registration produced by the operation.</returns>
    public static IFutureRegistration RegisterFuture<T>(this IServiceCollection collection, IContainerRegistrar registrar, Type? futureDefinitionType)
        where T : class, SagaStateMachine<FutureState>
    {
        if (futureDefinitionType == null)
            return RegisterFuture<T, DefaultFutureDefinition<T>>(collection, registrar);

        if (!futureDefinitionType.TryGetSingleClosedGenericArguments(typeof(IFutureDefinition<>), out Type[] types) || types[0] != typeof(T))
        {
            throw new ArgumentException($"{TypeCache.GetShortName(futureDefinitionType)} is not a future definition of {TypeCache<T>.ShortName}",
                nameof(futureDefinitionType));
        }

        var register = (IFutureRegistrar)(Activator.CreateInstance(typeof(FutureDefinitionRegistrar<,>).MakeGenericType(typeof(T), futureDefinitionType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
    }

    /// <summary>Registers future.</summary>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <param name="futureType">The runtime future type used by the operation.</param>
    /// <param name="futureDefinitionType">The runtime future definition type used by the operation.</param>
    /// <returns>The future registration produced by the operation.</returns>
    public static IFutureRegistration RegisterFuture(this IServiceCollection collection, IContainerRegistrar registrar, Type futureType,
        Type? futureDefinitionType = null)
    {
        if (!futureType.ImplementsInterface<SagaStateMachine<FutureState>>())
            throw new ArgumentException($"The registered type must be a future: {TypeCache.GetShortName(futureType)}");

        futureDefinitionType ??= typeof(DefaultFutureDefinition<>).MakeGenericType(futureType);

        if (!futureDefinitionType.TryGetSingleClosedGenericArguments(typeof(ISagaDefinition<>), out Type[] types) || types[0] != futureType)
        {
            throw new ArgumentException(
                $"{TypeCache.GetShortName(futureDefinitionType)} is not a future definition of {TypeCache.GetShortName(futureType)}",
                nameof(futureDefinitionType));
        }

        var sagaRegistrar =
            (IFutureRegistrar)(Activator.CreateInstance(typeof(FutureDefinitionRegistrar<,>).MakeGenericType(futureType, futureDefinitionType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return sagaRegistrar.Register(collection, registrar);
    }


    interface IFutureRegistrar
    {
        IFutureRegistration Register(IServiceCollection collection, IContainerRegistrar registrar);
    }


    class FutureRegistrar<TFuture> :
        IFutureRegistrar
        where TFuture : class, SagaStateMachine<FutureState>
    {
        public virtual IFutureRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, FutureConsumerKind>());
            collection.TryAddSingleton<TFuture>();

            return registrar.GetOrAddRegistration<IFutureRegistration>(typeof(TFuture), _ => new FutureRegistration<TFuture>(registrar));
        }
    }


    class FutureDefinitionRegistrar<TFuture, TDefinition> :
        FutureRegistrar<TFuture>
        where TDefinition : class, IFutureDefinition<TFuture>
        where TFuture : class, SagaStateMachine<FutureState>
    {
        public override IFutureRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            var registration = base.Register(collection, registrar);

            registrar.AddDefinition<IFutureDefinition<TFuture>, TDefinition>();

            return registration;
        }
    }
}
