using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for saga registration.
/// </summary>
public static class SagaRegistrationExtensions
{
    /// <summary>
    /// Adds a class-based saga and allows endpoint configuration. State-machine sagas are registered
    /// through the dedicated <c>AddSagaStateMachine</c> family.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure"></param>
    /// <typeparam name="T">The saga type</typeparam>
    /// <typeparam name="TDefinition">The saga definition type</typeparam>
    public static ISagaRegistrationConfigurator<T> AddSaga<T, TDefinition>(this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
        where T : class, ISaga
        where TDefinition : class, ISagaDefinition<T>
    {
        return SagaRegistrationConfiguratorExtensions.AddSaga(configurator, typeof(TDefinition), configure);
    }

    /// <summary>
    /// Adds all sagas in the specified assemblies. If using state machine sagas, they should be added first using AddSagaStateMachines.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="filter"></param>
    /// <param name="assemblies">The assemblies to scan for consumers</param>
    public static void AddSagas(this IRegistrationConfigurator configurator, Func<Type, bool> filter, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var types = AssemblyTypeCache.FindTypes(assemblies, SagaRegistrationMetadata.IsSagaOrDefinition);

        AddSagas(configurator, filter, types.FindTypes(TypeClassification.Concrete | TypeClassification.Closed).ToArray());
    }

    /// <summary>
    /// Adds all sagas in the specified assemblies. If using state machine sagas, they should be added first using AddSagaStateMachines.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="assemblies">The assemblies to scan for consumers</param>
    public static void AddSagas(this IRegistrationConfigurator configurator, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var types = AssemblyTypeCache.FindTypes(assemblies, SagaRegistrationMetadata.IsSagaOrDefinition);

        AddSagas(configurator, types.FindTypes(TypeClassification.Concrete | TypeClassification.Closed).ToArray());
    }

    /// <summary>
    /// Adds all sagas in the specified assemblies matching the namespace. If you are using both state machine and regular sagas, be
    /// sure to call AddSagaStateMachinesFromNamespaceContaining prior to calling this one.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="filter"></param>
    public static void AddSagasFromNamespaceContaining<T>(this IRegistrationConfigurator configurator, Func<Type, bool>? filter = null)
    {
        AddSagasFromNamespaceContaining(configurator, typeof(T), filter);
    }

    /// <summary>
    /// Adds all sagas in the specified assemblies matching the namespace. If you are using both state machine and regular sagas, be
    /// sure to call AddSagaStateMachinesFromNamespaceContaining prior to calling this one.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="type">The type to use to identify the assembly and namespace to scan</param>
    /// <param name="filter"></param>
    public static void AddSagasFromNamespaceContaining(this IRegistrationConfigurator configurator, Type type, Func<Type, bool>? filter = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type.Assembly == null || type.Namespace == null)
            throw new ArgumentException($"The type {TypeCache.GetShortName(type)} is not in an assembly with a valid namespace", nameof(type));

        AddSagas(configurator, filter, FindTypesInNamespace(type, SagaRegistrationMetadata.IsSagaOrDefinition));
    }

    /// <summary>
    /// Adds the specified saga and saga definition types
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="types">The state machine types to add</param>
    public static void AddSagas(this IRegistrationConfigurator configurator, params Type[] types)
    {
        AddSagas(configurator, null, types);
    }

    /// <summary>
    /// Adds the specified saga types
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="filter"></param>
    /// <param name="types">The state machine types to add</param>
    public static void AddSagas(this IRegistrationConfigurator configurator, Func<Type, bool>? filter, params Type[] types)
    {
        filter ??= t => true;

        IEnumerable<Type> sagaTypes = types.Where(x => x.ImplementsInterface<ISaga>() && !x.ImplementsInterface<SagaStateMachineInstance>());
        IEnumerable<Type> sagaDefinitionTypes = types.Where(x => x.ImplementsInterface(typeof(ISagaDefinition<>)));

        var sagas = from c in sagaTypes
                    join d in sagaDefinitionTypes on c equals d.GetSingleClosedGenericArgument(typeof(ISagaDefinition<>)) into dc
                    from d in dc.DefaultIfEmpty()
                    where filter(c)
                    select new
                    {
                        SagaType = c,
                        DefinitionType = d
                    };

        foreach (var saga in sagas)
            configurator.AddSaga(saga.SagaType, saga.DefinitionType);
    }

