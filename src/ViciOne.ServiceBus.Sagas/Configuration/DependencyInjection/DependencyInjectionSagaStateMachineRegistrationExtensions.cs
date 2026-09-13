using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for dependency injection saga state machine registration.</summary>
public static class DependencyInjectionSagaStateMachineRegistrationExtensions
{
    /// <summary>Registers saga state machine.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSagaStateMachine<T, TSaga>(this IServiceCollection collection)
        where T : class, ISagaStateMachine<TSaga>
        where TSaga : class, ISagaStateMachineInstance
    {
        return RegisterSagaStateMachine<T, TSaga>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers saga state machine.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSagaStateMachine<T, TSaga>(this IServiceCollection collection, IContainerRegistrar registrar)
        where T : class, ISagaStateMachine<TSaga>
        where TSaga : class, ISagaStateMachineInstance
    {
        return new SagaRegistrar<T, TSaga>().Register(collection, registrar);
    }

    /// <summary>Registers saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSaga<T, TSaga, TDefinition>(this IServiceCollection collection)
        where T : class, ISagaStateMachine<TSaga>
        where TSaga : class, ISagaStateMachineInstance
        where TDefinition : class, ISagaDefinition<TSaga>
    {
        return RegisterSaga<T, TSaga, TDefinition>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSaga<T, TSaga, TDefinition>(this IServiceCollection collection, IContainerRegistrar registrar)
        where T : class, ISagaStateMachine<TSaga>
        where TSaga : class, ISagaStateMachineInstance
        where TDefinition : class, ISagaDefinition<TSaga>
    {
        return new SagaDefinitionRegistrar<T, TSaga, TDefinition>().Register(collection, registrar);
    }

    /// <summary>Registers saga state machine.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="sagaDefinitionType">The runtime saga definition type used by the operation.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSagaStateMachine<T, TSaga>(this IServiceCollection collection, Type sagaDefinitionType)
        where T : class, ISagaStateMachine<TSaga>
        where TSaga : class, ISagaStateMachineInstance
    {
        return RegisterSagaStateMachine<T, TSaga>(collection, new DependencyInjectionContainerRegistrar(collection), sagaDefinitionType);
    }

    /// <summary>Registers saga state machine.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <param name="sagaDefinitionType">The runtime saga definition type used by the operation.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSagaStateMachine<T, TSaga>(this IServiceCollection collection, IContainerRegistrar registrar,
        Type? sagaDefinitionType)
        where T : class, ISagaStateMachine<TSaga>
        where TSaga : class, ISagaStateMachineInstance
    {
        if (sagaDefinitionType == null)
            return RegisterSagaStateMachine<T, TSaga>(collection, registrar);

        if (!sagaDefinitionType.TryGetSingleClosedGenericArguments(typeof(ISagaDefinition<>), out Type[] types) || types[0] != typeof(TSaga))
        {
            throw new ArgumentException($"{TypeCache.GetShortName(sagaDefinitionType)} is not a saga definition of {TypeCache<TSaga>.ShortName}",
                nameof(sagaDefinitionType));
        }

        var register = (ISagaRegistrar)(Activator.CreateInstance(
            typeof(SagaDefinitionRegistrar<,,>).MakeGenericType(typeof(T), typeof(TSaga), sagaDefinitionType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
    }

    /// <summary>Registers saga state machine.</summary>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="sagaDefinitionType">The runtime saga definition type used by the operation.</param>
    /// <returns>The saga registration produced by the operation.</returns>
    public static ISagaRegistration RegisterSagaStateMachine(this IServiceCollection collection, IContainerRegistrar registrar, Type sagaType,
        Type? sagaDefinitionType = null)
    {
        if (!sagaType.TryGetSingleClosedGenericArguments(typeof(ISagaStateMachine<>), out Type[] instanceTypes))
            throw new ArgumentException($"The saga type must be a saga state machine: {TypeCache.GetShortName(sagaType)}");

        if (!instanceTypes[0].ImplementsInterface<ISagaStateMachineInstance>())
            throw new ArgumentException($"The instance type must be a saga state machine instance: {TypeCache.GetShortName(instanceTypes[0])}");

        if (sagaDefinitionType != null)
        {
            if (!sagaDefinitionType.TryGetSingleClosedGenericArguments(typeof(ISagaDefinition<>), out Type[] types) || types[0] != instanceTypes[0])
            {
                throw new ArgumentException(
                    $"{TypeCache.GetShortName(sagaDefinitionType)} is not a saga definition of {TypeCache.GetShortName(instanceTypes[0])}",
                    nameof(sagaDefinitionType));
            }

            var sagaRegistrar = (ISagaRegistrar)(Activator.CreateInstance(typeof(SagaDefinitionRegistrar<,,>).MakeGenericType(sagaType,
                instanceTypes[0], sagaDefinitionType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

            return sagaRegistrar.Register(collection, registrar);
        }

        var register = (ISagaRegistrar)(Activator.CreateInstance(typeof(SagaRegistrar<,>).MakeGenericType(sagaType, instanceTypes[0])) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
    }


    interface ISagaRegistrar
    {
        ISagaRegistration Register(IServiceCollection collection, IContainerRegistrar registrar);
    }


    class SagaRegistrar<TStateMachine, TSaga> :
        ISagaRegistrar
        where TStateMachine : class, ISagaStateMachine<TSaga>
        where TSaga : class, ISagaStateMachineInstance
    {
        public virtual ISagaRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, SagaConsumerKind>());
            collection.AddSingleton<TStateMachine>();
            collection.AddSingleton<ISagaStateMachine<TSaga>>(provider => provider.GetRequiredService<TStateMachine>());

            return registrar.GetOrAddRegistration<ISagaRegistration>(typeof(TSaga), _ => new SagaStateMachineRegistration<TStateMachine, TSaga>(registrar));
        }
    }


    class SagaDefinitionRegistrar<TStateMachine, TSaga, TDefinition> :
        SagaRegistrar<TStateMachine, TSaga>
        where TStateMachine : class, ISagaStateMachine<TSaga>
        where TSaga : class, ISagaStateMachineInstance
        where TDefinition : class, ISagaDefinition<TSaga>
    {
        public override ISagaRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            var registration = base.Register(collection, registrar);

            registrar.AddDefinition<ISagaDefinition<TSaga>, TDefinition>();

            return registration;
        }
    }
}
