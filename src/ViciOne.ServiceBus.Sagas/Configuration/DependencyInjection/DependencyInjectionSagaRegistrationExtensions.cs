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
        if (typeof(T).ImplementsInterface<ISagaStateMachineInstance>())
            throw new ArgumentException($"State machine sagas must be registered using RegisterSagaStateMachine: {TypeCache<T>.ShortName}");

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
        if (typeof(T).ImplementsInterface<ISagaStateMachineInstance>())
            throw new ArgumentException($"State machine sagas must be registered using RegisterSagaStateMachine: {TypeCache<T>.ShortName}");

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
        if (sagaDefinitionType == null)
            return RegisterSaga<T>(collection, registrar);

        if (typeof(T).ImplementsInterface<ISagaStateMachineInstance>())
            throw new ArgumentException($"State machine sagas must be registered using RegisterSagaStateMachine: {TypeCache<T>.ShortName}");

        if (!sagaDefinitionType.TryGetSingleClosedGenericArguments(typeof(ISagaDefinition<>), out Type[] types) || types[0] != typeof(T))
        {
            throw new ArgumentException($"{TypeCache.GetShortName(sagaDefinitionType)} is not a saga definition of {TypeCache<T>.ShortName}",
                nameof(sagaDefinitionType));
        }

        var register = (ISagaRegistrar)(Activator.CreateInstance(typeof(SagaDefinitionRegistrar<,>).MakeGenericType(typeof(T), sagaDefinitionType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

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
        if (sagaType.ImplementsInterface<ISagaStateMachineInstance>())
            throw new ArgumentException($"State machine sagas must be registered using RegisterSagaStateMachine: {TypeCache.GetShortName(sagaType)}");

        if (sagaDefinitionType != null)
        {
            if (!sagaDefinitionType.TryGetSingleClosedGenericArguments(typeof(ISagaDefinition<>), out Type[] types) || types[0] != sagaType)
            {
                throw new ArgumentException($"{TypeCache.GetShortName(sagaDefinitionType)} is not a saga definition of {TypeCache.GetShortName(sagaType)}",
                    nameof(sagaDefinitionType));
            }

            var sagaRegistrar = (ISagaRegistrar)(Activator.CreateInstance(typeof(SagaDefinitionRegistrar<,>).MakeGenericType(sagaType, sagaDefinitionType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

            return sagaRegistrar.Register(collection, registrar);
        }

        var register = (ISagaRegistrar)(Activator.CreateInstance(typeof(SagaRegistrar<>).MakeGenericType(sagaType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
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
            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, SagaConsumerKind>());
            return registrar.GetOrAddRegistration<ISagaRegistration>(typeof(TSaga), _ => new SagaRegistration<TSaga>(registrar));
        }
    }


    class SagaDefinitionRegistrar<TSaga, TDefinition> :
        SagaRegistrar<TSaga>
        where TDefinition : class, ISagaDefinition<TSaga>
        where TSaga : class, ISaga
    {
        public override ISagaRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            var registration = base.Register(collection, registrar);

            registrar.AddDefinition<ISagaDefinition<TSaga>, TDefinition>();

            return registration;
        }
    }
}