    /// <summary>
    /// Adds a SagaStateMachine to the registry and updates the registrar prior to registering so that the default
    /// saga registrar isn't notified.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TStateMachine">The state machine type</typeparam>
    /// <typeparam name="T">The state machine instance type</typeparam>
    /// <typeparam name="TDefinition">The saga definition type</typeparam>
    public static ISagaRegistrationConfigurator<T> AddSagaStateMachine<TStateMachine, T, TDefinition>(this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
        where T : class, SagaStateMachineInstance
        where TStateMachine : class, SagaStateMachine<T>
        where TDefinition : class, ISagaDefinition<T>
    {
        return SagaRegistrationConfiguratorExtensions.AddSagaStateMachine<TStateMachine, T>(configurator, typeof(TDefinition), configure);
    }

    /// <summary>
    /// Adds SagaStateMachines to the registry, using the factory method, and updates the registrar prior to registering so that the default
    /// saga registrar isn't notified.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="assemblies">The assemblies to scan for state machines</param>
    public static void AddSagaStateMachines(this IRegistrationConfigurator configurator, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var types = AssemblyTypeCache.FindTypes(assemblies,
            type => SagaRegistrationMetadata.IsSagaStateMachineOrDefinition(type) && !SagaRegistrationMetadata.IsConsumerKindOwned(type));

        configurator.AddSagaStateMachines(types.FindTypes(TypeClassification.Concrete | TypeClassification.Closed).ToArray());
    }

    /// <summary>
    /// Adds all saga state machines in the specified assemblies matching the namespace. If you are using both state machine and regular sagas, be
    /// sure to call AddSagasFromNamespaceContaining after calling this one.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="filter"></param>
    public static void AddSagaStateMachinesFromNamespaceContaining<T>(this IRegistrationConfigurator configurator, Func<Type, bool>? filter = null)
    {
        AddSagaStateMachinesFromNamespaceContaining(configurator, typeof(T), filter);
    }

    /// <summary>
    /// Adds all saga state machines in the specified assemblies matching the namespace. If you are using both state machine and regular sagas, be
    /// sure to call AddSagasFromNamespaceContaining after calling this one.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="type">The type to use to identify the assembly and namespace to scan</param>
    /// <param name="filter"></param>
    public static void AddSagaStateMachinesFromNamespaceContaining(this IRegistrationConfigurator configurator, Type type, Func<Type, bool>? filter = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type.Assembly == null || type.Namespace == null)
            throw new ArgumentException($"The type {TypeCache.GetShortName(type)} is not in an assembly with a valid namespace", nameof(type));

        AddSagaStateMachines(configurator, filter,
            FindTypesInNamespace(type, x => SagaRegistrationMetadata.IsSagaStateMachineOrDefinition(x) && !SagaRegistrationMetadata.IsConsumerKindOwned(x)));
    }

    /// <summary>
    /// Adds SagaStateMachines to the registry, using the factory method, and updates the registrar prior to registering so that the default
    /// saga registrar isn't notified.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="types">The state machine types to add</param>
    public static void AddSagaStateMachines(this IRegistrationConfigurator configurator, params Type[] types)
    {
        AddSagaStateMachines(configurator, null, types);
    }

    /// <summary>
    /// Adds SagaStateMachines to the registry, using the factory method, and updates the registrar prior to registering so that the default
    /// saga registrar isn't notified.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="filter"></param>
    /// <param name="types">The state machine types to add</param>
    public static void AddSagaStateMachines(this IRegistrationConfigurator configurator, Func<Type, bool>? filter, params Type[] types)
    {
        filter ??= t => true;

        IEnumerable<Type> sagaTypes = types.Where(x => x.ImplementsInterface(typeof(SagaStateMachine<>)));
        IEnumerable<Type> sagaDefinitionTypes = types.Where(x => x.ImplementsInterface(typeof(ISagaDefinition<>)));

        var sagas = from c in sagaTypes
                    let it = c.GetSingleClosedGenericArgument(typeof(SagaStateMachine<>))
                    join d in sagaDefinitionTypes on it equals d.GetSingleClosedGenericArgument(typeof(ISagaDefinition<>)) into dc
                    from d in dc.DefaultIfEmpty()
                    where filter(c) || filter(it)
                    select new
                    {
                        SagaType = c,
                        DefinitionType = d
                    };

        foreach (var saga in sagas)
            configurator.AddSagaStateMachine(saga.SagaType, saga.DefinitionType);
    }


    static Type[] FindTypesInNamespace(Type type, Func<Type, bool> typeFilter)
    {
        if (type.Namespace == null)
            throw new ArgumentException("The type must have a valid namespace", nameof(type));

        var dottedNamespace = type.Namespace + ".";

        bool Filter(Type candidate)
        {
            return typeFilter(candidate)
                && candidate.Namespace != null
                && (candidate.Namespace.StartsWith(dottedNamespace, StringComparison.OrdinalIgnoreCase)
                    || candidate.Namespace.Equals(type.Namespace, StringComparison.OrdinalIgnoreCase));
        }

        return AssemblyTypeCache.FindTypes(type.Assembly, TypeClassification.Concrete | TypeClassification.Closed, Filter).ToArray();
    }
}
