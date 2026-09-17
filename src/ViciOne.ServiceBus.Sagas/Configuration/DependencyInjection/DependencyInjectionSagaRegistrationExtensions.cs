using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for dependency injection saga registration.</summary>
public static class DependencyInjectionSagaRegistrationExtensions
{
    /// <summary>Registers saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSaga<T>(this IServiceCollection collection)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(collection);

        return RegisterSaga<T>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSaga<T>(this IServiceCollection collection, IContainerRegistrar registrar)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        EnsureConcreteSagaType(typeof(T), nameof(T));

        return new SagaRegistrar<T>().Register(collection, registrar);
    }

    /// <summary>Registers saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSaga<T, TDefinition>(this IServiceCollection collection)
        where T : class, ISaga
        where TDefinition : class, ISagaDefinition<T>
    {
        ArgumentNullException.ThrowIfNull(collection);

        return RegisterSaga<T, TDefinition>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSaga<T, TDefinition>(this IServiceCollection collection, IContainerRegistrar registrar)
        where T : class, ISaga
        where TDefinition : class, ISagaDefinition<T>
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        EnsureConcreteSagaType(typeof(T), nameof(T));
        EnsureSagaDefinitionType(typeof(TDefinition), typeof(T), nameof(TDefinition));

        return new SagaDefinitionRegistrar<T, TDefinition>().Register(collection, registrar);
    }

    /// <summary>Registers saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="sagaDefinitionType">The runtime saga definition type used by the operation.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSaga<T>(this IServiceCollection collection, Type sagaDefinitionType)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(sagaDefinitionType);

        return RegisterSaga<T>(collection, new DependencyInjectionContainerRegistrar(collection), sagaDefinitionType);
    }

    /// <summary>Registers saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <param name="sagaDefinitionType">The runtime saga definition type used by the operation.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSaga<T>(this IServiceCollection collection, IContainerRegistrar registrar, Type? sagaDefinitionType)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        EnsureConcreteSagaType(typeof(T), nameof(T));

        if (sagaDefinitionType == null)
            return new SagaRegistrar<T>().Register(collection, registrar);

        EnsureSagaDefinitionType(sagaDefinitionType, typeof(T), nameof(sagaDefinitionType));

        var register = (ISagaRegistrar)Activator.CreateInstance(
            typeof(SagaDefinitionRegistrar<,>).MakeGenericType(typeof(T), sagaDefinitionType))!;

        return register.Register(collection, registrar);
    }

    /// <summary>Registers saga.</summary>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="sagaDefinitionType">The runtime saga definition type used by the operation.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSaga(this IServiceCollection collection, IContainerRegistrar registrar, Type sagaType,
        Type? sagaDefinitionType = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(sagaType);
        EnsureConcreteSagaType(sagaType, nameof(sagaType));

        if (sagaDefinitionType != null)
        {
            EnsureSagaDefinitionType(sagaDefinitionType, sagaType, nameof(sagaDefinitionType));

            var sagaRegistrar = (ISagaRegistrar)Activator.CreateInstance(
                typeof(SagaDefinitionRegistrar<,>).MakeGenericType(sagaType, sagaDefinitionType))!;

            return sagaRegistrar.Register(collection, registrar);
        }

        var register = (ISagaRegistrar)Activator.CreateInstance(typeof(SagaRegistrar<>).MakeGenericType(sagaType))!;

        return register.Register(collection, registrar);
    }

    static void EnsureConcreteSagaType(Type sagaType, string parameterName)
    {
        if (!sagaType.IsClass || sagaType.IsAbstract || sagaType.ContainsGenericParameters || !typeof(ISaga).IsAssignableFrom(sagaType))
        {
            throw new ArgumentException(
                $"{TypeCache.GetShortName(sagaType)} is not a concrete, closed saga implementation",
                parameterName);
        }

        if (sagaType.ImplementsInterface<ISagaStateMachineInstance>())
        {
            throw new ArgumentException(
                $"State machine sagas must be registered using RegisterSagaStateMachine: {TypeCache.GetShortName(sagaType)}",
                parameterName);
        }
    }

    static void EnsureSagaDefinitionType(Type sagaDefinitionType, Type sagaType, string parameterName)
    {
        if (!sagaDefinitionType.IsClass || sagaDefinitionType.IsAbstract || sagaDefinitionType.ContainsGenericParameters
            || !sagaDefinitionType.TryGetSingleClosedGenericArguments(typeof(ISagaDefinition<>), out Type[] types)
            || types[0] != sagaType)
        {
            throw new ArgumentException(
                $"{TypeCache.GetShortName(sagaDefinitionType)} is not a concrete, closed saga definition of {TypeCache.GetShortName(sagaType)}",
                parameterName);
        }
    }


    interface ISagaRegistrar
    {
        ISagaRegistration Register(IServiceCollection collection, IContainerRegistrar registrar);
    }


    class SagaRegistrar<TSaga> :
        ISagaRegistrar
        where TSaga : class, ISaga
    {
        public virtual ISagaRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(registrar);

            ISagaRegistration registration = registrar.GetOrAddRegistration<ISagaRegistration>(
                typeof(TSaga),
                _ => new SagaRegistration<TSaga>(registrar));

            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, SagaConsumerKind>());

            return registration;
        }
    }


    class SagaDefinitionRegistrar<TSaga, TDefinition> :
        SagaRegistrar<TSaga>
        where TDefinition : class, ISagaDefinition<TSaga>
        where TSaga : class, ISaga
    {
        public override ISagaRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            registrar.AddDefinition<ISagaDefinition<TSaga>, TDefinition>();

            return base.Register(collection, registrar);
        }
    }
}
