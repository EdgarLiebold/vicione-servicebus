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
        ArgumentNullException.ThrowIfNull(collection);

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
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        EnsureConcreteClosedClass(typeof(T), nameof(T), "saga state machine");

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
        ArgumentNullException.ThrowIfNull(collection);

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
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        EnsureConcreteClosedClass(typeof(T), nameof(T), "saga state machine");
        EnsureConcreteClosedClass(typeof(TDefinition), nameof(TDefinition), "saga definition");

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
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(sagaDefinitionType);

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
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        EnsureConcreteClosedClass(typeof(T), nameof(T), "saga state machine");

        if (sagaDefinitionType == null)
            return new SagaRegistrar<T, TSaga>().Register(collection, registrar);

        EnsureSagaDefinitionType(sagaDefinitionType, typeof(TSaga));

        var register = (ISagaRegistrar)(Activator.CreateInstance(
            typeof(SagaDefinitionRegistrar<,,>).MakeGenericType(typeof(T), typeof(TSaga), sagaDefinitionType))!);

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
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(sagaType);

        Type instanceType = EnsureSagaStateMachineType(sagaType);

        if (sagaDefinitionType != null)
        {
            EnsureSagaDefinitionType(sagaDefinitionType, instanceType);

            var sagaRegistrar = (ISagaRegistrar)(Activator.CreateInstance(typeof(SagaDefinitionRegistrar<,,>).MakeGenericType(sagaType,
                instanceType, sagaDefinitionType))!);

            return sagaRegistrar.Register(collection, registrar);
        }

        var register = (ISagaRegistrar)Activator.CreateInstance(typeof(SagaRegistrar<,>).MakeGenericType(sagaType, instanceType))!;

        return register.Register(collection, registrar);
    }

    static Type EnsureSagaStateMachineType(Type sagaType)
    {
        if (!sagaType.IsClass || sagaType.IsAbstract || sagaType.ContainsGenericParameters)
            throw CreateSagaStateMachineTypeException(sagaType);

        var stateMachineTypes = sagaType.GetClosedGenericTypes(typeof(ISagaStateMachine<>));
        if (stateMachineTypes.Count != 1)
            throw CreateSagaStateMachineTypeException(sagaType);

        Type instanceType = stateMachineTypes[0].GetGenericArguments()[0];
        if (!instanceType.ImplementsInterface<ISagaStateMachineInstance>())
            throw CreateSagaStateMachineTypeException(sagaType);

        return instanceType;
    }

    static ArgumentException CreateSagaStateMachineTypeException(Type sagaType)
    {
        return new ArgumentException(
            $"{TypeCache.GetShortName(sagaType)} is not a concrete, closed saga state machine with exactly one saga state contract",
            nameof(sagaType));
    }

    static void EnsureSagaDefinitionType(Type sagaDefinitionType, Type sagaType)
    {
        if (!sagaDefinitionType.IsClass || sagaDefinitionType.IsAbstract || sagaDefinitionType.ContainsGenericParameters)
            throw CreateSagaDefinitionTypeException(sagaDefinitionType, sagaType);

        var definitionTypes = sagaDefinitionType.GetClosedGenericTypes(typeof(ISagaDefinition<>));

        if (definitionTypes.Count != 1 || definitionTypes[0].GetGenericArguments()[0] != sagaType)
            throw CreateSagaDefinitionTypeException(sagaDefinitionType, sagaType);
    }

    static ArgumentException CreateSagaDefinitionTypeException(Type sagaDefinitionType, Type sagaType)
    {
        return new ArgumentException(
            $"{TypeCache.GetShortName(sagaDefinitionType)} is not a concrete, closed saga definition of {TypeCache.GetShortName(sagaType)}",
            nameof(sagaDefinitionType));
    }

    static void EnsureConcreteClosedClass(Type type, string parameterName, string role)
    {
        if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
        {
            throw new ArgumentException(
                $"{TypeCache.GetShortName(type)} is not a concrete, closed {role} implementation",
                parameterName);
        }
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
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(registrar);

            ISagaRegistration registration = registrar.GetOrAddRegistration<ISagaRegistration>(
                typeof(TSaga),
                _ => new SagaStateMachineRegistration<TStateMachine, TSaga>(registrar));

            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, SagaConsumerKind>());
            collection.TryAddSingleton<TStateMachine>();
            collection.TryAddSingleton<ISagaStateMachine<TSaga>>(provider => provider.GetRequiredService<TStateMachine>());

            return registration;
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
            registrar.AddDefinition<ISagaDefinition<TSaga>, TDefinition>();

            return base.Register(collection, registrar);
        }
    }
}
